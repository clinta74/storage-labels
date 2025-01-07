global using MediatR;
global using Ardalis.Result;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Endpoints.V1;

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

var app = builder.Build();

app.MapV1Endpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Run();
