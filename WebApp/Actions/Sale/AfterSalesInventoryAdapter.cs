using Common.Utilities;
using Microsoft.Data.SqlClient;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// اقلام حواله گارانتی و پیش‌فاکتور از راهکاران (ERPS)، همان مسیر Rahkaran در HTS:
	/// بدون مرکز فروش = مصرف پروژه LGS3 (نوع سند ۵۳ منهای برگشت نوع ۷)؛
	/// با مرکز فروش = اقلام سفارش SLS3.
	/// </summary>
	public static class AfterSalesInventoryAdapter
	{
		public static int ToHamkaranYear(string? yearText)
		{
			if (string.IsNullOrWhiteSpace(yearText) || !int.TryParse(yearText.Trim(), out var year))
				return 0;
			if (year >= 80 && year <= 99)
				return year;
			if (year >= 100 && year < 1300)
				return year;
			if (year >= 1300 && year <= 1399)
				return year % 100;
			if (year >= 1400)
				return (year - 1399) + 99;
			return 0;
		}

		public static string ToPersianYear(int hamkaranYear)
		{
			if (hamkaranYear <= 0)
				return "";
			return hamkaranYear <= 99 ? "13" + hamkaranYear.ToString("00") : (hamkaranYear + 1300).ToString();
		}

		public static async Task<InventoryFetchResult> FetchPiecesAsync(
			string? erpsConnectionString,
			int vchNo,
			string? year,
			long? saleOfficeCode,
			CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(erpsConnectionString))
				return InventoryFetchResult.Fail("رشته اتصال راهکاران (ERPS) تنظیم نشده است");

			var yearHamkaran = ToHamkaranYear(year);
			if (vchNo <= 0 || yearHamkaran <= 0)
				return InventoryFetchResult.Fail("شماره سند و سال مالی را وارد کنید");

			try
			{
				await using var conn = new SqlConnection(erpsConnectionString);
				await conn.OpenAsync(cn);
				var rows = saleOfficeCode is > 0
					? await ReadPrefactorAsync(conn, vchNo, yearHamkaran, saleOfficeCode.Value, cn)
					: await ReadProjectAsync(conn, vchNo, yearHamkaran, cn);
				if (rows.Count == 0)
				{
					return InventoryFetchResult.Fail(saleOfficeCode is > 0
						? "پیش‌فاکتور با این شماره سند و مرکز فروش در راهکاران یافت نشد"
						: "حواله گارانتی با این شماره سند در راهکاران یافت نشد");
				}
				return InventoryFetchResult.Ok(rows);
			}
			catch (Exception ex)
			{
				return InventoryFetchResult.Fail("خواندن اقلام سند از راهکاران ممکن نشد: " + (ex.InnerException?.Message ?? ex.Message));
			}
		}

		private static async Task<List<InventoryPieceRow>> ReadPrefactorAsync(
			SqlConnection conn, int vchNo, int year, long saleOfficeCode, CancellationToken cn)
		{
			const string sql = @"
SELECT
	i.OrderItemID AS PieceId,
	o.[Date] AS VchDate,
	p.PartID AS Partref,
	p.Code + N' | ' + p.[Name] AS PartName,
	i.ProductUnitRef AS MunitRef,
	i.Quantity AS Qty,
	CAST(1 AS bit) AS FromSaleOrder
FROM SLS3.OrderItem AS i
INNER JOIN SLS3.[Order] AS o ON o.OrderID = i.OrderRef
INNER JOIN SLS3.SalesOffice AS so ON so.SalesOfficeID = o.SalesOfficeRef
INNER JOIN SLS3.Product AS prod ON prod.ProductID = i.ProductRef
INNER JOIN LGS3.Part AS p ON p.PartID = prod.PartRef
WHERE TRY_CONVERT(bigint, i.OrderNumber) = @vchNo
	AND o.FiscalYearRef = @year
	AND so.Code = @saleOfficeCode";
			return await ReadRowsAsync(conn, sql, vchNo, year, saleOfficeCode, cn);
		}

		private static async Task<List<InventoryPieceRow>> ReadProjectAsync(
			SqlConnection conn, int vchNo, int year, CancellationToken cn)
		{
			const string sql = @"
SELECT
	i.InventoryVoucherItemID AS PieceId,
	h.[Date] AS VchDate,
	i.PartRef AS Partref,
	p.Code + N' | ' + p.[Name] AS PartName,
	i.UnitRef AS MunitRef,
	i.Quantity - ISNULL(ret.SumQuantity, 0) AS Qty,
	CAST(0 AS bit) AS FromSaleOrder
FROM LGS3.InventoryVoucherItem AS i
INNER JOIN LGS3.InventoryVoucher AS h ON h.InventoryVoucherID = i.InventoryVoucherRef
INNER JOIN LGS3.Part AS p ON p.PartID = i.PartRef
LEFT JOIN (
	SELECT retItem.ReferenceRef AS BaseItemId,
		SUM(retItem.Quantity) AS SumQuantity
	FROM LGS3.InventoryVoucherItem AS retItem
	WHERE retItem.InventoryVoucherSpecificationRef = 7
		AND retItem.ReferenceRef IN (
			SELECT i2.InventoryVoucherItemID
			FROM LGS3.InventoryVoucherItem AS i2
			INNER JOIN LGS3.InventoryVoucher AS h2 ON h2.InventoryVoucherID = i2.InventoryVoucherRef
			WHERE h2.InventoryVoucherSpecificationRef = 53
				AND h2.FiscalYearRef = @year
				AND TRY_CONVERT(bigint, h2.Number) = @vchNo
		)
	GROUP BY retItem.ReferenceRef
) AS ret ON ret.BaseItemId = i.InventoryVoucherItemID
WHERE h.InventoryVoucherSpecificationRef = 53
	AND h.FiscalYearRef = @year
	AND TRY_CONVERT(bigint, h.Number) = @vchNo
	AND i.Quantity - ISNULL(ret.SumQuantity, 0) > 0";
			return await ReadRowsAsync(conn, sql, vchNo, year, null, cn);
		}

		private static async Task<List<InventoryPieceRow>> ReadRowsAsync(
			SqlConnection conn, string sql, int vchNo, int year, long? saleOfficeCode, CancellationToken cn)
		{
			await using var cmd = new SqlCommand(sql, conn);
			cmd.Parameters.Add(new SqlParameter("@vchNo", vchNo));
			cmd.Parameters.Add(new SqlParameter("@year", year));
			if (saleOfficeCode != null)
				cmd.Parameters.Add(new SqlParameter("@saleOfficeCode", saleOfficeCode.Value.ToString()));
			await using var reader = await cmd.ExecuteReaderAsync(cn);
			var rows = new List<InventoryPieceRow>();
			while (await reader.ReadAsync(cn))
			{
				rows.Add(new InventoryPieceRow
				{
					Id = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
					Date = ReadShamsiDate(reader, 1),
					Partref = reader.IsDBNull(2) ? null : Convert.ToInt64(reader.GetValue(2)),
					ProductTitle = reader.IsDBNull(3) ? null : reader.GetValue(3)?.ToString(),
					MunitRef = reader.IsDBNull(4) ? null : Convert.ToInt64(reader.GetValue(4)),
					Value = reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader.GetValue(5)),
					FromSaleOrder = !reader.IsDBNull(6) && Convert.ToBoolean(reader.GetValue(6))
				});
			}
			return rows;
		}

		private static string? ReadShamsiDate(SqlDataReader reader, int ordinal)
		{
			if (reader.IsDBNull(ordinal))
				return null;
			var value = reader.GetValue(ordinal);
			return value is DateTime date ? date.ToShamsiDate() : value?.ToString();
		}
	}

	public sealed class InventoryPieceRow
	{
		public long Id { get; set; }
		public string? Date { get; set; }
		public string? ProductTitle { get; set; }
		public decimal Value { get; set; }
		public long? Partref { get; set; }
		public long? MunitRef { get; set; }
		public decimal LastBuyPrice { get; set; }
		public bool FromSaleOrder { get; set; }
		public long? HavayarPartId { get; set; }
		public long? HavayarOrderDetailId { get; set; }
		public long? HavayarPartUnitId { get; set; }
	}

	public sealed class InventoryFetchResult
	{
		public bool Success { get; init; }
		public string? Message { get; init; }
		public List<InventoryPieceRow> Items { get; init; } = [];

		public static InventoryFetchResult Ok(List<InventoryPieceRow> items)
			=> new() { Success = true, Items = items };

		public static InventoryFetchResult Fail(string message)
			=> new() { Success = false, Message = message };
	}
}
