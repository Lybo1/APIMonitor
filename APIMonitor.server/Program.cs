using System.Text;
using APIMonitor.server.Data;
using APIMonitor.server.Hubs;
using APIMonitor.server.Identity;
using APIMonitor.server.Identity.Seeding;
using APIMonitor.server.Identity.Services.RoleServices;
using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.Middleware;
using APIMonitor.server.Services.MacAddressService;
using APIMonitor.server.Services.NotificationsService;
using APIMonitor.server.Services.RateLimitService;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace APIMonitor.server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = CreateWebApplicationBuilder(args);
            ConfigureServices(builder);

            WebApplication app = builder.Build();

            ConfigureMiddleware(app);

            await SeedRolesIfNeeded(app);

            await app.RunAsync();
        }

        private static WebApplicationBuilder CreateWebApplicationBuilder(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            
            ConfigureSerilog(builder);
            
            return builder;
        }

        private static void ConfigureSerilog(WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/apimonitor.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            builder.Host.UseSerilog();
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            
            builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
            
            builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
            })
            .AddMicrosoftAccount(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]!;
                options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
            })
            .AddFacebook(options =>
            {
                options.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
                options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                };
            });

            builder.Services.AddDataProtection();

            builder.Services
                .AddFluentEmail(builder.Configuration["Email:From"])
                .AddSmtpSender(
                    host: builder.Configuration["Email:SmtpHost"],
                    port: int.Parse(builder.Configuration["Email:SmtpPort"]!),
                    username: builder.Configuration["Email:Username"],
                    password: builder.Configuration["Email:Password"]
                );

            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IRateLimitService, RateLimitService>();
            builder.Services.AddScoped<IMacAddressService, MacAddressService>();

            builder.Services.AddSignalR();

            builder.Services.AddMemoryCache();
            builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
            builder.Services.AddInMemoryRateLimiting();
            builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

            builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "AzureAdB2C");

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("https://your-frontend-domain.com")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "API Monitor", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme."
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }

        private static void ConfigureMiddleware(WebApplication app)
        {
            app.UseCors("AllowFrontend");

            if (app.Environment.IsProduction())
            {
                app.UseHttpsRedirection();
                app.UseHsts();
            }
            else if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(x =>
                {
                    x.SwaggerEndpoint("/swagger/v1/swagger.json", "API Monitor v1");
                    x.RoutePrefix = string.Empty;
                });
            }

            app.UseMiddleware<RequestInfoMiddleware>();

            app.UseSerilogRequestLogging();
            app.UseIpRateLimiting();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotificationHub>("/notifications");
        }

        private static async Task SeedRolesIfNeeded(WebApplication app)
        {
            if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
            {
                using IServiceScope scope = app.Services.CreateScope();
                RoleManager<IdentityRole<int>> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
                
                await RoleFactory.SeedRoles(roleManager);
            }
        }
    }
}
