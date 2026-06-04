using Infrastructure.DataAccessManager.EFCore.Converters;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DataAccessManager.EFCore.Common;

/// <summary>
/// Ensures every mapped <c>bool</c> uses <see cref="NullableBitBooleanValueConverter"/>
/// (legacy SQL <c>bit NULL</c> otherwise throws <c>SqlNullValueException</c> on read).
/// </summary>
public static class ModelBuilderNullableBitExtensions
{
    private static readonly NullableBitBooleanValueConverter Converter = new();

    public static void ApplyNullableBitAsBoolConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(bool))
                    property.SetValueConverter(Converter);
            }
        }
    }
}
