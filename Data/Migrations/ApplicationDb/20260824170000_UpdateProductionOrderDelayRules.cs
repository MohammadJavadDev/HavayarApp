using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations.ApplicationDb
{
	[DbContext(typeof(ApplicationDbContext))]
	[Migration("20260824170000_UpdateProductionOrderDelayRules")]
	public partial class UpdateProductionOrderDelayRules : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			var commonQuery = """
;WITH ItemCalc AS (
    SELECT
        poi.Id AS ProductionOrderItemId,
        poi.ProductionOrderId,
        poi.PreparationMiladiDate,
        poi.AgreedDeliverDate,
        poi.StandardDeliveryMiladiDate,
        poi.SendToIndustrialMiladiDate,
        CASE WHEN poi.PreparationMiladiDate IS NULL THEN 1 ELSE 0 END AS IsPreparationMissing,
        CASE
            WHEN poi.AgreedDeliverDate IS NULL THEN poi.StandardDeliveryMiladiDate
            WHEN poi.StandardDeliveryMiladiDate IS NULL THEN poi.AgreedDeliverDate
            WHEN poi.AgreedDeliverDate >= poi.StandardDeliveryMiladiDate THEN poi.AgreedDeliverDate
            ELSE poi.StandardDeliveryMiladiDate
        END AS ItemDeliverDate
    FROM Sale.ProductionOrderItem poi
    INNER JOIN Sale.ProductionOrder po ON po.Id = poi.ProductionOrderId
    WHERE poi.IsDeleted = 0
      AND poi.IsLatestVersion = 1
      AND poi.Status <> 581
      AND poi.SendToIndustrialMiladiDate IS NOT NULL
      AND po.IsActive = 1
      AND po.ProjectManagerId IS NULL
      AND po.DelayNotCalculated = 0
),
ItemDelay AS (
    SELECT
        ic.*,
        CASE
            WHEN ic.ItemDeliverDate IS NULL THEN 0
            WHEN ic.StandardDeliveryMiladiDate IS NULL
                 AND ic.AgreedDeliverDate IS NOT NULL
                 AND CAST(ic.AgreedDeliverDate AS date) < CAST(ic.SendToIndustrialMiladiDate AS date)
                 AND ic.PreparationMiladiDate IS NOT NULL
                 AND CAST(ic.PreparationMiladiDate AS date) = CAST(ic.SendToIndustrialMiladiDate AS date)
                THEN 0
            WHEN ic.PreparationMiladiDate IS NOT NULL
                THEN CASE
                    WHEN DATEDIFF(DAY, ic.ItemDeliverDate, ic.PreparationMiladiDate) > 0
                        THEN DATEDIFF(DAY, ic.ItemDeliverDate, ic.PreparationMiladiDate)
                    ELSE 0
                END
            WHEN CAST(GETDATE() AS date) > CAST(ic.ItemDeliverDate AS date)
                THEN DATEDIFF(DAY, ic.ItemDeliverDate, CAST(GETDATE() AS date))
            ELSE 0
        END AS ItemDelayDays
    FROM ItemCalc ic
),
RankedItemDelay AS (
    SELECT
        idl.*,
        ROW_NUMBER() OVER (
            PARTITION BY idl.ProductionOrderId
            ORDER BY idl.ItemDelayDays DESC, idl.ProductionOrderItemId
        ) AS rn
    FROM ItemDelay idl
),
MaxDelayItem AS (
    SELECT
        ProductionOrderId,
        ItemDeliverDate AS MaxDelayItemDeliverDate,
        PreparationMiladiDate AS MaxDelayItemPreparationDate
    FROM RankedItemDelay
    WHERE rn = 1
),
OrderAgg AS (
    SELECT
        idl.ProductionOrderId,
        MAX(idl.ItemDelayDays) AS MaxDelayDays,
        SUM(idl.IsPreparationMissing) AS MissingPreparationCount,
        COUNT(*) AS TotalItemsCount
    FROM ItemDelay idl
    GROUP BY idl.ProductionOrderId
)
""";

			var needsRegistrationQuery = commonQuery + """
, DelayAgg AS (
    SELECT
        pod.ProductionOrderId,
        SUM(ISNULL(pod.DelayDays, 0)) AS RegisteredDelayDays
    FROM Pln.ProductionOrderDelay pod
    WHERE pod.IsActive = 1
    GROUP BY pod.ProductionOrderId
)

--!--mainsection
SELECT
    r.ProductionOrderId,
    po.ProductionOrderNumber,
    mdi.MaxDelayItemDeliverDate AS MaxDeliverDateOfOrder,
    dbo.fn_MiladiToShamsi(mdi.MaxDelayItemDeliverDate) AS MaxDeliverDateOfOrderShamsi,
    mdi.MaxDelayItemPreparationDate AS PreparationDate,
    dbo.fn_MiladiToShamsi(mdi.MaxDelayItemPreparationDate) AS PreparationDateShamsi,
    r.MaxDelayDays AS DelayDay,
    CASE WHEN r.MissingPreparationCount = 0 THEN N'کامل شده' ELSE N'کامل نشده' END AS PreparationStatus,
    r.MissingPreparationCount,
    r.TotalItemsCount,
    ISNULL(da.RegisteredDelayDays, 0) AS RegisteredDelayDays,
    r.MaxDelayDays - ISNULL(da.RegisteredDelayDays, 0) AS NeedsDelayRegistrationDays
FROM OrderAgg r
INNER JOIN Sale.ProductionOrder po ON po.Id = r.ProductionOrderId
LEFT JOIN DelayAgg da ON da.ProductionOrderId = r.ProductionOrderId
LEFT JOIN MaxDelayItem mdi ON mdi.ProductionOrderId = r.ProductionOrderId
WHERE r.MaxDelayDays > 0
  AND ISNULL(da.RegisteredDelayDays, 0) < r.MaxDelayDays
""";

			var allDelayedOrdersQuery = commonQuery + """

--!--mainsection
SELECT
    r.ProductionOrderId,
    po.ProductionOrderNumber,
    mdi.MaxDelayItemDeliverDate AS MaxDeliverDateOfOrder,
    dbo.fn_MiladiToShamsi(mdi.MaxDelayItemDeliverDate) AS MaxDeliverDateOfOrderShamsi,
    mdi.MaxDelayItemPreparationDate AS PreparationDate,
    dbo.fn_MiladiToShamsi(mdi.MaxDelayItemPreparationDate) AS PreparationDateShamsi,
    r.MaxDelayDays AS DelayDay,
    CASE WHEN r.MissingPreparationCount = 0 THEN N'کامل شده' ELSE N'کامل نشده' END AS PreparationStatus,
    r.MissingPreparationCount,
    r.TotalItemsCount
FROM OrderAgg r
INNER JOIN Sale.ProductionOrder po ON po.Id = r.ProductionOrderId
LEFT JOIN MaxDelayItem mdi ON mdi.ProductionOrderId = r.ProductionOrderId
WHERE r.MaxDelayDays > 0
""";

			UpdateProfileQuery(migrationBuilder, "vw_All_Need_POD", needsRegistrationQuery);
			UpdateProfileQuery(migrationBuilder, "vw_All_POD", allDelayedOrdersQuery);

			migrationBuilder.Sql("""
UPDATE [system].[SavedQuery]
SET ColumnsJson = JSON_MODIFY(
        JSON_MODIFY(ColumnsJson, '$[3].DisplayName', N'تاریخ مجاز قلم دارای بیشترین تأخیر'),
        '$[4].DisplayName', N'تاریخ مجاز قلم دارای بیشترین تأخیر')
WHERE Name IN (N'vw_All_Need_POD', N'vw_All_POD')
  AND EntityFullName = N'entities.app.pln.productionorderdelay'
  AND ISJSON(ColumnsJson) = 1
  AND JSON_VALUE(ColumnsJson, '$[3].ColumnName') = N'MaxDeliverDateOfOrderShamsi'
  AND JSON_VALUE(ColumnsJson, '$[4].ColumnName') = N'MaxDeliverDateOfOrder';
""");
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			// این مهاجرت فقط تعریف نمایه‌های داده را با قواعد کسب‌وکار جاری همگام می‌کند.
			// بازگردانی خودکار، تغییرات احتمالی بعدی کاربران روی همان نمایه‌ها را از بین می‌برد.
		}

		private static void UpdateProfileQuery(MigrationBuilder migrationBuilder, string profileName, string query)
		{
			var escapedQuery = query.Replace("'", "''");
			var escapedProfileName = profileName.Replace("'", "''");

			migrationBuilder.Sql($"""
UPDATE [system].[SavedQuery]
SET QueryJson = JSON_MODIFY(QueryJson, '$.CustomQuery', N'{escapedQuery}')
WHERE Name = N'{escapedProfileName}'
  AND EntityFullName = N'entities.app.pln.productionorderdelay'
  AND ISJSON(QueryJson) = 1;
""");
		}
	}
}
