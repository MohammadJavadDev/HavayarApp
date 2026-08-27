using Common.Attributes;
using Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Job;
using System.Data;
using System.Globalization;

namespace App.BackgroundJob.Jobs.Bom;

public sealed class LegacyProductPriceHistoryMigrationJob(ApplicationDbContext db)
{
	private const string MigrationUser = "مهاجرت snapshot قیمت از سیستم قدیمی";

	[JobHandler(
		"انتقال snapshot قیمت محصولات از سیستم قدیمی",
		"تاریخچه روزانه قیمت محصول را با حفظ تاریخ شمسی و حذف اجرای تکراری هر روز از TotalSystem منتقل می‌کند.")]
	public async Task MigrateLegacyProductPriceHistory(
		IJobLogger? jobLogger = null,
		CancellationToken cancellationToken = default)
	{
		var connectionString = db.Database.GetConnectionString()
			?? throw new InvalidOperationException("Connection string دیتابیس برنامه تنظیم نشده است.");

		await using var connection = new SqlConnection(connectionString);
		await connection.OpenAsync(cancellationToken);

		var dateMap = await ReadLegacyDatesAsync(connection, cancellationToken);
		if (dateMap.Rows.Count == 0)
		{
			if (jobLogger != null)
				await jobLogger.LogWarningAsync("هیچ تاریخ snapshot در سیستم قدیمی یافت نشد.", 0, cancellationToken);
			return;
		}

		await CreateAndFillDateMapAsync(connection, dateMap, cancellationToken);

		var years = dateMap.AsEnumerable()
			.Select(row => row.Field<DateTime>("SnapshotDate").Year)
			.Distinct()
			.OrderBy(year => year)
			.ToArray();

		long insertedRows = 0;
		foreach (var year in years)
		{
			cancellationToken.ThrowIfCancellationRequested();

			await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
			try
			{
				await using var command = new SqlCommand(MigrationSql, connection, transaction)
				{
					CommandTimeout = 0
				};
				command.Parameters.Add("@FromDate", SqlDbType.Date).Value = new DateTime(year, 1, 1);
				command.Parameters.Add("@ToDate", SqlDbType.Date).Value = new DateTime(year + 1, 1, 1);
				command.Parameters.Add("@MigrationUser", SqlDbType.NVarChar, 150).Value = MigrationUser;

				var yearInsertedRows = await command.ExecuteNonQueryAsync(cancellationToken);
				await transaction.CommitAsync(cancellationToken);
				insertedRows += yearInsertedRows;

				if (jobLogger != null)
				{
					await jobLogger.LogInfoAsync(
						$"انتقال snapshotهای سال {year} تکمیل شد؛ {yearInsertedRows:N0} رکورد جدید.",
						cancellationToken);
				}
			}
			catch
			{
				await transaction.RollbackAsync(CancellationToken.None);
				throw;
			}
		}

		var totalMigratedRows = await CountMigratedRowsAsync(connection, cancellationToken);
		if (jobLogger != null)
		{
			await jobLogger.LogInfoAsync(
				$"انتقال snapshotهای قدیمی پایان یافت؛ {insertedRows:N0} رکورد جدید و در مجموع {totalMigratedRows:N0} رکورد مهاجرت‌شده موجود است.",
				cancellationToken);
		}
	}

	private static async Task<DataTable> ReadLegacyDatesAsync(
		SqlConnection connection,
		CancellationToken cancellationToken)
	{
		const string sql = """
SELECT DISTINCT LTRIM(RTRIM(history.CreatedDate)) AS SnapshotShamsiDate
FROM TMS.TotalSystem.dbo.Bom_ProductFormul_PriceHistory AS history
WHERE NULLIF(LTRIM(RTRIM(history.CreatedDate)), N'') IS NOT NULL;
""";

		var result = new DataTable();
		result.Columns.Add("SnapshotShamsiDate", typeof(string));
		result.Columns.Add("SnapshotDate", typeof(DateTime));

		await using var command = new SqlCommand(sql, connection) { CommandTimeout = 0 };
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		while (await reader.ReadAsync(cancellationToken))
		{
			var shamsiDate = reader.GetString(0);
			if (!TryParseShamsiDate(shamsiDate, out var snapshotDate))
				throw new InvalidOperationException($"تاریخ شمسی نامعتبر در تاریخچه قدیمی: {shamsiDate}");

			result.Rows.Add(shamsiDate, snapshotDate);
		}

		return result;
	}

