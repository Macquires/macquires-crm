namespace Application.Common.Telecom.SellingLine;

/// <summary>HTTP gateway hints for omnichannel activation routing.</summary>
public interface IActivationChannelContext
{
    bool IsDigitalGatewayRequest { get; }
}
