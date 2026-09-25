using Common.Attributes;
using Data;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Data;
using System.Data.Common;

namespace App.BackgroundJob.Jobs.Bom;

public sealed class FormulaSyncJob(
	ApplicationDbContext db,
	ProductPriceSnapshotJob productPriceSnapshotJob)
{
	[JobHandler(
		"بازسازی کامل BOM محصول از HTS",
		"گروه‌ها و BOM محصول را پس از اعتبارسنجی منبع، با انفجار قطعات فرمول HTS به‌صورت تخت از TotalSystem بازسازی می‌کند.")]
	public async Task RebuildFormulasFromHts(
		IJobLogger? jobLogger = null,
		CancellationToken cancellationToken = default)
	{
		await jobLogger?.LogInfoAsync(
			"شروع اعتبارسنجی و بازسازی BOM محصول از HTS. قطعات داخل فرمول با ضریب ضرب‌شده روی BOM محصول می‌نشینند؛ تاریخچه قیمت و درخواست‌های تغییر حفظ می‌شوند.",
			cancellationToken)!;

		var connection = db.Database.GetDbConnection();
		var openedHere = connection.State != ConnectionState.Open;

		try
		{
			if (openedHere)
				await db.Database.OpenConnectionAsync(cancellationToken);

			FormulaSyncResult result;
			await using (var transaction = await connection.BeginTransactionAsync(cancellationToken))
			{
				try
				{
					await using var command = connection.CreateCommand();
					command.Transaction = transaction;
					command.CommandTimeout = 0;
					command.CommandText = RebuildSql;

					await using var reader = await command.ExecuteReaderAsync(cancellationToken);
					if (!await reader.ReadAsync(cancellationToken))
						throw new InvalidOperationException("نتیجه بازسازی BOM محصول از SQL دریافت نشد.");

					result = ReadResult(reader);
					await reader.CloseAsync();

					await transaction.CommitAsync(cancellationToken);
				}
				catch
				{
					await transaction.RollbackAsync(CancellationToken.None);
					throw;
				}
			}

			await jobLogger?.LogInfoAsync(
				$"بازسازی BOM محصول با موفقیت انجام شد: " +
				$"{result.ProductGroupCount:N0} گروه محصول، " +
				$"{result.ProductFormulCount:N0} BOM محصول و " +
				$"{result.ProductFormulItemCount:N0} قلم قطعه تخت‌شده.",
				cancellationToken)!;

			if (result.UnmappedProductCount > 0)
			{
				await jobLogger?.LogWarningAsync(
					$"{result.UnmappedProductCount:N0} BOM محصول به‌دلیل نبود Part متناظر در سیستم جدید منتقل نشد.",
					0,
					cancellationToken)!;
			}

			if (result.SkippedDirectPartCount > 0 || result.SkippedFormulaPartCount > 0)
			{
				await jobLogger?.LogWarningAsync(
					$"رد شده: {result.SkippedDirectPartCount:N0} قلم مستقیم بدون Part نگاشت‌شده و " +
					$"{result.SkippedFormulaPartCount:N0} قلم فرمول بدون Part نگاشت‌شده.",
					0,
					cancellationToken)!;
			}

			await jobLogger?.LogInfoAsync(
				"بازسازی BOM محصول Commit شد؛ اسنپ‌شات قیمت امروز با داده‌های جدید بازسازی می‌شود.",
				cancellationToken)!;

			await productPriceSnapshotJob.CaptureDailyProductPriceSnapshot(jobLogger, cancellationToken);
		}
		catch (Exception exception)
		{
			await jobLogger?.LogExceptionAsync(exception, CancellationToken.None)!;
			throw;
		}
		finally
		{
			if (openedHere && connection.State == ConnectionState.Open)
				await db.Database.CloseConnectionAsync();
		}
	}

	private static FormulaSyncResult ReadResult(DbDataReader reader) => new(
		ProductGroupCount: reader.GetInt64(reader.GetOrdinal("ProductGroupCount")),
		ProductFormulCount: reader.GetInt64(reader.GetOrdinal("ProductFormulCount")),
		ProductFormulItemCount: reader.GetInt64(reader.GetOrdinal("ProductFormulItemCount")),
		UnmappedProductCount: reader.GetInt64(reader.GetOrdinal("UnmappedProductCount")),
		SkippedDirectPartCount: reader.GetInt64(reader.GetOrdinal("SkippedDirectPartCount")),
		SkippedFormulaPartCount: reader.GetInt64(reader.GetOrdinal("SkippedFormulaPartCount")));

	private sealed record FormulaSyncResult(
		long ProductGroupCount,
		long ProductFormulCount,
		long ProductFormulItemCount,
		long UnmappedProductCount,
		long SkippedDirectPartCount,
		long SkippedFormulaPartCount);

	private const string RebuildSql = """
SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
    داده منبع پیش از هر حذف در temp tableها آماده می‌شود. در نتیجه اگر Linked Server،
    نگاشت Part یا یکپارچگی منبع مشکل داشته باشد، هیچ داده‌ای از سیستم جدید حذف نمی‌شود.

    فرمول HTS فقط برای انفجار قطعات خوانده می‌شود و در Bom.Formul* درج نمی‌شود.
    اقلام BOM محصول فقط قطعه هستند؛ ضریب = UsingRate قلم محصول × UsingRate قلم فرمول،
    سپس SUM روی (ProductFormulId, PartId) مطابق Vw_Bom_Product_Items.
*/

SELECT
    CAST(source.ProductGroup_ID AS BIGINT) AS Id,
    source.ProductGroup_Title AS Name
INTO #ProductGroup
FROM TMS.TotalSystem.dbo.Bom_ProductGroup AS source;

SELECT
    CAST(source.ProductFormul_ID AS BIGINT) AS Id,
    targetProduct.Id AS ProductId,
    CAST(source.ProductGroup_FK AS BIGINT) AS ProductNameGroupId,
    ISNULL(source.Comment, N'') AS Comment,
    NULLIF(LTRIM(RTRIM(source.CreatedDate)), '') AS CreatedOnShamsiDateTime
INTO #ProductFormul
FROM TMS.TotalSystem.dbo.Bom_ProductFormul AS source
INNER JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyProduct ON legacyProduct.Part_ID = source.Part_FK
INNER JOIN Inv.Part AS targetProduct ON targetProduct.Code = legacyProduct.Part_Code;

/* حفظ BOMهایی که تاریخچه قیمت دارند ولی دیگر از HTS نگاشت نمی‌شوند */
INSERT INTO #ProductFormul (Id, ProductId, ProductNameGroupId, Comment, CreatedOnShamsiDateTime)
SELECT
    pf.Id,
    pf.ProductId,
    pf.ProductNameGroupId,
    ISNULL(pf.Comment, N''),
    pf.CreatedOnShamsiDateTime
FROM Bom.ProductFormul AS pf
WHERE EXISTS (SELECT 1 FROM Bom.ProductPriceHistory h WHERE h.ProductFormulId = pf.Id)
  AND NOT EXISTS (SELECT 1 FROM #ProductFormul s WHERE s.Id = pf.Id);

INSERT INTO #ProductGroup (Id, Name)
SELECT pg.Id, pg.Name
FROM Bom.ProductGroup AS pg
WHERE EXISTS (
        SELECT 1
        FROM #ProductFormul pf
        WHERE pf.ProductNameGroupId = pg.Id
    )
  AND NOT EXISTS (SELECT 1 FROM #ProductGroup g WHERE g.Id = pg.Id);

/* قلم‌های خام تخت‌شده قبل از جمع */
SELECT
    ProductFormulId,
    PartId,
    UsingRate
INTO #ProductFormulItemRaw
FROM
(
    /* قلم مستقیم قطعه روی BOM محصول */
    SELECT
        CAST(source.ProductFormul_FK AS BIGINT) AS ProductFormulId,
        targetPart.Id AS PartId,
        ISNULL(source.UsingRate, 0) AS UsingRate
    FROM TMS.TotalSystem.dbo.Bom_ProductFormul_Itm AS source
    INNER JOIN #ProductFormul AS parent ON parent.Id = source.ProductFormul_FK
    INNER JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyPart ON legacyPart.Part_ID = source.Part_FK
    INNER JOIN Inv.Part AS targetPart ON targetPart.Code = legacyPart.Part_Code
    WHERE source.Part_FK IS NOT NULL

    UNION ALL

    /* انفجار اقلام فرمول با ضریب ضرب‌شده */
    SELECT
        CAST(source.ProductFormul_FK AS BIGINT) AS ProductFormulId,
        targetPart.Id AS PartId,
        ISNULL(source.UsingRate, 0) * ISNULL(formulaItem.UsingRate, 0) AS UsingRate
    FROM TMS.TotalSystem.dbo.Bom_ProductFormul_Itm AS source
    INNER JOIN #ProductFormul AS parent ON parent.Id = source.ProductFormul_FK
    INNER JOIN TMS.TotalSystem.dbo.Bom_Formul_Itm AS formulaItem
        ON formulaItem.Formul_Fk = source.Bom_Formul_FK
    INNER JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyPart ON legacyPart.Part_ID = formulaItem.Part_Fk
    INNER JOIN Inv.Part AS targetPart ON targetPart.Code = legacyPart.Part_Code
    WHERE source.Bom_Formul_FK IS NOT NULL
) AS exploded;

/* اقلام مستقیم محلی برای BOMهایی که فقط به‌خاطر تاریخچه قیمت حفظ شده‌اند */
INSERT INTO #ProductFormulItemRaw (ProductFormulId, PartId, UsingRate)
SELECT
    pfi.ProductFormulId,
    pfi.PartId,
    ISNULL(pfi.UsingRate, 0)
FROM Bom.ProductFormulItem AS pfi
WHERE pfi.PartId IS NOT NULL
  AND EXISTS (SELECT 1 FROM #ProductFormul pf WHERE pf.Id = pfi.ProductFormulId)
  AND NOT EXISTS (
        SELECT 1
        FROM TMS.TotalSystem.dbo.Bom_ProductFormul AS src
        WHERE CAST(src.ProductFormul_ID AS BIGINT) = pfi.ProductFormulId
    );

SELECT
    ProductFormulId,
    PartId,
    SUM(UsingRate) AS UsingRate
INTO #ProductFormulItem
FROM
(
    /* مطابق Vw_Bom_Product_Items که UNION (نه UNION ALL) است و ردیف‌های کاملاً یکسان را قبل از SUM حذف می‌کند */
    SELECT DISTINCT
        ProductFormulId,
        PartId,
        UsingRate
    FROM #ProductFormulItemRaw
) AS distinctRates
GROUP BY ProductFormulId, PartId;

DECLARE @UnmappedProductCount BIGINT =
(
    SELECT COUNT_BIG(*)
    FROM TMS.TotalSystem.dbo.Bom_ProductFormul AS source
    INNER JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyProduct ON legacyProduct.Part_ID = source.Part_FK
    LEFT JOIN Inv.Part AS targetProduct ON targetProduct.Code = legacyProduct.Part_Code
    WHERE targetProduct.Id IS NULL
);

DECLARE @SkippedDirectPartCount BIGINT =
(
    SELECT COUNT_BIG(*)
    FROM TMS.TotalSystem.dbo.Bom_ProductFormul_Itm AS source
    INNER JOIN #ProductFormul AS parent ON parent.Id = source.ProductFormul_FK
    LEFT JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyPart ON legacyPart.Part_ID = source.Part_FK
    LEFT JOIN Inv.Part AS targetPart ON targetPart.Code = legacyPart.Part_Code
    WHERE source.Part_FK IS NOT NULL
      AND targetPart.Id IS NULL
);

DECLARE @SkippedFormulaPartCount BIGINT =
(
    SELECT COUNT_BIG(*)
    FROM TMS.TotalSystem.dbo.Bom_ProductFormul_Itm AS source
    INNER JOIN #ProductFormul AS parent ON parent.Id = source.ProductFormul_FK
    INNER JOIN TMS.TotalSystem.dbo.Bom_Formul_Itm AS formulaItem
        ON formulaItem.Formul_Fk = source.Bom_Formul_FK
    LEFT JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyPart ON legacyPart.Part_ID = formulaItem.Part_Fk
    LEFT JOIN Inv.Part AS targetPart ON targetPart.Code = legacyPart.Part_Code
    WHERE source.Bom_Formul_FK IS NOT NULL
      AND targetPart.Id IS NULL
);

/* Preflight: هر خطا پیش از اولین DELETE رخ می‌دهد. */
IF EXISTS (SELECT Id FROM #ProductGroup GROUP BY Id HAVING COUNT_BIG(*) <> 1)
    THROW 51004, N'شناسه تکراری در گروه‌های محصول HTS یافت شد.', 1;

IF EXISTS (SELECT Id FROM #ProductFormul GROUP BY Id HAVING COUNT_BIG(*) <> 1)
    THROW 51005, N'شناسه تکراری در فرمول محصولات HTS یا نگاشت Product یافت شد.', 1;

IF EXISTS (SELECT ProductFormulId, PartId FROM #ProductFormulItem GROUP BY ProductFormulId, PartId HAVING COUNT_BIG(*) <> 1)
    THROW 51006, N'پس از جمع، جفت تکراری ProductFormulId/PartId در اقلام تخت‌شده باقی ماند.', 1;

IF EXISTS
(
    SELECT 1
    FROM #ProductFormul AS source
    LEFT JOIN #ProductGroup AS parent ON parent.Id = source.ProductNameGroupId
    WHERE source.ProductNameGroupId IS NOT NULL AND parent.Id IS NULL
)
    THROW 51008, N'گروه یکی از فرمول‌های محصول HTS در منبع وجود ندارد.', 1;

/*
    snapshotها باید حفظ شوند. اگر فرمول متناظر یکی از تاریخچه‌ها قابل بازسازی نباشد،
    عملیات به‌جای حذف تاریخچه متوقف می‌شود.
*/
IF EXISTS
(
    SELECT 1
    FROM Bom.ProductPriceHistory AS history
    LEFT JOIN #ProductFormul AS source ON source.Id = history.ProductFormulId
    WHERE source.Id IS NULL
)
    THROW 51009, N'حداقل یک فرمول دارای تاریخچه قیمت در داده قابل‌انتقال HTS وجود ندارد؛ عملیات متوقف شد.', 1;

/* حذف به ترتیب وابستگی FK — فقط BOM محصول؛ فرمول دیگر ذخیره نمی‌شود. */
ALTER TABLE Bom.ProductPriceHistory
    NOCHECK CONSTRAINT FK_ProductPriceHistory_ProductFormul_ProductFormulId;

DELETE FROM Bom.ProductFormulItem;
DELETE FROM Bom.ProductFormul;
DELETE FROM Bom.ProductGroup;

SET IDENTITY_INSERT Bom.ProductGroup ON;
BEGIN TRY
    INSERT INTO Bom.ProductGroup
    (
        Id, Name, CreatedByName, CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime, IsActive
    )
    SELECT Id, Name, N'HTS', NULL, NULL, 1
    FROM #ProductGroup;
END TRY
BEGIN CATCH
    SET IDENTITY_INSERT Bom.ProductGroup OFF;
    THROW;
END CATCH;
SET IDENTITY_INSERT Bom.ProductGroup OFF;

SET IDENTITY_INSERT Bom.ProductFormul ON;
BEGIN TRY
    INSERT INTO Bom.ProductFormul
    (
        Id, ProductId, ProductNameGroupId, Comment,
        CreatedByName, CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime, IsActive
    )
    SELECT
        Id, ProductId, ProductNameGroupId, Comment,
        N'HTS', NULL, CreatedOnShamsiDateTime, 1
    FROM #ProductFormul;
END TRY
BEGIN CATCH
    SET IDENTITY_INSERT Bom.ProductFormul OFF;
    THROW;
END CATCH;
SET IDENTITY_INSERT Bom.ProductFormul OFF;

/* شناسه اقلام identity جدید است؛ فقط PartId + UsingRate جمع‌شده. */
INSERT INTO Bom.ProductFormulItem
(
    ProductFormulId, PartId, UsingRate,
    CreatedByName, CreatedOnMiladiDateTime,
    CreatedOnShamsiDateTime, IsActive
)
SELECT
    ProductFormulId, PartId, UsingRate,
    N'HTS', NULL, NULL, 1
FROM #ProductFormulItem;

/* FK تاریخچه دوباره فعال و Trusted می‌شود؛ این مرحله کل تاریخچه را هم کنترل می‌کند. */
ALTER TABLE Bom.ProductPriceHistory WITH CHECK
    CHECK CONSTRAINT FK_ProductPriceHistory_ProductFormul_ProductFormulId;

DBCC CHECKIDENT ('Bom.ProductGroup', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Bom.ProductFormul', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Bom.ProductFormulItem', RESEED) WITH NO_INFOMSGS;

/* Postflight: شمارش هدف باید دقیقاً با staging برابر باشد. */
IF (SELECT COUNT_BIG(*) FROM Bom.ProductGroup) <> (SELECT COUNT_BIG(*) FROM #ProductGroup)
    THROW 51013, N'تعداد گروه‌های محصول درج‌شده با staging برابر نیست.', 1;

IF (SELECT COUNT_BIG(*) FROM Bom.ProductFormul) <> (SELECT COUNT_BIG(*) FROM #ProductFormul)
    THROW 51014, N'تعداد فرمول محصولات درج‌شده با staging برابر نیست.', 1;

IF (SELECT COUNT_BIG(*) FROM Bom.ProductFormulItem) <> (SELECT COUNT_BIG(*) FROM #ProductFormulItem)
    THROW 51015, N'تعداد اقلام BOM محصول درج‌شده با staging برابر نیست.', 1;

SELECT
    (SELECT COUNT_BIG(*) FROM #ProductGroup) AS ProductGroupCount,
    (SELECT COUNT_BIG(*) FROM #ProductFormul) AS ProductFormulCount,
    (SELECT COUNT_BIG(*) FROM #ProductFormulItem) AS ProductFormulItemCount,
    @UnmappedProductCount AS UnmappedProductCount,
    @SkippedDirectPartCount AS SkippedDirectPartCount,
    @SkippedFormulaPartCount AS SkippedFormulaPartCount;
""";
}
