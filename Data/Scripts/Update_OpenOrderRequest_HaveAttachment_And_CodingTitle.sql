/*
حاوی پیوست: مدرک اصلی کالا نوع 264/265/271 هم حساب شود.
عنوان کدینگ: ستون Inv.Part.CodingTitle و کپی یکباره از HTS، به‌علاوه ستون گرید.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'Inv.Part', N'CodingTitle') IS NULL
    ALTER TABLE [Inv].[Part] ADD [CodingTitle] NVARCHAR(128) NULL;
GO

UPDATE p
SET CodingTitle = NULLIF(LTRIM(RTRIM(h.CodingTitle)), N'')
FROM [Inv].[Part] AS p
INNER JOIN [TMS].[TotalSystem].[dbo].[Inv_Part] AS h
    ON h.Part_ID = p.HtsId
WHERE p.HtsId <> 0
  AND (p.CodingTitle IS NULL OR LTRIM(RTRIM(p.CodingTitle)) = N'')
  AND NULLIF(LTRIM(RTRIM(h.CodingTitle)), N'') IS NOT NULL;

PRINT N'CodingTitle rows filled: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));

DECLARE @OldAtt nvarchar(max) = N'OR EXISTS (SELECT 1 FROM [Sup].[OpenOrderRequestVpis] AS [v] WHERE [v].[OpenOrderRequestId] = [t1].[Id])';
DECLARE @NewAtt nvarchar(max) = N'OR EXISTS (SELECT 1 FROM [Sup].[OpenOrderRequestVpis] AS [v] WHERE [v].[OpenOrderRequestId] = [t1].[Id])\r\n                       OR EXISTS (SELECT 1 FROM [Inv].[PartDocument] AS [pd] WHERE [pd].[PartId] = [t1].[PartId] AND [pd].[Main] = 1 AND [pd].[Type] IN (264, 265, 271))';

DECLARE @OldName nvarchar(200) = N'[t2].[Name] AS [t2_Name],';
DECLARE @NewName nvarchar(400) = N'[t2].[Name] AS [t2_Name],\r\n    [t2].[CodingTitle] AS [t2_CodingTitle],';

DECLARE @Col nvarchar(max) = N',{"TableName":"Inv.Part","ColumnName":"t2_CodingTitle","DisplayName":"عنوان کدینگ","Alliance":"t2_CodingTitle","Address":"[t2].[CodingTitle]","SystemTypeName":"String","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":0,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":220,"Filterable":true,"Sortable":true,"ClassName":""}';

UPDATE q
SET QueryJson =
        CASE
            WHEN CHARINDEX(N'[Inv].[PartDocument]', q.QueryJson) = 0 THEN REPLACE(
                CASE
                    WHEN CHARINDEX(N'[t2].[CodingTitle]', q.QueryJson) > 0 THEN q.QueryJson
                    ELSE REPLACE(q.QueryJson, @OldName, @NewName)
                END,
                @OldAtt, @NewAtt)
            WHEN CHARINDEX(N'[t2].[CodingTitle]', q.QueryJson) = 0 THEN REPLACE(q.QueryJson, @OldName, @NewName)
            ELSE q.QueryJson
        END,
    ColumnsJson = CASE
        WHEN CHARINDEX(N't2_CodingTitle', q.ColumnsJson) > 0 THEN q.ColumnsJson
        WHEN q.Name = N'vw_openRequestConfig' THEN LEFT(q.ColumnsJson, LEN(q.ColumnsJson) - 1) + REPLACE(@Col, N'"Visible":true', N'"Visible":false') + N']'
        ELSE LEFT(q.ColumnsJson, LEN(q.ColumnsJson) - 1) + @Col + N']'
    END,
    ModifiedDateMiladiDateTime = GETDATE()
FROM system.SavedQuery AS q
WHERE CHARINDEX(N'HaveAttachment', q.QueryJson) > 0
  AND CHARINDEX(@OldAtt, q.QueryJson) > 0;

PRINT N'Profiles updated: ' + CAST(@@ROWCOUNT AS NVARCHAR(20));
