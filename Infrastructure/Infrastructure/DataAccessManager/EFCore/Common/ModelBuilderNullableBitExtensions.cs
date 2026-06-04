using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.DataAccessManager.EFCore.Common;

/// <summary>
/// Applies <see cref="NullableBitPropertyExtensions"/> to every mapped non-nullable <c>bool</c>
/// (legacy SQL <c>bit NULL</c> rows otherwise throw <c>SqlNullValueException</c> on read).
/// </summary>
public static class ModelBuilderNullableBitExtensions
{
    private static readonly ValueConverter<bool, bool?> BoolFromNullableBit = new(
        v => v,
        v => v ?? false);

    public static void ApplyNullableBitAsBoolConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(bool) && property.GetValueConverter() is null)
                    property.SetValueConverter(BoolFromNullableBit);
            }
        }
    }
}
