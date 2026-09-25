/*
================================================================================
Backfill_PartDocument_HtsId_FromInvPartAttachment.sql
================================================================================
پر کردن Inv.PartDocument.HtsId از کلید واقعی HTS:
  PartDocument.HtsId = Inv_Part_Attachment.Part_Attachment_ID

نگاشت فقط وقتی یکتا باشد:
  Part.HtsId = Inv_Part.Part_ID
  و نام فایل FileEntity.OriginalName = Attachment_FileName

پیش‌نیاز: Linked Server [TMS]
UTF-8 with BOM؛ sqlcmd -f 65001
================================================================================
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (SELECT 1 FROM sys.servers WHERE name = N'TMS' AND is_linked = 1)
BEGIN
    RAISERROR(N'Linked Server [TMS] وجود ندارد.', 16, 1);
    RETURN;
END

BEGIN TRY
    BEGIN TRANSACTION;

    ;WITH matches AS (
        SELECT
            pd.Id AS DocId,
            h.Part_Attachment_ID AS HtsAttId,
            COUNT(*) OVER (PARTITION BY pd.Id) AS MatchCntPerDoc
        FROM Inv.PartDocument pd
        INNER JOIN Inv.Part p
            ON p.Id = pd.PartId
           AND p.HtsId <> 0
        INNER JOIN dbo.FileEntity f
            ON f.Id = pd.AttachmentId
        INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part_Attachment] h
            ON h.Part_FK = p.HtsId
           AND h.Attachment_FileName = f.OriginalName
        WHERE pd.HtsId = 0
          AND pd.AttachmentId > 0
    ),
    uniqueMatches AS (
        SELECT DocId, HtsAttId,
               ROW_NUMBER() OVER (PARTITION BY HtsAttId ORDER BY DocId) AS rnHts
        FROM matches
        WHERE MatchCntPerDoc = 1
    )
    UPDATE pd
    SET pd.HtsId = u.HtsAttId
    FROM Inv.PartDocument pd
    INNER JOIN uniqueMatches u ON u.DocId = pd.Id AND u.rnHts = 1
    WHERE pd.HtsId = 0
      AND NOT EXISTS (
          SELECT 1
          FROM Inv.PartDocument other
          WHERE other.HtsId = u.HtsAttId
            AND other.Id <> pd.Id
      );

    DECLARE @Updated INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT
        @Updated AS PartDocumentHtsIdUpdated,
        (SELECT COUNT(*) FROM Inv.PartDocument WHERE HtsId <> 0) AS PartDocWithHtsId,
        (SELECT COUNT(*) FROM Inv.PartDocument) AS PartDocTotal;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH
