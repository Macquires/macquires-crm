using Application.Common.Integrations;
using Application.Common.Telecom;
using Application.Common.Telecom.Termination;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class TerminationWorkflowContractTests
{
    [Fact]
    public void TelecomNumberSequence_Termination_uses_TRM_prefix()
    {
        var (entity, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.Termination);
        Assert.Equal("TelecomOp_Termination", entity);
        Assert.Equal("TRM-", prefix);
    }

    [Fact]
    public void ProvisionBuilder_maps_termination_line_to_billing_and_hlr()
    {
        var line = new TelecomLineProvisionContext("0991111222", "8900111222333444555", "417011122233344", "MOB", "PREPAID", null);
        var op = new TelecomOperationRequest
        {
            Id = "trm-op-1",
            Number = "TRM-00001",
            Kind = TelecomOperationKind.Termination,
            CorrelationId = "corr-trm",
            SubscriberProfileId = "prof-1",
            MsisdnAssetId = "msisdn-1",
            TerminationType = TerminationWellKnown.Voluntary,
            TerminationReason = "CustomerRequest",
        };

        var billing = TelecomProvisionRequestBuilder.ToBillingRequest(op, line);
        var network = TelecomProvisionRequestBuilder.ToNetworkRequest(op, line);

        Assert.Equal(TelecomOperationKind.Termination, billing.Kind);
        Assert.Equal("0991111222", billing.Msisdn);
        Assert.Equal(TelecomOperationKind.Termination, network.Kind);
        Assert.Equal("0991111222", network.Msisdn);
    }

    [Fact]
    public void BssOperationNames_include_termination_final_bill()
    {
        Assert.Equal("CbsGenerateFinalBill", TelecomBssOperations.CbsGenerateFinalBill);
        Assert.Equal("HlrDeactivateSubscriber", TelecomBssOperations.HlrDeactivateSubscriber);
    }

    [Fact]
    public void KindLabelAr_includes_TRM()
    {
        Assert.Equal("إنهاء خط TRM", TelecomOperationLabels.KindLabelAr(TelecomOperationKind.Termination));
    }
}
