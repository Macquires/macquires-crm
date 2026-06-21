using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Telecom;
using Application.Features.CustomerManager.Queries;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.Customer360;

internal sealed record SubscriptionDocumentContext(
    string OperationId,
    TelecomDocumentStatus DocumentStatus,
    string? IdentityDocumentStorageKey,
    string? KycDocumentReferenceId);

public interface ICustomer360SubscriptionAssembler
{
    Task<IReadOnlyList<Customer360SubscriptionDto>> AssembleAsync(
        string customerId,
        string? customerCreatedById,
        CancellationToken cancellationToken);
}

public sealed class Customer360SubscriptionAssembler : ICustomer360SubscriptionAssembler
{
    private readonly IQueryContext _query;

    public Customer360SubscriptionAssembler(IQueryContext query)
    {
        _query = query;
    }

    public async Task<IReadOnlyList<Customer360SubscriptionDto>> AssembleAsync(
        string customerId,
        string? customerCreatedById,
        CancellationToken cancellationToken)
    {
        var profiles = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == customerId)
            .Include(p => p.SimInventories)
            .ToListAsync(cancellationToken);

        var profileIds = profiles.Select(p => p.Id).ToList();
        if (profileIds.Count == 0)
        {
            return [];
        }

        var profileById = profiles.ToDictionary(p => p.Id);

        var subscriptions = Customer360SubscriptionDeduplicator.DeduplicateByLine(
            await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                .Where(s => profileIds.Contains(s.SubscriberProfileId))
                .Include(s => s.MsisdnAsset)
                .Include(s => s.Product)
                .Include(s => s.ProductOffering!)
                    .ThenInclude(o => o.Components)
                .Include(s => s.SubscriptionTypeLookup)
                .AsSplitQuery()
                .OrderByDescending(s => s.IsPrimaryLine)
                .ThenBy(s => s.CreatedAtUtc)
                .ToListAsync(cancellationToken))
            .Where(s => !string.IsNullOrWhiteSpace(s.MsisdnAsset?.Msisdn))
            .ToList();

        if (subscriptions.Count == 0)
        {
            return [];
        }

