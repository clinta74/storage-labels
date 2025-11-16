global using Mediator;
global using Ardalis.Result;
global using StorageLabelsApi.Extensions;

using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Endpoints;
using Microsoft.AspNetCore.Authorization;
using StorageLabelsApi.Authorization;
using StorageLabelsApi.Services;
using StorageLabelsApi.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using StorageLabelsApi.Models;
using StorageLabelsApi.Transformer;
using Swashbuckle.AspNetCore.SwaggerUI;
using StorageLabelsApi.Models.Settings;

const string OpenApiDocumentName = "storage-labels-api";

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .Configure<Auth0Settings>(builder.Configuration.GetSection(nameof(Auth0Settings)))
    .AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped)
    .AddLogging()
    .AddOpenApi(OpenApiDocumentName, options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
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

var auth0 = builder.Configuration.GetSection(nameof(Auth0Settings)).Get<Auth0Settings>()
    ?? throw new ArgumentException("Auth0 settings not found.");

auth0.Validate();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = auth0.DomainUrl;
        options.Audience = auth0.Audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ClockSkew = TimeSpan.FromSeconds(5),
            NameClaimType = ClaimTypes.NameIdentifier,
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();
builder.Services.AddSingleton<StorageLabelsApi.Filters.RateLimiter>(sp => new StorageLabelsApi.Filters.RateLimiter(100, TimeSpan.FromMinutes(1)));

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Policies.Permissions)
    {
        options.AddPolicy(permission, policy => policy.Requirements.Add(new HasScopeRequirement(permission, auth0.DomainUrl)));
    }
});

builder.Services.AddTransient<IAuth0ManagementApiClient>(provider => new Auth0ManagementApiClient(auth0.ApiClientId, auth0.ClientSecret, auth0.Domain));

// Register file system abstraction
builder.Services.AddSingleton<System.IO.Abstractions.IFileSystem, System.IO.Abstractions.FileSystem>();

// Register encryption services
builder.Services.AddScoped<IImageEncryptionService, ImageEncryptionService>();
builder.Services.AddSingleton<IKeyRotationService, KeyRotationService>();
builder.Services.AddSingleton<IRotationProgressNotifier, RotationProgressNotifier>();

builder.Services.AddScoped<UserExistsEndpointFilter>();

var app = builder.Build();

// Apply migrations
using (var serviceScope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
{
    using (var context = serviceScope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>())
    {
        context.Database.Migrate();
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
    .AllowAnyHeader());

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

app.MapAll();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Storage Lables API";
        options.SwaggerEndpoint($"/openapi/{OpenApiDocumentName}.json", OpenApiDocumentName);
        options.OAuthClientId(auth0.ClientId);
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
