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
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.ComponentModel;

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

var auth0 = builder.Configuration.GetSection("Auth0").Get<StorageLabelsApi.Models.Settings.Auth0>() 
    ?? throw new ArgumentException("Auth0 settings not found.");

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
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

builder.Services.AddAuthorization(options =>
{
    foreach (var permission in Authorization.Permissions)
    {
        options.AddPolicy(permission, policy => policy.Requirements.Add(new HasScopeRequirement(permission, auth0.DomainUrl)));
    }
});

builder.Services.AddTransient<IAuth0ManagementApiClient>(provider => new Auth0ManagementApiClient(auth0.ClientId, auth0.ClientSecret, auth0.DomainUrl));

builder.Services.AddScoped<UserExistsFilter>();

var app = builder.Build();

app.MapV1Endpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
