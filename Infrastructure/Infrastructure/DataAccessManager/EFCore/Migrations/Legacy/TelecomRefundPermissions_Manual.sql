-- §15 Refund (RFD-) — showroom request + BackOffice approve + execute.

DECLARE @PermRequest nvarchar(128) = N'telecom.line.refund_request';

INSERT INTO dbo.RolePermission (RoleName, PermissionKey, GrantedAtUtc, GrantedById)
SELECT v.RoleName, @PermRequest, SYSUTCDATETIME(), NULL
FROM (VALUES (N'TelecomAdmin'), (N'TelecomBackOffice'), (N'TelecomShowroom')) AS v(RoleName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleName = v.RoleName AND rp.PermissionKey = @PermRequest);
GO

DECLARE @PermApprove nvarchar(128) = N'telecom.line.refund_approve';

INSERT INTO dbo.RolePermission (RoleName, PermissionKey, GrantedAtUtc, GrantedById)
SELECT v.RoleName, @PermApprove, SYSUTCDATETIME(), NULL
FROM (VALUES (N'TelecomAdmin'), (N'TelecomBackOffice')) AS v(RoleName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleName = v.RoleName AND rp.PermissionKey = @PermApprove);
GO

DECLARE @PermExec nvarchar(128) = N'telecom.line.refund';

INSERT INTO dbo.RolePermission (RoleName, PermissionKey, GrantedAtUtc, GrantedById)
SELECT v.RoleName, @PermExec, SYSUTCDATETIME(), NULL
FROM (VALUES (N'TelecomAdmin'), (N'TelecomBackOffice'), (N'TelecomShowroom')) AS v(RoleName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.RolePermission rp
    WHERE rp.RoleName = v.RoleName AND rp.PermissionKey = @PermExec);
GO
