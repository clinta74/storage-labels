global using MediatR;
global using Ardalis.Result;
global using StorageLabelsApi.Extensions;

using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
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
using Microsoft.Extensions.Options;

const string OpenApiDocumentName = "storage-labels-api";

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .Configure<Auth0Settings>(builder.Configuration.GetSection(nameof(Auth0Settings)))
    .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))
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

// Database configuration
var dataSource = builder.Configuration["DATA_SOURCE"];
var initialCatalog = builder.Configuration["INITIAL_CATALOG"];
var dbPassword = builder.Configuration["DB_PASSWORD"];
var userID = builder.Configuration["DB_USERNAME"];

var sqlBuilder = new SqlConnectionStringBuilder()
{
    DataSource = dataSource,
    InitialCatalog = initialCatalog,
    Password = dbPassword,
    UserID = userID,
    IntegratedSecurity = false,
    TrustServerCertificate = true,
};

builder.Services.AddDbContext<StorageLabelsDbContext>(options =>
{
    options.UseSqlServer(sqlBuilder.ConnectionString);
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

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Policies.Permissions)
    {
        options.AddPolicy(permission, policy => policy.Requirements.Add(new HasScopeRequirement(permission, auth0.DomainUrl)));
    }
});

builder.Services.AddTransient<IAuth0ManagementApiClient>(provider => new Auth0ManagementApiClient(auth0.ApiClientId, auth0.ClientSecret, auth0.Domain));

builder.Services.AddScoped<UserExistsEndpointFilter>();

var app = builder.Build();

app.UseCors(config => config
    .WithExposedHeaders("x-total-count")
    .WithOrigins(
    [
        "http://localhost:4000",
        "https://storage-labels.pollyspeople.net",
    ])
    .AllowAnyMethod()
    .AllowAnyHeader());

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
