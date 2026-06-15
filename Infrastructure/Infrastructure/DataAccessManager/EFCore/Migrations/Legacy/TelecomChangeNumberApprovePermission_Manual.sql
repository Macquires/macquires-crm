-- §5 Change Number — BackOffice approve (premium / financial gate).

DECLARE @PermKey nvarchar(128) = N'telecom.line.change_number_approve';

INSERT INTO dbo.RolePermission (RoleName, PermissionKey, GrantedAtUtc, GrantedById)
SELECT v.RoleName, @PermKey, SYSUTCDATETIME(), NULL
FROM (VALUES (N'TelecomAdmin'), (N'TelecomBackOffice')) AS v(RoleName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleName = v.RoleName AND rp.PermissionKey = @PermKey);
GO

DECLARE @PermExec nvarchar(128) = N'telecom.line.change_number';

INSERT INTO dbo.RolePermission (RoleName, PermissionKey, GrantedAtUtc, GrantedById)
SELECT v.RoleName, @PermExec, SYSUTCDATETIME(), NULL
FROM (VALUES (N'TelecomAdmin'), (N'TelecomBackOffice'), (N'TelecomShowroom')) AS v(RoleName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleName = v.RoleName AND rp.PermissionKey = @PermExec);
GO

