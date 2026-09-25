/*
================================================================================
Backfill_OpenOrderRequestEmailToSupplier_AttachmentIds.sql
================================================================================
لینک شناسه فایل‌های از قبل مهاجرت‌شده به ستون‌های پیوست EmailToSupplier:
  Attachment_FK            → OpenOrderRequestAttachment.HtsId → AttachmentId (FileEntity)
  PartAttachmentId         → PartDocument.HtsId → AttachmentId
  EdmsDocumentAttachmentId → Document.HtsId → MainFileId
فایل‌های جاافتاده را جاب OpenOrderRequestEmailAttachmentJob با UploadAsync پر می‌کند.

پیش‌نیاز:
  - ستون‌های AttachmentId / PartAttachmentId / EdmsDocumentAttachmentId (migration)
  - برای Part: ابتدا Backfill_PartDocument_HtsId_FromInvPartAttachment.sql
    (PartDocument.HtsId = Inv_Part_Attachment.Part_Attachment_ID)

UTF-8 with BOM؛ sqlcmd -f 65001
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'Sup.OpenOrderRequestEmailToSupplier', N'AttachmentId') IS NULL
BEGIN
    RAISERROR(N'ستون AttachmentId وجود ندارد. ابتدا migration را اعمال کنید.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] وجود ندارد.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Att INT = 0, @Part INT = 0, @Edms INT = 0;

    UPDATE e
    SET e.AttachmentId = a.AttachmentId
    FROM Sup.OpenOrderRequestEmailToSupplier e
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_EmailToSupplier] h
        ON h.OpenOrderRequest_EmailToSupplier_ID = e.HtsId
    INNER JOIN Sup.OpenOrderRequestAttachment a
        ON a.HtsId = h.Attachment_FK
       AND a.AttachmentId IS NOT NULL
    WHERE e.AttachmentId IS NULL
      AND h.Attachment_FK IS NOT NULL;

    SET @Att = @@ROWCOUNT;

    UPDATE e
    SET e.PartAttachmentId = pd.AttachmentId
    FROM Sup.OpenOrderRequestEmailToSupplier e
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_EmailToSupplier] h
        ON h.OpenOrderRequest_EmailToSupplier_ID = e.HtsId
    INNER JOIN Inv.PartDocument pd
        ON pd.HtsId = h.PartAttachmentId
       AND pd.AttachmentId > 0
    WHERE e.PartAttachmentId IS NULL
      AND h.PartAttachmentId IS NOT NULL;

    SET @Part = @@ROWCOUNT;

    UPDATE e
    SET e.EdmsDocumentAttachmentId = d.MainFileId
    FROM Sup.OpenOrderRequestEmailToSupplier e
    INNER JOIN [TMS].[TotalSystem].[dbo].[Sup_OpenOrderRequest_EmailToSupplier] h
        ON h.OpenOrderRequest_EmailToSupplier_ID = e.HtsId
    INNER JOIN Edms.Document d
        ON d.HtsId = h.EdmsDocumentAttachmentId
       AND d.MainFileId IS NOT NULL
    WHERE e.EdmsDocumentAttachmentId IS NULL
      AND h.EdmsDocumentAttachmentId IS NOT NULL;

    SET @Edms = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT
        @Att AS LinkedOpenOrderAttachment,
        @Part AS LinkedPartAttachment,
        @Edms AS LinkedEdmsAttachment,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequestEmailToSupplier WHERE AttachmentId IS NOT NULL) AS WithAtt,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequestEmailToSupplier WHERE PartAttachmentId IS NOT NULL) AS WithPart,
        (SELECT COUNT(*) FROM Sup.OpenOrderRequestEmailToSupplier WHERE EdmsDocumentAttachmentId IS NOT NULL) AS WithEdms;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
