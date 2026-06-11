using ASPNET.BackEnd;
using ASPNET.BackEnd.Common.Middlewares;
using ASPNET.FrontEnd;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

//>>> Create Logs folder for Serilog
var logPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "app_data", "logs");
if (!Directory.Exists(logPath))
{
    Directory.CreateDirectory(logPath);
}

builder.Services.AddBackEndServices(builder.Configuration);
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
