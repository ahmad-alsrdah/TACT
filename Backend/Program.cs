using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TACT.Data;
using TACT.Models;
using TACT.Services;
using Microsoft.OpenApi.Models;
using Serilog;
using Log = Serilog.Log;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting TACT Backend Server...");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddIdentity<User, IdentityRole<long>>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequiredLength = 8;
        
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddScoped<IJwtService, JwtService>();
    builder.Services.AddTransient<IEmailService, EmailService>();
    builder.Services.AddHttpClient<AiService>();
    builder.Services.AddAuthorization();
    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste your JWT token only (do NOT include the word 'Bearer' — Swagger adds it automatically)"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        });
    });

    var app = builder.Build();

    app.UseMiddleware<TACT.Middleware.ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} | Client IP: {ClientIP} | Device: {ClientDevice} in {Elapsed:0.0000} ms";

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        
            if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.ToString();
            }
            diagnosticContext.Set("ClientIP", ipAddress);

            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
            var clientDevice = "Unknown Client";

            if (!string.IsNullOrEmpty(userAgent))
            {
                if (userAgent.Contains("PostmanRuntime")) clientDevice = "Postman Client";
                else if (userAgent.Contains("Android")) clientDevice = "Android Device";
                else if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) clientDevice = "iOS Device";
                else if (userAgent.Contains("Chrome")) clientDevice = "Chrome Browser";
                else clientDevice = userAgent.Length > 30 ? userAgent.Substring(0, 30) + "..." : userAgent; // اختصار النصوص الطويلة جداً
            }
        
            diagnosticContext.Set("ClientDevice", clientDevice);
        
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("UserAgent", userAgent);
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseStaticFiles();
        app.UseSwagger();      
        app.UseSwaggerUI(options =>
        {
            options.InjectStylesheet("/swagger-ui/dark-theme.css");
        });    
    }

    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    
    app.MapControllers();
    app.MapGet("/", () => "Root Endpoint!!");

    app.Run();
}
catch (Exception ex) when (ex.GetType().Name != "HostAbortedException") 
{
    Log.Fatal(ex, "The TACT application failed to start correctly.");
}
finally
{
    Log.CloseAndFlush();
}