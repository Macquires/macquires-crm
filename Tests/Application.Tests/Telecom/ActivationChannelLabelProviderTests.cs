using Application.Common.Settings;
using Application.Common.Settings.Telecom;
using Application.Common.Telecom.SellingLine;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class ActivationChannelLabelProviderTests
{
    [Fact]
    public async Task GetAllAsync_uses_global_settings_with_POS_defaults()
    {
        var settings = new FakeGlobalSettingsProvider(new Dictionary<string, string>
        {
            [GlobalSettingKeys.TelecomActivationChannelShowroomLabelEn] = "Retail POS",
        });

        var provider = new ActivationChannelLabelProvider(settings);
        var labels = await provider.GetAllAsync();

        Assert.Equal("Retail POS", labels.Showroom.LabelEn);
        Assert.Equal("نقطة البيع", labels.Showroom.LabelAr);
        Assert.Equal((int)ActivationChannel.Showroom, labels.Showroom.Code);
    }

    private sealed class FakeGlobalSettingsProvider : IGlobalSettingsProvider
    {
        private readonly IReadOnlyDictionary<string, string> _map;

        public FakeGlobalSettingsProvider(IReadOnlyDictionary<string, string> map) => _map = map;

        public Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_map.TryGetValue(key, out var value) ? value : null);

        public Task<bool> GetBoolAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(defaultValue);

        public Task<IReadOnlyList<string>> GetCsvListAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<int> GetIntAsync(string key, int defaultValue, int min = int.MinValue, int max = int.MaxValue, CancellationToken cancellationToken = default) =>
            Task.FromResult(defaultValue);

        public Task<decimal> GetDecimalAsync(string key, decimal defaultValue, decimal min = decimal.MinValue, decimal max = decimal.MaxValue, CancellationToken cancellationToken = default) =>
            Task.FromResult(defaultValue);

        public Task<bool> GetCatalogBoolAsync(SettingDefinition definition, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<int> GetCatalogIntAsync(SettingDefinition definition, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<decimal> GetCatalogDecimalAsync(SettingDefinition definition, CancellationToken cancellationToken = default) =>
            Task.FromResult(0m);

        public Task<string> GetCatalogStringAsync(SettingDefinition definition, CancellationToken cancellationToken = default) =>
            Task.FromResult(definition.DefaultValue);

        public Task<IReadOnlyList<string>> GetCatalogCsvListAsync(SettingDefinition definition, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);
    }
}
