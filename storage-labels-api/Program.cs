global using Mediator;
global using Ardalis.Result;
global using StorageLabelsApi.Extensions;

using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Datalayer.Models;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Endpoints;
using Microsoft.AspNetCore.Authorization;
using StorageLabelsApi.Authorization;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;
using StorageLabelsApi.Services.Authentication;
using StorageLabelsApi.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.Settings;
using StorageLabelsApi.Transformer;
using Swashbuckle.AspNetCore.SwaggerUI;
using StorageLabelsApi.Middleware;

const string OpenApiDocumentName = "storage-labels-api";

var builder = WebApplication.CreateBuilder(args);

// Configure Authentication and JWT settings
builder.Services
    .Configure<AuthenticationSettings>(builder.Configuration.GetSection("Authentication"))
    .Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"))
    .Configure<RefreshTokenSettings>(builder.Configuration.GetSection("RefreshTokens"))
    .Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimit"))
    .AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped)
    .AddLogging()
    .AddOpenApi(OpenApiDocumentName, options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "Storage Labels API";
            document.Info.Version = "1.0";
            document.Info.Description = "API for managing storage boxes, items, and locations";
            return Task.CompletedTask;
        });
    })
    .AddCors()
    .ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure Npgsql to handle DateTime mapping
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddHttpContextAccessor();

// PostgreSQL Database configuration
var postgresBuilder = new NpgsqlConnectionStringBuilder()
{
    Host = builder.Configuration["POSTGRES_HOST"] ?? throw new ArgumentException("POSTGRES_HOST not configured"),
    Database = builder.Configuration["POSTGRES_DATABASE"] ?? throw new ArgumentException("POSTGRES_DATABASE not configured"),
    Username = builder.Configuration["POSTGRES_USERNAME"] ?? throw new ArgumentException("POSTGRES_USERNAME not configured"),
    Password = builder.Configuration["POSTGRES_PASSWORD"] ?? throw new ArgumentException("POSTGRES_PASSWORD not configured"),
    Port = int.Parse(builder.Configuration["POSTGRES_PORT"] ?? "5432"),
    SslMode = Enum.Parse<SslMode>(builder.Configuration["POSTGRES_SSL_MODE"] ?? "Prefer"),
    Timezone = "UTC",
};

builder.Services.AddDbContext<StorageLabelsDbContext>(options =>
{
    options.UseNpgsql(
        postgresBuilder.ConnectionString,
        npgsqlOptions => npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    );
}, ServiceLifetime.Transient);

// Get authentication settings
var authSettings = builder.Configuration.GetSection("Authentication").Get<AuthenticationSettings>()
    ?? throw new ArgumentException("Authentication settings not found.");

if (authSettings.Mode == AuthenticationMode.Local)
{
    // Get JWT settings for local authentication
    var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
        ?? throw new ArgumentException("JWT settings not found.");
    
    // Generate secret if it's missing or is a placeholder
    if (jwtSettings.IsSecretPlaceholder())
    {
        var generatedSecret = JwtSettings.GenerateSecret();
        jwtSettings.Secret = generatedSecret;
        
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
        logger.JwtSecretNotConfigured();
        logger.JwtSecretTemporary();
        logger.JwtSecretProductionWarning();
        logger.JwtSecretGenerated(generatedSecret);
    }
    
    jwtSettings.Validate();

    // Configure ASP.NET Core Identity
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        // Password settings from configuration
        options.Password.RequireDigit = authSettings.Local.RequireDigit;
        options.Password.RequiredLength = authSettings.Local.MinimumPasswordLength;
        options.Password.RequireNonAlphanumeric = authSettings.Local.RequireNonAlphanumeric;
        options.Password.RequireUppercase = authSettings.Local.RequireUppercase;
        options.Password.RequireLowercase = authSettings.Local.RequireLowercase;
        
        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(authSettings.Local.LockoutDurationMinutes);
        options.Lockout.MaxFailedAccessAttempts = authSettings.Local.MaxFailedAccessAttempts;
        options.Lockout.AllowedForNewUsers = authSettings.Local.LockoutEnabled;
        
        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<StorageLabelsDbContext>()
    .AddDefaultTokenProviders();

    // Configure JWT Bearer authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.FromSeconds(5),
            NameClaimType = ClaimTypes.NameIdentifier,
        };
    });

    // Register authentication services
    builder.Services.AddScoped<JwtTokenService>();
    builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    builder.Services.AddScoped<IAuthenticationService, LocalAuthenticationService>();
    builder.Services.AddScoped<RoleInitializationService>();
}
else if (authSettings.Mode == AuthenticationMode.None)
{
    // Configure ASP.NET Core Identity even in NoAuth mode (needed for user management handlers)
    // But don't let it register authentication schemes - we'll do that explicitly
    builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // Minimal password requirements since auth is bypassed
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 1;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<StorageLabelsDbContext>()
    .AddDefaultTokenProviders();

    // No authentication - minimal auth configuration
    builder.Services.AddAuthentication("NoAuth")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, NoAuthAuthenticationHandler>("NoAuth", options => { });
    
    builder.Services.AddScoped<JwtTokenService>();
    builder.Services.AddScoped<IAuthenticationService, NoAuthenticationService>();
}
else
{
    throw new ArgumentException($"Invalid authentication mode: {authSettings.Mode}");
}

builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Policies.AllPermissions)
    {
        options.AddPolicy(permission, policy => policy.Requirements.Add(new HasScopeRequirement(permission, authSettings.Mode == AuthenticationMode.Local ? "local" : "none")));
    }
});

// Configure built-in rate limiting (.NET 10)
builder.Services.AddConfiguredRateLimiting(builder.Configuration);

// Register file system abstraction
builder.Services.AddSingleton<System.IO.Abstractions.IFileSystem, System.IO.Abstractions.FileSystem>();

// Register TimeProvider
builder.Services.AddSingleton(TimeProvider.System);

// Register encryption services
builder.Services.AddScoped<IImageEncryptionService, ImageEncryptionService>();
builder.Services.AddSingleton<IKeyRotationService, KeyRotationService>();
builder.Services.AddSingleton<IRotationProgressNotifier, RotationProgressNotifier>();

// Register search service (PostgreSQL FTS in production)
builder.Services.AddScoped<ISearchService, PostgreSqlSearchService>();

builder.Services.AddScoped<UserExistsEndpointFilter>();

var app = builder.Build();

// Initialize database using mediator handler
using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    var mediator = serviceScope.ServiceProvider.GetRequiredService<IMediator>();
    var initResult = await mediator.Send(new StorageLabelsApi.Handlers.Initialization.InitializeDatabaseRequest());
    
    if (!initResult.IsSuccess)
    {
        throw new InvalidOperationException("Failed to initialize database");
    }
}

app.UseCors(config => config
    .WithExposedHeaders("x-total-count")
    .WithOrigins(
    [
        "http://localhost:4000",
        "https://storage-labels.pollyspeople.net",
    ])
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'none'";
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapAll();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Storage Labels API";
        options.SwaggerEndpoint($"/openapi/{OpenApiDocumentName}.json", "Storage Labels API");
        options.DefaultModelRendering(ModelRendering.Example);
        options.DefaultModelExpandDepth(1);
    });
}

app.UseExceptionHandler(exceptionHandlerApp
    => exceptionHandlerApp.Run(async context
        => await Results.Problem()
                     .ExecuteAsync(context)));

app.UseHttpsRedirection();

app.Run();
