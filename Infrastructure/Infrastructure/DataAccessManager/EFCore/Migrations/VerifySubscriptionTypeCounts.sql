-- Run on your CRM database to see how many subscriptions exist per legacy enum value
-- (after migration, use JOIN to TelecomSubscriptionTypes instead of int column).

-- Resolve actual subscription table name:
-- SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
-- WHERE TABLE_NAME IN ('TelecomSubscriptions', 'TelecomSubscription');

/*
-- Example once column SubscriptionType (int) still exists:
SELECT SubscriptionType, COUNT(*) AS Cnt
FROM dbo.TelecomSubscriptions
GROUP BY SubscriptionType
ORDER BY SubscriptionType;

-- After migration (FK only):
SELECT t.Code, COUNT(*) AS Cnt
FROM dbo.TelecomSubscriptions s
INNER JOIN dbo.TelecomSubscriptionTypes t ON t.Id = s.SubscriptionTypeId
GROUP BY t.Code
ORDER BY t.Code;
*/
