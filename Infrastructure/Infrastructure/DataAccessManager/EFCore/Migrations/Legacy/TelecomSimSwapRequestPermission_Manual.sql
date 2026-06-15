-- §4 SIM Swap — showroom request permission.

DECLARE @PermKey nvarchar(128) = N'telecom.line.simswap_request';

INSERT INTO dbo.RolePermission (RoleName, PermissionKey, GrantedAtUtc, GrantedById)
SELECT v.RoleName, @PermKey, SYSUTCDATETIME(), NULL
FROM (VALUES (N'TelecomAdmin'), (N'TelecomBackOffice'), (N'TelecomShowroom')) AS v(RoleName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleName = v.RoleName AND rp.PermissionKey = @PermKey);
GO
