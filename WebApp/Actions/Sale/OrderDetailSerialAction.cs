using System.Data;
using Data.Contracts;
using Data.Contracts.Actions;
using Data.SystemAuth;
using Entities.App.Sale;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using Services.ProductionOrderServices;
using System.Text;

namespace WebApp.Actions.Sale
{
	/// <summary>
	/// معادل ایمیل‌های DoOperation مانیتورینگ فروش HTS: خروج از کارخانه و راه‌اندازی.
	/// </summary>
	public class OrderDetailSerialAction(
		IUnitOfWork unitOfWork,
		IUserService userService,
		ISdk sdk,
		IConfiguration configuration)
	{
		public const string RoleFactoryExitNotify = "Sale.AfterSales.FactoryExitNotify";
		public const string RoleFactoryExitCustomerCall = "Sale.AfterSales.FactoryExitCustomerCall";
		public const string LaunchNotifyEmail = "Ahadi.s@havayar.com";

		private DateTime? _previousFinalExit;
		private string? _previousLaunch;

		[EntityAction(typeof(OrderDetailSerial), EntityActionTrigger.BeforeUpdate,
			"OrderDetailSerialCaptureMonitoringDates", "ثبت تاریخ قبلی خروج و راه‌اندازی برای ایمیل مانیتورینگ", Priority = 1)]
		public async Task CapturePreviousDates(OrderDetailSerial entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			var existing = await unitOfWork.Repository<OrderDetailSerial>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);
			_previousFinalExit = existing?.FinalExitMiladiDate;
			_previousLaunch = existing?.LaunchShamsiDate;
		}

		[EntityAction(typeof(OrderDetailSerial), EntityActionTrigger.AfterUpdate,
			"OrderDetailSerialMonitoringEmails", "ایمیل خروج از کارخانه و راه‌اندازی کالا", Priority = 1)]
		public async Task AfterUpdateEmails(OrderDetailSerial entity, CancellationToken ct)
		{
			if (entity.Id == null || entity.Id == 0)
				return;

			var finalExitChanged = entity.FinalExitMiladiDate.HasValue
				&& entity.FinalExitMiladiDate != _previousFinalExit;
			var launchChanged = !string.IsNullOrWhiteSpace(entity.LaunchShamsiDate)
				&& !string.Equals(entity.LaunchShamsiDate, _previousLaunch, StringComparison.Ordinal);

			if (!finalExitChanged && !launchChanged)
				return;

			var model = await unitOfWork.Repository<OrderDetailSerial>().TableNoTracking
				.Include(s => s.OrderDetail).ThenInclude(od => od.Part)
				.Include(s => s.OrderDetail).ThenInclude(od => od.Sale_Order).ThenInclude(o => o.Customer).ThenInclude(c => c.Party)
				.FirstOrDefaultAsync(c => c.Id == entity.Id, ct);
			if (model == null)
				return;

			var partCode = model.OrderDetail?.Part?.Code ?? "";
			var partName = model.OrderDetail?.Part?.Name ?? "";
			var customerTitle = model.OrderDetail?.Sale_Order?.Customer?.Party?.FullName ?? "";
			var customerCode = model.OrderDetail?.Sale_Order?.Customer?.Code?.ToString() ?? "";
			var userName = sdk.CurrentUser?.FullNameFn;
			if (string.IsNullOrWhiteSpace(userName))
				userName = sdk.CurrentUser?.FullName ?? "";
			var userEmail = ToHavayarEmail(sdk.CurrentUser?.Email ?? sdk.CurrentUser?.Username);

			if (finalExitChanged && (IsScrewCompressor(partCode) || IsPackageWithCompressor(partName)))
			{
				await SendExitEmailAsync(model, partCode, partName, customerTitle, customerCode, userName, userEmail, false, ct);
				await SendExitEmailAsync(model, partCode, partName, customerTitle, customerCode, userName, userEmail, true, ct);
			}

			if (launchChanged)
				await SendLaunchEmailAsync(model, partCode, partName, customerTitle, customerCode, userName, userEmail, ct);
		}

