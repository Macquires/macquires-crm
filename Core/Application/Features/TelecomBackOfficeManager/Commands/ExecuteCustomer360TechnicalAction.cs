using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Features.TelecomManager.Commands;
using Application.Features.VasManager.Commands;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public sealed class ExecuteCustomer360TechnicalActionResult
{
    public string ActionType { get; init; } = "";
    public string MessageAr { get; init; } = "";
    public string? OperationNumber { get; init; }
    public bool Success { get; init; } = true;
}

public sealed class ExecuteCustomer360TechnicalActionRequest : IRequest<ExecuteCustomer360TechnicalActionResult>, IRequireAnyPermission
{
    public string CustomerId { get; init; } = "";
    public string ActionType { get; init; } = "";
    public string? SubscriberProfileId { get; init; }
    public string? Msisdn { get; init; }
    public string? TargetProductCode { get; init; }
    public string? ProductOfferingId { get; init; }
    public string? SimIccid { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.CustomerProvisioningAny;
}

public sealed class ExecuteCustomer360TechnicalActionValidator : AbstractValidator<ExecuteCustomer360TechnicalActionRequest>
{
    public ExecuteCustomer360TechnicalActionValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ActionType).NotEmpty();
    }
}

public sealed class ExecuteCustomer360TechnicalActionHandler
    : IRequestHandler<ExecuteCustomer360TechnicalActionRequest, ExecuteCustomer360TechnicalActionResult>
{
    private static readonly string[] ProvisioningPermissions =
    [
        PermissionCatalog.TelecomCustomerProvisioning,
        PermissionCatalog.TelecomLineMigrate,
        PermissionCatalog.TelecomVasToggle,
    ];

    private static readonly string[] NetworkPermissions =
    [
        PermissionCatalog.TelecomNetworkHlrResync,
        PermissionCatalog.TelecomLineSimSwap,
        PermissionCatalog.TelecomLineActivate,
    ];

    private readonly IMediator _mediator;
    private readonly IQueryContext _query;
    private readonly IPermissionEvaluator _permissions;
    private readonly IOperatorContext _operator;
    private readonly IUserAuditService _audit;

    public ExecuteCustomer360TechnicalActionHandler(
        IMediator mediator,
        IQueryContext query,
        IPermissionEvaluator permissions,
        IOperatorContext operatorContext,
        IUserAuditService audit)
    {
        _mediator = mediator;
        _query = query;
        _permissions = permissions;
        _operator = operatorContext;
        _audit = audit;
    }

    public async Task<ExecuteCustomer360TechnicalActionResult> Handle(
        ExecuteCustomer360TechnicalActionRequest request,
        CancellationToken cancellationToken)
    {
        if (!_operator.IsAuthenticated || string.IsNullOrEmpty(_operator.UserId))
        {
            throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ هذه العملية.");
        }

        var actorId = OperatorActor.RequireUserId(_operator);
        var customerId = request.CustomerId.Trim();
        var action = (request.ActionType ?? "").Trim();
        if (string.IsNullOrEmpty(action))
        {
            throw new BusinessRuleViolationException("نوع العملية مطلوب.");
        }

        var customerExists = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(c => c.Id == customerId, cancellationToken);
        if (!customerExists)
        {
            throw new BusinessRuleViolationException("المشترك غير موجود.");
        }

        var ctx = await ResolveLineContextAsync(customerId, request, cancellationToken);

        return action.ToUpperInvariant() switch
        {
            "ACTIVATEVAS" => await ActivateVasAsync(request, ctx, actorId, cancellationToken),
            "PACKAGEMIGRATION" => await PackageMigrationAsync(request, ctx, actorId, cancellationToken),
            "SIMSWAP" => await SimSwapAsync(request, ctx, actorId, cancellationToken),
            "LINEACTIVATION" => await LineActivationAsync(request, ctx, actorId, cancellationToken),
            _ => throw new BusinessRuleViolationException($"نوع العملية غير مدعوم: {action}"),
        };
    }

    private async Task<ExecuteCustomer360TechnicalActionResult> ActivateVasAsync(
        ExecuteCustomer360TechnicalActionRequest request,
        LineContext ctx,
        string actorId,
        CancellationToken cancellationToken)
    {
        await EnsureAnyPermissionAsync(ProvisioningPermissions, cancellationToken);

        var code = (request.TargetProductCode ?? "").Trim();
        if (string.IsNullOrEmpty(code))
        {
            throw new BusinessRuleViolationException("رمز خدمة VAS مطلوب.");
        }

        if (string.IsNullOrEmpty(ctx.Msisdn))
        {
            throw new BusinessRuleViolationException("لا يوجد MSISDN صالح للخط المحدد.");
        }

        var vasResult = await _mediator.Send(
            new ToggleSubscriberVasServiceRequest
            {
                Msisdn = ctx.Msisdn,
                ServiceCode = code,
                Action = VasToggleAction.Activate,
            },
            cancellationToken);

        await LogCustomer360ActionAsync(
            actorId,
            "ActivateVas",
            ctx,
            new { request.TargetProductCode, vasResult.OperationNumber },
            cancellationToken);

        return new ExecuteCustomer360TechnicalActionResult
        {
            ActionType = "ActivateVas",
            OperationNumber = vasResult.OperationNumber,
            MessageAr = $"تم تفعيل خدمة {vasResult.ServiceCode} على الخط {vasResult.Msisdn} وتحديث CBS/HLR.",
        };
    }

    private async Task<ExecuteCustomer360TechnicalActionResult> PackageMigrationAsync(
        ExecuteCustomer360TechnicalActionRequest request,
        LineContext ctx,
        string actorId,
        CancellationToken cancellationToken)
    {
        await EnsureAnyPermissionAsync(ProvisioningPermissions, cancellationToken);

        var offeringId = await ResolveProductOfferingIdAsync(request, ctx, cancellationToken);
        var create = await _mediator.Send(
            new CreateTelecomOperationRequest
            {
                Kind = TelecomOperationKind.Migration,
                SubscriberProfileId = ctx.SubscriberProfileId,
                MsisdnAssetId = ctx.MsisdnAssetId,
                ProductOfferingId = offeringId,
                Notes = request.Notes,
            },
            cancellationToken);

        var op = create.Data ?? throw new BusinessRuleViolationException("تعذّر إنشاء طلب الترحيل.");
        await MarkDocumentAndConfirmAsync(op.Id, cancellationToken);

        await LogCustomer360ActionAsync(
            actorId,
            "PackageMigration",
            ctx,
            new { offeringId, op.Number },
            cancellationToken);

        return new ExecuteCustomer360TechnicalActionResult
        {
            ActionType = "PackageMigration",
            OperationNumber = op.Number,
            MessageAr = "تم تنفيذ ترحيل الباقة بنجاح وتحديث سيرفرات الشبكة والـ CBS فوراً.",
        };
    }

    private async Task<ExecuteCustomer360TechnicalActionResult> SimSwapAsync(
        ExecuteCustomer360TechnicalActionRequest request,
        LineContext ctx,
        string actorId,
        CancellationToken cancellationToken)
    {
        await EnsureAnyPermissionAsync(NetworkPermissions, cancellationToken);

        var iccid = (request.SimIccid ?? "").Trim();
        if (string.IsNullOrEmpty(iccid))
        {
            throw new BusinessRuleViolationException("ICCID الشريحة الجديدة مطلوب.");
        }

        var create = await _mediator.Send(
            new CreateTelecomOperationRequest
            {
                Kind = TelecomOperationKind.SimSwap,
                SubscriberProfileId = ctx.SubscriberProfileId,
                MsisdnAssetId = ctx.MsisdnAssetId,
                SimIccid = iccid,
                Notes = request.Notes,
            },
            cancellationToken);

        var op = create.Data ?? throw new BusinessRuleViolationException("تعذّر إنشاء طلب تبديل الشريحة.");
        await MarkDocumentAndConfirmAsync(op.Id, cancellationToken);

        await LogCustomer360ActionAsync(actorId, "SimSwap", ctx, new { iccid, op.Number }, cancellationToken);

        return new ExecuteCustomer360TechnicalActionResult
        {
            ActionType = "SimSwap",
            OperationNumber = op.Number,
            MessageAr = "تم تبديل الشريحة ومزامنة HLR/CBS بنجاح.",
        };
    }

    private async Task<ExecuteCustomer360TechnicalActionResult> LineActivationAsync(
        ExecuteCustomer360TechnicalActionRequest request,
        LineContext ctx,
        string actorId,
        CancellationToken cancellationToken)
    {
        await EnsureAnyPermissionAsync(NetworkPermissions, cancellationToken);

        var offeringId = await ResolveProductOfferingIdAsync(request, ctx, cancellationToken);
        var iccid = (request.SimIccid ?? "").Trim();

        var create = await _mediator.Send(
            new CreateTelecomOperationRequest
            {
                Kind = TelecomOperationKind.NewActivation,
                SubscriberProfileId = ctx.SubscriberProfileId,
                MsisdnAssetId = ctx.MsisdnAssetId,
                ProductOfferingId = offeringId,
                SimIccid = string.IsNullOrEmpty(iccid) ? null : iccid,
                Notes = request.Notes,
            },
            cancellationToken);

        var op = create.Data ?? throw new BusinessRuleViolationException("تعذّر إنشاء طلب التفعيل.");
        await MarkDocumentAndConfirmAsync(op.Id, cancellationToken);

        await LogCustomer360ActionAsync(actorId, "LineActivation", ctx, new { offeringId, op.Number }, cancellationToken);

        return new ExecuteCustomer360TechnicalActionResult
        {
            ActionType = "LineActivation",
            OperationNumber = op.Number,
            MessageAr = "تم تفعيل الخط وربطه بشبكة HLR وCBS بنجاح.",
        };
    }

    private async Task MarkDocumentAndConfirmAsync(string operationId, CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new UploadTelecomOperationDocumentRequest { Id = operationId },
            cancellationToken);

        await _mediator.Send(
            new ConfirmTelecomOperationRequest { Id = operationId },
            cancellationToken);
    }

    private async Task<string> ResolveProductOfferingIdAsync(
        ExecuteCustomer360TechnicalActionRequest request,
        LineContext ctx,
        CancellationToken cancellationToken)
    {
        var offeringId = (request.ProductOfferingId ?? "").Trim();
        if (!string.IsNullOrEmpty(offeringId))
        {
            return offeringId;
        }

        var code = (request.TargetProductCode ?? "").Trim();
        if (string.IsNullOrEmpty(code))
        {
            throw new BusinessRuleViolationException("يجب اختيار الباقة أو إدخال رمز العرض.");
        }

        var now = DateTime.UtcNow;
        var match = await (
            from o in _query.ProductOffering.AsNoTracking().IsDeletedEqualTo(false)
            where o.IsActive
                  && (o.ValidFromUtc == null || o.ValidFromUtc <= now)
                  && (o.ValidToUtc == null || o.ValidToUtc >= now)
                  && (o.Id == code || o.Code == code || o.ServiceIdSocCode == code)
            join p in _query.Product.AsNoTracking().IsDeletedEqualTo(false) on o.ProductId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            where p == null || p.ServiceCode == code || p.Id == code
            select o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(match))
        {
            throw new BusinessRuleViolationException("العرض التجاري المختار غير موجود في الكتالوج.");
        }

        return match;
    }

    private async Task<LineContext> ResolveLineContextAsync(
        string customerId,
        ExecuteCustomer360TechnicalActionRequest request,
        CancellationToken cancellationToken)
    {
        var profileId = (request.SubscriberProfileId ?? "").Trim();
        var msisdn = string.IsNullOrWhiteSpace(request.Msisdn)
            ? null
            : TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.Msisdn);

        if (!string.IsNullOrEmpty(profileId))
        {
            var row = await (
                from p in _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                join s in _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo() on p.Id equals s.SubscriberProfileId
                join m in _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo() on s.MsisdnAssetId equals m.Id into mj
                from m in mj.DefaultIfEmpty()
                where p.CustomerId == customerId && p.Id == profileId
                orderby s.IsPrimaryLine descending
                select new LineContext(p.Id, s.MsisdnAssetId, m.Msisdn))
                .FirstOrDefaultAsync(cancellationToken);

            if (row == null)
            {
                throw new BusinessRuleViolationException("ملف المشترك التقني غير مرتبط بهذا العميل.");
            }

            if (!string.IsNullOrEmpty(msisdn) && !string.Equals(row.Msisdn, msisdn, StringComparison.Ordinal))
            {
                throw new BusinessRuleViolationException("رقم الخط لا يطابق ملف المشترك المحدد.");
            }

            return row with { Msisdn = row.Msisdn ?? msisdn };
        }

        if (!string.IsNullOrEmpty(msisdn))
        {
            var byMsisdn = await (
                from p in _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                join s in _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo() on p.Id equals s.SubscriberProfileId
                join m in _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo() on s.MsisdnAssetId equals m.Id
                where p.CustomerId == customerId && m.Msisdn == msisdn
                select new LineContext(p.Id, s.MsisdnAssetId, m.Msisdn))
                .FirstOrDefaultAsync(cancellationToken);

            if (byMsisdn != null)
            {
                return byMsisdn;
            }
        }

        var primary = await (
            from p in _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            join s in _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo() on p.Id equals s.SubscriberProfileId
            join m in _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo() on s.MsisdnAssetId equals m.Id into mj
            from m in mj.DefaultIfEmpty()
            where p.CustomerId == customerId
            orderby s.IsPrimaryLine descending, s.CreatedAtUtc
            select new LineContext(p.Id, s.MsisdnAssetId, m.Msisdn))
            .FirstOrDefaultAsync(cancellationToken);

        if (primary == null)
        {
            throw new BusinessRuleViolationException("لا يوجد خط نشط لهذا المشترك.");
        }

        return primary;
    }

    private async Task EnsureAnyPermissionAsync(string[] keys, CancellationToken cancellationToken)
    {
        foreach (var key in keys)
        {
            if (await _permissions.HasPermissionAsync(_operator.UserId!, key, cancellationToken))
            {
                return;
            }
        }

        throw new BusinessRuleViolationException("ليس لديك صلاحية لتنفيذ هذه العملية.");
    }

    private async Task LogCustomer360ActionAsync(
        string actorId,
        string actionType,
        LineContext ctx,
        object payload,
        CancellationToken cancellationToken)
    {
        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorId,
                ActionType = UserAuditActionTypes.TelecomOperationConfirmed,
                EntityType = "Customer360",
                EntityId = ctx.SubscriberProfileId,
                SummaryAr = $"Customer 360 — {actionType} على {ctx.Msisdn ?? "—"}",
                Payload = payload,
            },
            cancellationToken);
    }

    private sealed record LineContext(string SubscriberProfileId, string? MsisdnAssetId, string? Msisdn);
}
