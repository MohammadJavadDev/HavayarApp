/*
  QueryJson of the open-order profiles contains raw CR/LF (0x0D) inside the
  CustomQuery string. JSON requires those to be escaped. They were inserted
  when CodingTitle and the part-document clause were spliced in with real newlines.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

UPDATE q
SET QueryJson = REPLACE(REPLACE(REPLACE(q.QueryJson, CHAR(13) + CHAR(10), N'\r\n'), CHAR(13), N'\r'), CHAR(10), N'\n'),
    ModifiedDateMiladiDateTime = GETDATE()
FROM system.SavedQuery AS q
WHERE q.EntityFullName LIKE N'%OpenOrderRequest%'
  AND (CHARINDEX(CHAR(13), q.QueryJson) > 0 OR CHARINDEX(CHAR(10), q.QueryJson) > 0);

PRINT N'Profiles escaped: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

IF EXISTS (
    SELECT 1
    FROM system.SavedQuery AS q
    WHERE q.EntityFullName LIKE N'%OpenOrderRequest%'
      AND (
            ISJSON(q.QueryJson) <> 1
            OR CHARINDEX(CHAR(13), q.QueryJson) > 0
            OR CHARINDEX(CHAR(10), q.QueryJson) > 0
          )
)
    THROW 51041, N'Open-order QueryJson is still invalid.', 1;

DECLARE @BadCustom int = (
    SELECT COUNT(*)
    FROM system.SavedQuery AS q
    CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery nvarchar(max) '$.CustomQuery') AS j
    WHERE q.EntityFullName LIKE N'%OpenOrderRequest%'
      AND q.Name <> N'OpenOrderRequestEmailToSupplier_List'
      AND (
            j.CustomQuery IS NULL
            OR CHARINDEX(N'[t2].[CodingTitle]', j.CustomQuery) = 0
            OR CHARINDEX(N'[Inv].[PartDocument]', j.CustomQuery) = 0
          )
);

IF @BadCustom <> 0
    THROW 51042, N'CustomQuery lost CodingTitle or PartDocument.', 1;

PRINT N'QueryJson is valid.';
