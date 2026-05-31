using Application.Common.Integrations;
using Application.Common.Telecom;
using Domain.Enums;

namespace Application.Tests.Telecom;

public class TelecomBssOperationsTests
{
    [Fact]
    public void BillingRequest_ReversePhase_UsesReverseEnum()
    {
        var line = new TelecomLineProvisionContext("0991234567", "8901", "41701", "SVC1", "PREPAID", 10m);
        var op = new Domain.Entities.TelecomOperationRequest
        {
            Id = "op-1",
            Number = "ACT-001",
            Kind = TelecomOperationKind.NewActivation,
            CorrelationId = "corr-1",
            SubscriberProfileId = "prof-1",
        };

        var reverse = TelecomProvisionRequestBuilder.ToBillingRequest(
            op,
            line,
            TelecomBillingProvisionPhase.Reverse);

        Assert.Equal(TelecomBillingProvisionPhase.Reverse, reverse.Phase);
        Assert.Equal(10m, reverse.InitialDeposit);
    }

    [Fact]
    public void NetworkRequest_IncludesImsiForHlr()
    {
        var line = new TelecomLineProvisionContext("0991234567", "8901", "417019999999999", null, "PREPAID", null);
        var op = new Domain.Entities.TelecomOperationRequest
        {
            Id = "op-2",
            Number = "ACT-002",
            Kind = TelecomOperationKind.NewActivation,
            CorrelationId = "corr-2",
            SubscriberProfileId = "prof-1",
        };

        var net = TelecomProvisionRequestBuilder.ToNetworkRequest(op, line);
        Assert.Equal("417019999999999", net.Imsi);
        Assert.Equal(TelecomOperationKind.NewActivation, net.Kind);
    }

    [Fact]
    public void BssOperationNames_AreStable()
    {
        Assert.Equal("CbsCreateAccountProfile", TelecomBssOperations.CbsCreateAccountProfile);
        Assert.Equal("HlrCreateSubscriber", TelecomBssOperations.HlrCreateSubscriber);
    }
}
