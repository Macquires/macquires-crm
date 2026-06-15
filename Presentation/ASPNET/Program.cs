using ASPNET.BackEnd;
using ASPNET.BackEnd.Common.Middlewares;
using ASPNET.FrontEnd;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    var devOverrides = new Dictionary<string, string?>();
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    {
        devOverrides["Jwt:Key"] = "DevOnly_NotForProduction_ChangeViaUserSecrets!";
    }

    if (string.IsNullOrWhiteSpace(builder.Configuration["FieldEncryption:KeyBase64"]))
    {
        devOverrides["FieldEncryption:KeyBase64"] = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA=";
    }

    if (string.IsNullOrWhiteSpace(builder.Configuration["FieldEncryption:SearchHashKeyBase64"]))
    {
        devOverrides["FieldEncryption:SearchHashKeyBase64"] = "ISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+P0A=";
    }

    if (string.IsNullOrWhiteSpace(builder.Configuration["AspNetIdentity:DefaultAdmin:Password"]))
    {
        devOverrides["AspNetIdentity:DefaultAdmin:Password"] = "123456";
    }

    if (devOverrides.Count > 0)
    {
        builder.Configuration.AddInMemoryCollection(devOverrides);
    }
}

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
    ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(
        builder.Configuration["OTEL_SERVICE_NAME"] ?? "nsuite-telecom"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("nSuite.Telecom");

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
        }
        else if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }
    });

//>>> Create Logs folder for Serilog
var logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "app_data", "logs");
if (!Directory.Exists(logPath))
{
    Directory.CreateDirectory(logPath);
}

builder.Services.AddBackEndServices(builder.Configuration, builder.Environment);
builder.Services.AddFrontEndServices();

var app = builder.Build();

app.RegisterBackEndBuilder(app.Environment, app, builder.Configuration);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();
app.UseCors();
app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
app.UseMiddleware<GlobalApiExceptionHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<Infrastructure.Security.OperatorContextMiddleware>();
app.UseMiddleware<ASPNET.BackEnd.Common.Middlewares.MaintenanceMiddleware>();
app.UseMiddleware<ASPNET.BackEnd.Common.Middlewares.PermissionLandingRedirectMiddleware>();
app.UseMiddleware<ASPNET.BackEnd.Common.Middlewares.StrictPersonaPathMiddleware>();
app.UseMiddleware<ASPNET.BackEnd.Common.Middlewares.UserActivityMiddleware>();
app.UseMiddleware<ASPNET.BackEnd.Common.Middlewares.AdministrationPageGuardMiddleware>();
app.MapStaticAssets();

app.MapFrontEndRoutes();
app.MapBackEndRoutes();

app.Run();

public partial class Program;
