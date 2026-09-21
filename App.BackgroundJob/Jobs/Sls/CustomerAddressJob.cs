using Common.Attributes;
using Data;
using Microsoft.EntityFrameworkCore;
using Services.Job;

namespace App.BackgroundJob.Jobs.Sls
{
	/// <summary>
	/// معادل HTS WriteData.Update_Sales_CustomerAddressAgencyDls
	/// (HtsTaskService.AfterSalesAndRepairSystemTask هر ۲۰ دقیقه، ساعت ۶–۲۱).
	/// فقط ردیف‌هایی که AgencyDlId خالی است از آدرس پیش‌فرض ERPS Type=2 پر می‌شوند.
	/// </summary>
	public class CustomerAddressJob(ApplicationDbContext db)
	{
		[JobHandler("تخصیص تفصیل نمایندگی سایت مشتری از منطقه راهکاران")]
		public async Task FillEmptyAgencyDlFromRahkaranRegion(IJobLogger? jobLogger = null, CancellationToken cn = default)
		{
			var now = DateTime.Now;
			if (now.Hour < 6 || now.Hour > 21)
			{
				await jobLogger?.LogInfoAsync("خارج از بازه ۶ تا ۲۱ — اجرا نشد (مطابق HTS)", cn);
				return;
			}

			await jobLogger?.LogInfoAsync("شروع تخصیص تفصیل نمایندگی برای سایت‌های بدون DL", cn);

			// ERPS روی لینک‌سرور جدا (172.20.40.73) است؛ از [TMS].[TotalSystem].[ERPS]... نرو
			// چون بیشتر از ۳ پیشوند می‌شود. HTS روی TotalSystem از ERPS.Erps.GNR3.* استفاده می‌کند.

			var sql = """
				;WITH A AS
				(
					SELECT
						CASE
							WHEN ErpsAddress.RegionalDivisionRef IN (121000103, 300000047, 58, 59, 52, 89, 300000100, 37, 44)
							  OR ErpsRegionalDivision.ParentRef IN (121000103, 300000047, 58, 59, 52, 89, 300000100, 377)
								THEN 26118
							WHEN ErpsAddress.RegionalDivisionRef IN (62, 121000052, 72, 9)
							  OR ErpsRegionalDivision.ParentRef IN (62, 121000052, 72, 9)
								THEN 21480
							WHEN ErpsAddress.RegionalDivisionRef IN (12, 33, 34, 11, 13, 30, 49)
							  OR ErpsRegionalDivision.ParentRef IN (12, 33, 34, 11, 13, 30, 49)
								THEN 391729
							WHEN ErpsAddress.RegionalDivisionRef IN (15, 300000032, 300000096)
							  OR ErpsRegionalDivision.ParentRef IN (15, 300000032, 300000096)
								THEN 389616
							WHEN ErpsAddress.RegionalDivisionRef IN (26)
								THEN 387620
							WHEN ErpsAddress.RegionalDivisionRef IN (31, 66, 56, 98, 32, 50, 47, 73, 94)
								THEN 162365
							WHEN ErpsAddress.RegionalDivisionRef IN (8, 121000179, 121000002)
								THEN 383882
							WHEN ErpsAddress.RegionalDivisionRef IN (17, 18)
								THEN 14197
							WHEN ErpsAddress.RegionalDivisionRef IN (6, 19, 71, 121000114)
								THEN 387620
							WHEN ErpsAddress.RegionalDivisionRef IN (42, 43, 40, 45, 54, 121000049)
								THEN 378723
							WHEN CustomerAddress.Address_Title LIKE N'%تهران غرب%'
								THEN 380341
						END AS HtsAgencyDlId,
						CustomerAddress.Customer_Address_ID AS HtsAddressId
					FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_Address] AS CustomerAddress
					INNER JOIN [TMS].[TotalSystem].[dbo].[Crm_Customer] AS Customer
						ON Customer.Customer_ID = CustomerAddress.Customer_FK
					INNER JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] AS ManCompany
						ON ManCompany.ManCompany_ID = Customer.ManCompany_FK
					INNER JOIN [ERPS].[Erps].[GNR3].[Party] AS ErpsParty
						ON ErpsParty.PartyID = ManCompany.Hamkaran_ManCompany_FK
					INNER JOIN [ERPS].[Erps].[SLS3].[Customer] AS ErpsCustomer
						ON ErpsCustomer.PartyRef = ErpsParty.PartyID
					INNER JOIN [ERPS].[Erps].[SLS3].[CustomerAddress] AS ErpsCustomerAddress
						ON ErpsCustomerAddress.CustomerRef = ErpsCustomer.CustomerID
					INNER JOIN [ERPS].[Erps].[GNR3].[Address] AS ErpsAddress
						ON ErpsAddress.AddressID = ErpsCustomerAddress.AddressRef
					INNER JOIN [ERPS].[Erps].[GNR3].[RegionalDivision] AS ErpsRegionalDivision
						ON ErpsRegionalDivision.RegionalDivisionID = ErpsAddress.RegionalDivisionRef
					WHERE CustomerAddress.AgencyDlId IS NULL
					  AND ErpsCustomerAddress.IsDefault = 1
					  AND ErpsCustomerAddress.[Type] = 2
				)
				UPDATE ca
				SET
					ca.AgencyDlId = dl.Id,
					ca.AgencyPartyId = p.Id,
					ca.ModifiedById = 1,
					ca.ModifiedByName = N'job-customeraddress-agencydl',
					ca.ModifiedDateMiladiDateTime = SYSDATETIME(),
					ca.ModifiedDateShamsiDateTime = CONVERT(NVARCHAR(30), SYSDATETIME(), 120)
				FROM SLS.CustomerAddress AS ca
				INNER JOIN A ON A.HtsAddressId = ca.HtsId
				INNER JOIN [TMS].[TotalSystem].[dbo].[Acc_DL] AS ad ON ad.Acc_DL_ID = A.HtsAgencyDlId
				INNER JOIN FIN.DL AS dl ON dl.HamkaranId = ad.Hamkaran_Acc_DL_FK
				LEFT JOIN [TMS].[TotalSystem].[dbo].[Gnr_ManCompany] AS mc
					ON LTRIM(RTRIM(ISNULL(mc.DlRef, N''))) IN (
						LTRIM(RTRIM(CAST(ad.Hamkaran_Acc_DL_FK AS NVARCHAR(20)))),
						RIGHT(N'00000' + LTRIM(RTRIM(CAST(ad.Hamkaran_Acc_DL_FK AS NVARCHAR(20)))), 5)
					)
				LEFT JOIN Gnr.Party AS p ON p.HamkaranId = mc.Hamkaran_ManCompany_FK
				WHERE ca.AgencyDlId IS NULL
				  AND A.HtsAgencyDlId IS NOT NULL;
				""";

			var affected = await db.Database.ExecuteSqlRawAsync(sql, cn);
			await jobLogger?.LogInfoAsync($"تعداد ردیف به‌روزشده: {affected}", cn);
		}
	}
}
