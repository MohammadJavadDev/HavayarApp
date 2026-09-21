-- Phase 9 TechnicalQuery
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @Now DATETIME2 = GETDATE();
DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
DECLARE @SeedUser NVARCHAR(150) = N'sync-after-sales-phase9';
IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN RAISERROR(N'Linked Server [TMS] یافت نشد.', 16, 1); RETURN; END;
BEGIN TRY
BEGIN TRANSACTION;
MERGE Sale.TechnicalQuery AS t
USING (
    SELECT CAST(s.Id AS BIGINT) AS HtsId, s.CartableStatusId, c.Id AS CustomerId, p.Id AS PartId, s.Serial,
           s.TechnicalProblemDescription, s.InstallationDate AS InstallationMiladiDate, s.InstallationDateInText AS InstallationShamsiDate,
           s.FirstEmergingProblemDate AS FirstEmergingProblemMiladiDate, s.FirstEmergingProblemDateInText AS FirstEmergingProblemShamsiDate,
           s.LastEmergingProblemDate AS LastEmergingProblemMiladiDate, s.LastEmergingProblemDateInText AS LastEmergingProblemShamsiDate,
           s.NumberOfEmergedProblem, CAST(CASE WHEN s.CartableStatusId IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS InApproveQueue
    FROM [TMS].[TotalSystem].[dbo].[Sale_TQ] s
    LEFT JOIN SLS.Customer c ON c.HtsId = s.CustomerId
    LEFT JOIN Inv.Part p ON p.HtsId = s.PartId
) AS s ON t.HtsId = s.HtsId
WHEN MATCHED THEN UPDATE SET t.CartableStatusId=s.CartableStatusId, t.CustomerId=s.CustomerId, t.PartId=s.PartId, t.Serial=s.Serial,
    t.TechnicalProblemDescription=s.TechnicalProblemDescription, t.InstallationMiladiDate=s.InstallationMiladiDate, t.InstallationShamsiDate=s.InstallationShamsiDate,
    t.FirstEmergingProblemMiladiDate=s.FirstEmergingProblemMiladiDate, t.FirstEmergingProblemShamsiDate=s.FirstEmergingProblemShamsiDate,
    t.LastEmergingProblemMiladiDate=s.LastEmergingProblemMiladiDate, t.LastEmergingProblemShamsiDate=s.LastEmergingProblemShamsiDate,
    t.NumberOfEmergedProblem=s.NumberOfEmergedProblem, t.InApproveQueue=s.InApproveQueue,
    t.ModifiedById=1, t.ModifiedByName=@SeedUser, t.ModifiedDateMiladiDateTime=@Now, t.ModifiedDateShamsiDateTime=@NowShamsi
WHEN NOT MATCHED THEN INSERT (HtsId, CartableStatusId, CustomerId, PartId, Serial, TechnicalProblemDescription, InstallationMiladiDate, InstallationShamsiDate,
    FirstEmergingProblemMiladiDate, FirstEmergingProblemShamsiDate, LastEmergingProblemMiladiDate, LastEmergingProblemShamsiDate, NumberOfEmergedProblem, InApproveQueue,
    CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
VALUES (s.HtsId, s.CartableStatusId, s.CustomerId, s.PartId, s.Serial, s.TechnicalProblemDescription, s.InstallationMiladiDate, s.InstallationShamsiDate,
    s.FirstEmergingProblemMiladiDate, s.FirstEmergingProblemShamsiDate, s.LastEmergingProblemMiladiDate, s.LastEmergingProblemShamsiDate, s.NumberOfEmergedProblem, s.InApproveQueue,
    1, @SeedUser, @Now, @NowShamsi, 1, @SeedUser, @Now, @NowShamsi, 1);
COMMIT; PRINT N'Phase 9 sync committed';
END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH;
