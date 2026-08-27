using CatalogApi.Data;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    var tenantId = Environment.GetEnvironmentVariable("EntraTenantId");
    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
    options.Audience = Environment.GetEnvironmentVariable("EntraClientId");
    
});
builder.Services.AddAuthorizationBuilder();

// Add EF Core DbContext
builder.Services.AddDbContext<CatalogDbContext>(options =>
{
    var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
    // Ensure the "SqlConnectionString" environment variable is set in local.settings.json and Azure App Settings
    options.UseSqlServer(connectionString);

    
});

builder.Build().Run();
