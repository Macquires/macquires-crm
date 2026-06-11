namespace Application.Common.Telecom.SellingLine;

public sealed class ActivationChannelLabelsDto
{
    public ActivationChannelLabelEntryDto Showroom { get; init; } = new();
    public ActivationChannelLabelEntryDto Dealer { get; init; } = new();
    public ActivationChannelLabelEntryDto Digital { get; init; } = new();
}

public sealed class ActivationChannelLabelEntryDto
{
    public int Code { get; init; }
    public string LabelAr { get; init; } = "";
    public string LabelEn { get; init; } = "";
}