	private static async Task CreateAndFillDateMapAsync(
		SqlConnection connection,
		DataTable dateMap,
		CancellationToken cancellationToken)
	{
		const string createSql = """
CREATE TABLE #LegacySnapshotDateMap
(
    SnapshotShamsiDate NVARCHAR(10) NOT NULL PRIMARY KEY,
    SnapshotDate DATE NOT NULL
);
""";

		await using (var command = new SqlCommand(createSql, connection))
			await command.ExecuteNonQueryAsync(cancellationToken);

		using var bulkCopy = new SqlBulkCopy(connection)
		{
			DestinationTableName = "#LegacySnapshotDateMap",
			BatchSize = 1000,
			BulkCopyTimeout = 0
		};
		bulkCopy.ColumnMappings.Add("SnapshotShamsiDate", "SnapshotShamsiDate");
		bulkCopy.ColumnMappings.Add("SnapshotDate", "SnapshotDate");
		await bulkCopy.WriteToServerAsync(dateMap, cancellationToken);
	}

	private static async Task<long> CountMigratedRowsAsync(
		SqlConnection connection,
		CancellationToken cancellationToken)
	{
		const string sql = """
SELECT COUNT_BIG(*)
FROM Bom.ProductPriceHistory
WHERE CreatedByName = @MigrationUser;
""";

		await using var command = new SqlCommand(sql, connection);
		command.Parameters.Add("@MigrationUser", SqlDbType.NVarChar, 150).Value = MigrationUser;
		return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
	}

	private static bool TryParseShamsiDate(string value, out DateTime date)
	{
		date = default;
		var normalized = NormalizeDigits(value.Trim());
		var parts = normalized.Split(['/', '-'], StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length != 3
			|| !int.TryParse(parts[0], out var year)
			|| !int.TryParse(parts[1], out var month)
			|| !int.TryParse(parts[2], out var day))
			return false;

		try
		{
			date = new PersianCalendar().ToDateTime(year, month, day, 0, 0, 0, 0);
			return true;
		}
		catch (ArgumentOutOfRangeException)
		{
			return false;
		}
	}

	private static string NormalizeDigits(string value)
	{
		return value
			.Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
			.Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
			.Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
			.Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
	}

	private const string MigrationSql = """
;WITH RankedLegacyHistory AS
(
    SELECT
        history.ProductFormul_FK AS ProductFormulId,
        formula.ProductId,
        product.Code AS PartCode,
        product.Name AS PartName,
        dateMap.SnapshotDate,
        dateMap.SnapshotShamsiDate,
        CAST(ISNULL(history.BuyPrice, 0) AS DECIMAL(28, 5)) AS BuyPrice,
        CAST(ISNULL(history.BuyPriceInEuro, 0) AS DECIMAL(28, 5)) AS BuyPriceInEuro,
        history.CreatedTime,
        ROW_NUMBER() OVER
        (
            PARTITION BY history.ProductFormul_FK, history.CreatedDate
            ORDER BY TRY_CONVERT(TIME(0), history.CreatedTime) DESC,
                     history.ProductFormul_PriceHistory_ID DESC
        ) AS RowNumber
    FROM TMS.TotalSystem.dbo.Bom_ProductFormul_PriceHistory AS history
    INNER JOIN #LegacySnapshotDateMap AS dateMap
        ON dateMap.SnapshotShamsiDate = LTRIM(RTRIM(history.CreatedDate))
    INNER JOIN Bom.ProductFormul AS formula
        ON formula.Id = history.ProductFormul_FK
    INNER JOIN Inv.Part AS product
        ON product.Id = formula.ProductId
    WHERE dateMap.SnapshotDate >= @FromDate
      AND dateMap.SnapshotDate < @ToDate
)
INSERT INTO Bom.ProductPriceHistory
(
    ProductFormulId,
    ProductId,
    PartCode,
    PartName,
    SnapshotDate,
    SnapshotShamsiDate,
    CurrencyRate,
    InternalBuyPrice,
    ExternalBuyPrice,
    BuyPrice,
    BuyPriceInEuro,
    SalePrice,
    ComponentCount,
    PricedComponentCount,
    CreatedOnMiladiDateTime,
    CreatedOnShamsiDateTime,
    CreatedByName,
    IsActive
)
SELECT
    source.ProductFormulId,
    source.ProductId,
    source.PartCode,
    source.PartName,
    source.SnapshotDate,
    source.SnapshotShamsiDate,
    0,
    0,
    0,
    source.BuyPrice,
    source.BuyPriceInEuro,
    CAST(source.BuyPrice * CASE
        WHEN product.SaleRate IS NULL OR product.SaleRate = 0 THEN 1
        ELSE product.SaleRate
    END AS DECIMAL(28, 5)),
    0,
    0,
    CAST(source.SnapshotDate AS DATETIME2),
    LEFT(CONCAT(source.SnapshotShamsiDate, N' ', ISNULL(source.CreatedTime, N'')), 30),
    @MigrationUser,
    1
FROM RankedLegacyHistory AS source
INNER JOIN Inv.Part AS product ON product.Id = source.ProductId
WHERE source.RowNumber = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM Bom.ProductPriceHistory AS target
      WHERE target.SnapshotDate = source.SnapshotDate
        AND target.ProductFormulId = source.ProductFormulId
  );
""";
}
