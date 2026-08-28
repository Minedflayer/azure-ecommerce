using CatalogApi.Data;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Azure.Messaging.ServiceBus;

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
    options.UseSqlServer(connectionString);
});

builder.Services.AddSingleton(s =>
{
    var connectionString = Environment.GetEnvironmentVariable("ServiceBusConnection");
    return new ServiceBusClient(connectionString); 
});

builder.Build().Run();
