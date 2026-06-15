using ASPNET.BackEnd.Health;
using Application;
using ASPNET.BackEnd.Common.Handlers;
using Infrastructure;
using Infrastructure.DataAccessManager.EFCore;
using Infrastructure.SeedManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.OpenApi.Models;
using System.Text.Json;

namespace ASPNET.BackEnd;

public static class BackEndConfiguration
{
    public static IServiceCollection AddBackEndServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        //>>> Application Layer
        services.AddApplicationServices();

        //>>> Infrastructure Layer
        services.AddInfrastructureServices(configuration);

        services.AddExceptionHandler<CustomExceptionHandler>();
        services.AddSignalR();
        services.AddScoped<Application.Common.Integrations.IIntegrationLiveBroadcaster, ASPNET.BackEnd.Hubs.SignalRIntegrationLiveBroadcaster>();
        services.AddScoped<Application.Common.Telecom.Analytics.IExecutiveAlertBroadcaster, ASPNET.BackEnd.Hubs.SignalRExecutiveAlertBroadcaster>();

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("telecom-financial", limiter =>
            {
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.PermitLimit = 60;
                limiter.QueueLimit = 0;
            });
        });

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var healthChecks = services.AddHealthChecks();
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            healthChecks.AddSqlServer(connectionString, name: "sqlserver");
        }

        var redis = configuration.GetConnectionString("Redis") ?? configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redis))
        {
            healthChecks.AddRedis(redis, name: "redis");
        }

        var rabbitConnection = configuration["RabbitMq:ConnectionString"];
        if (configuration.GetValue("RabbitMq:Enabled", false) && !string.IsNullOrWhiteSpace(rabbitConnection))
        {
            healthChecks.AddCheck("rabbitmq", new RabbitMqAsyncHealthCheck(rabbitConnection));
        }

        var simulatorUrl = configuration["TelecomIntegrations:SimulatorBaseUrl"];
        if (!string.IsNullOrWhiteSpace(simulatorUrl))
        {
            healthChecks.AddUrlGroup(new Uri($"{simulatorUrl.TrimEnd('/')}/health"), name: "simulator");
        }

        //>>> Common

        services.AddHttpContextAccessor();
        services.AddCors(opt =>
        {
            opt.AddDefaultPolicy(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
        });
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = environment.IsDevelopment();
            });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Syriatel CRM API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });

        });


        services.Configure<ApiBehaviorOptions>(x =>
        {
            x.SuppressModelStateInvalidFilter = true;
        });

        return services;
    }

    public static IEndpointRouteBuilder MapBackEndRoutes(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllers();
        endpoints.MapHub<ASPNET.BackEnd.Hubs.IntegrationLiveHub>("/hubs/integration-live");
        endpoints.MapHub<ASPNET.BackEnd.Hubs.ExecutiveAlertsHub>("/hubs/executive-alerts");
        endpoints.MapHealthChecks("/health");

        return endpoints;
    }

    public static IApplicationBuilder RegisterBackEndBuilder(
        this IApplicationBuilder app,
        IWebHostEnvironment environment,
        IHost host,
        IConfiguration configuration
        )
    {
        using (var scope = host.Services.CreateScope())
        {
            var encryption = scope.ServiceProvider.GetRequiredService<Application.Common.Security.IFieldEncryptionService>();
            Infrastructure.DataAccessManager.EFCore.Converters.FieldEncryptionScope.Initialize(encryption);
        }

        // >>> Create database
        host.CreateDatabase();

        //seed database with system data
        host.SeedSystemData();

        //seed database with demo data
        if (configuration.GetValue<bool>("IsDemoVersion"))
        {
            host.SeedDemoData();
        }

        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Syriatel CRM API V1");
            });
        }

        return app;
    }


}
