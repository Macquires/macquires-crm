using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.DataAccessManager.EFCore.Common;

/// <summary>Maps SQL <c>bit NULL</c> to CLR <c>bool</c> as false (legacy rows before NOT NULL migration).</summary>
public static class NullableBitPropertyExtensions
{
    private static readonly ValueConverter<bool, bool?> BoolFromNullableBit = new(
        v => v,
        v => v ?? false);

    public static PropertyBuilder<bool> HasNullableBitAsBool(this PropertyBuilder<bool> property) =>
        property.HasConversion(BoolFromNullableBit);
}
