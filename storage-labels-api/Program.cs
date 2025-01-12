global using MediatR;
global using Ardalis.Result;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Endpoints.V1;
using Microsoft.AspNetCore.Authorization;
using StorageLabelsApi.Authorization;
using StorageLabelsApi.Services;
using StorageLabelsApi.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using StorageLabelsApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddLogging();
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
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

var domain = $"https://{builder.Configuration["Auth0:Domain"]}/";
var audience = builder.Configuration["Auth0:Audience"];
var clientSecret = builder.Configuration["Auth0:ClientSecret"];
var apiClientId = builder.Configuration["Auth0:ApiClientId"];

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = domain;
        options.Audience = audience;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ClockSkew = TimeSpan.FromSeconds(5),
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Authorization.Permissions)
    {
        options.AddPolicy(permission, policy => policy.Requirements.Add(new HasScopeRequirement(permission, domain)));
    }
});

builder.Services.AddTransient<IAuth0ManagementApiClient>(provider => new Auth0ManagementApiClient(apiClientId, clientSecret, domain));

builder.Services.AddScoped<UserExistsFilter>();

var app = builder.Build();

app.MapV1Endpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
