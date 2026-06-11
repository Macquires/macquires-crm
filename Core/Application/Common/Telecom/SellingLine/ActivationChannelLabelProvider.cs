using Application.Common.Settings;
using Application.Common.Settings.Telecom;
using Domain.Enums;

namespace Application.Common.Telecom.SellingLine;

public sealed class ActivationChannelLabelProvider : IActivationChannelLabelProvider
{
    private readonly IGlobalSettingsProvider _settings;

    public ActivationChannelLabelProvider(IGlobalSettingsProvider settings) => _settings = settings;

    public async Task<ActivationChannelLabelsDto> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var defaults = new TelecomActivationRulesDto();
        var showroomAr = await ReadLabelAsync(GlobalSettingKeys.TelecomActivationChannelShowroomLabelAr, defaults.ChannelShowroomLabelAr, cancellationToken);
        var showroomEn = await ReadLabelAsync(GlobalSettingKeys.TelecomActivationChannelShowroomLabelEn, defaults.ChannelShowroomLabelEn, cancellationToken);
        var dealerAr = await ReadLabelAsync(GlobalSettingKeys.TelecomActivationChannelDealerLabelAr, defaults.ChannelDealerLabelAr, cancellationToken);
        var dealerEn = await ReadLabelAsync(GlobalSettingKeys.TelecomActivationChannelDealerLabelEn, defaults.ChannelDealerLabelEn, cancellationToken);
        var digitalAr = await ReadLabelAsync(GlobalSettingKeys.TelecomActivationChannelDigitalLabelAr, defaults.ChannelDigitalLabelAr, cancellationToken);
        var digitalEn = await ReadLabelAsync(GlobalSettingKeys.TelecomActivationChannelDigitalLabelEn, defaults.ChannelDigitalLabelEn, cancellationToken);

        return new ActivationChannelLabelsDto
        {
            Showroom = new ActivationChannelLabelEntryDto
            {
                Code = (int)ActivationChannel.Showroom,
                LabelAr = showroomAr,
                LabelEn = showroomEn,
            },
            Dealer = new ActivationChannelLabelEntryDto
            {
                Code = (int)ActivationChannel.Dealer,
                LabelAr = dealerAr,
                LabelEn = dealerEn,
            },
            Digital = new ActivationChannelLabelEntryDto
            {
                Code = (int)ActivationChannel.Digital,
                LabelAr = digitalAr,
                LabelEn = digitalEn,
            },
        };
    }

    public async Task<string> GetLabelArAsync(ActivationChannel channel, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        return channel switch
        {
            ActivationChannel.Showroom => all.Showroom.LabelAr,
            ActivationChannel.Dealer => all.Dealer.LabelAr,
            ActivationChannel.Digital => all.Digital.LabelAr,
            _ => channel.ToString(),
        };
    }

    private async Task<string> ReadLabelAsync(string key, string fallback, CancellationToken cancellationToken)
    {
        var raw = await _settings.GetValueAsync(key, cancellationToken);
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim();
    }
}
