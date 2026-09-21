/*
  Fix_SaleMissionSalary_Personel.sql
  HTS حق ماموریت را روی HRM_Personel می‌بندد، نه Gnr_User.
  ستون PersonelId + پر کردن از کد راهکاران + به‌روزرسانی نمایه.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'fix-missionsalary-personel';

    IF COL_LENGTH('Sale.MissionSalary', 'PersonelId') IS NULL
    BEGIN
        RAISERROR(N'Sale.MissionSalary.PersonelId missing — run ALTER first.', 16, 1);
        RETURN;
    END

    UPDATE ms
    SET ms.PersonelId = hp.Id,
        ms.ModifiedById = 1,
        ms.ModifiedByName = @SeedUser,
        ms.ModifiedDateMiladiDateTime = @Now,
        ms.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.MissionSalary ms
    INNER JOIN [TMS].[TotalSystem].[dbo].[HRM_Personel] p ON p.Personel_ID = ms.HtsPersonelId
    INNER JOIN Hcm.Personel hp ON TRY_CONVERT(int, hp.Code) = p.Hamkaran_Personel_FK
    WHERE ms.PersonelId IS NULL OR ms.PersonelId = 0;
    PRINT N'  PersonelId from HRM code: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE ms
    SET ms.PersonelId = hp.Id,
        ms.ModifiedById = 1,
        ms.ModifiedByName = @SeedUser,
        ms.ModifiedDateMiladiDateTime = @Now,
        ms.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.MissionSalary ms
    INNER JOIN system.[User] u ON u.Id = ms.ExpertId
    CROSS APPLY (
        SELECT TOP (1) p.Id
        FROM Hcm.Personel p
        WHERE u.PartyId IS NOT NULL AND p.PartyId = u.PartyId
        ORDER BY p.Id
    ) hp
    WHERE (ms.PersonelId IS NULL OR ms.PersonelId = 0);
    PRINT N'  PersonelId from Expert.Party: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    UPDATE ms
    SET ms.ExpertId = u.Id,
        ms.ModifiedById = 1,
        ms.ModifiedByName = @SeedUser,
        ms.ModifiedDateMiladiDateTime = @Now,
        ms.ModifiedDateShamsiDateTime = @NowShamsi
    FROM Sale.MissionSalary ms
    INNER JOIN Hcm.Personel hp ON hp.Id = ms.PersonelId
    CROSS APPLY (
        SELECT TOP (1) usr.Id
        FROM system.[User] usr
        WHERE hp.PartyId IS NOT NULL AND usr.PartyId = hp.PartyId
        ORDER BY usr.Id
    ) u
    WHERE (ms.ExpertId IS NULL OR ms.ExpertId = 0);
    PRINT N'  ExpertId from Personel.Party: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    DECLARE @QueryJsonSkeleton NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';
    DECLARE @PSelect NVARCHAR(MAX) = N'SELECT
    [t1].[Id] AS [t1_Id],
    COALESCE(
        NULLIF(LTRIM(RTRIM(CONCAT(ISNULL([hp].[Name], N''''), N'' '', ISNULL([hp].[Family], N'''')))), N''''),
        [t3].[FullName],
        [t2].[NameFa],
        [t2].[Username]
    ) AS [tExpert_Name],
    [t1].[BreakfastFee] AS [t1_BreakfastFee],
    [t1].[LunchFee] AS [t1_LunchFee],
    [t1].[DinnerFee] AS [t1_DinnerFee],
    [t1].[NightRight] AS [t1_NightRight],
    [t1].[WorkOvertimeFee] AS [t1_WorkOvertimeFee],
    [t1].[MissionRight] AS [t1_MissionRight],
    [t1].[MissionHolidayRight] AS [t1_MissionHolidayRight]
FROM [Sale].[MissionSalary] AS [t1]
LEFT JOIN [Hcm].[Personel] AS [hp] ON [t1].[PersonelId] = [hp].[Id]
LEFT JOIN [Gnr].[Party] AS [t3] ON [hp].[PartyId] = [t3].[Id]
LEFT JOIN [system].[User] AS [t2] ON [t1].[ExpertId] = [t2].[Id]';

    UPDATE system.SavedQuery
    SET QueryJson = JSON_MODIFY(@QueryJsonSkeleton, '$.CustomQuery', @PSelect),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'AfterSales_MissionSalary_List';
    PRINT N'  Profile query updated: ' + CAST(@@ROWCOUNT AS nvarchar(20));

    COMMIT TRANSACTION;
    PRINT N'=== DONE Fix_SaleMissionSalary_Personel ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

SELECT
    COUNT(*) AS Total,
    SUM(CASE WHEN PersonelId IS NOT NULL AND PersonelId <> 0 THEN 1 ELSE 0 END) AS PersonelSet,
    SUM(CASE WHEN ExpertId IS NOT NULL AND ExpertId <> 0 THEN 1 ELSE 0 END) AS ExpertSet,
    SUM(CASE WHEN (PersonelId IS NULL OR PersonelId = 0) THEN 1 ELSE 0 END) AS PersonelMissing
FROM Sale.MissionSalary;

SELECT TOP 8 ms.Id, ms.PersonelId, hp.Name, hp.Family, ms.ExpertId, u.Username
FROM Sale.MissionSalary ms
LEFT JOIN Hcm.Personel hp ON hp.Id = ms.PersonelId
LEFT JOIN system.[User] u ON u.Id = ms.ExpertId
WHERE ms.Id IN (46, 50, 62, 64, 1, 2, 3, 4)
ORDER BY ms.Id;
