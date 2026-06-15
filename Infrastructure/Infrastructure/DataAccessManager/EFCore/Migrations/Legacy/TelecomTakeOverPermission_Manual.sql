-- §7 Transfer of Ownership — BackOffice approval permission (RolePermission matrix).

DECLARE @PermKey nvarchar(128) = N'telecom.line.transfer_ownership';

INSERT INTO dbo.RolePermission (RoleName, PermissionKey, GrantedAtUtc, GrantedById)
SELECT v.RoleName, @PermKey, SYSUTCDATETIME(), NULL
FROM (VALUES (N'TelecomAdmin'), (N'TelecomBackOffice')) AS v(RoleName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleName = v.RoleName AND rp.PermissionKey = @PermKey);
GO
