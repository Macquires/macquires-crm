using Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        //>>> AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        //>>> FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        //>>> MediatR
        services.AddMediatR(x =>
        {
            x.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
            x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });

        //>>> Register non-handler services in Application.Features
        // MediatR already registers IRequestHandler<,> via RegisterServicesFromAssembly.
        // Do not register framework interfaces (e.g. IEquatable<> on records/DTOs) — that breaks DI validation.
        var assembly = Assembly.GetExecutingAssembly();
        var featureTypes = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.Namespace != null && type.Namespace.StartsWith("Application.Features", StringComparison.Ordinal));

        foreach (var type in featureTypes)
        {
            var allInterfaces = type.GetInterfaces();
            var applicationInterfaces = allInterfaces
                .Where(i => i.Namespace != null && i.Namespace.StartsWith("Application.", StringComparison.Ordinal))
                .ToList();

            foreach (var serviceInterface in applicationInterfaces)
                services.AddScoped(serviceInterface, type);

            if (allInterfaces.Length == 0)
                services.AddScoped(type);
        }

        return services;
    }
}

