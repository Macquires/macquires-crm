-- Optional manual companion to runtime patch: ApplyTelecomSubscriptionTypeSchemaPatches in Infrastructure/DataAccessManager/EFCore/DI.cs
-- The host startup patch is authoritative for local/dev; keep this file for DBA review / production runbooks.

-- See DI.ApplyTelecomSubscriptionTypeSchemaPatches for full DDL + data migration (creates TelecomSubscriptionTypes,
-- seeds PREPAID/POSTPAID/HYBRID, adds SubscriptionTypeId, backfills from SubscriptionType int, adds FK, drops int column).
