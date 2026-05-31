using Domain.Enums;

namespace Application.Common.Telecom;

/// <summary>Lightweight keyword intent parser for voice-AI ticket simulation (demo / integration stub).</summary>
public static class VoiceAiTicketIntentParser
{
    public sealed record ParsedIntent(
        TechnicalTicketIssueType IssueType,
        TechnicalTicketPriority Priority,
        string SummaryAr);

    public static ParsedIntent Parse(string rawTranscript)
    {
        var text = TelecomPhoneNormalizer.NormalizeIndicDigitsToAscii(rawTranscript ?? string.Empty)
            .Trim()
            .ToLowerInvariant();

        if (ContainsAny(text, "شريحة", "sim", "pin", "حظر", "محظور", "block"))
        {
            return new ParsedIntent(
                TechnicalTicketIssueType.SimBlock,
                TechnicalTicketPriority.High,
                BuildSummary(rawTranscript));
        }

        if (ContainsAny(text, "شحنت", "شحن", "رصيد", "كاش", "فاتورة", "فوترة", "billing", "دفع"))
        {
            var critical = ContainsAny(text, "واقف", "موقوف", "ما نزل", "ما وصل", "صفر");
            return new ParsedIntent(
                TechnicalTicketIssueType.Billing,
                critical ? TechnicalTicketPriority.Critical : TechnicalTicketPriority.High,
                BuildSummary(rawTranscript));
        }

        if (ContainsAny(text, "نت", "انترنت", "إنترنت", "data", "شبكة", "تغطية", "4g", "5g", "واقف", "بطي"))
        {
            return new ParsedIntent(
                TechnicalTicketIssueType.Network,
                ContainsAny(text, "واقف", "انقطاع", "ما في", "مقطوع")
                    ? TechnicalTicketPriority.Critical
                    : TechnicalTicketPriority.High,
                BuildSummary(rawTranscript));
        }

        if (ContainsAny(text, "تفعيل", "باقة", "provision", "معلق", "تعليق"))
        {
            return new ParsedIntent(
                TechnicalTicketIssueType.Provisioning,
                TechnicalTicketPriority.Medium,
                BuildSummary(rawTranscript));
        }

        return new ParsedIntent(
            TechnicalTicketIssueType.Network,
            TechnicalTicketPriority.Medium,
            BuildSummary(rawTranscript));
    }

    private static string BuildSummary(string? transcript)
    {
        var body = string.IsNullOrWhiteSpace(transcript) ? "—" : transcript.Trim();
        return $"[تذكرة آلية بواسطة ذكاء الكول سنتر] الشكوى: {body}";
    }

    private static bool ContainsAny(string haystack, params string[] needles)
    {
        foreach (var n in needles)
        {
            if (haystack.Contains(n, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
