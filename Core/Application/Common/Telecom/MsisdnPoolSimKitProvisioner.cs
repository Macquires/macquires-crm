using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom;

public sealed class MsisdnPoolSimKitProvisioner : IMsisdnPoolSimKitProvisioner
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MsisdnPoolSimKitProvisioner(
        IQueryContext query,
        ICommandRepository<SimInventory> simRepository,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _simRepository = simRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task EnsureForPoolAssetAsync(
        MsisdnAsset asset,
        bool reserveSim,
        CancellationToken cancellationToken = default)
    {
        var derivedIccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(asset.Msisdn);
        if (string.IsNullOrWhiteSpace(derivedIccid))
        {
            throw new BusinessRuleViolationException("VAL-02-03: لا يمكن اشتقاق ICCID من رقم MSISDN.");
        }

        if (!IccidValidator.TryValidate(derivedIccid, out var iccid, out var iccidError))
        {
            throw new BusinessRuleViolationException($"VAL-02-03: {iccidError}");
        }

        var imsi = MsisdnAssetKitResolver.DeriveImsiFromMsisdn(asset.Msisdn);
        var simId = await _query.SimInventory.AsNoTracking()
            .Where(s => !s.IsDeleted && s.Iccid == iccid)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken);

        SimInventory sim;
        if (!string.IsNullOrEmpty(simId))
        {
            sim = await _simRepository.GetAsync(simId, cancellationToken)
                ?? throw new BusinessRuleViolationException("VAL-02-03: الشريحة (ICCID) غير موجودة في المستودع.");
        }
        else
        {
            sim = SimInventory.Create(iccid, imsi: imsi);
            await _simRepository.CreateAsync(sim);
        }

        if (sim.Status is SimStatus.Active or SimStatus.Suspended)
        {
            throw new BusinessRuleViolationException(
                $"VAL-02-03: الشريحة {iccid} مرتبطة بخط نشط ولا يمكن استخدامها من المستودع.");
        }

        if (reserveSim && sim.Status == SimStatus.Available)
        {
            sim.TransitionTo(SimStatus.Reserved);
        }

        if (!string.Equals(asset.PairedIccid, iccid, StringComparison.Ordinal))
        {
            asset.PairedIccid = iccid;
        }

        if (!string.Equals(asset.PairedImsi, imsi, StringComparison.Ordinal))
        {
            asset.PairedImsi = imsi;
        }

        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
