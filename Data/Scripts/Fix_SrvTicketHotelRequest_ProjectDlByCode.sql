/*
Fix_SrvTicketHotelRequest_ProjectDlByCode.sql
Repairs Srv.TicketHotelRequest.ProjectDlId after the HamkaranId mismatch.

Acc_DL.Hamkaran_Acc_DL_FK stores the Rahkaran DL Code, not DLID.
Match FIN.DL.Code numerically (TypeId = 5, AccDLType_FK = 16) so leading zeros match.
Local rows (HtsId = 0) are left unchanged.
Idempotent. UTF-8 with BOM.

sqlcmd -S PortalSRV\PORTAL -d HavayarApp -C -b -I -f 65001 -i "Data\Scripts\Fix_SrvTicketHotelRequest_ProjectDlByCode.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1);
    RETURN;
END;

BEGIN TRANSACTION;

;WITH Map AS (
    SELECT OldDlId, NewDlId
    FROM (
        SELECT
            dl.Acc_DL_ID AS OldDlId,
            fdl.Id AS NewDlId,
            ROW_NUMBER() OVER (PARTITION BY dl.Acc_DL_ID ORDER BY fdl.Id) AS rn
        FROM [TMS].[TotalSystem].[dbo].[Acc_DL] dl
        INNER JOIN FIN.DL fdl
            ON fdl.TypeId = 5
           AND dl.AccDLType_FK = 16
           AND ISNULL(dl.Hamkaran_Acc_DL_FK, 0) <> 0
           AND TRY_CAST(fdl.Code AS bigint) = CAST(dl.Hamkaran_Acc_DL_FK AS bigint)
    ) x
    WHERE rn = 1
)
UPDATE t
SET t.ProjectDlId = CASE WHEN m.ProjectDlId IS NULL THEN NULL ELSE map.NewDlId END
FROM Srv.TicketHotelRequest t
INNER JOIN [TMS].[TotalSystem].[dbo].[Srv_Mission] m ON m.Id = t.HtsId
LEFT JOIN Map map ON map.OldDlId = m.ProjectDlId
WHERE t.HtsId <> 0
  AND ISNULL(t.ProjectDlId, 0) <> ISNULL(CASE WHEN m.ProjectDlId IS NULL THEN NULL ELSE map.NewDlId END, 0);

PRINT N'TicketHotelRequest.ProjectDlId updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

UPDATE system.SavedQuery
SET QueryJson = REPLACE(QueryJson, N'CAST([t5].[HamkaranId] AS nvarchar(50))', N'[t5].[Code]'),
    ColumnsJson = REPLACE(ColumnsJson, N'CAST([t5].[HamkaranId] AS nvarchar(50))', N'[t5].[Code]')
WHERE Name LIKE N'Srv_TicketHotelRequest%'
  AND (CHARINDEX(N'CAST([t5].[HamkaranId] AS nvarchar(50))', QueryJson) > 0
    OR CHARINDEX(N'CAST([t5].[HamkaranId] AS nvarchar(50))', ColumnsJson) > 0);

PRINT N'SavedQuery project column updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

COMMIT TRANSACTION;
