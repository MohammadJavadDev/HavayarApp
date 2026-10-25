/*
  مدارک پروژه‌های خاتمه‌یافته (۲) و متوقف‌شده (۴) از همه کارتابل‌های مدرک خارج می‌شوند.
  فهرست مدارک (Edms_Document_AllRevisions) همه مدارک را نگه می‌دارد.
  UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @Clause NVARCHAR(80) = N' AND ISNULL([t2].[Status], -1) NOT IN (2, 4)';

    UPDATE system.SavedQuery
    SET QueryJson = JSON_MODIFY(
            QueryJson,
            '$.CustomQuery',
            JSON_VALUE(QueryJson, '$.CustomQuery') + @Clause
        ),
        ModifiedById = 1,
        ModifiedByName = N'patch-edms-cartable-closed-projects',
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name IN (
        N'Edms_Document_ClientReceive',
        N'Edms_Document_ProducerCartable',
        N'Edms_Document_ReviewApproveCartable',
        N'Edms_Document_DccApproveCartable',
        N'Edms_Document_ReadyTransmittal',
        N'Edms_Document_ReplySheet',
        N'Edms_Document_EngineeringManagerReviewCartable'
    )
      AND CHARINDEX(N'NOT IN (2, 4)', ISNULL(JSON_VALUE(QueryJson, '$.CustomQuery'), N'')) = 0;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery
        WHERE Name = N'Edms_Document_AllRevisions'
          AND CHARINDEX(N'NOT IN (2, 4)', ISNULL(JSON_VALUE(QueryJson, '$.CustomQuery'), N'')) > 0
    )
        THROW 51649, N'فهرست مدارک نباید فیلتر پروژه خاتمه‌یافته داشته باشد.', 1;

    IF EXISTS (
        SELECT 1
        FROM system.SavedQuery
        WHERE Name IN (
            N'Edms_Document_ClientReceive',
            N'Edms_Document_ProducerCartable',
            N'Edms_Document_ReviewApproveCartable',
            N'Edms_Document_DccApproveCartable',
            N'Edms_Document_ReadyTransmittal',
            N'Edms_Document_ReplySheet',
            N'Edms_Document_EngineeringManagerReviewCartable'
        )
          AND (
              ISJSON(QueryJson) <> 1
              OR CHARINDEX(N'NOT IN (2, 4)', ISNULL(JSON_VALUE(QueryJson, '$.CustomQuery'), N'')) = 0
          )
    )
        THROW 51650, N'فیلتر پروژه خاتمه‌یافته روی یکی از کارتابل‌ها اعمال نشد.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT Name, Title,
    CASE WHEN CHARINDEX(N'NOT IN (2, 4)', ISNULL(JSON_VALUE(QueryJson, '$.CustomQuery'), N'')) > 0 THEN 1 ELSE 0 END AS ExcludesClosed
FROM system.SavedQuery
WHERE Name LIKE N'Edms_Document_%'
ORDER BY Id;
