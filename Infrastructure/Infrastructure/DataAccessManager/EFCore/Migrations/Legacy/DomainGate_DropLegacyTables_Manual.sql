-- Domain Gate Purge B: drop legacy ERP tables (DEV ONLY — backup first)
-- Execute only after Application no longer references these entities.

/*
DROP TABLE IF EXISTS [InventoryTransaction];
DROP TABLE IF EXISTS [SalesOrderItem];
DROP TABLE IF EXISTS [SalesOrder];
-- ... extend per environment audit
*/

PRINT 'DomainGate_DropLegacyTables_Manual.sql — customize DROP list per DBA runbook before execution.';
