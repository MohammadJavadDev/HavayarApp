/*
Fix_ProductionOrder_ProjectManager_FromTotalSystem.sql
Fills Sale.ProductionOrder.ProjectManagerId (NULL only) from HTS.
HTS Pln_ProductionOrder.ProjectManagerId -> HRM_Personel.Personel_ID (not Gnr_User).
Match system.User by NameFa = PrsName + ' ' + PrsFamily (ي/ك normalised).
Encoding: UTF-8 with BOM. Apply with sqlcmd -f 65001.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRAN;

;WITH s AS (
    SELECT o.Number, o.RahkaranId,
           LTRIM(RTRIM(pr.PrsName)) + N' ' + LTRIM(RTRIM(pr.PrsFamily)) AS OldName
    FROM [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] o
    JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] pr ON pr.Personel_ID = o.ProjectManagerId
    WHERE o.ProjectManagerId IS NOT NULL
)
UPDATE p
SET p.ProjectManagerId = u.Id
FROM s
JOIN Sale.ProductionOrder p
  ON p.ProductionOrderNumber = s.Number
  OR (s.RahkaranId IS NOT NULL AND p.HamkaranId = s.RahkaranId)
CROSS APPLY (
    SELECT TOP 1 x.Id FROM system.[User] x
    WHERE REPLACE(REPLACE(x.NameFa, NCHAR(0x064A), NCHAR(0x06CC)), NCHAR(0x0643), NCHAR(0x06A9))
        = REPLACE(REPLACE(s.OldName, NCHAR(0x064A), NCHAR(0x06CC)), NCHAR(0x0643), NCHAR(0x06A9))
    ORDER BY x.Id
) u
WHERE p.ProjectManagerId IS NULL;

PRINT N'Updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

COMMIT;