        var productIds = subscriptions
            .Select(s => s.ProductId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var offeringIds = subscriptions
            .Select(s => s.ProductOfferingId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        static bool OfferingIsUsable(ProductOffering? offering) => offering is { IsDeleted: false };

        var needsOfferingByProduct = subscriptions.Any(s =>
            string.IsNullOrEmpty(s.ProductOfferingId) && !string.IsNullOrEmpty(s.ProductId));
        var needsOfferingById = subscriptions.Any(s =>
            !string.IsNullOrEmpty(s.ProductOfferingId) && !OfferingIsUsable(s.ProductOffering));

        var offeringsByProductId = needsOfferingByProduct && productIds.Count > 0
            ? await _query.ProductOffering.AsNoTracking()
                .Where(o => !o.IsDeleted && o.ProductId != null && productIds.Contains(o.ProductId))
                .Include(o => o.Components)
                .GroupBy(o => o.ProductId!)
                .ToDictionaryAsync(g => g.Key, g => g.OrderBy(x => x.SortOrder).First(), cancellationToken)
            : new Dictionary<string, ProductOffering>();

        var offeringsById = needsOfferingById && offeringIds.Count > 0
            ? await _query.ProductOffering.AsNoTracking()
                .Where(o => !o.IsDeleted && offeringIds.Contains(o.Id))
                .Include(o => o.Components)
                .ToDictionaryAsync(o => o.Id, cancellationToken)
            : new Dictionary<string, ProductOffering>();

        var msisdnAssetIds = subscriptions
            .Select(s => s.MsisdnAssetId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var operationSimByAsset = msisdnAssetIds.Count == 0
            ? new Dictionary<string, string>()
            : await (
                from op in _query.TelecomOperationRequest.AsNoTracking()
                where op.MsisdnAssetId != null
                      && op.SimInventoryId != null
                      && msisdnAssetIds.Contains(op.MsisdnAssetId)
                orderby op.CreatedAtUtc descending
                select new { op.MsisdnAssetId, op.SimInventoryId })
                .GroupBy(x => x.MsisdnAssetId!)
                .ToDictionaryAsync(g => g.Key, g => g.First().SimInventoryId!, cancellationToken);

        var operationSimIds = operationSimByAsset.Values.Distinct().ToList();
        var operationSims = operationSimIds.Count == 0
            ? new Dictionary<string, SimInventory>()
            : await _query.SimInventory.AsNoTracking()
                .Where(s => operationSimIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);

        var documentContextByMsisdnAssetId = msisdnAssetIds.Count == 0
            ? new Dictionary<string, SubscriptionDocumentContext>()
            : await (
                from op in _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
                where op.MsisdnAssetId != null
                      && msisdnAssetIds.Contains(op.MsisdnAssetId)
                      && op.Kind == TelecomOperationKind.NewActivation
                      && (op.DocumentStatus != TelecomDocumentStatus.Missing
                          || op.KycDocumentReferenceId != null
                          || op.IdentityDocumentStorageKey != null)
                orderby op.CreatedAtUtc descending
                select new
                {
                    op.MsisdnAssetId,
                    op.Id,
                    op.DocumentStatus,
                    op.IdentityDocumentStorageKey,
                    op.KycDocumentReferenceId,
                })
                .GroupBy(x => x.MsisdnAssetId!)
                .ToDictionaryAsync(
                    g => g.Key,
                    g =>
                    {
                        var row = g.First();
                        return new SubscriptionDocumentContext(
                            row.Id,
                            row.DocumentStatus,
                            row.IdentityDocumentStorageKey,
                            row.KycDocumentReferenceId);
                    },
                    cancellationToken);

        var iccidCandidates = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sub in subscriptions)
        {
            var asset = sub.MsisdnAsset;
            if (asset == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(asset.PairedIccid))
            {
                iccidCandidates.Add(asset.PairedIccid.Trim());
            }

            var derived = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(asset.Msisdn);
            if (!string.IsNullOrWhiteSpace(derived))
            {
                iccidCandidates.Add(derived);
            }
        }

        var simsByIccid = iccidCandidates.Count == 0
            ? new Dictionary<string, SimInventory>(StringComparer.Ordinal)
            : await _query.SimInventory.AsNoTracking()
                .Where(s => iccidCandidates.Contains(s.Iccid))
                .ToDictionaryAsync(s => s.Iccid, StringComparer.Ordinal, cancellationToken);

        var simulateDemoWallet = Customer360WalletBuilder.ShouldSimulateDemoWallet(customerCreatedById);

        var primaryLineAssigned = false;
        return subscriptions.Select(s =>
        {
            ProductOffering? offering = null;
            if (!string.IsNullOrEmpty(s.ProductOfferingId))
            {
                if (s.ProductOffering != null && !s.ProductOffering.IsDeleted)
                {
                    offering = s.ProductOffering;
                }
                else if (offeringsById.TryGetValue(s.ProductOfferingId, out var direct))
                {
                    offering = direct;
                }
            }

            if (offering == null && !string.IsNullOrEmpty(s.ProductId))
            {
                offeringsByProductId.TryGetValue(s.ProductId, out offering);
            }

            profileById.TryGetValue(s.SubscriberProfileId, out var profile);
            var asset = s.MsisdnAsset;

            SimInventory? operationSim = null;
            if (asset != null
                && !string.IsNullOrEmpty(s.MsisdnAssetId)
                && operationSimByAsset.TryGetValue(s.MsisdnAssetId, out var opSimId)
                && operationSims.TryGetValue(opSimId, out var opSim))
            {
                operationSim = opSim;
            }

            var profileSim = MsisdnAssetKitResolver.ResolveLinkedSim(profile?.SimInventories);
            string? iccid = null;
            string? imsi = null;
            SimInventory? resolvedSim = operationSim ?? profileSim;

            if (asset != null)
            {
                (iccid, imsi) = MsisdnAssetKitResolver.Resolve(asset, profileSim, operationSim, simsByIccid);
                if (resolvedSim == null
                    && !string.IsNullOrWhiteSpace(iccid)
                    && simsByIccid.TryGetValue(iccid, out var simByIccid))
                {
                    resolvedSim = simByIccid;
                }
            }

            var defaultSimStatus = MsisdnAssetKitResolver.DefaultSimStatusForProfile(profile?.OperationalStatus);
            var simTypeLabel = MsisdnAssetKitResolver.SimTypeCode(resolvedSim?.SimType ?? SimType.Physical);
            var simStatusLabel = resolvedSim != null
                ? MsisdnAssetKitResolver.SimStatusLabel(resolvedSim.Status)
                : defaultSimStatus.HasValue
                    ? MsisdnAssetKitResolver.SimStatusLabel(defaultSimStatus)
                    : "—";

            var packageComponents = (offering?.Components ?? [])
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.SortOrder)
                .Select(c => new Customer360PackageComponentDto(
                    c.ComponentType,
                    c.Label,
                    c.Quota,
                    c.QuotaUnit,
                    c.IsUnlimited,
                    c.SortOrder))
                .ToList();

            var documentStatus = s.DocumentStatus;
            string? documentOperationId = null;
            string? documentSource = null;
            if (!string.IsNullOrEmpty(s.MsisdnAssetId)
                && documentContextByMsisdnAssetId.TryGetValue(s.MsisdnAssetId, out var docCtx))
            {
                if (documentStatus == TelecomDocumentStatus.Missing)
                {
                    documentStatus = docCtx.DocumentStatus;
                }

                if (!string.IsNullOrWhiteSpace(docCtx.IdentityDocumentStorageKey))
                {
                    documentOperationId = docCtx.OperationId;
                    documentSource = "identity";
                }
                else if (!string.IsNullOrWhiteSpace(docCtx.KycDocumentReferenceId))
                {
                    documentOperationId = docCtx.OperationId;
                    documentSource = "kyc";
                }
            }

            var isPrimaryLine = s.IsPrimaryLine && !primaryLineAssigned;
            if (isPrimaryLine)
            {
                primaryLineAssigned = true;
            }

            return new Customer360SubscriptionDto(
                s.Id,
                s.SubscriberProfileId,
                asset?.Msisdn,
                s.MsisdnAssetId,
                isPrimaryLine,
                s.ProductId,
                s.Product?.Name,
                offering?.Id,
                offering?.Name ?? offering?.NameEn,
                offering?.NameEn ?? offering?.Name,
                s.SubscriptionTypeLookup?.NameAr ?? s.SubscriptionTypeLookup?.Code,
                s.SubscriptionTypeLookup?.NameEn ?? s.SubscriptionTypeLookup?.NameAr ?? s.SubscriptionTypeLookup?.Code,
                s.SubscriptionTypeLookup?.Code,
                simTypeLabel,
                iccid,
                profile?.OperationalStatus.ToString(),
                documentStatus switch
                {
                    TelecomDocumentStatus.Uploaded => "مرفوع",
                    TelecomDocumentStatus.Verified => "موثّق",
                    TelecomDocumentStatus.Rejected => "مرفوض",
                    _ => "ناقص",
                },
                profile?.ServiceLineType switch
                {
                    ServiceLineType.Broadband => "إنترنت",
                    ServiceLineType.FixedLine => "خط ثابت",
                    _ => "موبايل",
                },
                profile?.LanguagePreference switch
                {
                    LanguagePreference.English => "English",
                    _ => "العربية",
                },
                profile?.ActivationDateUtc,
                profile?.LoyaltyPoints ?? 0,
                profile?.LoyaltyTier,
                simulateDemoWallet ? profile?.PrepaidBalance : profile?.PrepaidBalance ?? 0m,
                profile?.PostpaidCreditLimit is < 0 ? profile.PostpaidCreditLimit : simulateDemoWallet ? profile?.PostpaidCreditLimit : null,
                profile?.ChurnRiskScore,
                simStatusLabel,
                imsi,
                s.CreatedAtUtc,
                documentOperationId,
                documentSource,
                packageComponents);
        }).ToList();
    }
}
