-- ============================================
-- مهاجرت Inv_CodingCategory / Inv_CodingRequest
-- از TotalSystem (Linked Server: TMS) به HavayarApp
--
-- نگاشت کاربر: Gnr_User.Username  ->  system.[User].Username
-- نگاشت کالا:   Inv_Part.Part_Code  ->  Inv.Part.Code
--
-- قبل از اجرا:
--   1) روی دیتابیس HavayarApp اجرا شود
--   2) Linked Server [TMS] به TotalSystem در دسترس باشد
--   3) جداول Inv.PartCodingCategory و Inv.PartCodingCategoryRequest ساخته شده باشند
--   4) در صورت نیاز نام schema جدول FileEntity را در بخش Attachment بررسی کنید
-- ============================================

SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    /* ─────────────────────────────────────────────
       0) User map (Old User_ID -> New User.Id)
       ───────────────────────────────────────────── */
    IF OBJECT_ID('tempdb..#UserMap') IS NOT NULL DROP TABLE #UserMap;

    SELECT
        CAST(ou.User_ID AS INT)              AS OldUserId,
        nu.Id                                AS NewUserId,
        nu.Name                              AS NewUserName,
        ou.Username                          AS Username
    INTO #UserMap
    FROM [TMS].[TotalSystem].[dbo].[Gnr_User] ou
    INNER JOIN [system].[User] nu
        ON nu.Username = ou.Username;

    /* ─────────────────────────────────────────────
       1) پاکسازی داده قبلی (ابتدا درخواست‌ها)
       ───────────────────────────────────────────── */
    DELETE FROM Inv.PartCodingCategoryRequest;
    DELETE FROM Inv.PartCodingCategory;

    /* ─────────────────────────────────────────────
       2) PartCodingCategory
       ───────────────────────────────────────────── */
    SET IDENTITY_INSERT Inv.PartCodingCategory ON;

    INSERT INTO Inv.PartCodingCategory
    (
        Id,
        ParentId,
        Code,
        Title,
        Atributies,
        Description,
        IsRequested,
        CreatedById,
        ModifiedById,
        CreatedByName,
        ModifiedByName,
        CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime,
        ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        CAST(c.Id AS BIGINT)                              AS Id,
        CAST(c.ParentId AS BIGINT)                        AS ParentId,
        c.Code,
        c.Title,
        c.Atributies,
        c.Description,
        c.IsRequested,
        um.NewUserId                                      AS CreatedById,
        NULL                                              AS ModifiedById,
        um.NewUserName                                    AS CreatedByName,
        NULL                                              AS ModifiedByName,
        c.CreatedDate                                     AS CreatedOnMiladiDateTime,
        NULL                                              AS CreatedOnShamsiDateTime,
        c.UpdatedDate                                     AS ModifiedDateMiladiDateTime,
        NULL                                              AS ModifiedDateShamsiDateTime,
        1                                                 AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Inv_CodingCategory] c
    LEFT JOIN #UserMap um
        ON um.OldUserId = c.CreatedUserId;

    SET IDENTITY_INSERT Inv.PartCodingCategory OFF;
    DBCC CHECKIDENT ('Inv.PartCodingCategory', RESEED) WITH NO_INFOMSGS;

    /* ─────────────────────────────────────────────
       3) FileEntity برای پیوست درخواست‌ها
          (جدول FileEntity در schema [system] است؛ در صورت تفاوت اصلاح کنید)
       ───────────────────────────────────────────── */
    IF OBJECT_ID('tempdb..#RequestAttachmentMap') IS NOT NULL DROP TABLE #RequestAttachmentMap;

    CREATE TABLE #RequestAttachmentMap
    (
        OldRequestId INT NOT NULL PRIMARY KEY,
        NewFileId    BIGINT NOT NULL
    );

    INSERT INTO [system].[FileEntity]
    (
        PhysicalPath,
        OriginalName,
        ContentType,
        Size,
        EntityType,
        EntityPropName,
        EntityId,
        CreatedById,
        CreatedByName,
        CreatedOnMiladiDateTime,
        IsActive
    )
    OUTPUT
        inserted.EntityId,
        inserted.Id
    INTO #RequestAttachmentMap (OldRequestId, NewFileId)
    SELECT
        COALESCE(NULLIF(LTRIM(RTRIM(r.AttachmentFilePath)), N''), N'migration/no-file') AS PhysicalPath,
        COALESCE(NULLIF(LTRIM(RTRIM(r.AttachmentFileName)), N''), N'migration-placeholder.pdf') AS OriginalName,
        CASE
            WHEN r.AttachmentFileName LIKE N'%.pdf' THEN N'application/pdf'
            WHEN r.AttachmentFileName LIKE N'%.zip' THEN N'application/zip'
            WHEN r.AttachmentFileName LIKE N'%.png' THEN N'image/png'
            WHEN r.AttachmentFileName LIKE N'%.jpg' OR r.AttachmentFileName LIKE N'%.jpeg' THEN N'image/jpeg'
            ELSE N'application/octet-stream'
        END AS ContentType,
        0 AS Size,
        N'Entities.App.Inv.PartCodingCategoryRequest' AS EntityType,
        N'Attachment' AS EntityPropName,
        CAST(r.Id AS BIGINT) AS EntityId,
        um.NewUserId,
        um.NewUserName,
        COALESCE(CAST(r.CreatedDate AS DATETIME), GETDATE()) AS CreatedOnMiladiDateTime,
        1 AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Inv_CodingRequest] r
    LEFT JOIN #UserMap um
        ON um.OldUserId = r.CreatedUserId;

    /* ─────────────────────────────────────────────
       4) PartCodingCategoryRequest
       ───────────────────────────────────────────── */
    SET IDENTITY_INSERT Inv.PartCodingCategoryRequest ON;

    INSERT INTO Inv.PartCodingCategoryRequest
    (
        Id,
        PartCodingCategoryId,
        Code,
        Title,
        Atributies,
        Description,
        CreatedPartId,
        AttachmentId,
        CreatedById,
        ModifiedById,
        CreatedByName,
        ModifiedByName,
        CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime,
        ModifiedDateShamsiDateTime,
        IsActive
    )
    SELECT
        CAST(r.Id AS BIGINT)                              AS Id,
        CAST(r.CodingCategoryId AS BIGINT)                AS PartCodingCategoryId,
        COALESCE(NULLIF(LTRIM(RTRIM(r.RequestedPartCode)), N''), r.RequestPartCode) AS Code,
        r.RequestPartName                                 AS Title,
        r.RequestAtributies                               AS Atributies,
        CONCAT(
            CASE
                WHEN NULLIF(LTRIM(RTRIM(r.RequestPartCode)), N'') IS NOT NULL
                    THEN N'[پیشوند کد: ' + r.RequestPartCode + N'] '
                ELSE N''
            END,
            COALESCE(r.Comment, N'')
        )                                                 AS Description,
        np.Id                                             AS CreatedPartId,
        ram.NewFileId                                     AS AttachmentId,
        umCreator.NewUserId                               AS CreatedById,
        umConfirmer.NewUserId                             AS ModifiedById,
        umCreator.NewUserName                             AS CreatedByName,
        umConfirmer.NewUserName                           AS ModifiedByName,
        COALESCE(CAST(r.CreatedDate AS DATETIME), GETDATE()) AS CreatedOnMiladiDateTime,
        NULLIF(LTRIM(RTRIM(r.CreatedDateInText)), N'')    AS CreatedOnShamsiDateTime,
        r.CompletionDate                                  AS ModifiedDateMiladiDateTime,
        NULLIF(LTRIM(RTRIM(r.CompletionDateInText)), N'') AS ModifiedDateShamsiDateTime,
        1                                                 AS IsActive
    FROM [TMS].[TotalSystem].[dbo].[Inv_CodingRequest] r
    INNER JOIN #RequestAttachmentMap ram
        ON ram.OldRequestId = r.Id
    LEFT JOIN #UserMap umCreator
        ON umCreator.OldUserId = r.CreatedUserId
    LEFT JOIN #UserMap umConfirmer
        ON umConfirmer.OldUserId = r.ConfirmerUserId
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] op
        ON op.Part_ID = r.CreatedPartId
    LEFT JOIN Inv.Part np
        ON np.Code = op.Part_Code;

    SET IDENTITY_INSERT Inv.PartCodingCategoryRequest OFF;
    DBCC CHECKIDENT ('Inv.PartCodingCategoryRequest', RESEED) WITH NO INFOMSGS;

    COMMIT TRANSACTION;

    PRINT N'مهاجرت PartCodingCategory / PartCodingCategoryRequest با موفقیت انجام شد.';

    /* گزارش موارد بدون نگاشت کاربر */
    SELECT
        r.Id              AS OldRequestId,
        r.CreatedUserId   AS OldCreatedUserId,
        ou.Username       AS OldUsername
    FROM [TMS].[TotalSystem].[dbo].[Inv_CodingRequest] r
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_User] ou
        ON ou.User_ID = r.CreatedUserId
    LEFT JOIN #UserMap um
        ON um.OldUserId = r.CreatedUserId
    WHERE um.NewUserId IS NULL;

    /* گزارش درخواست‌هایی که CreatedPartId نگاشت نشد */
    SELECT
        r.Id                AS OldRequestId,
        r.CreatedPartId     AS OldPartId,
        op.Part_Code        AS OldPartCode
    FROM [TMS].[TotalSystem].[dbo].[Inv_CodingRequest] r
    LEFT JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] op
        ON op.Part_ID = r.CreatedPartId
    LEFT JOIN Inv.Part np
        ON np.Code = op.Part_Code
    WHERE r.CreatedPartId IS NOT NULL
      AND np.Id IS NULL;

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
