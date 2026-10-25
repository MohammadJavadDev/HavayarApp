/*
  کارتابل بررسی و تایید: وضعیت برگه پاسخ (۲۸) برای بررسی‌کننده یا تاییدکننده VPIS،
  تا وقتی آخرین کامنت مال خودشان نیست.
  شاخه‌های Issue، ApproveByReviewer، IssueForClient و ApproveByClient دست نمی‌خورند.
  UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @Name NVARCHAR(100) = N'Edms_Document_ReviewApproveCartable';
    DECLARE @Q NVARCHAR(MAX) = (SELECT QueryJson FROM system.SavedQuery WHERE Name = @Name);
    DECLARE @Nl NVARCHAR(4) = N'\r\n';
    DECLARE @Old NVARCHAR(500) = N'AND ISNULL(lastComment.CreatedById, 0) <> @CuId' + @Nl + N'    )' + @Nl + N') AND ISNULL([t2].[Status], -1) NOT IN (2, 4)';
    DECLARE @New NVARCHAR(MAX) =
        N'AND ISNULL(lastComment.CreatedById, 0) <> @CuId' + @Nl + N'    )' + @Nl
        + N'    OR (' + @Nl
        + N'        [t1].[Status] = 28' + @Nl
        + N'        AND ISNULL(lastComment.CreatedById, 0) <> @CuId' + @Nl
        + N'        AND (' + @Nl
        + N'            N'','' + ISNULL([t3].[ReviewersId], N'''') + N'','' LIKE N''%,'' + CAST(@CuId AS nvarchar(20)) + N'',%''' + @Nl
        + N'            OR [t3].[ApproverId] = @CuId' + @Nl
        + N'        )' + @Nl
        + N'    )' + @Nl
        + N') AND ISNULL([t2].[Status], -1) NOT IN (2, 4)';

    IF @Q IS NULL
        THROW 51650, N'نمایه کارتابل بررسی و تایید یافت نشد.', 1;

    IF CHARINDEX(N'[t1].[Status] = 28', @Q) = 0
    BEGIN
        IF CHARINDEX(@Old, @Q) = 0
        BEGIN
            SET @Nl = N'\n';
            SET @Old = N'AND ISNULL(lastComment.CreatedById, 0) <> @CuId' + @Nl + N'    )' + @Nl + N') AND ISNULL([t2].[Status], -1) NOT IN (2, 4)';
            SET @New =
                N'AND ISNULL(lastComment.CreatedById, 0) <> @CuId' + @Nl + N'    )' + @Nl
                + N'    OR (' + @Nl
                + N'        [t1].[Status] = 28' + @Nl
                + N'        AND ISNULL(lastComment.CreatedById, 0) <> @CuId' + @Nl
                + N'        AND (' + @Nl
                + N'            N'','' + ISNULL([t3].[ReviewersId], N'''') + N'','' LIKE N''%,'' + CAST(@CuId AS nvarchar(20)) + N'',%''' + @Nl
                + N'            OR [t3].[ApproverId] = @CuId' + @Nl
                + N'        )' + @Nl
                + N'    )' + @Nl
                + N') AND ISNULL([t2].[Status], -1) NOT IN (2, 4)';
        END

        IF CHARINDEX(@Old, @Q) = 0
            THROW 51651, N'محل افزودن وضعیت برگه پاسخ در کارتابل بررسی و تایید پیدا نشد.', 1;

        SET @Q = REPLACE(@Q, @Old, @New);
        IF ISJSON(@Q) <> 1
            THROW 51652, N'QueryJson کارتابل بررسی و تایید بعد از اصلاح معتبر نیست.', 1;
        IF CHARINDEX(N'[t1].[Status] = 2 AND', @Q) = 0
            OR CHARINDEX(N'[t1].[Status] = 4 AND', @Q) = 0
            OR CHARINDEX(N'[t1].[Status] = 30', @Q) = 0
            OR CHARINDEX(N'[t1].[Status] = 13', @Q) = 0
            OR CHARINDEX(N'[t1].[Status] <> 5', @Q) = 0
            OR CHARINDEX(N'[t1].[Status] = 28', @Q) = 0
            THROW 51653, N'شاخه‌های قبلی کارتابل بررسی و تایید حفظ نشد.', 1;

        UPDATE system.SavedQuery
        SET QueryJson = @Q,
            ModifiedById = 1,
            ModifiedByName = N'patch-edms-review-approve-replysheet',
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi
        WHERE Name = @Name;
    END

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT
    q.Name,
    ISJSON(q.QueryJson) AS IsJson,
    CASE WHEN CHARINDEX(N'[t1].[Status] = 28', q.QueryJson) > 0 THEN 1 ELSE 0 END AS ReplaySheet,
    CASE WHEN CHARINDEX(N'[t1].[Status] = 2 AND', q.QueryJson) > 0 THEN 1 ELSE 0 END AS Issue,
    CASE WHEN CHARINDEX(N'[t1].[Status] = 4 AND', q.QueryJson) > 0 THEN 1 ELSE 0 END AS ApproveByReviewer,
    CASE WHEN CHARINDEX(N'[t1].[Status] = 30', q.QueryJson) > 0 THEN 1 ELSE 0 END AS IssueForClient,
    CASE WHEN CHARINDEX(N'[t1].[Status] = 13', q.QueryJson) > 0 THEN 1 ELSE 0 END AS ApproveByClient,
    CASE WHEN CHARINDEX(N'[t1].[Status] <> 5', q.QueryJson) > 0 THEN 1 ELSE 0 END AS ExcludesCommentedByReviewer
FROM system.SavedQuery q
WHERE q.Name = N'Edms_Document_ReviewApproveCartable';
