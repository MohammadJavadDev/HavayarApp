/*
  کارتابل بررسی و تایید، هم‌تراز با فیلتر کاربر عادی HTS (ControlDocuments):
  - IssueForClient (۳۰) فقط برای بررسی‌کننده یا تاییدکننده‌ای که آخرین کامنت را خودش ثبت کرده.
  - ApproveByClient (۱۳) فقط وندوری، بدون مهر تایید، و تا وقتی آخرین کامنت از تاییدکننده نیست.
  - CommentedByReviewer (۵) در این کارتابل نمی‌ماند.
  - ReplaySheet (۲۸) برای بررسی‌کننده یا تاییدکننده همین VPIS، تا وقتی آخرین کامنت مال خودشان نیست.
  نمایه‌های دیگر دست نمی‌خورند.
  UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @Name NVARCHAR(100) = N'Edms_Document_ReviewApproveCartable';
    DECLARE @Sql NVARCHAR(MAX) = N'DECLARE @CuId BIGINT = TRY_CAST(@CurrentUserId AS BIGINT);
--!--mainsection
SELECT
    CAST(CASE WHEN [t1].[RevieweMiladiDateTime] IS NOT NULL THEN 1 ELSE 0 END AS bit) AS [IsChecked],
    [t2].[Code] AS [t2_Code],
    [t2].[Name] AS [t2_Name],
    [t3].[Code] AS [t3_Code],
    [t3].[Title] AS [t3_Title],
    [t1].[Revision] AS [t1_Revision],
    [t1].[Status] AS [t1_Status],
    [t1].[Status] AS [t1_StatusId],
    [t1].[IsLatest] AS [IsLatest],
    [t1].[GoalOfProduction] AS [t1_GoalOfProduction],
    [t3].[Displaying] AS [t3_Displaying],
    [t4].[OriginalName] AS [t4_OriginalName],
    [t1].[Comment] AS [t1_Comment],
    [t1].[CreatedOnShamsiDateTime] AS [t1_CreatedOnShamsiDateTime],
    [t1].[ModifiedDateShamsiDateTime] AS [t1_ModifiedDateShamsiDateTime],
    [t6].[Name] AS [t6_Name],
    [t5].[Name] AS [t5_Name],
    [t7].[Name] AS [t7_Name],
    [t1].[Id] AS [t1_Id],
    [t1].[ProjectId] AS [t1_ProjectId]
FROM [Edms].[Document] AS [t1]
LEFT JOIN [Edms].[Project] AS [t2] ON [t1].[ProjectId] = [t2].[Id]
LEFT JOIN [Edms].[ProjectVpis] AS [t3] ON [t1].[DocumentVpisId] = [t3].[Id]
LEFT JOIN [dbo].[FileEntity] AS [t4] ON [t4].[Id] = [t1].[MainFileId]
LEFT JOIN [system].[User] AS [t5] ON [t1].[ReviewerId] = [t5].[Id]
LEFT JOIN [system].[User] AS [t6] ON [t3].[ProducerId] = [t6].[Id]
LEFT JOIN [system].[User] AS [t7] ON [t1].[ApproverId] = [t7].[Id]
OUTER APPLY (
    SELECT TOP (1) c.CreatedById
    FROM [Edms].[DocumentComment] AS c
    WHERE c.DocumentId = [t1].[Id]
    ORDER BY c.Id DESC
) AS lastComment
WHERE [t1].[IsLatest] = 1
AND [t1].[Status] <> 5
AND (
    ([t1].[Status] = 2 AND N'','' + ISNULL([t3].[ReviewersId], N'''') + N'','' LIKE N''%,'' + CAST(@CuId AS nvarchar(20)) + N'',%'')
    OR ([t1].[Status] = 2 AND [t3].[ApproverId] = @CuId)
    OR ([t1].[Status] = 4 AND [t3].[ApproverId] = @CuId)
    OR (
        [t1].[Status] = 30
        AND ISNULL(lastComment.CreatedById, 0) = @CuId
        AND (
            N'','' + ISNULL([t3].[ReviewersId], N'''') + N'','' LIKE N''%,'' + CAST(@CuId AS nvarchar(20)) + N'',%''
            OR [t3].[ApproverId] = @CuId
        )
    )
    OR (
        ISNULL([t2].[ProjectIsVendoriType], 0) = 1
        AND [t1].[Status] = 13
        AND [t1].[ApprovedMiladiDateTime] IS NULL
        AND [t3].[ApproverId] = @CuId
        AND ISNULL(lastComment.CreatedById, 0) <> @CuId
    )
    OR (
        [t1].[Status] = 28
        AND ISNULL(lastComment.CreatedById, 0) <> @CuId
        AND (
            N'','' + ISNULL([t3].[ReviewersId], N'''') + N'','' LIKE N''%,'' + CAST(@CuId AS nvarchar(20)) + N'',%''
            OR [t3].[ApproverId] = @CuId
        )
    )
) AND ISNULL([t2].[Status], -1) NOT IN (2, 4)';
    DECLARE @Qj NVARCHAR(MAX);

    IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
        THROW 51649, N'نمایه کارتابل بررسی و تایید یافت نشد.', 1;

    SET @Qj = N'{"Tables":[],"Relations":[],"Filters":[],"CustomConditions":[],"CustomQuery":"'
        + STRING_ESCAPE(@Sql, 'json')
        + N'","Parameters":[],"Selects":null}';

    IF ISJSON(@Qj) <> 1
        THROW 51647, N'QueryJson نمایه کارتابل بررسی و تایید بعد از اصلاح معتبر نیست.', 1;

    UPDATE system.SavedQuery
    SET QueryJson = @Qj,
        ModifiedById = 1,
        ModifiedByName = N'patch-edms-review-approve-hts',
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = @Name;

    IF CHARINDEX(N'lastComment.CreatedById, 0) = @CuId', (SELECT QueryJson FROM system.SavedQuery WHERE Name = @Name)) = 0
        THROW 51648, N'شرط آخرین اقدام‌کننده برای Issue for client اعمال نشد.', 1;

    IF CHARINDEX(N'[t1].[Status] <> 5', (SELECT QueryJson FROM system.SavedQuery WHERE Name = @Name)) = 0
        THROW 51648, N'حذف کامنت بررسی‌کننده از کارتابل بررسی و تایید از بین رفته است.', 1;

    IF CHARINDEX(N'[t1].[Status] = 28', (SELECT QueryJson FROM system.SavedQuery WHERE Name = @Name)) = 0
        THROW 51648, N'وضعیت برگه پاسخ به کارتابل بررسی و تایید اضافه نشد.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT
    q.Name,
    CASE WHEN CHARINDEX(N'lastComment.CreatedById, 0) = @CuId', q.QueryJson) > 0 THEN 1 ELSE 0 END AS IssueForClientLastActor,
    CASE WHEN CHARINDEX(N'lastComment.CreatedById, 0) <> @CuId', q.QueryJson) > 0 THEN 1 ELSE 0 END AS ApproveByClientLeavesAfterActor,
    CASE WHEN CHARINDEX(N'[t1].[Status] <> 5', q.QueryJson) > 0 THEN 1 ELSE 0 END AS ExcludesReviewerComment,
    CASE WHEN CHARINDEX(N'ApprovedMiladiDateTime] IS NULL', q.QueryJson) > 0 THEN 1 ELSE 0 END AS ApproveByClientNeedsOpenStamp,
    CASE WHEN CHARINDEX(N'[t1].[Status] = 28', q.QueryJson) > 0 THEN 1 ELSE 0 END AS ReplaySheetForReviewerOrApprover
FROM system.SavedQuery q
WHERE q.Name = N'Edms_Document_ReviewApproveCartable';
