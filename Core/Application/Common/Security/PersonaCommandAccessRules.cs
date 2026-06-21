using Application.Common.Services.SecurityManager;

namespace Application.Common.Security;

/// <summary>Maps MediatR request types to allowed personas when strict mode is on.</summary>
public static class PersonaCommandAccessRules
{
    private static readonly (Func<Type, bool> Match, TelecomMenuPersona[] Personas)[] Rules =
    [
        (t => t.Name.Contains("CreateUser", StringComparison.Ordinal) || t.Name.Contains("UpdateUser", StringComparison.Ordinal) || t.Name.Contains("DeleteUser", StringComparison.Ordinal) || t.Name.Contains("UpdateUserRole", StringComparison.Ordinal) || t.Name.Contains("GetUserList", StringComparison.Ordinal) || t.Name.Contains("OrgUnit", StringComparison.Ordinal),
            [TelecomMenuPersona.SysAdmin]),
        (t => t.Namespace?.Contains("DashboardWidget", StringComparison.Ordinal) == true
               && (t.Name.StartsWith("Create", StringComparison.Ordinal)
                   || t.Name.StartsWith("Update", StringComparison.Ordinal)
                   || t.Name.StartsWith("Delete", StringComparison.Ordinal)),
            [TelecomMenuPersona.SysAdmin]),
        (t => t.Name.Contains("ConfirmTelecom", StringComparison.Ordinal)
               || t.Name.Contains("EnqueueInventoryBulkImport", StringComparison.Ordinal)
               || t.Name.Contains("UploadInventoryBulkImport", StringComparison.Ordinal),
            [TelecomMenuPersona.BackOffice, TelecomMenuPersona.SysAdmin]),
        (t => t.Name.Contains("UploadTelecomOperationDocument", StringComparison.Ordinal)
               || t.Name.Contains("UploadTelecomOperationIdentityDocument", StringComparison.Ordinal),
            [TelecomMenuPersona.BackOffice, TelecomMenuPersona.SysAdmin, TelecomMenuPersona.Retail]),
        (t => t.Name.Contains("ImportSim", StringComparison.Ordinal),
            [TelecomMenuPersona.BackOffice, TelecomMenuPersona.SysAdmin]),
        (t => t.Namespace?.Contains("ProductCatalog", StringComparison.Ordinal) == true && (t.Name.StartsWith("Create", StringComparison.Ordinal) || t.Name.StartsWith("Update", StringComparison.Ordinal) || t.Name.StartsWith("Delete", StringComparison.Ordinal)),
            [TelecomMenuPersona.BackOffice, TelecomMenuPersona.SysAdmin, TelecomMenuPersona.Retail]),
        (t => t.Namespace?.Contains("TelecomBackOfficeManager", StringComparison.Ordinal) == true
               && (t.Name.Contains("CreateTechnicalTicket", StringComparison.Ordinal)
                   || t.Name.Contains("SimulateVoiceAiIncomingCall", StringComparison.Ordinal)),
            [TelecomMenuPersona.CallCenter, TelecomMenuPersona.SysAdmin]),
        (t => t.Namespace?.Contains("TelecomBackOfficeManager", StringComparison.Ordinal) == true
               && t.Name.Contains("EscalateTechnicalTicket", StringComparison.Ordinal),
            [TelecomMenuPersona.CallCenter, TelecomMenuPersona.BackOffice, TelecomMenuPersona.SysAdmin]),
        (t => t.Namespace?.Contains("TelecomBackOfficeManager", StringComparison.Ordinal) == true
               && (t.Name.Contains("ResolveTechnicalTicket", StringComparison.Ordinal)
                   || t.Name.Contains("UpdateTechnicalTicketStatus", StringComparison.Ordinal)
                   || t.Name.Contains("HlrResync", StringComparison.Ordinal)
                   || t.Name.Contains("ForceCbsSync", StringComparison.Ordinal)
                   || t.Name.Contains("QueryLiveNetworkStatus", StringComparison.Ordinal)
                   || t.Name.Contains("ToggleProductOffering", StringComparison.Ordinal)),
            [TelecomMenuPersona.BackOffice, TelecomMenuPersona.SysAdmin]),
        (t => t.Name.Contains("ExecuteCustomer360TechnicalAction", StringComparison.Ordinal),
            [TelecomMenuPersona.BackOffice, TelecomMenuPersona.SysAdmin, TelecomMenuPersona.CallCenter, TelecomMenuPersona.Retail]),
    ];

    public static IReadOnlyList<TelecomMenuPersona>? GetAllowedPersonas(Type requestType)
    {
        foreach (var (match, personas) in Rules)
        {
            if (match(requestType))
            {
                return personas;
            }
        }

        return null;
    }
}
