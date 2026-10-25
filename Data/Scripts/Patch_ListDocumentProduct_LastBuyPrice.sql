/*
  Patch_ListDocumentProduct_LastBuyPrice.sql
  ستون «قیمت آخرین خرید» روی نمایه‌های دارای قیمت مدارک محصولات.
  معادل HTS: Vw_Bom_PartLastPriceWithoutPriceList.UnitPrice
  (آخرین استعلام، نوع غیر از 1675، وضعیت غیر از 2834، ریال برای داخلی و یورو برای خارجی).
  UTF-8 BOM. Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @Col NVARCHAR(MAX) = N'{"TableName":null,"ColumnName":"LastBuyPrice","DisplayName":"قیمت آخرین خرید","Alliance":"LastBuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},';
    DECLARE @Marker NVARCHAR(200) = N'{"TableName":null,"ColumnName":"PartExteraInfoTypeName"';
    DECLARE @Cte NVARCHAR(MAX) = N'), ProductLastBuy AS (SELECT price.PartId, price.UnitPrice, ROW_NUMBER() OVER (PARTITION BY price.PartId ORDER BY price.CreatedOnMiladiDateTime DESC, price.Id DESC) AS RowNumber FROM Sup.InquiryPartPrice AS price INNER JOIN Inv.Part AS part ON part.Id = price.PartId WHERE price.Type <> 1675 AND ISNULL(price.PriceStatus, 1) <> 2834 AND price.PriceUnitId = CASE WHEN ISNULL(part.[Foreign], 0) = 0 THEN 1 ELSE 4 END )';

    DECLARE @Names TABLE (Name NVARCHAR(200) NOT NULL PRIMARY KEY, Kind NVARCHAR(20) NOT NULL);
    INSERT INTO @Names (Name, Kind) VALUES
        (N'vw_ListDocumentProductPricenew', N'product'),
        (N'vw_ListDocumentProductPriceSale', N'product'),
        (N'vw_ListDocumentProductPrice', N'product'),
        (N'ListDocumentProduct_Bom_Buy', N'bom'),
        (N'ListDocumentProduct_Bom_Sale', N'bom'),
        (N'vw_ListDocumentProductPriceBom', N'bom');

    DECLARE @Name NVARCHAR(200), @Kind NVARCHAR(20), @Query NVARCHAR(MAX), @Cols NVARCHAR(MAX);
    DECLARE @Nl NCHAR(2), @Anchor NVARCHAR(400), @Pos INT;

    DECLARE ldp CURSOR LOCAL FAST_FORWARD FOR
        SELECT n.Name, n.Kind, j.CustomQuery, q.ColumnsJson
        FROM @Names AS n
        INNER JOIN system.SavedQuery AS q ON q.Name = n.Name
        CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') AS j;

    OPEN ldp;
    FETCH NEXT FROM ldp INTO @Name, @Kind, @Query, @Cols;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @Query NOT LIKE N'%AS LastBuyPrice%'
        BEGIN
            IF CHARINDEX(CHAR(13) + CHAR(10), @Query) > 0
                SET @Nl = CHAR(13) + CHAR(10);
            ELSE
                SET @Nl = CHAR(10);

            IF @Kind = N'product'
            BEGIN
                SET @Anchor = N'FROM Bom.vw_ProductItem AS item' + @Nl + N')' + @Nl + N'--!--mainsection';
                IF CHARINDEX(@Anchor, @Query) = 0
                    THROW 50001, 'product anchor not found', 1;
                SET @Query = REPLACE(@Query, @Anchor, N'FROM Bom.vw_ProductItem AS item' + @Nl + @Cte + @Nl + N'--!--mainsection');

                IF CHARINDEX(N'base.ProductId AS PartId,', @Query) = 0
                    THROW 50002, 'product PartId select not found', 1;
                SET @Query = REPLACE(@Query, N'base.ProductId AS PartId,', N'base.ProductId AS PartId,' + @Nl + N'    lastBuy.UnitPrice AS LastBuyPrice,');

                IF CHARINDEX(N'LEFT JOIN FinalPrice AS price ON price.ProductFormulId = base.Id', @Query) = 0
                    THROW 50003, 'product FinalPrice join not found', 1;
                SET @Query = REPLACE(
                    @Query,
                    N'LEFT JOIN FinalPrice AS price ON price.ProductFormulId = base.Id',
                    N'LEFT JOIN FinalPrice AS price ON price.ProductFormulId = base.Id' + @Nl + N'LEFT JOIN ProductLastBuy AS lastBuy ON lastBuy.PartId = base.ProductId AND lastBuy.RowNumber = 1');
            END
            ELSE
            BEGIN
                IF CHARINDEX(N'ISNULL(BomUsingRate, 1) AS BomUsingRate,', @Query) = 0
                    THROW 50004, 'bom using-rate select not found', 1;
                SET @Query = REPLACE(
                    @Query,
                    N'ISNULL(BomUsingRate, 1) AS BomUsingRate,',
                    N'ISNULL(BomUsingRate, 1) AS BomUsingRate,' + @Nl + N'    bom.InternalUnitPrice AS LastBuyPrice,');
            END
        END

        IF @Cols NOT LIKE N'%"Alliance":"LastBuyPrice"%'
        BEGIN
            SET @Pos = CHARINDEX(@Marker, @Cols);
            IF @Pos = 0
                THROW 50005, 'columns marker not found', 1;
            SET @Cols = STUFF(@Cols, @Pos, 0, @Col);
        END

        UPDATE system.SavedQuery
        SET QueryJson = JSON_MODIFY(QueryJson, '$.CustomQuery', @Query),
            ColumnsJson = @Cols
        WHERE Name = @Name;

        FETCH NEXT FROM ldp INTO @Name, @Kind, @Query, @Cols;
    END
    CLOSE ldp;
    DEALLOCATE ldp;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    IF CURSOR_STATUS('local', 'ldp') >= -1
    BEGIN
        CLOSE ldp;
        DEALLOCATE ldp;
    END;
    THROW;
END CATCH;
