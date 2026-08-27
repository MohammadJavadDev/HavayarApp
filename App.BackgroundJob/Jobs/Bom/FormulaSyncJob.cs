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
		"بازسازی کامل فرمول‌ها از HTS",
		"گروه‌ها، فرمول‌ها و اقلام BOM را پس از اعتبارسنجی منبع، به‌صورت تراکنشی از TotalSystem بازسازی می‌کند.")]
	public async Task RebuildFormulasFromHts(
		IJobLogger? jobLogger = null,
		CancellationToken cancellationToken = default)
	{
		await jobLogger?.LogInfoAsync(
			"شروع اعتبارسنجی و بازسازی کامل فرمول‌ها از HTS. تاریخچه قیمت و درخواست‌های تغییر فرمول حفظ می‌شوند.",
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
						throw new InvalidOperationException("نتیجه بازسازی فرمول‌ها از SQL دریافت نشد.");

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
				$"بازسازی فرمول‌ها با موفقیت انجام شد: " +
				$"{result.FormulGroupCount:N0} گروه فرمول، " +
				$"{result.FormulCount:N0} فرمول، " +
				$"{result.FormulItemCount:N0} قلم فرمول، " +
				$"{result.ProductGroupCount:N0} گروه محصول، " +
				$"{result.ProductFormulCount:N0} فرمول محصول و " +
				$"{result.ProductFormulItemCount:N0} قلم فرمول محصول.",
				cancellationToken)!;

			if (result.UnmappedProductCount > 0)
			{
				await jobLogger?.LogWarningAsync(
					$"{result.UnmappedProductCount:N0} فرمول محصول به‌دلیل نبود Part متناظر در سیستم جدید منتقل نشد.",
					0,
					cancellationToken)!;
			}

			if (result.UnmappedFormulItemPartCount > 0 || result.UnmappedProductFormulItemPartCount > 0)
			{
				await jobLogger?.LogWarningAsync(
					$"Part متناظر برای {result.UnmappedFormulItemPartCount:N0} قلم فرمول و " +
					$"{result.UnmappedProductFormulItemPartCount:N0} قلم فرمول محصول یافت نشد؛ PartId این اقلام NULL ثبت شد.",
					0,
					cancellationToken)!;
			}

			await jobLogger?.LogInfoAsync(
				"بازسازی فرمول‌ها Commit شد؛ اسنپ‌شات قیمت امروز با داده‌های جدید بازسازی می‌شود.",
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
		FormulGroupCount: reader.GetInt64(reader.GetOrdinal("FormulGroupCount")),
		FormulCount: reader.GetInt64(reader.GetOrdinal("FormulCount")),
		FormulItemCount: reader.GetInt64(reader.GetOrdinal("FormulItemCount")),
		ProductGroupCount: reader.GetInt64(reader.GetOrdinal("ProductGroupCount")),
		ProductFormulCount: reader.GetInt64(reader.GetOrdinal("ProductFormulCount")),
		ProductFormulItemCount: reader.GetInt64(reader.GetOrdinal("ProductFormulItemCount")),
		UnmappedProductCount: reader.GetInt64(reader.GetOrdinal("UnmappedProductCount")),
		UnmappedFormulItemPartCount: reader.GetInt64(reader.GetOrdinal("UnmappedFormulItemPartCount")),
		UnmappedProductFormulItemPartCount: reader.GetInt64(reader.GetOrdinal("UnmappedProductFormulItemPartCount")));

	private sealed record FormulaSyncResult(
		long FormulGroupCount,
		long FormulCount,
		long FormulItemCount,
		long ProductGroupCount,
		long ProductFormulCount,
		long ProductFormulItemCount,
		long UnmappedProductCount,
		long UnmappedFormulItemPartCount,
		long UnmappedProductFormulItemPartCount);

	private const string RebuildSql = """
SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
    داده منبع پیش از هر حذف در temp tableها آماده می‌شود. در نتیجه اگر Linked Server،
    نگاشت Part یا یکپارچگی منبع مشکل داشته باشد، هیچ داده‌ای از سیستم جدید حذف نمی‌شود.
*/
SELECT
    CAST(source.FormulGroup_ID AS BIGINT) AS Id,
    ISNULL(source.FormulGroup_Title, N'') AS Title
INTO #FormulGroup
FROM TMS.TotalSystem.dbo.Bom_FormulGroup AS source;

SELECT
    CAST(source.Formul_ID AS BIGINT) AS Id,
    ISNULL(source.Formul_Title, N'') AS Title,
    CAST(source.FormulGroup_FK AS BIGINT) AS GroupId,
    NULLIF(LTRIM(RTRIM(source.CreatedDate)), '') AS CreatedOnShamsiDateTime
INTO #Formul
FROM TMS.TotalSystem.dbo.Bom_Formul AS source
WHERE source.FormulGroup_FK IS NOT NULL;

SELECT
    CAST(source.Formul_Itm_ID AS BIGINT) AS Id,
    CAST(source.Formul_Fk AS BIGINT) AS FormulId,
    targetPart.Id AS PartId,
    ISNULL(source.UsingRate, 0) AS UsingRate,
    source.CreatedDate AS CreatedOnMiladiDateTime,
    source.CreatedDateInText AS CreatedOnShamsiDateTime,
    source.UpdatedDate AS ModifiedDateMiladiDateTime,
    source.UpdatedDateInText AS ModifiedDateShamsiDateTime,
    source.SourcePath
INTO #FormulItem
FROM TMS.TotalSystem.dbo.Bom_Formul_Itm AS source
INNER JOIN #Formul AS parent ON parent.Id = source.Formul_Fk
LEFT JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyPart ON legacyPart.Part_ID = source.Part_Fk
LEFT JOIN Inv.Part AS targetPart ON targetPart.Code = legacyPart.Part_Code;

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

SELECT
    CAST(source.ProductFormul_Itm_ID AS BIGINT) AS Id,
    CAST(source.ProductFormul_FK AS BIGINT) AS ProductFormulId,
    targetPart.Id AS PartId,
    source.UsingRate,
    CASE WHEN targetFormul.Id IS NULL THEN NULL ELSE CAST(source.Bom_Formul_FK AS BIGINT) END AS FormulId,
    source.CreatedDate AS CreatedOnMiladiDateTime,
    source.CreatedDateInText AS CreatedOnShamsiDateTime,
    source.UpdatedDate AS ModifiedDateMiladiDateTime,
    source.UpdateDateInText AS ModifiedDateShamsiDateTime
INTO #ProductFormulItem
FROM TMS.TotalSystem.dbo.Bom_ProductFormul_Itm AS source
INNER JOIN #ProductFormul AS parent ON parent.Id = source.ProductFormul_FK
LEFT JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyPart ON legacyPart.Part_ID = source.Part_FK
LEFT JOIN Inv.Part AS targetPart ON targetPart.Code = legacyPart.Part_Code
LEFT JOIN #Formul AS targetFormul ON targetFormul.Id = source.Bom_Formul_FK;

DECLARE @UnmappedProductCount BIGINT =
(
    SELECT COUNT_BIG(*)
    FROM TMS.TotalSystem.dbo.Bom_ProductFormul AS source
    INNER JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyProduct ON legacyProduct.Part_ID = source.Part_FK
    LEFT JOIN Inv.Part AS targetProduct ON targetProduct.Code = legacyProduct.Part_Code
    WHERE targetProduct.Id IS NULL
);

DECLARE @UnmappedFormulItemPartCount BIGINT =
(
    SELECT COUNT_BIG(*)
    FROM #FormulItem
    WHERE PartId IS NULL
);

DECLARE @UnmappedProductFormulItemPartCount BIGINT =
(
    SELECT COUNT_BIG(*)
    FROM TMS.TotalSystem.dbo.Bom_ProductFormul_Itm AS source
    INNER JOIN #ProductFormul AS parent ON parent.Id = source.ProductFormul_FK
    LEFT JOIN TMS.TotalSystem.dbo.Inv_Part AS legacyPart ON legacyPart.Part_ID = source.Part_FK
    LEFT JOIN Inv.Part AS targetPart ON targetPart.Code = legacyPart.Part_Code
    WHERE source.Part_FK IS NOT NULL
      AND targetPart.Id IS NULL
);

/* Preflight: هر خطا پیش از اولین DELETE رخ می‌دهد. */
IF EXISTS (SELECT Id FROM #FormulGroup GROUP BY Id HAVING COUNT_BIG(*) <> 1)
    THROW 51001, N'شناسه تکراری در گروه‌های فرمول HTS یافت شد.', 1;

IF EXISTS (SELECT Id FROM #Formul GROUP BY Id HAVING COUNT_BIG(*) <> 1)
    THROW 51002, N'شناسه تکراری در فرمول‌های HTS یا نگاشت آن‌ها یافت شد.', 1;

IF EXISTS (SELECT Id FROM #FormulItem GROUP BY Id HAVING COUNT_BIG(*) <> 1)
    THROW 51003, N'شناسه تکراری در اقلام فرمول HTS یا نگاشت Part یافت شد.', 1;

IF EXISTS (SELECT Id FROM #ProductGroup GROUP BY Id HAVING COUNT_BIG(*) <> 1)
    THROW 51004, N'شناسه تکراری در گروه‌های محصول HTS یافت شد.', 1;

IF EXISTS (SELECT Id FROM #ProductFormul GROUP BY Id HAVING COUNT_BIG(*) <> 1)
    THROW 51005, N'شناسه تکراری در فرمول محصولات HTS یا نگاشت Product یافت شد.', 1;

IF EXISTS (SELECT Id FROM #ProductFormulItem GROUP BY Id HAVING COUNT_BIG(*) <> 1)
    THROW 51006, N'شناسه تکراری در اقلام فرمول محصول HTS یا نگاشت Part یافت شد.', 1;

IF EXISTS
(
    SELECT 1
    FROM #Formul AS source
    LEFT JOIN #FormulGroup AS parent ON parent.Id = source.GroupId
    WHERE parent.Id IS NULL
)
    THROW 51007, N'گروه یکی از فرمول‌های HTS در منبع وجود ندارد.', 1;

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

/* حذف به ترتیب وابستگی FK */
ALTER TABLE Bom.ProductPriceHistory
    NOCHECK CONSTRAINT FK_ProductPriceHistory_ProductFormul_ProductFormulId;

DELETE FROM Bom.ProductFormulItem;
DELETE FROM Bom.ProductFormul;
DELETE FROM Bom.ProductGroup;
DELETE FROM Bom.FormulItem;
DELETE FROM Bom.Formul;
DELETE FROM Bom.FormulGroup;

SET IDENTITY_INSERT Bom.FormulGroup ON;
BEGIN TRY
    INSERT INTO Bom.FormulGroup
    (
        Id, Title, CreatedByName, CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime, IsActive
    )
    SELECT Id, Title, N'HTS', NULL, NULL, 1
    FROM #FormulGroup;
END TRY
BEGIN CATCH
    SET IDENTITY_INSERT Bom.FormulGroup OFF;
    THROW;
END CATCH;
SET IDENTITY_INSERT Bom.FormulGroup OFF;

SET IDENTITY_INSERT Bom.Formul ON;
BEGIN TRY
    INSERT INTO Bom.Formul
    (
        Id, Title, GroupId, CreatedByName, CreatedOnMiladiDateTime,
        CreatedOnShamsiDateTime, IsActive
    )
    SELECT Id, Title, GroupId, N'HTS', NULL, CreatedOnShamsiDateTime, 1
    FROM #Formul;
END TRY
BEGIN CATCH
    SET IDENTITY_INSERT Bom.Formul OFF;
    THROW;
END CATCH;
SET IDENTITY_INSERT Bom.Formul OFF;

SET IDENTITY_INSERT Bom.FormulItem ON;
BEGIN TRY
    INSERT INTO Bom.FormulItem
    (
        Id, FormulId, PartId, UsingRate, SourcePath,
        CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive
    )
    SELECT
        Id, FormulId, PartId, UsingRate, SourcePath,
        N'HTS', CASE WHEN ModifiedDateMiladiDateTime IS NULL THEN NULL ELSE N'HTS' END,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, 1
    FROM #FormulItem;
END TRY
BEGIN CATCH
    SET IDENTITY_INSERT Bom.FormulItem OFF;
    THROW;
END CATCH;
SET IDENTITY_INSERT Bom.FormulItem OFF;

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

SET IDENTITY_INSERT Bom.ProductFormulItem ON;
BEGIN TRY
    INSERT INTO Bom.ProductFormulItem
    (
        Id, ProductFormulId, PartId, FormulId, UsingRate,
        CreatedByName, ModifiedByName,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive
    )
    SELECT
        Id, ProductFormulId, PartId, FormulId, UsingRate,
        N'HTS', CASE WHEN ModifiedDateMiladiDateTime IS NULL THEN NULL ELSE N'HTS' END,
        CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
        ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, 1
    FROM #ProductFormulItem;
END TRY
BEGIN CATCH
    SET IDENTITY_INSERT Bom.ProductFormulItem OFF;
    THROW;
END CATCH;
SET IDENTITY_INSERT Bom.ProductFormulItem OFF;

/* FK تاریخچه دوباره فعال و Trusted می‌شود؛ این مرحله کل تاریخچه را هم کنترل می‌کند. */
ALTER TABLE Bom.ProductPriceHistory WITH CHECK
    CHECK CONSTRAINT FK_ProductPriceHistory_ProductFormul_ProductFormulId;

DBCC CHECKIDENT ('Bom.FormulGroup', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Bom.Formul', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Bom.FormulItem', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Bom.ProductGroup', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Bom.ProductFormul', RESEED) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Bom.ProductFormulItem', RESEED) WITH NO_INFOMSGS;

/* Postflight: شمارش هدف باید دقیقاً با staging برابر باشد. */
IF (SELECT COUNT_BIG(*) FROM Bom.FormulGroup) <> (SELECT COUNT_BIG(*) FROM #FormulGroup)
    THROW 51010, N'تعداد گروه‌های فرمول درج‌شده با staging برابر نیست.', 1;

IF (SELECT COUNT_BIG(*) FROM Bom.Formul) <> (SELECT COUNT_BIG(*) FROM #Formul)
    THROW 51011, N'تعداد فرمول‌های درج‌شده با staging برابر نیست.', 1;

IF (SELECT COUNT_BIG(*) FROM Bom.FormulItem) <> (SELECT COUNT_BIG(*) FROM #FormulItem)
    THROW 51012, N'تعداد اقلام فرمول درج‌شده با staging برابر نیست.', 1;

IF (SELECT COUNT_BIG(*) FROM Bom.ProductGroup) <> (SELECT COUNT_BIG(*) FROM #ProductGroup)
    THROW 51013, N'تعداد گروه‌های محصول درج‌شده با staging برابر نیست.', 1;

IF (SELECT COUNT_BIG(*) FROM Bom.ProductFormul) <> (SELECT COUNT_BIG(*) FROM #ProductFormul)
    THROW 51014, N'تعداد فرمول محصولات درج‌شده با staging برابر نیست.', 1;

IF (SELECT COUNT_BIG(*) FROM Bom.ProductFormulItem) <> (SELECT COUNT_BIG(*) FROM #ProductFormulItem)
    THROW 51015, N'تعداد اقلام فرمول محصول درج‌شده با staging برابر نیست.', 1;

SELECT
    (SELECT COUNT_BIG(*) FROM #FormulGroup) AS FormulGroupCount,
    (SELECT COUNT_BIG(*) FROM #Formul) AS FormulCount,
    (SELECT COUNT_BIG(*) FROM #FormulItem) AS FormulItemCount,
    (SELECT COUNT_BIG(*) FROM #ProductGroup) AS ProductGroupCount,
    (SELECT COUNT_BIG(*) FROM #ProductFormul) AS ProductFormulCount,
    (SELECT COUNT_BIG(*) FROM #ProductFormulItem) AS ProductFormulItemCount,
    @UnmappedProductCount AS UnmappedProductCount,
    @UnmappedFormulItemPartCount AS UnmappedFormulItemPartCount,
    @UnmappedProductFormulItemPartCount AS UnmappedProductFormulItemPartCount;
""";
}
