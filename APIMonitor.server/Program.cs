using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using APIMonitor.server.Data;
using APIMonitor.server.Identity;
using APIMonitor.server.Services.ApiScannerService;
using APIMonitor.server.Services.ThreatDetectionService;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using APIMonitor.server.Hubs;
using APIMonitor.server.Identity.Services.RoleServices;
using APIMonitor.server.Identity.Services.TokenServices;
using APIMonitor.server.Middleware;
using APIMonitor.server.Services.AuditLogService;
using APIMonitor.server.Services.GeoLocationService;
using APIMonitor.server.Services.IpBlockService;
using APIMonitor.server.Services.MacAddressService;
using APIMonitor.server.Services.NotificationsService;
using APIMonitor.server.Services.RateLimitService;

internal class Program
{
    private static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();

        string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
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
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });

        builder.Services.AddControllers();
        builder.Services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 1024 * 1024 * 10;
            options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            options.EnableDetailedErrors = true;
        });
        builder.Services.AddMemoryCache();

        // Add HTTP client without Polly retry policy
        builder.Services.AddHttpClient<IApiScannerService, ApiScannerService>("ApiScannerClient");

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        builder.Services.AddScoped<IRoleService, RoleService>();
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IApiScannerService, ApiScannerService>();

        builder.Services.AddDataProtection();

        // builder.Services.AddHttpClient<IGeoLocationService, ApiGeoLocationService>();
        builder.Services.AddScoped<IAuditLogService, AuditLogService>();
        // builder.Services.AddScoped<IGeoLocationService, ApiGeoLocationService>();
        builder.Services.AddScoped<IIpBlockService, IpBlockService>();
        builder.Services.AddScoped<IMacAddressService, MacAddressService>();
        builder.Services.AddScoped<INotificationService, NotificationService>();
        builder.Services.AddScoped<IRateLimitService, RateLimitService>();
        builder.Services.AddScoped<RoleManager<IdentityRole<int>>>();
        builder.Services.AddScoped<IThreatDetectionService, ThreatDetectionService>();

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

        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("adminPolicy", httpContext =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });
        });

        WebApplication app = builder.Build();

        app.UseMiddleware<AuditLoggingMiddleware>();
        app.UseMiddleware<IpBanMiddleware>();
        app.UseMiddleware<RequestInfoMiddleware>();

        using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
        {
            IServiceProvider services = scope.ServiceProvider;
            UserManager<User> userManager = services.GetRequiredService<UserManager<User>>();
            RoleManager<IdentityRole<int>> roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

            string[] roles = new[] { "Admin", "User" };

            foreach (string role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
                }
            }

            Task<User?> userTask = userManager.FindByEmailAsync("lyuboslavSSS@proton.me");
            User? user = await userTask;

            if (user == null)
            {
                user = new User
                {
                    UserName = "lyuboslav",
                    Email = "lyuboslavSSS@proton.me",
                    EmailConfirmed = true,
                    IsAdmin = true
                };

                await userManager.CreateAsync(user, "DefaultPassword123!");
            }

            if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(x =>
            {
                x.SwaggerEndpoint("/swagger/v1/swagger.json", "API Monitor v1");
                x.RoutePrefix = string.Empty;
            });
        }

        app.UseRouting();

        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHub<NotificationHub>("/notificationHub");

        app.Run();
    }
}