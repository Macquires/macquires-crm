using Domain.Common;

namespace Domain.Entities;

/// <summary>Syrian city reference data (country is always Syria — see <see cref="SyriaGeoDefaults"/>).</summary>
public class GeoCity : BaseEntity
{
    public string? Name { get; set; }
    public string? Governorate { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
