using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Systems;

public class GeoCitySeeder
{
    private readonly DataContext _context;

    public GeoCitySeeder(DataContext context) => _context = context;

    public async Task GenerateDataAsync()
    {
        if (await _context.GeoCity.AnyAsync())
        {
            return;
        }

        var cities = new (string Name, string Governorate, int SortOrder)[]
        {
            ("دمشق", "دمشق", 1),
            ("حلب", "حلب", 2),
            ("حمص", "حمص", 3),
            ("حماة", "حماة", 4),
            ("اللاذقية", "اللاذقية", 5),
            ("طرطوس", "طرطوس", 6),
            ("إدلب", "إدلب", 7),
            ("درعا", "درعا", 8),
            ("السويداء", "السويداء", 9),
            ("الحسكة", "الحسكة", 10),
            ("الرقة", "الرقة", 11),
            ("دير الزور", "دير الزور", 12),
            ("القنيطرة", "القنيطرة", 13),
            ("دوما", "ريف دمشق", 14),
            ("التل", "ريف دمشق", 15),
            ("جرمانا", "ريف دمشق", 16),
            ("جبلة", "اللاذقية", 17),
            ("بانياس", "طرطوس", 18),
            ("منبج", "حلب", 19),
            ("القامشلي", "الحسكة", 20),
        };

        foreach (var (name, governorate, sortOrder) in cities)
        {
            await _context.GeoCity.AddAsync(new GeoCity
            {
                Name = name,
                Governorate = governorate,
                IsActive = true,
                SortOrder = sortOrder,
            });
        }

        await _context.SaveChangesAsync();
    }
}