		private async Task SendExitEmailAsync(
			OrderDetailSerial entity,
			string partCode,
			string partName,
			string customerTitle,
			string customerCode,
			string userName,
			string? userEmail,
			bool forCustomerCall,
			CancellationToken ct)
		{
			var title = forCustomerCall
				? "اعلان خروج کالا از کارخانه - جهت برقراری تماس با مشتری"
				: "اعلان خروج کالا از کارخانه";
			ExitCrmInfo? crm = null;
			if (forCustomerCall)
				crm = await ResolveExitCrmInfoAsync(entity, ct);
			var body = BuildExitBody(entity, partCode, partName, customerTitle, customerCode, userName, crm);
			var roleName = forCustomerCall ? RoleFactoryExitCustomerCall : RoleFactoryExitNotify;
			var htsGroupId = forCustomerCall ? 570 : 542;
			var receivers = await ResolveRecipientsAsync(roleName, htsGroupId, ct);
			await AddNotificationsAsync(title, body, entity.Id, receivers, userEmail, ct);
		}

		private async Task SendLaunchEmailAsync(
			OrderDetailSerial entity,
			string partCode,
			string partName,
			string customerTitle,
			string customerCode,
			string userName,
			string? userEmail,
			CancellationToken ct)
		{
			var body = BuildLaunchBody(entity, partCode, partName, customerTitle, customerCode, userName);
			await AddNotificationsAsync(
				"اعلان راه اندازی کالا",
				body,
				entity.Id,
				new List<string> { LaunchNotifyEmail },
				userEmail,
				ct);
		}

