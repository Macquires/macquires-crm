using Application.Common.Audit;
using Application.Common.Behaviors;
using Application.Common.Events;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        //>>> AutoMapper (15+: configure assemblies inside the callback)
        services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

        //>>> FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddSingleton<ISubscriptionBindingService, SubscriptionBindingService>();
        services.AddSingleton<ICustomerLineLimitPolicy, DefaultCustomerLineLimitPolicy>();
        services.AddScoped<ISubscriptionBindingExecutor, SubscriptionBindingExecutor>();
        services.AddScoped<ITelecomOperationOrchestrator, TelecomOperationOrchestrator>();
        services.AddScoped<ITelecomActivationWorkflow, TelecomActivationWorkflow>();
        services.AddScoped<ITechnicalTicketQueueIngestionService, TechnicalTicketQueueIngestionService>();
        services.AddScoped<ISubscriptionBindingCompensator, SubscriptionBindingCompensator>();
        services.AddScoped<ITelecomHlrFailureCompensator, TelecomHlrFailureCompensator>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddSingleton<VasMsisdnLock>();
        services.AddScoped<ISubscriberAccessAuditService, SubscriberAccessAuditService>();

        //>>> MediatR
        services.AddMediatR(x =>
        {
            x.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
            x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
            x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PersonaStrictAuthorizationBehaviour<,>));
            x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PermissionAuthorizationBehaviour<,>));
        });

        //>>> Register non-handler services in Application.Features
        // MediatR already registers IRequestHandler<,> via RegisterServicesFromAssembly.
        // Do not register framework interfaces (e.g. IEquatable<> on records/DTOs) — that breaks DI validation.
        // Do not register MediatR request marker interfaces — they are not DI services.
        var excludedServiceInterfaces = new HashSet<Type>
        {
            typeof(IRequirePermission),
            typeof(IRequireAnyPermission),
        };

        var assembly = Assembly.GetExecutingAssembly();
        var featureTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Namespace != null && type.Namespace.StartsWith("Application.Features", StringComparison.Ordinal));

        foreach (var type in featureTypes)
        {
            var allInterfaces = type.GetInterfaces();
            var applicationInterfaces = allInterfaces
                .Where(i => i.Namespace != null && i.Namespace.StartsWith("Application.", StringComparison.Ordinal))
                .Where(i => !excludedServiceInterfaces.Contains(i))
                .ToList();

            foreach (var serviceInterface in applicationInterfaces)
                services.AddScoped(serviceInterface, type);

            if (allInterfaces.Length == 0)
                services.AddScoped(type);
        }

        return services;
    }
}

