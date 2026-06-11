using Application.Common.Audit;
using Application.Common.Behaviors;
using Application.Common.Events;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.PaymentServices;
using Application.Common.Telecom.ChangeGsm;
using Application.Common.Telecom.ChangeNumber;
using Application.Common.Telecom.Termination;
using Application.Common.Telecom.OfferSubscription;
using Application.Common.Telecom.Suspension;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.SellingLine;
using Application.Common.Telecom.SimSwap;
using Application.Common.Telecom.TakeOver;
using Application.Common.Telecom.DeviceSales;
using Application.Common.Telecom.Refund;
using Application.Common.Telecom.BackOffice;
using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.Billing;
using Application.Common.Telecom.Inventory;
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
        services.AddScoped<IBillingRoutingOrchestrator, BillingRoutingOrchestrator>();
        services.AddScoped<ISellingLineEligibilityChecker, SellingLineEligibilityChecker>();
        services.AddScoped<IMsisdnPoolSimKitProvisioner, MsisdnPoolSimKitProvisioner>();
        services.AddScoped<IActivationChannelLabelProvider, ActivationChannelLabelProvider>();
        services.AddScoped<IChangeGsmEligibilityChecker, ChangeGsmEligibilityChecker>();
        services.AddScoped<ISimSwapEligibilityChecker, SimSwapEligibilityChecker>();
        services.AddScoped<ISimSwapCompletionService, SimSwapCompletionService>();
        services.AddScoped<ITakeOverEligibilityChecker, TakeOverEligibilityChecker>();
        services.AddScoped<ITakeOverCompletionService, TakeOverCompletionService>();
        services.AddScoped<IChangeGsmCompletionService, ChangeGsmCompletionService>();
        services.AddScoped<IChangeNumberEligibilityChecker, ChangeNumberEligibilityChecker>();
        services.AddScoped<IChangeNumberCompletionService, ChangeNumberCompletionService>();
        services.AddScoped<ITerminationEligibilityChecker, TerminationEligibilityChecker>();
        services.AddScoped<ITerminationCompletionService, TerminationCompletionService>();
        services.AddScoped<IOfferSubscriptionEligibilityChecker, OfferSubscriptionEligibilityChecker>();
        services.AddScoped<IMigrationCompletionService, MigrationCompletionService>();
        services.AddScoped<IVasCompletionService, VasCompletionService>();
        services.AddScoped<ISuspensionEligibilityChecker, SuspensionEligibilityChecker>();
        services.AddScoped<ISuspensionCompletionService, SuspensionCompletionService>();
        services.AddScoped<IReconnectEligibilityChecker, ReconnectEligibilityChecker>();
        services.AddScoped<IReconnectCompletionService, ReconnectCompletionService>();
        services.AddScoped<IPaymentServicesEligibilityChecker, PaymentServicesEligibilityChecker>();
        services.AddScoped<IPaymentServicesOrchestrator, PaymentServicesOrchestrator>();
        services.AddScoped<IPaymentServicesAuditWriter, PaymentServicesAuditWriter>();
        services.AddScoped<IPaymentServicesFraudTicketService, PaymentServicesFraudTicketService>();
        services.AddScoped<IPaymentServicesReversalService, PaymentServicesReversalService>();
        services.AddScoped<IDeviceSalesEligibilityChecker, DeviceSalesEligibilityChecker>();
        services.AddScoped<IDeviceSaleCompletionService, DeviceSaleCompletionService>();
        services.AddScoped<IRefundEligibilityChecker, RefundEligibilityChecker>();
        services.AddScoped<IRefundCompletionService, RefundCompletionService>();
        services.AddScoped<IBadDebtEligibilityChecker, BadDebtEligibilityChecker>();
        services.AddScoped<IBadDebtCompletionService, BadDebtCompletionService>();
        services.AddScoped<IBackOfficePaymentReferenceValidator, BackOfficePaymentReferenceValidator>();
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddSingleton<VasMsisdnLock>();
        services.AddScoped<ISubscriberAccessAuditService, SubscriberAccessAuditService>();
        services.AddScoped<ITelecomInventoryRulesProvider, TelecomInventoryRulesProvider>();
        services.AddScoped<IMsisdnRecyclingService, MsisdnRecyclingService>();

        //>>> MediatR
        services.AddMediatR(x =>
        {
            x.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CreateCustomerPosQuickRegisterNormalizerBehaviour<,>));
            x.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CreateTelecomOperationRequestActivationChannelNormalizerBehaviour<,>));
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

