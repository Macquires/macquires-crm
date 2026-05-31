namespace Infrastructure.SeedManager.Demos;

/// <summary>Realistic Syrian Arabic demo names and addresses for Customer 360 presentations.</summary>
internal static class DemoSyrianSubscriberCatalog
{
    public static readonly string[] FullNames =
    [
        "أحمد الخطيب", "سارة النابلسي", "مروان الحمصي", "إيمان الحلبي", "داوود الدمشقي",
        "جمانة اللاذقاني", "كريم الحموي", "رشا الطرطوسي", "جاسم السويداني", "ليلى الدرعي",
        "فارس الفراتي", "نادين الحسكي", "عمر حمود", "سلمى قاسم", "فادي ناصر",
        "لينا يوسف", "بسام جولاني", "هند العطار", "ياسر بركات", "رنا شمس",
        "طارق زيدان", "ميساء حجازي", "وليد صفوان", "غادة عيسى", "سامر قدورة",
        "نور الهدى الأسعد", "زياد منصور", "ريم أبو خليل", "حسام الديري", "لمى الشهابي",
        "بلال نجار", "دينا حافظ", "معتز العلي", "سوسن برّا", "أنس المقداد",
    ];

    public static readonly string[] Cities = ["دمشق", "حلب", "حمص", "اللاذقية", "طرطوس", "إدلب", "درعا", "السويداء"];

    public static readonly string[] Streets =
    [
        "شارع الحجاز", "مشروع دمر — سكن جديد", "أبو رمانة — بناء النخيل", "المزة — فيلات غربية",
        "الشعلان — قرب السينما", "الجميلية", "العزيزية", "باب توما", "القصور", "المالكي",
    ];

    public static readonly string[] Occupations =
    [
        "مهندس اتصالات", "طبيب", "محاسب", "معلّم", "صاحب محل", "موظف حكومي",
        "ممرضة", "مطوّر برمجيات", "سائق أجرة", "صيدلي", "مصمم جرافيك", "مدير مبيعات",
    ];

    public static readonly string[] ContactNotes =
    [
        "جهة اتصال رئيسية للفوترة والعقود.",
        "مسؤول عن تجديد الباقات والدعم الفني.",
        "يتم التواصل معه في حالات الطوارئ التشغيلية.",
        "مفوّض بالتوقيع على طلبات الخدمة.",
    ];

    public static string GetName(int index) => FullNames[Math.Abs(index) % FullNames.Length];

    public static bool IsPlaceholderDisplayName(string? displayName) =>
        string.IsNullOrWhiteSpace(displayName)
        || displayName.StartsWith("مشترك ديمو", StringComparison.Ordinal);
}
