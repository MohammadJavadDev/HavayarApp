using Microsoft.Data.SqlClient;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// خواندن اقلام سند انبار/پیش‌فاکتور از TotalSystem + راهکاران.
	/// SQL فقط این‌جا است — کنترلر Panel کوئری خام نمی‌زند.
	/// </summary>
	public static class AfterSalesInventoryAdapter
	{
		public static int ToHamkaranYear(string? yearText)
		{
			if (string.IsNullOrWhiteSpace(yearText))
				return 0;
			if (!int.TryParse(yearText.Trim(), out var year))
				return 0;
			if (yearText.Trim().Length <= 2)
				return year >= 80 && year <= 99 ? year : year + 100;
			return year <= 1399 ? year % 100 : (year - 1399) + 99;
		}

		public static string ToPersianYear(int hamkaranYear)
		{
			if (hamkaranYear <= 0)
				return "";
			return hamkaranYear <= 99 ? "13" + hamkaranYear.ToString("00") : (hamkaranYear + 1300).ToString();
		}

		public static async Task<InventoryFetchResult> FetchPiecesAsync(
			string? htsConnectionString,
			int vchNo,
			string yearText,
			long? saleCenterId,
			CancellationToken cn)
		{
			if (string.IsNullOrWhiteSpace(htsConnectionString))
				return InventoryFetchResult.Fail("رشته اتصال Hts / راهکاران تنظیم نشده است");

			var year = ToHamkaranYear(yearText);
			if (vchNo <= 0 || year <= 0)
				return InventoryFetchResult.Fail("شماره سند و سال مالی را وارد کنید");

			try
			{
				await using var conn = new SqlConnection(htsConnectionString);
				await conn.OpenAsync(cn);
				var rows = await ReadProjectAsync(conn, vchNo, year, cn);
				if (rows.Count == 0 && saleCenterId is > 0)
					rows = await ReadPrefactorAsync(conn, vchNo, year, saleCenterId.Value, cn);
				if (rows.Count == 0)
					return InventoryFetchResult.Fail("حواله گارانتی با این شماره سند در راهکاران یافت نشد");
				return InventoryFetchResult.Ok(rows);
			}
			catch (Exception ex)
			{
				return InventoryFetchResult.Fail("خواندن اقلام سند ممکن نشد: " + (ex.InnerException?.Message ?? ex.Message));
			}
		}

		private static async Task<List<InventoryPieceRow>> ReadPrefactorAsync(
			SqlConnection conn, int vchNo, int year, long saleCenterId, CancellationToken cn)
		{
			const string sql = @"
SELECT DISTINCT
	Sale_OrderDetail.OrderDetail_ID AS PieceId,
	VchDate_Shamsi AS VchDate,
	Part.Part_ID AS Partref,
	Part.Part_Code + N' | ' + Part.Part_Name AS PartName,
	CAST(NULL AS int) AS MunitRef,
	Qty,
	LastPrice.UnitPrice AS LastBuyPrice,
	CAST(1 AS bit) AS FromSaleOrder
FROM Sale_OrderDetail
INNER JOIN Inv_Part AS Part ON Sale_OrderDetail.Part_FK = Part.Part_ID
INNER JOIN Sale_Order ON Sale_Order.Order_ID = Sale_OrderDetail.Order_FK
LEFT JOIN dbo.Vw_Bom_PartLastPriceSimple AS LastPrice ON LastPrice.Hamkaran_Part_FK = Part.Hamkaran_Part_FK
WHERE VchNo = @vchNo
	AND [Year] = @year
	AND (Branch_FK = @saleCenterId OR OrderDetail_ID = @saleCenterId)
	AND Hamkaran_Vchitm_FK IN (
		SELECT Item.OrderItemID
		FROM ERPS.Erps.SLS3.OrderItem AS Item
		JOIN Erps.Erps.SLS3.[Order] AS SaleOrder ON SaleOrder.OrderID = Item.OrderRef
		JOIN ERPS.Erps.SLS3.SalesOffice AS SalesOffice ON SalesOffice.SalesOfficeID = SaleOrder.SalesOfficeRef
		WHERE Item.OrderNumber = @vchNo
			AND SaleOrder.FiscalYearRef = @year
			AND SalesOffice.Code = CAST(@saleCenterId AS nvarchar(50))
	)";
			return await ReadRowsAsync(conn, sql, vchNo, year, saleCenterId, cn);
		}

		private static async Task<List<InventoryPieceRow>> ReadProjectAsync(
			SqlConnection conn, int vchNo, int year, CancellationToken cn)
		{
			const string sql = @"
SELECT DISTINCT
	i.VchItmID AS PieceId,
	dbo.GregorianToPersian(h.VchDate) AS VchDate,
	i.Partref AS Partref,
	Part.PartCode + N' | ' + Part.PartName COLLATE Persian_100_CI_AI AS PartName,
	i.MunitRef AS MunitRef,
	i.FinalQty - ISNULL(i2.Qty, 0) AS Qty,
	LastPrice.UnitPrice AS LastBuyPrice,
	CAST(0 AS bit) AS FromSaleOrder
FROM erp.newhavayar.inv.InvVchItm i
INNER JOIN erp.newhavayar.inv.InvVchHdr h ON h.VchHdrID = i.VchHdrRef
INNER JOIN erp.newhavayar.INV.Part Part ON Part.Serial = i.PartRef
LEFT JOIN dbo.Vw_Bom_PartLastPriceSimple AS LastPrice ON LastPrice.Hamkaran_Part_FK = Part.Serial
LEFT JOIN (
	SELECT RefNum, SUM(FinalQty) AS Qty
	FROM erp.newhavayar.INV.InvVchItm
	WHERE BaseVchType = 53 AND VchType = 7
	GROUP BY RefNum
) AS i2 ON i2.RefNum = i.VchItmID
WHERE h.VchType = 53
	AND h.[year] = @year
	AND h.VchNum = @vchNo
	AND i.FinalQty - ISNULL(i2.Qty, 0) > 0
UNION
SELECT DISTINCT
	i.InventoryVoucherItemID AS PieceId,
	dbo.GregorianToPersian(h.[Date]) AS VchDate,
	i.PartRef AS Partref,
	Part.Code + N' | ' + Part.[Name] AS PartName,
	i.ReferenceType AS MunitRef,
	i.Quantity - ISNULL(i2.SumQuantity, 0) AS Qty,
	LastPrice.UnitPrice AS LastBuyPrice,
	CAST(0 AS bit) AS FromSaleOrder
FROM Erps.Erps.LGS3.InventoryVoucherItem i
INNER JOIN Erps.Erps.LGS3.InventoryVoucher h ON h.InventoryVoucherID = i.InventoryVoucherRef
INNER JOIN Erps.Erps.LGS3.Part Part ON Part.PartID = i.PartRef
LEFT JOIN dbo.Vw_Bom_PartLastPriceSimple AS LastPrice ON LastPrice.Hamkaran_Part_FK = Part.PartID
LEFT JOIN (
	SELECT InventoryVoucherItem.ReferenceRef AS Number,
		SUM(InventoryVoucherItem.Quantity) AS SumQuantity
	FROM Erps.Erps.LGS3.InventoryVoucherItem AS InventoryVoucherItem
	JOIN Erps.Erps.LGS3.InventoryVoucherItem AS BaseVchItem
		ON BaseVchItem.InventoryVoucherItemID = InventoryVoucherItem.ReferenceRef
	WHERE InventoryVoucherItem.InventoryVoucherSpecificationRef = 7
		AND BaseVchItem.InventoryVoucherSpecificationRef = 53
	GROUP BY InventoryVoucherItem.ReferenceRef
) AS i2 ON i2.Number = i.InventoryVoucherItemID
WHERE h.InventoryVoucherSpecificationRef = 53
	AND h.FiscalYearRef = @year
	AND h.Number = @vchNo
	AND i.Quantity - ISNULL(i2.SumQuantity, 0) > 0";
			return await ReadRowsAsync(conn, sql, vchNo, year, null, cn);
		}

		private static async Task<List<InventoryPieceRow>> ReadRowsAsync(
			SqlConnection conn, string sql, int vchNo, int year, long? saleCenterId, CancellationToken cn)
		{
			await using var cmd = new SqlCommand(sql, conn);
			cmd.Parameters.Add(new SqlParameter("@vchNo", vchNo));
			cmd.Parameters.Add(new SqlParameter("@year", year));
			if (saleCenterId != null)
				cmd.Parameters.Add(new SqlParameter("@saleCenterId", saleCenterId.Value));
			await using var reader = await cmd.ExecuteReaderAsync(cn);
			var rows = new List<InventoryPieceRow>();
			while (await reader.ReadAsync(cn))
			{
				rows.Add(new InventoryPieceRow
				{
					Id = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0)),
					Date = reader.IsDBNull(1) ? null : reader.GetValue(1)?.ToString(),
					Partref = reader.IsDBNull(2) ? null : Convert.ToInt64(reader.GetValue(2)),
					ProductTitle = reader.IsDBNull(3) ? null : reader.GetValue(3)?.ToString(),
					MunitRef = reader.IsDBNull(4) ? null : Convert.ToInt64(reader.GetValue(4)),
					Value = reader.IsDBNull(5) ? 0 : Convert.ToDecimal(reader.GetValue(5)),
					LastBuyPrice = reader.IsDBNull(6) ? 0 : Convert.ToDecimal(reader.GetValue(6)),
					FromSaleOrder = !reader.IsDBNull(7) && Convert.ToBoolean(reader.GetValue(7))
				});
			}
			return rows;
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
