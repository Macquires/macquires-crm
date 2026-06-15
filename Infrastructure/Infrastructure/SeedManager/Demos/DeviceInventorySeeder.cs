using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// مخزون معرض سيريتل — هواتف، وينجل، وراوترات 4G/5G بأسعار وIMEI واقعية.
/// </summary>
public sealed class DeviceInventorySeeder
{
    private const string SeedSkuPrefix = "SYR-SHW-";

    private readonly DataContext _context;

    public DeviceInventorySeeder(DataContext context) => _context = context;

    public async Task EnsureDemoInventoryAsync()
    {
        // Seed runs without branch context — RLS hides branch-scoped inventory rows.
        var seedInventory = _context.DeviceInventory.IgnoreQueryFilters();

        if (await seedInventory.AnyAsync(x => !x.IsDeleted && x.Sku != null && x.Sku.StartsWith(SeedSkuPrefix)))
        {
            return;
        }

        var branches = await _context.OrgUnit
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Kind == OrgUnitKind.Branch && x.IsActive)
            .OrderBy(x => x.NameAr)
            .ToListAsync();

        if (branches.Count == 0)
        {
            return;
        }

        var existingImeis = await seedInventory
            .Where(x => !x.IsDeleted)
            .Select(x => x.Imei)
            .ToHashSetAsync();

