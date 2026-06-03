-- Demo: mark some available MSISDN as premium (Silver/Gold/Platinum) for CNR wizard testing.
SET QUOTED_IDENTIFIER ON;
GO

;WITH pick AS (
    SELECT TOP (12)
        Id,
        ROW_NUMBER() OVER (ORDER BY Msisdn) AS rn
    FROM dbo.MsisdnAsset
    WHERE IsDeleted = 0
      AND PoolStatus = 0
      AND Category = 0
)
UPDATE m
SET Category = CASE pick.rn % 3 WHEN 0 THEN 1 WHEN 1 THEN 2 ELSE 3 END
FROM dbo.MsisdnAsset m
INNER JOIN pick ON m.Id = pick.Id;

GO
