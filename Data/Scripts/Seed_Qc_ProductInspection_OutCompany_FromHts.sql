/*
Seed_Qc_ProductInspection_OutCompany_FromHts.sql
انتقال بازرسی محصول در محل پیمانکار (InspectionType 470) از TotalSystem.
رد می‌شود: رکورد بدون ProductionOrderItemNewId (از جمله HTS 238) و قلم بدون تطبیق یکتا.
HasInspection فقط برای نوع 469 است و اینجا عوض نمی‌شود.
UTF-8 BOM. Idempotent.
sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P <pwd> -C -b -I -f 65001 -i "Data\Scripts\Seed_Qc_ProductInspection_OutCompany_FromHts.sql"
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-qc-productinspection-outcompany';

    IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
        THROW 50001, N'Linked server TMS is missing.', 1;

    IF COL_LENGTH(N'Qc.ProductInspection', N'Contractor') IS NULL
        THROW 50002, N'Column Qc.ProductInspection.Contractor is missing. Apply the migration first.', 1;

    IF OBJECT_ID('tempdb..#Cand') IS NOT NULL DROP TABLE #Cand;
    SELECT q.Id
    INTO #Cand
    FROM [TMS].[TotalSystem].[dbo].[Qc_ProductInspection] q
    WHERE q.InspectionType_FK = 470
      AND q.ProductionOrderItemNewId IS NOT NULL;

    IF OBJECT_ID('tempdb..#Match') IS NOT NULL DROP TABLE #Match;
    CREATE TABLE #Match (HtsId INT NOT NULL PRIMARY KEY, ItemId BIGINT NOT NULL);

    ;WITH rah AS (
        SELECT q.Id AS HtsId, i.Id AS ItemId,
            COUNT(*) OVER (PARTITION BY q.Id) AS Cnt
        FROM #Cand c
        INNER JOIN [TMS].[TotalSystem].[dbo].[Qc_ProductInspection] q ON q.Id = c.Id
        INNER JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] h ON h.Id = q.ProductionOrderItemNewId
        INNER JOIN Sale.ProductionOrderItem i ON i.RahkaranHistoryId = h.RahkaranId
        WHERE h.RahkaranId IS NOT NULL
    )
    INSERT INTO #Match (HtsId, ItemId)
    SELECT HtsId, MIN(ItemId)
    FROM rah
    WHERE Cnt = 1
    GROUP BY HtsId;

    ;WITH ser AS (
        SELECT q.Id AS HtsId, i.Id AS ItemId
        FROM #Cand c
        INNER JOIN [TMS].[TotalSystem].[dbo].[Qc_ProductInspection] q ON q.Id = c.Id
        INNER JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrderItem] h ON h.Id = q.ProductionOrderItemNewId
        INNER JOIN [TMS].[TotalSystem].[dbo].[Pln_ProductionOrder] po ON po.Id = h.ProductionOrderId
        INNER JOIN Sale.ProductionOrder npo ON npo.Number = CAST(po.Number AS nvarchar(50))
        INNER JOIN Sale.ProductionOrderItem i
            ON i.ProductionOrderId = npo.Id
           AND LTRIM(RTRIM(i.Serial)) = LTRIM(RTRIM(h.Serial))
           AND ISNULL(i.IsDeleted, 0) = 0
           AND ISNULL(i.IsLatestVersion, 0) = 1
        WHERE h.RahkaranId IS NULL
          AND NOT EXISTS (SELECT 1 FROM #Match m WHERE m.HtsId = q.Id)
    )
    INSERT INTO #Match (HtsId, ItemId)
    SELECT HtsId, MIN(ItemId)
    FROM ser
    GROUP BY HtsId
    HAVING COUNT(DISTINCT ItemId) = 1;

    DECLARE @Skipped NVARCHAR(MAX);
    SELECT @Skipped = STRING_AGG(CAST(q.Id AS nvarchar(20)), N',')
    FROM [TMS].[TotalSystem].[dbo].[Qc_ProductInspection] q
    WHERE q.InspectionType_FK = 470
      AND NOT EXISTS (SELECT 1 FROM #Match m WHERE m.HtsId = q.Id);
    DECLARE @Matched INT = (SELECT COUNT(*) FROM #Match);
    PRINT N'  Matched: ' + CAST(@Matched AS nvarchar(20));
    PRINT N'  Skipped (no unique item or no NewId): ' + ISNULL(@Skipped, N'');

    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;
    SELECT u.User_ID AS HtsUserId,
        COALESCE(app.Id, 1) AS AppUserId,
        COALESCE(NULLIF(LTRIM(RTRIM(u.FullName)), N''), app.DisplayName, N'seed') AS FullName
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] u
    OUTER APPLY (
        SELECT TOP 1 su.Id, COALESCE(NULLIF(LTRIM(RTRIM(su.NameFa)), N''), su.Name) AS DisplayName
        FROM system.[User] su
        WHERE LOWER(su.Username) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), NULLIF(LTRIM(RTRIM(u.Username)), N'')))
           OR LOWER(REPLACE(ISNULL(su.Email, N''), N'@havayar.com', N'')) = LOWER(COALESCE(NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N''), u.Username))
        ORDER BY su.Id
    ) app;

    IF OBJECT_ID('tempdb..#PersonMap') IS NOT NULL DROP TABLE #PersonMap;
    SELECT hp.Personel_ID AS HtsId, MIN(p.Id) AS AppId
    INTO #PersonMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_Personel] hp
    INNER JOIN Hcm.Personel p ON p.Code = hp.PrsCode
    GROUP BY hp.Personel_ID;

    IF OBJECT_ID('tempdb..#OrgMap') IS NOT NULL DROP TABLE #OrgMap;
    SELECT hou.OrgUnit_ID AS HtsId, COALESCE(byCode.Id, byTitle.Id) AS AppId
    INTO #OrgMap
    FROM [TMS].[TotalSystem].[dbo].[HRM_OrgUnit] hou
    OUTER APPLY (
        SELECT TOP 1 ou.Id
        FROM Hrm.OrgUnit ou
        WHERE hou.Hamkaran_Unit_FK IS NOT NULL AND hou.Hamkaran_Unit_FK <> 0
          AND ou.HamkaranUnitId = hou.Hamkaran_Unit_FK
        ORDER BY ou.Id
    ) byCode
    OUTER APPLY (
        SELECT TOP 1 ou.Id
        FROM Hrm.OrgUnit ou
        WHERE byCode.Id IS NULL
          AND LTRIM(RTRIM(ou.Title)) = LTRIM(RTRIM(hou.OrgUnit_Title))
        ORDER BY ou.Id
    ) byTitle;

    DECLARE @NoPerson NVARCHAR(MAX);
    SELECT @NoPerson = STRING_AGG(CAST(m.HtsId AS nvarchar(20)), N',')
    FROM #Match m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Qc_ProductInspection] q ON q.Id = m.HtsId
    LEFT JOIN #PersonMap per ON per.HtsId = q.TestPersonel_FK
    WHERE per.AppId IS NULL;
    PRINT N'  Skipped (no inspector map): ' + ISNULL(@NoPerson, N'');

    PRINT N'=== [1] ProductInspection 470 ===';
    SET IDENTITY_INSERT Qc.ProductInspection ON;
    INSERT INTO Qc.ProductInspection (
        Id, HtsId, InspectionType, ProductionOrderNumber, ProductionOrderItemId, ManualSerial, SalesDepartmentId, ProjectTitle, Contractor,
        TestStartShamsiDate, TestStartMiladiDate, TestStartTime, TestEndShamsiDate, TestEndMiladiDate, TestEndTime,
        TestPersonelId, InspectionResult, IsCompleted, Comment, IsActive,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    SELECT
        q.Id, q.Id, 470, q.ProductionOrderNumber, m.ItemId,
        LEFT(NULLIF(LTRIM(RTRIM(q.ManualSerial)), N''), 50),
        org.AppId,
        LEFT(NULLIF(LTRIM(RTRIM(q.ProjectTitle)), N''), 1024),
        LEFT(NULLIF(LTRIM(RTRIM(q.Contractor)), N''), 1024),
        LEFT(NULLIF(LTRIM(RTRIM(q.TestStartDateInText)), N''), 10), q.TestStartDate, LEFT(NULLIF(LTRIM(RTRIM(q.TestStartTime)), N''), 5),
        LEFT(NULLIF(LTRIM(RTRIM(q.TestEndDateInText)), N''), 10), q.TestEndDate, LEFT(NULLIF(LTRIM(RTRIM(q.TestEndTime)), N''), 5),
        per.AppId, q.InspectionResult, q.IsCompleted, LEFT(NULLIF(LTRIM(RTRIM(q.Comment)), N''), 2048), 1,
        ISNULL(cu.AppUserId, 1), LEFT(ISNULL(cu.FullName, @SeedUser), 150),
        CASE WHEN q.CreatedDate IS NULL THEN @Now
             WHEN TRY_CAST(LEFT(LTRIM(RTRIM(q.CreatedTime)), 2) AS int) IS NULL THEN CAST(q.CreatedDate AS datetime2)
             ELSE DATEADD(minute,
                    ISNULL(TRY_CAST(LEFT(LTRIM(RTRIM(q.CreatedTime)), 2) AS int), 0) * 60
                    + ISNULL(TRY_CAST(SUBSTRING(LTRIM(RTRIM(q.CreatedTime)), 4, 2) AS int), 0),
                    CAST(q.CreatedDate AS datetime2)) END,
        LEFT(LTRIM(RTRIM(ISNULL(q.CreatedDate_Shamsi, N''))) + CASE WHEN NULLIF(LTRIM(RTRIM(q.CreatedTime)), N'') IS NULL THEN N'' ELSE N' ' + LTRIM(RTRIM(q.CreatedTime)) END, 30),
        ISNULL(uu.AppUserId, ISNULL(cu.AppUserId, 1)), LEFT(ISNULL(uu.FullName, ISNULL(cu.FullName, @SeedUser)), 150),
        CASE WHEN q.UpdatedDate IS NULL THEN
            CASE WHEN q.CreatedDate IS NULL THEN @Now ELSE CAST(q.CreatedDate AS datetime2) END
            ELSE CAST(q.UpdatedDate AS datetime2) END,
        COALESCE(NULLIF(LTRIM(RTRIM(q.UpdatedDateInText)), N''), NULLIF(LTRIM(RTRIM(q.CreatedDate_Shamsi)), N''), @NowShamsi)
    FROM #Match m
    INNER JOIN [TMS].[TotalSystem].[dbo].[Qc_ProductInspection] q ON q.Id = m.HtsId
    INNER JOIN #PersonMap per ON per.HtsId = q.TestPersonel_FK
    LEFT JOIN #OrgMap org ON org.HtsId = q.SalesDepartment_FK
    LEFT JOIN #UserMap cu ON cu.HtsUserId = q.CreatedUser_FK
    LEFT JOIN #UserMap uu ON uu.HtsUserId = q.UpdatedUserId
    WHERE NOT EXISTS (SELECT 1 FROM Qc.ProductInspection p WHERE p.HtsId = q.Id OR p.Id = q.Id);
    PRINT N'  Inserted inspections: ' + CAST(@@ROWCOUNT AS nvarchar(20));
    SET IDENTITY_INSERT Qc.ProductInspection OFF;

    PRINT N'=== [2] Failures ===';
    SET IDENTITY_INSERT Qc.ProductInspectionFailure ON;
    INSERT INTO Qc.ProductInspectionFailure (
        Id, HtsId, ProductInspectionId, FailureTypeId, ProductionOrderItemCommentId,
        FailureStartShamsiDate, FailureStartMiladiDate, FailureStartTime,
        FailureEndShamsiDate, FailureEndMiladiDate, FailureEndTime, Comment, IsActive,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    SELECT
        f.Id, f.Id, p.Id, ft.Id, cmt.Id,
        NULLIF(LTRIM(RTRIM(f.FailureStartDateInText)), N''), f.FailureStartDate, NULLIF(LTRIM(RTRIM(f.FailureStartTime)), N''),
        NULLIF(LTRIM(RTRIM(f.FailureEndDateInText)), N''), f.FailureEndDate, NULLIF(LTRIM(RTRIM(f.FailureEndTime)), N''),
        LEFT(NULLIF(LTRIM(RTRIM(f.Comment)), N''), 4000), 1,
        ISNULL(cu.AppUserId, 1), LEFT(ISNULL(cu.FullName, @SeedUser), 150),
        CASE WHEN TRY_CONVERT(date, f.FailureStartDate) IS NULL THEN @Now ELSE CAST(f.FailureStartDate AS datetime2) END,
        LEFT(LTRIM(RTRIM(ISNULL(f.CreatedDate, N''))) + CASE WHEN NULLIF(LTRIM(RTRIM(f.CreatedTime)), N'') IS NULL THEN N'' ELSE N' ' + LTRIM(RTRIM(f.CreatedTime)) END, 30),
        ISNULL(cu.AppUserId, 1), LEFT(ISNULL(cu.FullName, @SeedUser), 150), @Now, @NowShamsi
    FROM [TMS].[TotalSystem].[dbo].[Qc_ProductInspection_Failure] f
    INNER JOIN Qc.ProductInspection p ON p.HtsId = f.ProductInspection_FK AND p.InspectionType = 470
    INNER JOIN Qc.FailureType ft ON ft.Id = f.FailureType_FK
    LEFT JOIN Sale.ProductionOrderItemComment cmt ON cmt.HtsId = f.ProductionOrderItemCommentId AND f.ProductionOrderItemCommentId IS NOT NULL AND cmt.HtsId <> 0
    LEFT JOIN #UserMap cu ON cu.HtsUserId = f.CreatedUser_FK
    WHERE NOT EXISTS (SELECT 1 FROM Qc.ProductInspectionFailure x WHERE x.HtsId = f.Id OR x.Id = f.Id);
    PRINT N'  Inserted failures: ' + CAST(@@ROWCOUNT AS nvarchar(20));
    SET IDENTITY_INSERT Qc.ProductInspectionFailure OFF;

    PRINT N'=== [3] Measured parameters ===';
    SET IDENTITY_INSERT Qc.ProductInspectionMeasuredParameter ON;
    INSERT INTO Qc.ProductInspectionMeasuredParameter (
        Id, HtsId, ProductInspectionId, MeasurementParameterItemId, MeasurementParameterSubItemId,
        MeasurementStartShamsiDate, MeasurementStartMiladiDate, MeasurementStartTime,
        MeasurementEndShamsiDate, MeasurementEndMiladiDate, MeasurementEndTime,
        MeasurementResult, MeasurementValue, Comment, IsActive,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    SELECT
        mp.Id, mp.Id, p.Id, it.Id, sub.Id,
        LEFT(NULLIF(LTRIM(RTRIM(mp.MeasurementStartDateInText)), N''), 10), mp.MeasurementStartDate, LEFT(NULLIF(LTRIM(RTRIM(mp.MeasurementStartTime)), N''), 5),
        LEFT(NULLIF(LTRIM(RTRIM(mp.MeasurementEndDateInText)), N''), 10), mp.MeasurementEndDate, LEFT(NULLIF(LTRIM(RTRIM(mp.MeasurementEndTime)), N''), 5),
        mp.MeasurementResult, mp.MeasurementValue, LEFT(NULLIF(LTRIM(RTRIM(mp.Comment)), N''), 4000), 1,
        ISNULL(cu.AppUserId, 1), LEFT(ISNULL(cu.FullName, @SeedUser), 150), @Now,
        LEFT(LTRIM(RTRIM(ISNULL(mp.CreatedDate, N''))) + CASE WHEN NULLIF(LTRIM(RTRIM(mp.CreatedTime)), N'') IS NULL THEN N'' ELSE N' ' + LTRIM(RTRIM(mp.CreatedTime)) END, 30),
        ISNULL(cu.AppUserId, 1), LEFT(ISNULL(cu.FullName, @SeedUser), 150), @Now, @NowShamsi
    FROM [TMS].[TotalSystem].[dbo].[Qc_ProductInspection_MeasuredParameter] mp
    INNER JOIN Qc.ProductInspection p ON p.HtsId = mp.ProductInspection_FK AND p.InspectionType = 470
    INNER JOIN Qc.MeasurementParameterItem it ON it.Id = mp.MeasurementParameterItemId
    LEFT JOIN Qc.MeasurementParameterSubItem sub ON sub.Id = mp.MeasurementParameterSubItemId
    LEFT JOIN #UserMap cu ON cu.HtsUserId = mp.CreatedUser_FK
    WHERE NOT EXISTS (SELECT 1 FROM Qc.ProductInspectionMeasuredParameter x WHERE x.HtsId = mp.Id OR x.Id = mp.Id);
    PRINT N'  Inserted parameters: ' + CAST(@@ROWCOUNT AS nvarchar(20));
    SET IDENTITY_INSERT Qc.ProductInspectionMeasuredParameter OFF;

    PRINT N'=== [4] Attachments (path only) ===';
    SET IDENTITY_INSERT Qc.ProductInspectionAttachment ON;
    INSERT INTO Qc.ProductInspectionAttachment (
        Id, HtsId, ProductInspectionId, FileId, OriginalFileName, SourceFilePath, Comment, IsActive,
        CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime)
    SELECT
        a.Id, a.Id, p.Id, NULL,
        LEFT(NULLIF(LTRIM(RTRIM(a.Attachment_FileName)), N''), 255),
        LEFT(NULLIF(LTRIM(RTRIM(a.AttachmentFilePath)), N''), 1024),
        LEFT(NULLIF(LTRIM(RTRIM(a.Attachment_Comment)), N''), 4000), 1,
        ISNULL(cu.AppUserId, 1), LEFT(ISNULL(cu.FullName, @SeedUser), 150), @Now,
        LEFT(LTRIM(RTRIM(ISNULL(a.CreatedDate, N''))) + CASE WHEN NULLIF(LTRIM(RTRIM(a.CreatedTime)), N'') IS NULL THEN N'' ELSE N' ' + LTRIM(RTRIM(a.CreatedTime)) END, 30),
        ISNULL(cu.AppUserId, 1), LEFT(ISNULL(cu.FullName, @SeedUser), 150), @Now, @NowShamsi
    FROM [TMS].[TotalSystem].[dbo].[Qc_ProductInspection_Attachment] a
    INNER JOIN Qc.ProductInspection p ON p.HtsId = a.ProductInspection_FK AND p.InspectionType = 470
    LEFT JOIN #UserMap cu ON cu.HtsUserId = a.CreatedUser_FK
    WHERE NOT EXISTS (SELECT 1 FROM Qc.ProductInspectionAttachment x WHERE x.HtsId = a.Id OR x.Id = a.Id);
    PRINT N'  Inserted attachments: ' + CAST(@@ROWCOUNT AS nvarchar(20));
    SET IDENTITY_INSERT Qc.ProductInspectionAttachment OFF;

    DECLARE @MaxId BIGINT;
    SELECT @MaxId = MAX(Id) FROM Qc.ProductInspection;
    IF @MaxId IS NOT NULL DBCC CHECKIDENT ('Qc.ProductInspection', RESEED, @MaxId);
    SELECT @MaxId = MAX(Id) FROM Qc.ProductInspectionFailure;
    IF @MaxId IS NOT NULL DBCC CHECKIDENT ('Qc.ProductInspectionFailure', RESEED, @MaxId);
    SELECT @MaxId = MAX(Id) FROM Qc.ProductInspectionMeasuredParameter;
    IF @MaxId IS NOT NULL DBCC CHECKIDENT ('Qc.ProductInspectionMeasuredParameter', RESEED, @MaxId);
    SELECT @MaxId = MAX(Id) FROM Qc.ProductInspectionAttachment;
    IF @MaxId IS NOT NULL DBCC CHECKIDENT ('Qc.ProductInspectionAttachment', RESEED, @MaxId);

    COMMIT TRANSACTION;
    PRINT N'=== DONE Seed_Qc_ProductInspection_OutCompany_FromHts ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;
