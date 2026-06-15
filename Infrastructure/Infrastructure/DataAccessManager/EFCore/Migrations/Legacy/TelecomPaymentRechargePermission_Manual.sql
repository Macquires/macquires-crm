-- §12 Payment Services — recharge permission for RBAC matrix.

DECLARE @PermRecharge nvarchar(128) = N'telecom.line.recharge';

INSERT INTO dbo.RolePermission (RoleName, PermissionKey, GrantedAtUtc, GrantedById)
SELECT v.RoleName, @PermRecharge, SYSUTCDATETIME(), NULL
FROM (VALUES (N'TelecomAdmin'), (N'TelecomBackOffice'), (N'TelecomShowroom'), (N'TelecomManagement')) AS v(RoleName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleName = v.RoleName AND rp.PermissionKey = @PermRecharge);
GO
