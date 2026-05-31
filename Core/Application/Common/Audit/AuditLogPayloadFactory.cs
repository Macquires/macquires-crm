namespace Application.Common.Audit;

/// <summary>Builds human-readable audit payloads for storage and end-user detail views.</summary>
public static class AuditLogPayloadFactory
{
    public static object SubscriberSearch(
        string searchChannel,
        string criteria,
        IReadOnlyList<SubscriberSearchMatchAuditDto> matches)
    {
        var count = matches?.Count ?? 0;
        var lines = new List<string>
        {
            $"الشاشة: {ChannelLabelAr(searchChannel)}",
            $"معيار البحث: {criteria}",
            $"عدد النتائج: {count}",
        };

        if (count == 0)
        {
            lines.Add("لم يُعثر على مشترك مطابق.");
        }
        else
        {
            lines.Add("المشتركون المطابقون:");
            var i = 1;
            foreach (var m in (matches ?? Array.Empty<SubscriberSearchMatchAuditDto>()).Take(10))
            {
                lines.Add($"{i}. {FormatMatchLine(m)}");
                i++;
            }

            if (count > 10)
            {
                lines.Add($"... و{count - 10} نتيجة إضافية");
            }
        }

        return new
        {
            narrativeAr = string.Join("\n", lines),
            channel = searchChannel,
            channelLabelAr = ChannelLabelAr(searchChannel),
            criteria,
            matchCount = count,
            matches = (matches ?? Array.Empty<SubscriberSearchMatchAuditDto>())
                .Take(10)
                .Select(m => new
                {
                    m.CustomerId,
                    nameAr = m.Name,
                    msisdn = m.PhoneOrMsisdn,
                    displayLine = FormatMatchLine(m),
                }),
        };
    }

    public static object ProfileView(string customerId, string? displayName, string? primaryPhoneOrMsisdn)
    {
        var lines = new List<string>
        {
            "نوع الحدث: اطلاع على ملف المشترك",
            $"اسم المشترك: {displayName ?? "—"}",
        };

        if (!string.IsNullOrWhiteSpace(primaryPhoneOrMsisdn))
        {
            lines.Add($"الخط / الجوال: {primaryPhoneOrMsisdn.Trim()}");
        }

        lines.Add($"معرّف المشترك: {customerId}");

        return new
        {
            narrativeAr = string.Join("\n", lines),
            customerId,
            displayName,
            primaryPhoneOrMsisdn,
        };
    }

    private static string FormatMatchLine(SubscriberSearchMatchAuditDto m)
    {
        var name = string.IsNullOrWhiteSpace(m.Name) ? null : m.Name.Trim();
        var phone = string.IsNullOrWhiteSpace(m.PhoneOrMsisdn) ? null : m.PhoneOrMsisdn.Trim();
        if (name != null && phone != null)
        {
            return $"{name} — {phone}";
        }

        if (name != null)
        {
            return name;
        }

        if (phone != null)
        {
            return phone;
        }

        return string.IsNullOrWhiteSpace(m.CustomerId) ? "—" : m.CustomerId;
    }

    /// <summary>HLR / VAS / network commands — human labels instead of raw GUIDs in the UI.</summary>
    public static object NetworkCommand(
        string msisdn,
        string? profileId,
        string? profileDisplay,
        string? technicalTicketId,
        string? ticketNumber,
        string? commandSummary = null)
    {
        var lines = new List<string> { $"الخط: {msisdn}" };

        if (!string.IsNullOrWhiteSpace(profileDisplay))
        {
            lines.Add($"ملف المشترك: {profileDisplay.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(ticketNumber))
        {
            lines.Add($"التذكرة: {ticketNumber.Trim()}");
        }
        else if (!string.IsNullOrWhiteSpace(commandSummary))
        {
            lines.Add(commandSummary.Trim());
        }

        return new
        {
            narrativeAr = string.Join("\n", lines),
            msisdn,
            profileId,
            profileDisplay,
            technicalTicketId,
            ticketNumber,
        };
    }

    private static string ChannelLabelAr(string channel) => channel switch
    {
        "CustomerRegistry" => "سجل المشتركين",
        "TelecomHub" => "مركز العمليات (Telecom Hub)",
        "BillingIntegration" => "التكامل مع نظام الفوترة",
        "OmniSearch" => "البحث السريع (Ctrl+K)",
        "UnifiedSearch" => "البحث الموحّد",
        _ => channel,
    };
}
