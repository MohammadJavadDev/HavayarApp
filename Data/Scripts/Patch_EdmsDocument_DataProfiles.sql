/*
Patch_EdmsDocument_DataProfiles.sql
هم‌ترازسازی هفت نمایه زنده Edms.Document با فیلتر صف کاربر عادی صفحات مدارک مهندسی HTS.

فقط Mode و QueryJson عوض می‌شود (CustomQuery + Filters=[] + CustomConditions=[]).
ColumnsJson / ActionOptions / CustomActionButtonsJson / EventScriptsJson / Title / Name / EntityFullName دست‌نخورده می‌ماند.

اعداد وضعیت = enum Havayar (DocumentStatusEnums)، نه عدد HTS.
@CurrentUserId پارامتر زمان اجرای نمایه است.

Apply (UTF-8):
  sqlcmd -S 172.20.40.42 -d HavayarApp -C -b -I -f 65001 -i Data\Scripts\Patch_EdmsDocument_DataProfiles.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'patch-edms-document-dataprofiles';

    IF (
        SELECT COUNT(*)
        FROM system.SavedQuery
        WHERE (Id = 7 AND Name = N'vw_DocumentMyCartabl')
           OR (Id = 10 AND Name = N'vw_DocumentMyArchive')
           OR (Id = 12 AND Name = N'vw_DocumentMyActions')
           OR (Id = 13 AND Name = N'vw_DocumentAllProjectInfo')
           OR (Id = 14 AND Name = N'vw_DocumentDCC')
           OR (Id = 15 AND Name = N'vw_DocumentReciveAndSend')
           OR (Id = 35 AND Name = N'vw_DocumentAllProjectInfonew')
    ) <> 7
        THROW 50001, N'expected the 7 Edms.Document SavedQuery rows by Id+Name', 1;

    DECLARE @QjTpl NVARCHAR(MAX) = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"","Parameters":[],"Selects":null}';

    DECLARE @SelectCommon NVARCHAR(MAX) = N'SELECT
    CAST(CASE WHEN [t1].[ReviewerId] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsReviewed],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t3].[Code] AS [t3_Code],
    [t3].[Title] AS [t3_Title],
    [t1].[Revision] AS [t1_Revision],
    [t1].[Status] AS [t1_Status],
    [t1].[IsLatest] AS [t1_IsLatest],
    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],
    [t3].[Displaying] AS [t3_Displaying],
    [t4].[OriginalName] AS [t4_OriginalName],
    [t1].[HourPrePerson] AS [t1_HourPrePerson],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t6].[Name] AS [t6_Name],
    [t5].[Name] AS [t5_Name],
    [t7].[Name] AS [t7_Name],
    [t2].[ProjectIsVendoriType] AS [t2_ProjectIsVendoriType],
    CAST(CASE WHEN [t1].[ReviewerId] IS NULL THEN 1 ELSE 0 END AS bit) AS [IsNotReviewed],
    [t1].[Id] AS [t1_Id]
';

    DECLARE @SelectReciveAndSend NVARCHAR(MAX) = N'SELECT
    CAST(CASE WHEN [t1].[ReviewerId] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsReviewed],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t3].[Code] AS [t3_Code],
    [t3].[Title] AS [t3_Title],
    [t1].[Revision] AS [t1_Revision],
    [t1].[Status] AS [t1_Status],
    [t1].[IsLatest] AS [t1_IsLatest],
    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],
    [t3].[Displaying] AS [t3_Displaying],
    [t4].[OriginalName] AS [t4_OriginalName],
    [t1].[HourPrePerson] AS [t1_HourPrePerson],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t6].[Name] AS [t6_Name],
    [t5].[Name] AS [t5_Name],
    [t7].[Name] AS [t7_Name],
    [t1].[RevieweShamsiDateTime] AS [t1_RevieweShamsiDateTime],
    [t1].[ApprovedShamsiDateTime] AS [t1_ApprovedShamsiDateTime],
    [t2].[ProjectIsVendoriType] AS [t2_ProjectIsVendoriType],
    CAST(CASE WHEN [t1].[ReviewerId] IS NULL THEN 1 ELSE 0 END AS bit) AS [IsNotReviewed],
    [t1].[Id] AS [t1_Id]
';

    DECLARE @SelectAllProjectInfoNew NVARCHAR(MAX) = N'SELECT
    CAST(CASE WHEN [t1].[ReviewerId] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsReviewed],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t3].[Code] AS [t3_Code],
    [t3].[Title] AS [t3_Title],
    [t1].[Revision] AS [t1_Revision],
    [t1].[Status] AS [t1_Status],
    [t1].[IsLatest] AS [t1_IsLatest],
    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],
    [t3].[Displaying] AS [t3_Displaying],
    [t4].[OriginalName] AS [t4_OriginalName],
    [t1].[HourPrePerson] AS [t1_HourPrePerson],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t6].[Name] AS [t6_Name],
    [t5].[Name] AS [t5_Name],
    [t7].[Name] AS [t7_Name],
    [t1].[Status] AS [t1_Status_id_lahtsfmkw],
    [t2].[ProjectIsVendoriType] AS [t2_ProjectIsVendoriType],
    CAST(CASE WHEN [t1].[ReviewerId] IS NULL THEN 1 ELSE 0 END AS bit) AS [IsNotReviewed],
    [t1].[Id] AS [t1_Id]
';

    DECLARE @FromShared NVARCHAR(MAX) = N'FROM [Edms].[Document] AS [t1]
INNER JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]
INNER JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]
LEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]
LEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]
LEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]
LEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]
';

    DECLARE @FromMyActions NVARCHAR(MAX) = @FromShared + N'OUTER APPLY (
    SELECT TOP (1) c.CreatedById
    FROM Edms.DocumentComment c
    WHERE c.DocumentId = t1.Id
    ORDER BY c.Id DESC
) AS lastComment
';

    DECLARE @IsReviewer NVARCHAR(MAX) = N'(
        [t1].[ReviewerId] = @CurrentUserId
        OR EXISTS (
            SELECT 1
            FROM STRING_SPLIT([t3].[ReviewersId], '','')
            WHERE TRIM(value) = CAST(@CurrentUserId AS NVARCHAR)
        )
    )';

    DECLARE @IsSensitiveUser NVARCHAR(MAX) = N'EXISTS (
        SELECT 1
        FROM STRING_SPLIT([t2].[SensitiveUsersIds], '','')
        WHERE TRIM(value) = CAST(@CurrentUserId AS NVARCHAR)
    )';

    /* 7 کارتابل من — HTS PersonalReferral عادی */
    DECLARE @Cq7 NVARCHAR(MAX) = @SelectCommon + @FromShared + N'WHERE
    [t1].[IsLatest] = 1
    AND [t2].[Status] = 1
    AND ([t3].[ProducerId] = @CurrentUserId OR [t1].[CreatedById] = @CurrentUserId)
    AND [t1].[Status] IN (3, 5, 6, 8, 10, 11, 12, 13, 14, 15, 16, 17, 21)';

    /* 10 آرشیو شخصی — HTS PersonalDocumentArchive عادی */
    DECLARE @Cq10 NVARCHAR(MAX) = @SelectCommon + @FromShared + N'WHERE
    [t3].[ProducerId] = @CurrentUserId
    OR [t1].[CreatedById] = @CurrentUserId
    OR [t1].[ReviewerId] = @CurrentUserId
    OR [t1].[ApproverId] = @CurrentUserId';

    /* 12 بررسی / تایید / رد — HTS ControlDocuments شاخه کاربر عادی */
    DECLARE @Cq12 NVARCHAR(MAX) = @SelectCommon + @FromMyActions + N'WHERE
    (
        ' + @IsReviewer + N'
        AND [t1].[Status] = 2
    )
    OR (
        [t1].[ApproverId] = @CurrentUserId
        AND lastComment.CreatedById <> @CurrentUserId
        AND (
            [t1].[Status] IN (2, 4, 7)
            OR (
                [t2].[ProjectIsVendoriType] = 1
                AND [t1].[Status] = 13
                AND [t1].[ApprovedMiladiDateTime] IS NULL
            )
        )
    )
    OR (
        (' + @IsReviewer + N' OR [t1].[ApproverId] = @CurrentUserId)
        AND lastComment.CreatedById = @CurrentUserId
        AND [t1].[Status] = 30
    )
    OR (
        (' + @IsReviewer + N' OR [t1].[ApproverId] = @CurrentUserId)
        AND lastComment.CreatedById <> @CurrentUserId
        AND [t1].[Status] = 28
    )';

    /* 13 اطلاعات کلیه پروژه ها — HTS AllDocumentArchive شاخه پیش‌فرض visibility */
    DECLARE @Cq13 NVARCHAR(MAX) = @SelectCommon + @FromShared + N'WHERE
    (
        [t2].[IsConfidential] = 0
        OR ' + @IsSensitiveUser + N'
    )
    AND (
        [t2].[ProjectIsVendoriType] = 1
        OR [t1].[Status] = 10
        OR (
            [t1].[RevieweMiladiDateTime] IS NOT NULL
            AND [t1].[ApprovedMiladiDateTime] IS NOT NULL
        )
        OR [t1].[Status] IN (16, 17)
    )';

    /* 14 کارتابل DCC */
    DECLARE @Cq14 NVARCHAR(MAX) = @SelectCommon + @FromShared + N'WHERE
    [t1].[RevieweMiladiDateTime] IS NOT NULL
    AND [t1].[ApprovedMiladiDateTime] IS NOT NULL
    AND (
        [t1].[Status] IN (4, 7)
        OR (
            [t2].[ProjectIsVendoriType] = 1
            AND [t1].[Status] = 13
        )
    )';

    /* 15 دریافت و ارسال اسناد — ستون‌های تاریخ بررسی/تایید حفظ می‌شود */
    DECLARE @Cq15 NVARCHAR(MAX) = @SelectReciveAndSend + @FromShared + N'WHERE
    [t1].[RevieweMiladiDateTime] IS NOT NULL
    AND [t1].[ApprovedMiladiDateTime] IS NOT NULL
    AND [t1].[Status] IN (9, 13, 17, 18, 20, 31)';

    /* 35 اطلاعات کلیه پروژه ها با ویرایش — HTS بارگذاری غیر FullAccess */
    DECLARE @Cq35 NVARCHAR(MAX) = @SelectAllProjectInfoNew + @FromShared + N'WHERE
    [t1].[IsLatest] = 1
    AND ([t3].[ProducerId] = @CurrentUserId OR [t1].[CreatedById] = @CurrentUserId)';

    UPDATE system.SavedQuery
    SET Mode = 1,
        QueryJson = JSON_MODIFY(@QjTpl, '$.CustomQuery', @Cq7),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 7 AND Name = N'vw_DocumentMyCartabl';
    IF @@ROWCOUNT <> 1
        THROW 50007, N'vw_DocumentMyCartabl (7) not updated', 1;

    UPDATE system.SavedQuery
    SET Mode = 1,
        QueryJson = JSON_MODIFY(@QjTpl, '$.CustomQuery', @Cq10),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 10 AND Name = N'vw_DocumentMyArchive';
    IF @@ROWCOUNT <> 1
        THROW 50010, N'vw_DocumentMyArchive (10) not updated', 1;

    UPDATE system.SavedQuery
    SET Mode = 1,
        QueryJson = JSON_MODIFY(@QjTpl, '$.CustomQuery', @Cq12),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 12 AND Name = N'vw_DocumentMyActions';
    IF @@ROWCOUNT <> 1
        THROW 50012, N'vw_DocumentMyActions (12) not updated', 1;

    UPDATE system.SavedQuery
    SET Mode = 1,
        QueryJson = JSON_MODIFY(@QjTpl, '$.CustomQuery', @Cq13),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 13 AND Name = N'vw_DocumentAllProjectInfo';
    IF @@ROWCOUNT <> 1
        THROW 50013, N'vw_DocumentAllProjectInfo (13) not updated', 1;

    UPDATE system.SavedQuery
    SET Mode = 1,
        QueryJson = JSON_MODIFY(@QjTpl, '$.CustomQuery', @Cq14),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 14 AND Name = N'vw_DocumentDCC';
    IF @@ROWCOUNT <> 1
        THROW 50014, N'vw_DocumentDCC (14) not updated', 1;

    UPDATE system.SavedQuery
    SET Mode = 1,
        QueryJson = JSON_MODIFY(@QjTpl, '$.CustomQuery', @Cq15),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 15 AND Name = N'vw_DocumentReciveAndSend';
    IF @@ROWCOUNT <> 1
        THROW 50015, N'vw_DocumentReciveAndSend (15) not updated', 1;

    UPDATE system.SavedQuery
    SET Mode = 1,
        QueryJson = JSON_MODIFY(@QjTpl, '$.CustomQuery', @Cq35),
        ModifiedById = 1,
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Id = 35 AND Name = N'vw_DocumentAllProjectInfonew';
    IF @@ROWCOUNT <> 1
        THROW 50035, N'vw_DocumentAllProjectInfonew (35) not updated', 1;

    COMMIT TRANSACTION;
    PRINT N'=== DONE: Patch_EdmsDocument_DataProfiles committed ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrState INT = ERROR_STATE();
    RAISERROR(@ErrMsg, @ErrSeverity, @ErrState);
END CATCH;

PRINT N'--- verify ---';
SELECT
    q.Id,
    q.Name,
    q.Title,
    q.Mode,
    JSON_QUERY(q.QueryJson, '$.Filters') AS Filters,
    JSON_QUERY(q.QueryJson, '$.CustomConditions') AS CustomConditions,
    JSON_VALUE(q.QueryJson, '$.CustomQuery') AS CustomQuery
FROM system.SavedQuery q
WHERE q.Id IN (7, 10, 12, 13, 14, 15, 35)
ORDER BY q.Id;
