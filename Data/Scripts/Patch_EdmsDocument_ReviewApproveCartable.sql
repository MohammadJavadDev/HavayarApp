/*
  کارتابل بررسی و تایید (Edms_Document_ReviewApproveCartable):
  - وضعیت Issue (۲) برای تاییدکننده همان VPIS هم دیده شود، نه فقط پروژه وندوری.
  - وضعیت کامنت بررسی‌کننده (۵) از این کارتابل خارج شود؛ تسک تهیه‌کننده است.
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
    DECLARE @Old NVARCHAR(300) = N'ISNULL([t2].[ProjectIsVendoriType], 0) = 1 AND [t1].[Status] IN (2, 5) AND [t3].[ApproverId] = @CuId';
    DECLARE @New NVARCHAR(120) = N'[t1].[Status] = 2 AND [t3].[ApproverId] = @CuId';
    DECLARE @Latest NVARCHAR(40) = N'[t1].[IsLatest] = 1';
    DECLARE @LatestNew NVARCHAR(70) = N'[t1].[IsLatest] = 1 AND [t1].[Status] <> 5';

    IF NOT EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
        THROW 51646, N'نمایه کارتابل بررسی و تایید یافت نشد.', 1;

    UPDATE system.SavedQuery
    SET QueryJson = REPLACE(QueryJson, @Old, @New),
        ModifiedById = 1,
        ModifiedByName = N'patch-edms-review-approve-cartable',
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = @Name
      AND CHARINDEX(@Old, QueryJson) > 0;

    UPDATE system.SavedQuery
    SET QueryJson = REPLACE(QueryJson, @Latest, @LatestNew),
        ModifiedById = 1,
        ModifiedByName = N'patch-edms-review-approve-cartable',
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = @Name
      AND CHARINDEX(@LatestNew, QueryJson) = 0
      AND CHARINDEX(@Latest, QueryJson) > 0;

    IF ISJSON((SELECT QueryJson FROM system.SavedQuery WHERE Name = @Name)) <> 1
        THROW 51647, N'QueryJson نمایه کارتابل بررسی و تایید بعد از اصلاح معتبر نیست.', 1;

    IF CHARINDEX(N'Status] IN (2, 5)', (SELECT QueryJson FROM system.SavedQuery WHERE Name = @Name)) > 0
        THROW 51648, N'شرط وضعیت ۵ هنوز در کارتابل بررسی و تایید مانده است.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT
    q.Name,
    CASE WHEN CHARINDEX(N'[t1].[Status] = 2 AND [t3].[ApproverId] = @CuId', q.QueryJson) > 0 THEN 1 ELSE 0 END AS IssueForApprover,
    CASE WHEN CHARINDEX(N'[t1].[Status] <> 5', q.QueryJson) > 0 THEN 1 ELSE 0 END AS ExcludesReviewerComment,
    CASE WHEN CHARINDEX(N'Status] IN (2, 5)', q.QueryJson) > 0 THEN 1 ELSE 0 END AS StillIncludesCommented
FROM system.SavedQuery q
WHERE q.Name = N'Edms_Document_ReviewApproveCartable';
