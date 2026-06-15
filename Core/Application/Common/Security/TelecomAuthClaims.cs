namespace Application.Common.Security;

public static class TelecomAuthClaims
{
    public const string PrimaryMenuPersona = "telecom_primary_persona";

    /// <summary>Signed JWT claim for operator home branch (OrgUnit id when Kind = Branch).</summary>
    public const string BranchId = "telecom_branch_id";
}

public static class TelecomPreviewHeaders
{
    public const string PreviewPersona = "X-Syr-Preview-Persona";
}
