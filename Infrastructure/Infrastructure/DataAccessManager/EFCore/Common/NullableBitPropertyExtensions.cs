using Infrastructure.DataAccessManager.EFCore.Converters;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccessManager.EFCore.Common;

/// <summary>Maps SQL <c>bit NULL</c> to CLR <c>bool</c> as false (legacy rows before NOT NULL migration).</summary>
public static class NullableBitPropertyExtensions
{
    public static PropertyBuilder<bool> HasNullableBitAsBool(this PropertyBuilder<bool> property) =>
        property.HasConversion(new NullableBitBooleanValueConverter());
}
