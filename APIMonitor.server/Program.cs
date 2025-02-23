// using System.Text;
// using Microsoft.AspNetCore.Authentication.Cookies;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Caching.Memory;
// using Microsoft.Extensions.Http;
// using Microsoft.Extensions.Http.Resilience;
// using Microsoft.OpenApi.Models;
// using Polly;
// using Polly.Extensions.Http;
// using Serilog;
// using Serilog.Events;
// using APIMonitor.server.Data;
// using APIMonitor.server.Hubs;
// using APIMonitor.server.Identity;
// using APIMonitor.server.Identity.Seeding;
// using APIMonitor.server.Identity.Services.RoleServices;
// using APIMonitor.server.Identity.Services.TokenServices;
// using APIMonitor.server.Middleware;
// using APIMonitor.server.Services.ApiScannerService;
// using APIMonitor.server.Services.AuditLogService;
// using APIMonitor.server.Services.GeoLocationService;
// using APIMonitor.server.Services.IpBlockService;
// using APIMonitor.server.Services.MacAddressService;
// using APIMonitor.server.Services.NotificationsService;
// using APIMonitor.server.Services.RateLimitService;
// using APIMonitor.server.Services.ThreatDetectionService;
// using AspNetCoreRateLimit;
// using Microsoft.IdentityModel.Tokens;
// using Microsoft.Extensions.DependencyInjection;
//
// WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
//
// // Configure logging
// Log.Logger = new LoggerConfiguration()
//     .MinimumLevel.Debug()
//     .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
//     .Enrich.FromLogContext()
//     .WriteTo.Console()
//     .WriteTo.File("logs/apimonitor.log", rollingInterval: RollingInterval.Day)
//     .CreateLogger();
//
// builder.Host.UseSerilog();
//
// // Setup DBContext
// string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
//
// // Setup Identity
// builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
// {
//     options.SignIn.RequireConfirmedAccount = true;
//     options.User.RequireUniqueEmail = true;
//     options.Password.RequireDigit = true;
//     options.Password.RequireLowercase = true;
//     options.Password.RequireUppercase = true;
//     options.Password.RequireNonAlphanumeric = true;
//     options.Password.RequiredLength = 6;
//     options.Password.RequiredUniqueChars = 1;
// })
// .AddEntityFrameworkStores<ApplicationDbContext>()
// .AddDefaultTokenProviders();
//
// // Add services
// builder.Services.AddControllers();
//
// // Authentication setup
// builder.Services.AddAuthentication(options =>
// {
//     options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
// })
// .AddCookie(options =>
// {
//     options.Cookie.HttpOnly = true;
//     options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//     options.Cookie.SameSite = SameSiteMode.None;
// })
// .AddJwtBearer(options =>
// {
//     options.TokenValidationParameters = new TokenValidationParameters
//     {
//         ValidateIssuer = true,
//         ValidateAudience = true,
//         ValidateLifetime = true,
//         ValidateIssuerSigningKey = true,
//         ValidIssuer = builder.Configuration["Jwt:Issuer"],
//         ValidAudience = builder.Configuration["Jwt:Audience"],
//         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
//     };
// });
//
// // Add services for GeoLocation, Rate Limiting, Caching, etc.
// builder.Services.AddDataProtection();
// builder.Services.AddHttpClient<IGeoLocationService, ApiGeoLocationService>();
// builder.Services.AddScoped<IAuditLogService, AuditLogService>();
// builder.Services.AddScoped<IGeoLocationService, ApiGeoLocationService>();
// builder.Services.AddScoped<IIpBlockService, IpBlockService>();
// builder.Services.AddScoped<IMacAddressService, MacAddressService>();
// builder.Services.AddScoped<INotificationService, NotificationService>();
// builder.Services.AddScoped<IRateLimitService, RateLimitService>();
// builder.Services.AddScoped<IRoleService, RoleService>();
// builder.Services.AddScoped<RoleManager<IdentityRole<int>>>();
// builder.Services.AddScoped<ITokenService, TokenService>();
// builder.Services.AddScoped<IThreatDetectionService, ThreatDetectionService>();
//
// // SignalR
// builder.Services.AddSignalR();
//
// // Memory Cache for Rate Limiting
// builder.Services.AddMemoryCache();
// builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
// builder.Services.AddInMemoryRateLimiting();
// builder.Services.AddSingleton<IMemoryCache, MemoryCache>();
// builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
//
// // Background service
// builder.Services.AddHostedService<ApiScannerBackgroundService>();
//
// // Add resilient API scanner
// builder.Services.AddHttpClient<IApiScannerService, ApiScannerService>()
//     .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5)))
//     .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
//         .RetryAsync(3))
//     .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
//         .CircuitBreakerAsync(5, TimeSpan.FromSeconds(10)));
//
// // CORS policy setup
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AllowFrontend", policy =>
//     {
//         policy.WithOrigins("https://your-frontend-domain.com")
//               .AllowAnyHeader()
//               .AllowAnyMethod()
//               .AllowCredentials();
//     });
// });
//
// // Swagger setup
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen(options =>
// {
//     options.SwaggerDoc("v1", new OpenApiInfo { Title = "API Monitor", Version = "v1" });
//     options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//     {
//         In = ParameterLocation.Header,
//         Name = "Authorization",
//         Type = SecuritySchemeType.ApiKey,
//         BearerFormat = "JWT",
//         Description = "JWT Authorization header using the Bearer scheme."
//     });
//     options.AddSecurityRequirement(new OpenApiSecurityRequirement
//     {
//         {
//             new OpenApiSecurityScheme
//             {
//                 Reference = new OpenApiReference
//                 {
//                     Type = ReferenceType.SecurityScheme,
//                     Id = "Bearer"
//                 }
//             },
//             Array.Empty<string>()
//         }
//     });
// });
//
// // Build app
// WebApplication app = builder.Build();
//
// // CORS setup
// app.UseCors("AllowFrontend");
//
// if (app.Environment.IsProduction())
// {
//     app.UseHttpsRedirection();
//     app.UseHsts();
// }
// else if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI(x =>
//     {
//         x.SwaggerEndpoint("/swagger/v1/swagger.json", "API Monitor v1");
//         x.RoutePrefix = string.Empty;
//     });
// }
//
// // Middleware for logging and IP blocking
// app.UseMiddleware<AuditLoggingMiddleware>();
// app.UseMiddleware<IpBanMiddleware>();
// app.UseMiddleware<RequestInfoMiddleware>();
//
// // Serilog request logging
// app.UseSerilogRequestLogging();
//
// // Rate limiting middleware
// app.UseIpRateLimiting();
//
// app.UseRouting();
//
// // Authentication and Authorization middleware
// app.UseAuthentication();
// app.UseAuthorization();
//
// // Map controllers and SignalR hubs
// app.MapControllers();
// app.MapHub<NotificationHub>("/notifications");
//
// // Seed roles if in development or staging
// // if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
// // {
// //     app.Use(async (context, next) =>
// //         {
// //             using (IServiceScope scope = app.Services.CreateScope())
// //             {
// //                 RoleManager<IdentityRole<int>> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
// //                 await RoleFactory.SeedRoles(roleManager);
// //             }
// //         });
// // }
//
// // Middleware to resolve scoped services
// app.Use(async (context, next) =>
// {
//     using (IServiceScope scope = app.Services.CreateScope()) // Ensure scoped service resolution
//     {
//         var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
//         // You can use the auditLogService here if necessary
//         await next();
//     }
// });
//
// // Run the application
// app.Run();

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
using Polly;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Setup DBContext
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// Setup Identity
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

// Authentication setup
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

// Add services
builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Background service
builder.Services.AddHostedService<ApiScannerBackgroundService>();

// Resilient API scanner
builder.Services.AddHttpClient<IApiScannerService, ApiScannerService>()
    .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5)))
    .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .RetryAsync(3))
    .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(10)));

// Swagger setup
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

// Build app
WebApplication app = builder.Build();

// Enable Swagger in Development environment
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

// Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Run the application
app.Run();
