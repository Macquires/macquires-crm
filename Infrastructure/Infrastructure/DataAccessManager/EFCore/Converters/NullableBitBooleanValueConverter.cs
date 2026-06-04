using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.DataAccessManager.EFCore.Converters;

/// <summary>Reads SQL <c>bit NULL</c> as <c>false</c> for CLR <c>bool</c>.</summary>
public sealed class NullableBitBooleanValueConverter : ValueConverter<bool, bool?>
{
    public NullableBitBooleanValueConverter()
        : base(v => (bool?)v, v => v ?? false)
    {
    }
}