        var catalog = SyriatelShowroomCatalog.Devices;
        var serial = 1;
        var entities = new List<DeviceInventory>();
        foreach (var branch in branches)
        {
            var branchIndex = branches.IndexOf(branch);
            for (var i = 0; i < catalog.Length; i++)
            {
                var def = catalog[i];
                var unitIndex = branchIndex * catalog.Length + i;
                var status = ResolveStatus(unitIndex);
                var imei = BuildImei(def.Tac8, serial++);

                if (existingImeis.Contains(imei))
                {
                    continue;
                }

                var entity = new DeviceInventory
                {
                    Imei = imei,
                    Model = def.ModelAr,
                    Sku = $"{SeedSkuPrefix}{def.SkuCode}",
                    ListPrice = def.ListPriceSyp,
                    BranchId = branch.Id,
                    Status = status,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-Random.Shared.Next(5, 120)),
                };

                if (status == DeviceInventoryStatus.Sold)
                {
                    entity.SoldAtUtc = DateTime.UtcNow.AddDays(-Random.Shared.Next(3, 40));
                }
                else if (status == DeviceInventoryStatus.Reserved)
                {
                    entity.ReservedByOperationId = $"DEV-DEMO-{branchIndex:D2}{i:D2}";
                }

                entities.Add(entity);
                existingImeis.Add(imei);
            }
        }

        // أجهزة إضافية بحالة حجر — فحص جودة المستودع المركزي (دمشق المزة)
        var quarantineBranch = branches.FirstOrDefault(b => b.NameAr.Contains("المزة", StringComparison.Ordinal))
            ?? branches[0];

        foreach (var def in SyriatelShowroomCatalog.QuarantinedSamples)
        {
            var imei = BuildImei(def.Tac8, serial++);
            if (existingImeis.Contains(imei))
            {
                continue;
            }

            entities.Add(new DeviceInventory
            {
                Imei = imei,
                Model = def.ModelAr,
                Sku = $"{SeedSkuPrefix}{def.SkuCode}-Q",
                ListPrice = def.ListPriceSyp,
                BranchId = quarantineBranch.Id,
                Status = DeviceInventoryStatus.Quarantined,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12),
            });
            existingImeis.Add(imei);
        }

        if (entities.Count == 0)
        {
            return;
        }

        await _context.DeviceInventory.AddRangeAsync(entities);
        await _context.SaveChangesAsync();
    }

    private static DeviceInventoryStatus ResolveStatus(int unitIndex) => unitIndex switch
        {
            _ when unitIndex % 11 == 0 => DeviceInventoryStatus.Sold,
            _ when unitIndex % 7 == 0 => DeviceInventoryStatus.Reserved,
            _ => DeviceInventoryStatus.Available,
        };

    /// <summary>IMEI من 15 رقماً مع رقم تحقق Luhn (TAC + مسلسل).</summary>
    internal static string BuildImei(string tac8, int serial)
    {
        if (tac8.Length != 8 || serial is < 0 or > 999_999)
        {
            throw new ArgumentException("TAC يجب أن يكون 8 أرقام والمسلسل حتى 6 أرقام.");
        }

        var body = $"{tac8}{serial:D6}";
        var check = ComputeImeiCheckDigit(body);
        return body + check;
    }

    private static char ComputeImeiCheckDigit(string imei14)
    {
        var sum = 0;
        for (var i = 0; i < 14; i++)
        {
            var d = imei14[i] - '0';
            if ((i + 1) % 2 == 0)
            {
                d *= 2;
                if (d > 9)
                {
                    d -= 9;
                }
            }

            sum += d;
        }

        return (char)('0' + (10 - sum % 10) % 10);
    }

    private static class SyriatelShowroomCatalog
    {
        internal sealed record DeviceDef(string ModelAr, string SkuCode, string Tac8, decimal ListPriceSyp);

        /// <summary>أجهزة معروضة في فروع سيريتل — أسعار تقريبية بالليرة السورية (معرض 2025).</summary>
        internal static readonly DeviceDef[] Devices =
        [
            // هواتف اقتصادية ومتوسطة — الأكثر مبيعاً في السوق السوري
            new("سامسونج غالاكسي A05 — 64GB", "SAM-A05-64", "35174610", 3_850_000m),
            new("سامسونج غالاكسي A15 — 128GB", "SAM-A15-128", "35174611", 5_200_000m),
            new("سامسونج غالاكسي A25 — 256GB", "SAM-A25-256", "35174612", 7_450_000m),
            new("سامسونج غالاكسي A54 — 128GB", "SAM-A54-128", "35174613", 9_800_000m),
            new("شاومي ريدمي 13C — 128GB", "XIA-13C-128", "86898803", 3_650_000m),
            new("شاومي ريدمي نوت 13 — 256GB", "XIA-N13-256", "86898804", 6_900_000m),
            new("هواوي nova Y72 — 128GB", "HW-NY72-128", "86197903", 5_750_000m),
            new("هواوي nova Y91 — 256GB", "HW-NY91-256", "86197904", 7_200_000m),
            new("أوبو A58 — 128GB", "OPPO-A58", "86146003", 4_950_000m),
            new("إنفينيكس هوت 40 — 256GB", "INF-H40-256", "86828703", 4_200_000m),
            // فئة عليا
            new("سامسونج غالاكسي S24 — 256GB", "SAM-S24-256", "35174620", 18_500_000m),
            new("آيفون 15 — 128GB", "APL-IP15-128", "35693803", 28_000_000m),
            new("آيفون 15 برو — 256GB", "APL-IP15P-256", "35693804", 38_500_000m),
            // وينجل وراوترات — خط سيريتل للإنترنت المنزلي
            new("زد تي إي MF971R — وينجل 4G سيريتل", "ZTE-WINGLE-4G", "86358603", 1_850_000m),
            new("هواوي B535-932 — راوتر 4G منزلي", "HW-B535-4G", "86197910", 2_450_000m),
            new("هواوي B628-855 — راوتر 4G+ LTE-A", "HW-B628-4G", "86197911", 2_950_000m),
            new("هواوي 5G CPE Pro 2 — راوتر 5G", "HW-5G-CPE2", "86197920", 5_600_000m),
            new("زد تي إي MC801A — راوتر 5G محمول", "ZTE-5G-MC801", "86358610", 4_800_000m),
            // تابلت وأجهزة مساندة
            new("سامسونج غالاكسي تاب A9 — Wi-Fi", "SAM-TABA9", "35174630", 4_100_000m),
            new("لينوفو M11 — تابلت تعليمي", "LEN-M11", "86746103", 3_300_000m),
        ];

        internal static readonly DeviceDef[] QuarantinedSamples =
        [
            new("سامسونج غالاكسي A15 — 128GB (مرتجع فحص)", "SAM-A15-128", "35174611", 5_200_000m),
            new("هواوي B535-932 — راوتر 4G (عيب تغليف)", "HW-B535-4G", "86197910", 2_450_000m),
        ];
    }
}