		private async Task AddNotificationsAsync(
			string title,
			string body,
			long? entityId,
			List<string> toEmails,
			string? ccEmail,
			CancellationToken ct)
		{
			var unique = toEmails
				.Where(e => !string.IsNullOrWhiteSpace(e))
				.Select(e => e.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
			if (unique.Count == 0)
				return;

			var ownerId = sdk.CurrentUser?.Id ?? 1;
			var cc = string.IsNullOrWhiteSpace(ccEmail) ? null : new List<string> { ccEmail };
			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = title,
				Body = body,
				EntityId = entityId,
				OwnerId = ownerId,
				ViewPath = "/Panel/Sale/OrderDetailSerial/MonitoringEdit?id=" + entityId,
				IsRead = false,
				IsSend = false,
				ToEmails = unique,
				CcEmails = cc
			}, ct);
			await unitOfWork.SaveChangesAsync(ct);
		}

		private async Task<List<string>> ResolveRecipientsAsync(string roleName, int htsGroupId, CancellationToken ct)
		{
			var emails = new List<string>();
			var roleUsers = await userService.GetUsersByRoleName(roleName);
			if (roleUsers != null)
			{
				foreach (var user in roleUsers)
				{
					var mail = ToHavayarEmail(user.Email ?? user.Username);
					if (!string.IsNullOrWhiteSpace(mail))
						emails.Add(mail);
				}
			}

			if (emails.Count > 0)
				return emails;

			emails.AddRange(await QueryHtsGroupEmailsAsync(htsGroupId, ct));
			return emails;
		}

		private async Task<List<string>> QueryHtsGroupEmailsAsync(int groupId, CancellationToken ct)
		{
			var cs = configuration.GetConnectionString("Hts");
			if (string.IsNullOrWhiteSpace(cs))
				return [];

			const string sql = @"
SELECT DISTINCT NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N'')
FROM dbo.Gnr_UserGroupMember m
INNER JOIN dbo.Gnr_User u ON u.User_ID = m.User_FK
WHERE m.UserGroup_FK = @groupId
  AND NULLIF(LTRIM(RTRIM(u.ActiveDirectoryUsername)), N'') IS NOT NULL";

			try
			{
				await using var conn = new SqlConnection(cs);
				await conn.OpenAsync(ct);
				await using var cmd = new SqlCommand(sql, conn);
				cmd.Parameters.Add(new SqlParameter("@groupId", groupId));
				await using var reader = await cmd.ExecuteReaderAsync(ct);
				var list = new List<string>();
				while (await reader.ReadAsync(ct))
				{
					var mail = ToHavayarEmail(reader.GetString(0));
					if (!string.IsNullOrWhiteSpace(mail))
						list.Add(mail);
				}
				return list;
			}
			catch
			{
				return [];
			}
		}

		/// <summary>
		/// کارشناس، نماینده مشتری و محل نصب برای ایمیل تماس با مشتری.
		/// اول از سفارش ساخت همین سریال و اعلام نیازمندی وصل به آن خوانده می‌شود.
		/// اگر آنجا خالی بود، همان مسیر راهکاران (سفارش ساخت، و در نبود آن پیش‌نویس قرارداد) استفاده می‌شود.
		/// </summary>
		private async Task<ExitCrmInfo?> ResolveExitCrmInfoAsync(OrderDetailSerial entity, CancellationToken ct)
		{
			var local = await QueryLocalSalesInfoAsync(entity.ProductionOrderNumber, ct);
			if (HasCustomerContact(local))
				return local;

			var htsOrderDetailId = entity.OrderDetail?.HtsId ?? 0;
			var remote = await QuerySalesInfoForCrmAsync(htsOrderDetailId, entity.ProductionOrderNumber, ct);
			return MergeCrm(local, remote);
		}

		/// <summary>
		/// سفارش ساخت (شماره روی سریال) ← اعلام نیازمندی.
		/// کارشناس از کاربر سفارش ساخت است؛ شخصِ اعلام نیازمندی با شناسه کارمند راهکاران اشتباه نگاشت شده است.
		/// </summary>
		private async Task<ExitCrmInfo?> QueryLocalSalesInfoAsync(string? productionOrderNumber, CancellationToken ct)
		{
			if (!long.TryParse(productionOrderNumber?.Trim(), out var number) || number <= 0)
				return null;

			var po = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
				.Where(p => p.ProductionOrderNumber == number)
				.Select(p => new
				{
					Expert = p.SalesExpert != null
						? (p.SalesExpert.NameFa != null && p.SalesExpert.NameFa != "" ? p.SalesExpert.NameFa : p.SalesExpert.Name)
						: null,
					Agent = p.RequirmentAdvertise != null ? p.RequirmentAdvertise.RepresentativeDetails : null,
					Phone = p.RequirmentAdvertise != null ? p.RequirmentAdvertise.RepresentativeContactNumber : null,
					Province = p.RequirmentAdvertise != null && p.RequirmentAdvertise.InstallationProvince != null
						? p.RequirmentAdvertise.InstallationProvince.Name
						: null,
					City = p.InstallationCity != null
						? p.InstallationCity.Name
						: (p.RequirmentAdvertise != null && p.RequirmentAdvertise.InstallationCity != null
							? p.RequirmentAdvertise.InstallationCity.Name
							: null),
					HasRequirement = p.RequirmentAdvertiseId != null
				})
				.FirstOrDefaultAsync(ct);

			var fromOrder = po == null
				? null
				: new ExitCrmInfo(po.Expert, po.Agent, po.Phone, po.Province, po.City);

			if (po is { HasRequirement: true })
				return fromOrder;

			var req = await unitOfWork.Repository<RequirmentAdvertise>().TableNoTracking
				.Where(r => r.ProductionOrderNumber == number)
				.OrderByDescending(r => r.Id)
				.Select(r => new
				{
					Agent = r.RepresentativeDetails,
					Phone = r.RepresentativeContactNumber,
					Province = r.InstallationProvince != null ? r.InstallationProvince.Name : null,
					City = r.InstallationCity != null ? r.InstallationCity.Name : null
				})
				.FirstOrDefaultAsync(ct);

			if (req == null)
				return fromOrder;

			return MergeCrm(fromOrder, new ExitCrmInfo(null, req.Agent, req.Phone, req.Province, req.City));
		}

		/// <summary>
		/// معادل HTS <c>ReadData.GetSalesInfoForCrm</c>، با این تفاوت که اعلام نیازمندی اول از سفارش ساخت
		/// (RequirementAdvertiseIdRef) خوانده می‌شود. پیش‌نویس قرارداد در خیلی از سفارش‌ها به اعلام نیازمندی وصل نیست.
		/// </summary>
		private async Task<ExitCrmInfo?> QuerySalesInfoForCrmAsync(long htsOrderDetailId, string? productionOrderNumber, CancellationToken ct)
		{
			var cs = configuration.GetConnectionString("Hts");
			if (string.IsNullOrWhiteSpace(cs))
				return null;

			const string sql = @"
SELECT TOP (1)
    SalesExpert, CustomerAgentInfo, CustomerAgentPhone, Proviance, City
FROM (
    SELECT
        1 AS Src,
        expert.FullName AS SalesExpert,
        ra.CustomerAgentInfo,
        ra.CustomerAgentPhone,
        prov.Name AS Proviance,
        COALESCE(city.Name, reqCity.Name) AS City
    FROM ERPS.Erps.USR3.Sale_ProductionOrder po
    INNER JOIN ERPS.Erps.USR3.Sale_RequirmentAdvertise ra
        ON ra.Sale_RequirmentAdvertiseID = COALESCE(
            po.RequirementAdvertiseIdRef,
            (
                SELECT TOP (1) r2.Sale_RequirmentAdvertiseID
                FROM ERPS.Erps.USR3.Sale_RequirmentAdvertise r2
                WHERE r2.ProductionNumber = po.ProductionOrderNumber
                ORDER BY r2.Sale_RequirmentAdvertiseID DESC
            ))
    LEFT JOIN ERPS.Erps.GNR3.RegionalDivision prov ON prov.RegionalDivisionID = ra.RegionalDivisionIdRef
    LEFT JOIN ERPS.Erps.HCM3.Employee emp ON emp.EmployeeID = ra.SalesExpertIdRef
    LEFT JOIN ERPS.Erps.GNR3.Party expert ON expert.PartyID = emp.PartyRef
    LEFT JOIN ERPS.Erps.GNR3.RegionalDivision city ON city.RegionalDivisionID = po.InstallationCityIdRef
    LEFT JOIN ERPS.Erps.GNR3.RegionalDivision reqCity ON reqCity.RegionalDivisionID = ra.InstallationCityRef
    WHERE @productionOrderNumber IS NOT NULL
      AND po.ProductionOrderNumber = TRY_CONVERT(decimal(28, 8), @productionOrderNumber)

    UNION ALL

    SELECT
        2 AS Src,
        ErpsSalesExpertParty.FullName AS SalesExpert,
        ErpsRequirmentAdvertise.CustomerAgentInfo,
        ErpsRequirmentAdvertise.CustomerAgentPhone,
        ErpsRequirmentAdvertiseRegionalDivision.Name AS Proviance,
        ErpsProductionOrderRegionalDivision.Name AS City
    FROM dbo.Sale_OrderDetail AS SaleOrderDetail
    INNER JOIN dbo.Sale_Order AS HtsOrder ON HtsOrder.Order_ID = SaleOrderDetail.Order_FK
    INNER JOIN dbo.Crm_Customer AS HtsCustomer ON HtsCustomer.Customer_ID = HtsOrder.Customer_FK
    INNER JOIN dbo.Gnr_ManCompany AS HtsManCompany ON HtsManCompany.ManCompany_ID = HtsCustomer.ManCompany_FK
    JOIN ERPS.Erps.SLS3.OrderItem AS ErpsOrderItem ON ErpsOrderItem.OrderItemID = SaleOrderDetail.Hamkaran_Vchitm_FK
    JOIN ERPS.Erps.SLS3.[Order] AS ErpsOrder ON ErpsOrder.OrderID = ErpsOrderItem.OrderRef
    LEFT JOIN ERPS.Erps.SLS3.Contract AS ErpsContract ON ErpsContract.ContractID = ErpsOrder.ContractRef
    LEFT JOIN ERPS.Erps.USR3.Sale_ContractDraft AS ErpsSaleContractDraft ON ErpsSaleContractDraft.ContractIdRef = ErpsContract.ContractID
    LEFT JOIN ERPS.Erps.USR3.Sale_RequirmentAdvertise AS ErpsRequirmentAdvertise ON ErpsRequirmentAdvertise.Sale_RequirmentAdvertiseID = ErpsSaleContractDraft.RequirementAdvertiseIdRef
    LEFT JOIN ERPS.Erps.GNR3.RegionalDivision AS ErpsRequirmentAdvertiseRegionalDivision ON ErpsRequirmentAdvertiseRegionalDivision.RegionalDivisionID = ErpsRequirmentAdvertise.RegionalDivisionIdRef
    LEFT JOIN ERPS.Erps.HCM3.Employee AS ErpsProductionOrderSalesExpert ON ErpsProductionOrderSalesExpert.EmployeeID = ErpsRequirmentAdvertise.SalesExpertIdRef
    LEFT JOIN ERPS.Erps.GNR3.Party AS ErpsSalesExpertParty ON ErpsSalesExpertParty.PartyID = ErpsProductionOrderSalesExpert.PartyRef
    LEFT JOIN ERPS.Erps.USR3.Sale_ProductionOrder ErpsProductionOrder ON ErpsProductionOrder.RequirementAdvertiseIdRef = ErpsRequirmentAdvertise.Sale_RequirmentAdvertiseID
    LEFT JOIN ERPS.Erps.GNR3.RegionalDivision AS ErpsProductionOrderRegionalDivision ON ErpsProductionOrderRegionalDivision.RegionalDivisionID = ErpsProductionOrder.InstallationCityIdRef
    WHERE SaleOrderDetail.OrderDetail_ID = @orderDetailId
      AND ErpsRequirmentAdvertise.Sale_RequirmentAdvertiseID IS NOT NULL
) src
ORDER BY Src";

			try
			{
				await using var conn = new SqlConnection(cs);
				await conn.OpenAsync(ct);
				await using var cmd = new SqlCommand(sql, conn);
				cmd.Parameters.Add(new SqlParameter("@orderDetailId", htsOrderDetailId));
				cmd.Parameters.Add(new SqlParameter("@productionOrderNumber", SqlDbType.NVarChar, 50)
				{
					Value = string.IsNullOrWhiteSpace(productionOrderNumber) ? DBNull.Value : productionOrderNumber.Trim()
				});
				await using var reader = await cmd.ExecuteReaderAsync(ct);
				if (!await reader.ReadAsync(ct))
					return null;

				return new ExitCrmInfo(
					ReadNullableString(reader, 0),
					ReadNullableString(reader, 1),
					ReadNullableString(reader, 2),
					ReadNullableString(reader, 3),
					ReadNullableString(reader, 4));
			}
			catch
			{
				return null;
			}
		}

		private static bool HasCustomerContact(ExitCrmInfo? info) =>
			info != null && (
				!string.IsNullOrWhiteSpace(info.CustomerAgentInfo)
				|| !string.IsNullOrWhiteSpace(info.CustomerAgentPhone));

		private static ExitCrmInfo? MergeCrm(ExitCrmInfo? primary, ExitCrmInfo? fallback)
		{
			if (primary == null)
				return fallback;
			if (fallback == null)
				return primary;

			return new ExitCrmInfo(
				FirstText(primary.SalesExpert, fallback.SalesExpert),
				FirstText(primary.CustomerAgentInfo, fallback.CustomerAgentInfo),
				FirstText(primary.CustomerAgentPhone, fallback.CustomerAgentPhone),
				FirstText(primary.Province, fallback.Province),
				FirstText(primary.City, fallback.City));
		}

		private static string? FirstText(string? preferred, string? fallback) =>
			string.IsNullOrWhiteSpace(preferred)
				? (string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim())
				: preferred.Trim();

		private static string? ReadNullableString(SqlDataReader reader, int ordinal) =>
			reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

		private static string BuildExitBody(
			OrderDetailSerial entity,
			string partCode,
			string partName,
			string customerTitle,
			string customerCode,
			string userName,
			ExitCrmInfo? crm)
		{
			var items = new StringBuilder();
			items.Append(ProductionOrderItemNotificationHelper.Li("عنوان کالا", partName));
			items.Append(ProductionOrderItemNotificationHelper.Li("کد کالا", partCode));
			items.Append($"<li style='color:red'>سریال : <strong>{entity.Serial}</strong></li>");
			items.Append(ProductionOrderItemNotificationHelper.Li("تاریخ خروج نهایی", entity.FinalExitShamsiDate));
			items.Append(ProductionOrderItemNotificationHelper.Li("ساعت خروج نهایی", entity.ExitTimeFinal));
			if (crm != null)
			{
				items.Append(ProductionOrderItemNotificationHelper.Li("کارشناس فروش", crm.SalesExpert));
				items.Append(ProductionOrderItemNotificationHelper.Li("نام و سمت نماینده مشتری", crm.CustomerAgentInfo));
				items.Append(ProductionOrderItemNotificationHelper.Li("شماره تماس نماینده مشتری", crm.CustomerAgentPhone));
				items.Append(ProductionOrderItemNotificationHelper.Li("استان محل نصب", crm.Province));
				items.Append(ProductionOrderItemNotificationHelper.Li("شهر محل نصب", crm.City));
				items.Append(ProductionOrderItemNotificationHelper.Li("اطلاعات گارانتی", "نامشخص"));
			}

			return ProductionOrderItemNotificationHelper.BuildEmailShell(
				$"به استحضار می رساند، تاریخ خروج نهایی کالا مربوط به مشتری {customerTitle} "
				+ $"با کد مشتری {customerCode} "
				+ "با مشخصات زیر "
				+ $"بواسطه {userName} در سیستم ثبت گردید"
				+ "<br /><br /><ul>"
				+ items
				+ "</ul>");
		}

		private static string BuildLaunchBody(
			OrderDetailSerial entity,
			string partCode,
			string partName,
			string customerTitle,
			string customerCode,
			string userName)
		{
			var items = new StringBuilder();
			items.Append(ProductionOrderItemNotificationHelper.Li("عنوان کالا", partName));
			items.Append(ProductionOrderItemNotificationHelper.Li("کد کالا", partCode));
			items.Append(ProductionOrderItemNotificationHelper.Li("سریال", entity.Serial));
			items.Append(ProductionOrderItemNotificationHelper.Li("تاریخ خروج نهایی", entity.FinalExitShamsiDate));
			items.Append(ProductionOrderItemNotificationHelper.Li("ساعت خروج نهایی", entity.ExitTimeFinal));
			items.Append(ProductionOrderItemNotificationHelper.Li("تاریخ راه اندازی", entity.LaunchShamsiDate));

			return ProductionOrderItemNotificationHelper.BuildEmailShell(
				$"به استحضار می رساند، تاریخ راه اندازی کالا مربوط به مشتری {customerTitle} "
				+ $"با کد مشتری {customerCode} "
				+ "با مشخصات زیر "
				+ $"بواسطه {userName} در سیستم ثبت گردید"
				+ "<br /><br /><ul>"
				+ items
				+ "</ul>");
		}

		private static string? ToHavayarEmail(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;
			var trimmed = value.Trim();
			return trimmed.Contains('@') ? trimmed : trimmed + "@havayar.com";
		}

		private static bool IsScrewCompressor(string partCode)
		{
			if (string.IsNullOrWhiteSpace(partCode) || !partCode.StartsWith("1801", StringComparison.Ordinal))
				return false;
			string[] excluded =
			[
				"180110", "180114", "180116", "180117", "180118", "180120", "180121",
				"180125", "180127", "1801281", "180135", "180140", "180141", "180142",
				"180143", "180144", "180145"
			];
			return excluded.All(prefix => !partCode.StartsWith(prefix, StringComparison.Ordinal));
		}

		private static bool IsPackageWithCompressor(string partName)
		{
			if (string.IsNullOrWhiteSpace(partName))
				return false;
			var hasPackage = partName.Contains("پکیج", StringComparison.Ordinal)
				|| partName.Contains("پكيج", StringComparison.Ordinal);
			return hasPackage && partName.Contains("hy", StringComparison.OrdinalIgnoreCase);
		}

		private sealed record ExitCrmInfo(
			string? SalesExpert,
			string? CustomerAgentInfo,
			string? CustomerAgentPhone,
			string? Province,
			string? City);
	}
}
