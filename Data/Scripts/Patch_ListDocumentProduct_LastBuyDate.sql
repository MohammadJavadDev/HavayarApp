/*
  Patch_ListDocumentProduct_LastBuyDate.sql
  ستون «تاریخ آخرین خرید» روی نمایه‌های دارای قیمت مدارک محصولات.
  معادل HTS: Vw_Bom_PartLastPrice.CreatedDate (تاریخ شمسی همان ردیف آخرین قیمت، بدون ساعت).
  در سیستم جدید: ده رقم اول CreatedOnShamsiDateTime همان استعلام آخرین قیمت.
  UTF-8 BOM. Idempotent.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
BEGIN TRY

    DECLARE @DateCol NVARCHAR(MAX) = N'{"TableName":null,"ColumnName":"LastBuyDate","DisplayName":"تاریخ آخرین خرید","Alliance":"LastBuyDate","Address":null,"SystemTypeName":"DateShamsi","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":5,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":null,"IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":120,"Filterable":true,"Sortable":true,"ClassName":""},';
    DECLARE @PriceCol NVARCHAR(MAX) = N'{"TableName":null,"ColumnName":"LastBuyPrice","DisplayName":"قیمت آخرین خرید","Alliance":"LastBuyPrice","Address":null,"SystemTypeName":"Decimal","SelectIndex":0,"Visible":true,"PrimaryKey":false,"SystemType":14,"SortDirection":null,"SortOrder":null,"GroupBy":false,"Options":[],"OptionSetting":null,"Aggregate":null,"Render":"function(data, type, row) { if (data == null || data === '''') return ''''; if (typeof MJUtil !== ''undefined'' && MJUtil.formatMoney) return MJUtil.formatMoney(data); return data; }","IsCustom":false,"CustomColType":null,"HtmlTemplate":null,"BtnConfig":null,"InputConfig":null,"Width":140,"Filterable":true,"Sortable":true,"ClassName":""},';
    DECLARE @Marker NVARCHAR(200) = N'{"TableName":null,"ColumnName":"PartExteraInfoTypeName"';

    DECLARE @Names TABLE (Name NVARCHAR(200) NOT NULL PRIMARY KEY, Kind NVARCHAR(20) NOT NULL);
    INSERT INTO @Names (Name, Kind) VALUES
        (N'vw_ListDocumentProductPricenew', N'product'),
        (N'vw_ListDocumentProductPriceSale', N'product'),
        (N'vw_ListDocumentProductPrice', N'product'),
        (N'ListDocumentProduct_Bom_Buy', N'bom'),
        (N'ListDocumentProduct_Bom_Sale', N'bom'),
        (N'vw_ListDocumentProductPriceBom', N'bom');

    DECLARE @Name NVARCHAR(200), @Kind NVARCHAR(20), @Query NVARCHAR(MAX), @Cols NVARCHAR(MAX);
    DECLARE @Nl NCHAR(2), @Pos INT;

    DECLARE ldp CURSOR LOCAL FAST_FORWARD FOR
        SELECT n.Name, n.Kind, j.CustomQuery, q.ColumnsJson
        FROM @Names AS n
        INNER JOIN system.SavedQuery AS q ON q.Name = n.Name
        CROSS APPLY OPENJSON(q.QueryJson) WITH (CustomQuery NVARCHAR(MAX) '$.CustomQuery') AS j;

    OPEN ldp;
    FETCH NEXT FROM ldp INTO @Name, @Kind, @Query, @Cols;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF @Query NOT LIKE N'%AS LastBuyDate%'
        BEGIN
            IF CHARINDEX(CHAR(13) + CHAR(10), @Query) > 0
                SET @Nl = CHAR(13) + CHAR(10);
            ELSE
                SET @Nl = CHAR(10);

            IF @Kind = N'product'
            BEGIN
                IF CHARINDEX(N'SELECT price.PartId, price.UnitPrice, price.CreatedOnShamsiDateTime, ROW_NUMBER()', @Query) = 0
                BEGIN
                    IF CHARINDEX(N'SELECT price.PartId, price.UnitPrice, ROW_NUMBER()', @Query) = 0
                        THROW 50011, 'product last-buy cte not found', 1;
                    SET @Query = REPLACE(
                        @Query,
                        N'SELECT price.PartId, price.UnitPrice, ROW_NUMBER()',
                        N'SELECT price.PartId, price.UnitPrice, price.CreatedOnShamsiDateTime, ROW_NUMBER()');
                END

                IF CHARINDEX(N'lastBuy.UnitPrice AS LastBuyPrice,', @Query) = 0
                BEGIN
                    IF CHARINDEX(N'base.ProductId AS PartId,', @Query) = 0
                        THROW 50012, 'product PartId select not found', 1;
                    SET @Query = REPLACE(
                        @Query,
                        N'base.ProductId AS PartId,',
                        N'base.ProductId AS PartId,' + @Nl
                        + N'    lastBuy.UnitPrice AS LastBuyPrice,' + @Nl
                        + N'    LEFT(lastBuy.CreatedOnShamsiDateTime, 10) AS LastBuyDate,');
                END
                ELSE IF CHARINDEX(N'AS LastBuyDate', @Query) = 0
                BEGIN
                    SET @Query = REPLACE(
                        @Query,
                        N'lastBuy.UnitPrice AS LastBuyPrice,',
                        N'lastBuy.UnitPrice AS LastBuyPrice,' + @Nl + N'    LEFT(lastBuy.CreatedOnShamsiDateTime, 10) AS LastBuyDate,');
                END
            END
            ELSE
            BEGIN
                IF CHARINDEX(N'ipp.CreatedOnMiladiDateTime,', @Query) = 0
                    THROW 50013, 'bom lastPrice date not found', 1;
                SET @Query = REPLACE(
                    @Query,
                    N'ipp.CreatedOnMiladiDateTime,',
                    N'ipp.CreatedOnMiladiDateTime,' + @Nl + N'        ipp.CreatedOnShamsiDateTime,');

                IF CHARINDEX(N'lp.CreatedOnMiladiDateTime AS InternalPriceDate', @Query) = 0
                    THROW 50014, 'bom InternalPriceDate not found', 1;
                SET @Query = REPLACE(
                    @Query,
                    N'lp.CreatedOnMiladiDateTime AS InternalPriceDate',
                    N'lp.CreatedOnMiladiDateTime AS InternalPriceDate,' + @Nl + N'        lp.CreatedOnShamsiDateTime AS LastBuyShamsi');

                IF CHARINDEX(N'bom.InternalUnitPrice AS LastBuyPrice,', @Query) = 0
                    THROW 50015, 'bom LastBuyPrice select not found', 1;
                SET @Query = REPLACE(
                    @Query,
                    N'bom.InternalUnitPrice AS LastBuyPrice,',
                    N'bom.InternalUnitPrice AS LastBuyPrice,' + @Nl + N'    LEFT(bom.LastBuyShamsi, 10) AS LastBuyDate,');
            END
        END

        SET @Pos = CHARINDEX(@Marker, @Cols);
        IF @Pos = 0
            THROW 50016, 'columns marker not found', 1;
        IF @Cols NOT LIKE N'%"Alliance":"LastBuyPrice"%'
            SET @Cols = STUFF(@Cols, @Pos, 0, @PriceCol);
        SET @Pos = CHARINDEX(@Marker, @Cols);
        IF @Cols NOT LIKE N'%"Alliance":"LastBuyDate"%'
            SET @Cols = STUFF(@Cols, @Pos, 0, @DateCol);

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
