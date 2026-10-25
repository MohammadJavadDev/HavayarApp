/*
  نمایه «ریپلای شیت»: مدارکی که فایل ریپلای شیت خالی است و فقط فایل ریپلای شیت دوم دارند،
  تا وقتی ApproveByApprover (۷) هستند در این نما می‌مانند.
  با ApprovedByDcc (۹) از این نما خارج می‌شوند و در کارتابل آماده ترانسمیتال دیده می‌شوند.
  ذخیره ترانسمیتال وضعیت را NotReview می‌کند.
  کارتابل تایید DCC این ردیف‌ها را نشان نمی‌دهد تا با ارسال رویژن جدید قاطی نشوند.
  UTF-8 with BOM. sqlcmd -f 65001
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY
    DECLARE @Now DATETIME2 = GETDATE();
    DECLARE @NowShamsi NVARCHAR(30) = CONVERT(NVARCHAR(30), @Now, 120);
    DECLARE @SeedUser NVARCHAR(150) = N'seed-edms-replysheet-profile';
    DECLARE @Exclusion NVARCHAR(300) = N' AND NOT ([t1].[ReplySheetId] IS NULL AND [t1].[SecondReplySheetId] IS NOT NULL)';
    DECLARE @DccWhere NVARCHAR(200) = N'WHERE [t1].[IsLatest] = 1 AND [t1].[Status] = 7';
    DECLARE @ReplyOld NVARCHAR(120) = N'AND [t1].[Status] IN (7, 9)';
    DECLARE @ReplyNew NVARCHAR(80) = N'AND [t1].[Status] = 7';
    DECLARE @ReadyWhere NVARCHAR(200) = N'WHERE [t1].[IsLatest] = 1 AND [t1].[Status] = 9';
    DECLARE @ReplyWhere NVARCHAR(400) = N'WHERE [t1].[IsLatest] = 1 AND [t1].[ReplySheetId] IS NULL AND [t1].[SecondReplySheetId] IS NOT NULL AND [t1].[Status] = 7';
    DECLARE @Name NVARCHAR(100) = N'Edms_Document_ReplySheet';
    DECLARE @Title NVARCHAR(200) = N'ریپلای شیت';
    DECLARE @Pid BIGINT;
    DECLARE @ReadyId BIGINT;
    DECLARE @Qj NVARCHAR(MAX);
    DECLARE @Btn NVARCHAR(MAX);

    UPDATE system.SavedQuery
    SET QueryJson = REPLACE(QueryJson, @Exclusion, N''),
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'Edms_Document_ReadyTransmittal'
      AND CHARINDEX(@Exclusion, QueryJson) > 0;

    UPDATE system.SavedQuery
    SET QueryJson = REPLACE(QueryJson, @DccWhere, @DccWhere + @Exclusion),
        ModifiedByName = @SeedUser,
        ModifiedDateMiladiDateTime = @Now,
        ModifiedDateShamsiDateTime = @NowShamsi
    WHERE Name = N'Edms_Document_DccApproveCartable'
      AND CHARINDEX(N'[t1].[ReplySheetId] IS NULL', QueryJson) = 0;

    SELECT @ReadyId = Id FROM system.SavedQuery WHERE Name = N'Edms_Document_ReadyTransmittal';
    IF @ReadyId IS NULL
        THROW 51641, N'Edms_Document_ReadyTransmittal was not found.', 1;

    IF EXISTS (SELECT 1 FROM system.SavedQuery WHERE Name = @Name)
    BEGIN
        UPDATE system.SavedQuery
        SET QueryJson = REPLACE(QueryJson, @ReplyOld, @ReplyNew),
            ModifiedById = 1,
            ModifiedByName = @SeedUser,
            ModifiedDateMiladiDateTime = @Now,
            ModifiedDateShamsiDateTime = @NowShamsi,
            IsActive = 1
        WHERE Name = @Name
          AND CHARINDEX(@ReplyOld, QueryJson) > 0;

        SELECT @Pid = Id, @Qj = QueryJson FROM system.SavedQuery WHERE Name = @Name;
    END
    ELSE
    BEGIN
        SELECT @Qj = QueryJson, @Btn = CustomActionButtonsJson FROM system.SavedQuery WHERE Id = @ReadyId;
        SET @Qj = REPLACE(@Qj, @ReadyWhere, @ReplyWhere);

        IF ISJSON(@Qj) <> 1
            THROW 51641, N'ReplySheet profile JSON is invalid.', 1;

        INSERT INTO system.SavedQuery
            (Name, Title, QueryJson, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        SELECT @Name, @Title, @Qj, ColumnsJson, DiagramJson, CustomActionButtonsJson, EventScriptsJson,
             Mode, Type, EntityFullName, ActionOptions,
             1, 1, @SeedUser, @SeedUser,
             @Now, @NowShamsi, @Now, @NowShamsi, 1
        FROM system.SavedQuery
        WHERE Id = @ReadyId;
        SET @Pid = SCOPE_IDENTITY();

        DELETE FROM system.RoleAccess WHERE ActionAccessType = 3 AND RowId = @Pid;
        INSERT INTO system.RoleAccess
            (Path, ActionAccessType, ActionAccessItemType, EntityName, DisplayName, EntityId, RowId, RoleId,
             CreatedById, ModifiedById, CreatedByName, ModifiedByName,
             CreatedOnMiladiDateTime, CreatedOnShamsiDateTime, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
        SELECT N'dataProfile_' + CAST(@Pid AS nvarchar(20)), ra.ActionAccessType, ra.ActionAccessItemType, ra.EntityName, @Title, NULL, @Pid, ra.RoleId,
            1, 1, @SeedUser, @SeedUser, @Now, @NowShamsi, @Now, @NowShamsi, 1
        FROM system.RoleAccess ra
        WHERE ra.ActionAccessType = 3 AND ra.RowId = @ReadyId;
    END

    IF CHARINDEX(N'[t1].[Status] = 7', @Qj) = 0 OR CHARINDEX(N'[t1].[Status] IN (7, 9)', @Qj) > 0
        THROW 51641, N'ReplySheet profile still includes ApprovedByDcc.', 1;

    IF EXISTS (
        SELECT 1 FROM system.SavedQuery
        WHERE Name = N'Edms_Document_ReadyTransmittal'
          AND CHARINDEX(@Exclusion, QueryJson) > 0)
        THROW 51641, N'Ready transmittal still excludes reply sheet documents.', 1;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;

SELECT Name,
    CASE WHEN CHARINDEX(N'[t1].[Status] IN (7, 9)', QueryJson) > 0 THEN 1 ELSE 0 END AS IncludesApproved,
    CASE WHEN CHARINDEX(N'AND NOT ([t1].[ReplySheetId] IS NULL AND [t1].[SecondReplySheetId] IS NOT NULL)', QueryJson) > 0 THEN 1 ELSE 0 END AS ExcludesReplySheet
FROM system.SavedQuery
WHERE Name IN (N'Edms_Document_ReplySheet', N'Edms_Document_ReadyTransmittal', N'Edms_Document_DccApproveCartable');
