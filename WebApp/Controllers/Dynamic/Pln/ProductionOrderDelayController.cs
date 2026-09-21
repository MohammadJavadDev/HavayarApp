using Common.Attributes;
using Common.Auth.Enums;
using Common.Utilities;
using Data.Contracts;
using Data.SystemAuth;
using Entities.App.Pln;
using Entities.App.Pln.Enums;
using Entities.App.Sale;
using Entities.App.Sale.Enums;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("تاخیرات سفارش ساخت", typeof(ProductionOrderDelay))]
	public class ProductionOrderDelayController(IUnitOfWork unitOfWork, IPropertyIdentityService identityService, IWebHostEnvironment _webHostEnvironment) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(ProductionOrderDelay productionOrderDelay, CancellationToken cn)
		{
			NormalizeDelayDays(productionOrderDelay);

			if (productionOrderDelay.Id == null || productionOrderDelay.Id == 0)
			{
				return await Add(productionOrderDelay, cn);
			}
			var exist = await unitOfWork.Repository<ProductionOrderDelay>().TableNoTracking.AnyAsync(c => c.Id == productionOrderDelay.Id, cn);
			if (exist)
			{
				return await Update(productionOrderDelay, cn);
			}
			return await Add(productionOrderDelay, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(ProductionOrderDelay productionOrderDelay, CancellationToken cn)
		{
			NormalizeDelayDays(productionOrderDelay);
			var entity = await unitOfWork.Repository<ProductionOrderDelay>().SaveAsync(productionOrderDelay, cn, true);
			await SendDelayNotificationAsync(entity, isNew: true, cn);
			return Ok(entity);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(ProductionOrderDelay productionOrderDelay, CancellationToken cn)
		{
			NormalizeDelayDays(productionOrderDelay);
			var entity = await unitOfWork.Repository<ProductionOrderDelay>().UpdateAsync(productionOrderDelay, cn, true);
			await SendDelayNotificationAsync(entity, isNew: false, cn);
			return Ok(entity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var model = unitOfWork.Repository<ProductionOrderDelay>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (model != null)
				await unitOfWork.Repository<ProductionOrderDelay>().DeleteAsync(model, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			if (id != null && id != 0)
			{
				var entity = unitOfWork.Repository<ProductionOrderDelay>().TableNoTracking
					.Include(c => c.ProductionOrder)
					.Include(c => c.ProductionOrderItem)
					.FirstOrDefault(c => c.Id == id);

				if (entity?.ProductionOrderId != null)
				{
					LoadExistingDelays(entity.ProductionOrderId.Value);
					await LoadCalculatedDelayRangeAsync(entity.ProductionOrderId.Value, cn);
					await LoadProductionOrderDetailsAsync(entity.ProductionOrderId.Value, cn);
				}

				return View(@"\Views\Panel\Pln\ProductionOrderDelay\Edit.cshtml", entity);
			}
			var newEntity = new ProductionOrderDelay();
			return View(@"\Views\Panel\Pln\ProductionOrderDelay\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(long? productionOrderId = null, CancellationToken cn = default)
		{
			var newEntity = new ProductionOrderDelay();

			if (productionOrderId != null && productionOrderId > 0)
			{
				var productionOrder = unitOfWork.Repository<ProductionOrder>().TableNoTracking
					.FirstOrDefault(c => c.Id == productionOrderId);

				if (productionOrder != null)
				{
					newEntity.ProductionOrderId = productionOrder.Id;
					newEntity.ProductionOrder = productionOrder;
					LoadExistingDelays(productionOrder.Id!.Value);
					await LoadCalculatedDelayRangeAsync(productionOrder.Id!.Value, cn);
					await LoadProductionOrderDetailsAsync(productionOrder.Id!.Value, cn);
				}
			}

			return View(@"\Views\Panel\Pln\ProductionOrderDelay\Edit.cshtml", newEntity);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\Pln\ProductionOrderDelay\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست تاخیرات سفارش ساخت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetByProductionOrderId(long productionOrderId, CancellationToken cn)
		{
			var delays = await unitOfWork.Repository<ProductionOrderDelay>().TableNoTracking
				.Where(c => c.ProductionOrderId == productionOrderId)
				.OrderByDescending(c => c.DelayStartMiladiDate)
				.ThenByDescending(c => c.Id)
				.ToListAsync(cn);

			var result = delays.Select(c => new
			{
				c.Id,
				c.ProductionOrderId,
				c.DelayResponsible,
				DelayResponsibleTitle = c.DelayResponsible.ToDisplay(),
				c.DelayDays,
				c.DelayStartMiladiDate,
				c.DelayStartShamsiDate,
				c.DelayEndMiladiDate,
				c.DelayEndShamsiDate,
				c.DelayReason,
				c.Description,
				c.CreatedByName,
				c.CreatedOnShamsiDateTime
			});

			return Ok(result);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("کاربران دریافت‌کننده ایمیل عامل توقف", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetResponsibleUsers(DelayResponsibleEnum delayResponsible, CancellationToken cn)
		{
			var data = await unitOfWork.Repository<ProductionOrderDelayResponsibleUser>()
				.TableNoTracking
				.FirstOrDefaultAsync(c => c.DelayResponsible == delayResponsible, cn);

			return Ok(data ?? new ProductionOrderDelayResponsibleUser
			{
				DelayResponsible = delayResponsible
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("بازه تاخیر محاسبه‌شده سفارش ساخت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetCalculatedDelayRange(long productionOrderId, CancellationToken cn)
		{
			var range = await CalculateOrderDelayRangeAsync(productionOrderId, cn);
			return Ok(range);
		}

		[HttpGet("[action]")]
		[ActionDisplayName("اطلاعات و اقلام سفارش ساخت", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> GetProductionOrderDetails(long productionOrderId, CancellationToken cn)
		{
			var details = await GetProductionOrderDetailsInternalAsync(productionOrderId, cn);
			return Ok(details);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خارج کردن از سیکل تاخیر", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> ExcludeFromDelayCycle(long productionOrderId, CancellationToken cn)
		{
			if (productionOrderId <= 0)
				return BadRequest("سفارش ساخت انتخاب نشده است");

			var order = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == productionOrderId, cn);

			if (order == null)
				return BadRequest("سفارش ساخت یافت نشد");

			if (!order.DelayNotCalculated)
			{
				order.DelayNotCalculated = true;
				order = await unitOfWork.Repository<ProductionOrder>().UpdateAsync(order, cn, true);
			}

			return Ok(order);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = _webHostEnvironment.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<ProductionOrderDelay>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		[HttpPost("[action]")]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
		{
			return Ok(await unitOfWork.Repository<ProductionOrderDelay>().FetchDataAsync(request, cn));
		}

		private void LoadExistingDelays(long productionOrderId)
		{
			ViewBag.ExistingDelays = unitOfWork.Repository<ProductionOrderDelay>().TableNoTracking
				.Where(c => c.ProductionOrderId == productionOrderId)
				.OrderByDescending(c => c.DelayStartMiladiDate)
				.ThenByDescending(c => c.Id)
				.ToList();
		}

		private async Task LoadCalculatedDelayRangeAsync(long productionOrderId, CancellationToken cn = default)
		{
			ViewBag.CalculatedDelayRange = await CalculateOrderDelayRangeAsync(productionOrderId, cn);
		}

		private async Task<object?> CalculateOrderDelayRangeAsync(long productionOrderId, CancellationToken cn)
		{
			var today = DateTime.Today;
			var orderEligibility = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
				.Where(po => po.Id == productionOrderId)
				.Select(po => new
				{
					po.ProductionOrderNumber,
					po.ProjectManagerId,
					po.DelayNotCalculated
				})
				.FirstOrDefaultAsync(cn);

			// سفارش‌های پروژه‌ای (واقعی یا علامت‌گذاری‌شده) از سیکل تأخیرات خارج هستند.
			if (orderEligibility == null ||
				orderEligibility.ProjectManagerId.HasValue ||
				orderEligibility.DelayNotCalculated)
				return null;

			var items = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.Where(poi => poi.ProductionOrderId == productionOrderId
					&& poi.IsDeleted == false
					&& poi.IsLatestVersion == true
					&& poi.Status != ProductionOrderItemStatusEnum.Invalid
					// تاریخ ارسال، ملاک قطعی ورود قلم به کارتابل صنایع است.
					&& poi.SendToIndustrialMiladiDate.HasValue)
				.Select(poi => new
				{
					poi.Id,
					poi.PreparationMiladiDate,
					poi.AgreedDeliverDate,
					poi.StandardDeliveryMiladiDate,
					poi.SendToIndustrialMiladiDate,
					poi.Serial,
					PartCode = poi.Part != null ? poi.Part.Code : poi.PartCode,
					PartName = poi.Part != null ? poi.Part.Name : null
				})
				.ToListAsync(cn);

			if (items.Count == 0)
				return null;

			var itemDelays = items.Select(i =>
			{
				var itemDeliverDate = GetItemDeliverDate(i.AgreedDeliverDate, i.StandardDeliveryMiladiDate);
				var preparationDate = i.PreparationMiladiDate?.Date;
				return new
				{
					i.Id,
					i.Serial,
					i.PartCode,
					i.PartName,
					PreparationMiladiDate = preparationDate,
					IsPreparationMissing = preparationDate == null ? 1 : 0,
					ItemDeliverDate = itemDeliverDate,
					ItemDelayDays = CalcItemDelayDays(
						itemDeliverDate,
						preparationDate,
						i.AgreedDeliverDate,
						i.StandardDeliveryMiladiDate,
						i.SendToIndustrialMiladiDate,
						today)
				};
			}).ToList();

			var maxDelayDays = itemDelays.Max(i => i.ItemDelayDays);
			if (maxDelayDays <= 0)
				return null;

			var registeredDelayDays = await unitOfWork.Repository<ProductionOrderDelay>().TableNoTracking
				.Where(pod => pod.ProductionOrderId == productionOrderId
					&& pod.IsActive == IsActiveEnum.Active)
				.SumAsync(pod => (int?)(pod.DelayDays ?? 0), cn) ?? 0;

			if (registeredDelayDays >= maxDelayDays)
				return null;

			var maxDelayItem = itemDelays
				.OrderByDescending(i => i.ItemDelayDays)
				.ThenBy(i => i.Id)
				.First();

			// تاریخ مجاز نمایش‌داده‌شده باید متعلق به همان قلمی باشد که بیشترین تأخیر را ساخته است؛
			// نمایش بیشترین تاریخ مجاز کل سفارش کنار تأخیر یک قلم دیگر، عدد را متناقض نشان می‌داد.
			var maxDeliverNullable = maxDelayItem.ItemDeliverDate;
			var preparationDate = maxDelayItem.PreparationMiladiDate;
			var delayRangeStart = maxDelayItem.ItemDeliverDate;
			var delayRangeEnd = preparationDate ?? today;

			var missingPreparationCount = itemDelays.Sum(i => i.IsPreparationMissing);
			var needsDelayRegistrationDays = maxDelayDays - registeredDelayDays;

			return new
			{
				ProductionOrderId = productionOrderId,
				ProductionOrderNumber = orderEligibility.ProductionOrderNumber,
				DelayCausePartName = string.IsNullOrWhiteSpace(maxDelayItem.PartName) ? "-" : maxDelayItem.PartName,
				DelayCausePartCode = string.IsNullOrWhiteSpace(maxDelayItem.PartCode) ? "-" : maxDelayItem.PartCode,
				DelayCauseSerial = string.IsNullOrWhiteSpace(maxDelayItem.Serial) ? "-" : maxDelayItem.Serial,
				MaxDeliverDateOfOrder = maxDeliverNullable,
				MaxDeliverDateOfOrderShamsi = maxDeliverNullable?.ToShamsiDate(),
				PreparationDate = preparationDate,
				PreparationDateShamsi = preparationDate?.ToShamsiDate(),
				DelayDay = maxDelayDays,
				PreparationStatus = missingPreparationCount == 0 ? "کامل شده" : "کامل نشده",
				MissingPreparationCount = missingPreparationCount,
				TotalItemsCount = itemDelays.Count,
				RegisteredDelayDays = registeredDelayDays,
				NeedsDelayRegistrationDays = needsDelayRegistrationDays,
				// بازه مجاز انتخاب تاریخ: از تاریخ تحویل قلم با بیشترین تاخیر تا تاریخ آماده‌سازی (یا امروز)
				DelayRangeStart = delayRangeStart,
				DelayRangeStartShamsi = delayRangeStart?.ToShamsiDate(),
				DelayRangeEnd = delayRangeEnd,
				DelayRangeEndShamsi = delayRangeEnd.ToShamsiDate(),
				HasDelay = true
			};
		}

		private static void NormalizeDelayDays(ProductionOrderDelay delay)
		{
			if (delay.DelayStartMiladiDate.HasValue && delay.DelayEndMiladiDate.HasValue)
			{
				var days = (delay.DelayEndMiladiDate.Value.Date - delay.DelayStartMiladiDate.Value.Date).TotalDays;
				delay.DelayDays = days < 0 ? 0 : (int)days;
			}
		}

		private async Task SendDelayNotificationAsync(ProductionOrderDelay delay, bool isNew, CancellationToken cn)
		{
			try
			{
				var productionOrder = delay.ProductionOrder;
				if (productionOrder == null && delay.ProductionOrderId.HasValue)
				{
					productionOrder = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
						.FirstOrDefaultAsync(c => c.Id == delay.ProductionOrderId, cn);
				}

				var responsible = await unitOfWork.Repository<ProductionOrderDelayResponsibleUser>().TableNoTracking
					.FirstOrDefaultAsync(c => c.DelayResponsible == delay.DelayResponsible, cn);

				var toEmails = new List<string>();
				if (responsible?.UsersEmail != null)
				{
					toEmails.AddRange(responsible.UsersEmail
						.Where(e => !string.IsNullOrWhiteSpace(e)));
				}

				if (CurrentUserEmail.HasValue())
					toEmails.Add(CurrentUserEmail!);

				toEmails = toEmails
					.Where(e => !string.IsNullOrWhiteSpace(e))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();

				if (toEmails.Count == 0)
					return;

				var orderNumber = productionOrder?.ProductionOrderNumber?.ToString() ?? "-";
				var actionText = isNew ? "ثبت" : "ویرایش";
				var title = $"اعلان {actionText} تاخیر سفارش ساخت شماره {orderNumber}";
				var body = BuildDelayNotificationBody(delay, productionOrder, isNew);

				await unitOfWork.Repository<Notification>().AddAsync(new Notification
				{
					Type = NotificationType.Email,
					Title = title,
					Body = body,
					EntityId = delay.Id,
					OwnerId = CurrentUserId ?? 0,
					ViewPath = $"/Panel/ProductionOrderDelay/Edit?id={delay.Id}",
					IsRead = false,
					IsSend = false,
					ToEmails = toEmails,
					CcEmails = new List<string>()
				}, cn);

				await unitOfWork.SaveChangesAsync(cn);
			}
			catch
			{
				// شکست اعلان نباید ذخیره را متوقف کند
			}
		}

		private string BuildDelayNotificationBody(ProductionOrderDelay delay, ProductionOrder? productionOrder, bool isNew)
		{
			var actor = CurrentUserFullName ?? "سیستم";
			var orderNumber = productionOrder?.ProductionOrderNumber?.ToString() ?? "-";
			var actionText = isNew ? "ثبت" : "ویرایش";

			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl' width='100%'>");
			sb.AppendLine("<tr style='background: #000aa0'><td colspan='2'><div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center'>گروه صنعتی هوایار</div></td></tr>");
			sb.AppendLine("<tr><td colspan='2'><div style='font-size:12pt;font-family:Zar;text-align:right;direction:rtl'>");
			sb.Append($"احتراماَ تاخیر سفارش ساخت شماره <strong>{orderNumber}</strong> توسط <strong>{actor}</strong> {actionText} گردید.");
			sb.AppendLine("</div></td></tr>");
			sb.AppendLine($"<tr><td>شماره سفارش ساخت</td><td>{orderNumber}</td></tr>");
			sb.AppendLine($"<tr><td>عامل توقف</td><td>{delay.DelayResponsible.ToDisplay()}</td></tr>");
			sb.AppendLine($"<tr><td>تعداد روز تاخیر</td><td>{delay.DelayDays?.ToString() ?? "-"}</td></tr>");
			sb.AppendLine($"<tr><td>تاریخ شروع تاخیر</td><td>{delay.DelayStartShamsiDate ?? "-"}</td></tr>");
			sb.AppendLine($"<tr><td>تاریخ پایان تاخیر</td><td>{delay.DelayEndShamsiDate ?? "-"}</td></tr>");
			sb.AppendLine($"<tr><td>علت تاخیر</td><td>{(string.IsNullOrWhiteSpace(delay.DelayReason) ? "-" : delay.DelayReason)}</td></tr>");
			sb.AppendLine($"<tr><td>توضیحات</td><td>{(string.IsNullOrWhiteSpace(delay.Description) ? "-" : delay.Description)}</td></tr>");
			sb.AppendLine("</table></div>");
			return sb.ToString();
		}

		private async Task LoadProductionOrderDetailsAsync(long productionOrderId, CancellationToken cn = default)
		{
			ViewBag.ProductionOrderDetails = await GetProductionOrderDetailsInternalAsync(productionOrderId, cn);
		}

		private async Task<ProductionOrderDetailsDto?> GetProductionOrderDetailsInternalAsync(long productionOrderId, CancellationToken cn = default)
		{
			var order = await unitOfWork.Repository<ProductionOrder>().TableNoTracking
				.Where(po => po.Id == productionOrderId)
				.Include(po => po.Contract)
					.ThenInclude(c => c.Customer)
						.ThenInclude(cu => cu.Party)
				.Include(po => po.SalesAgency)
					.ThenInclude(sa => sa.Party)
				.FirstOrDefaultAsync(cn);

			if (order == null)
				return null;

			var customerTitle = order.Contract?.Customer?.Party?.FullName
				?? order.SalesAgency?.Party?.FullName;

			if (string.IsNullOrWhiteSpace(customerTitle))
			{
				customerTitle = order.ProductionOrderNumber == 0 ? "* برنامه ریزی" : "-";
			}

			var items = await unitOfWork.Repository<ProductionOrderItem>().TableNoTracking
				.Include(i => i.Part)
				.Where(poi => poi.ProductionOrderId == productionOrderId
					&& poi.IsDeleted == false
					&& poi.IsLatestVersion == true
					&& poi.Status != ProductionOrderItemStatusEnum.Invalid)
				.OrderBy(poi => poi.Id)
				.ToListAsync(cn);

			var today = DateTime.Today;
			var canCalculateDelay = !order.ProjectManagerId.HasValue && !order.DelayNotCalculated;

			// اقلام ارسالی به صنایع که ملاک ورود به کارتابل صنایع و محاسبه تاریخ مجاز هستند
			var eligibleItems = items
				.Where(i => i.SendToIndustrialMiladiDate.HasValue)
				.ToList();

			var itemDelayMap = new Dictionary<long, int>();
			if (canCalculateDelay)
			{
				foreach (var item in eligibleItems)
				{
					if (item.Id.HasValue)
					{
						var itemDeliverDate = GetItemDeliverDate(item.AgreedDeliverDate, item.StandardDeliveryMiladiDate);
						var delay = CalcItemDelayDays(
							itemDeliverDate,
							item.PreparationMiladiDate?.Date,
							item.AgreedDeliverDate,
							item.StandardDeliveryMiladiDate,
							item.SendToIndustrialMiladiDate,
							today);
						itemDelayMap[item.Id.Value] = delay;
					}
				}
			}

			var maxDelayDays = itemDelayMap.Values.Count > 0 ? itemDelayMap.Values.Max() : 0;

			var itemDtos = items.Select((item, index) =>
			{
				var delayDays = (item.Id.HasValue && itemDelayMap.TryGetValue(item.Id.Value, out var d)) ? d : 0;
				var isCause = maxDelayDays > 0 && delayDays == maxDelayDays;
				var isDelayed = delayDays > 0;
				var validStandardDate = GetValidDeliverDate(item.StandardDeliveryMiladiDate);

				return new ProductionOrderItemDetailDto
				{
					Index = index + 1,
					Id = item.Id,
					PartCode = item.Part?.Code ?? "-",
					PartName = item.Part?.Name ?? "-",
					Amount = item.Amount?.ToString("G29") ?? "-",
					Serial = item.Serial ?? "-",
					AgreedDeliverDate = item.AgreedDeliverDate,
					AgreedDeliverShamsiDate = item.AgreedDeliverDate?.ToShamsiDate() ?? "-",
					StandardDeliveryDate = validStandardDate,
					StandardDeliveryShamsiDate = validStandardDate?.ToShamsiDate() ?? "-",
					PreparationDate = item.PreparationMiladiDate,
					PreparationShamsiDate = (!string.IsNullOrWhiteSpace(item.PreparationShamsiDate) ? item.PreparationShamsiDate : item.PreparationMiladiDate?.ToShamsiDate()) ?? "-",
					SendToIndustrialMiladiDate = item.SendToIndustrialMiladiDate,
					ItemDelayDays = delayDays,
					IsDelayCause = isCause,
					IsDelayed = isDelayed
				};
			})
			.OrderByDescending(i => i.IsDelayCause)
			.ThenByDescending(i => i.ItemDelayDays)
			.ThenBy(i => i.Index)
			.ToList();

			return new ProductionOrderDetailsDto
			{
				ProductionOrderId = order.Id ?? productionOrderId,
				ProductionOrderNumber = order.ProductionOrderNumber,
				CustomerTitle = customerTitle,
				MaxDelayDays = maxDelayDays,
				HasDelay = maxDelayDays > 0,
				Items = itemDtos
			};
		}

		private static DateTime? GetValidDeliverDate(DateTime? date)
		{
			// DateTime.MaxValue وقتی زمان استاندارد قابل محاسبه نبود به‌عنوان نگهبان ذخیره می‌شود.
			if (date == null || date.Value.Year >= 9000)
				return null;
			return date.Value.Date;
		}

		private static DateTime? GetItemDeliverDate(DateTime? agreed, DateTime? standard)
		{
			if (agreed == null)
				return standard?.Date;
			if (standard == null)
				return agreed?.Date;
			return agreed.Value.Date >= standard.Value.Date ? agreed.Value.Date : standard.Value.Date;
		}

		private static int CalcItemDelayDays(
			DateTime? itemDeliverDate,
			DateTime? preparationDate,
			DateTime? agreedDeliverDate,
			DateTime? standardDeliverDate,
			DateTime? sendToIndustrialDate,
			DateTime todayDate)
		{
			if (itemDeliverDate == null)
				return 0;

			// وقتی زمان استاندارد وجود ندارد، فروش تاریخ مجاز را پیش از ورود قلم به
			// کارتابل صنایع ثبت کرده و صنایع نیز قلم را همان روز آماده کرده است، تأخیر صفر است.
			if (!standardDeliverDate.HasValue &&
				agreedDeliverDate.HasValue &&
				sendToIndustrialDate.HasValue &&
				preparationDate.HasValue &&
				agreedDeliverDate.Value.Date < sendToIndustrialDate.Value.Date &&
				preparationDate.Value.Date == sendToIndustrialDate.Value.Date)
				return 0;

			if (preparationDate != null)
				return Math.Max(0, (int)(preparationDate.Value.Date - itemDeliverDate.Value.Date).TotalDays);

			if (todayDate > itemDeliverDate.Value.Date)
				return (int)(todayDate - itemDeliverDate.Value.Date).TotalDays;

			return 0;
		}
	}

	public class ProductionOrderDetailsDto
	{
		public long ProductionOrderId { get; set; }
		public long? ProductionOrderNumber { get; set; }
		public string CustomerTitle { get; set; } = string.Empty;
		public int MaxDelayDays { get; set; }
		public bool HasDelay { get; set; }
		public List<ProductionOrderItemDetailDto> Items { get; set; } = new();
	}

	public class ProductionOrderItemDetailDto
	{
		public int Index { get; set; }
		public long? Id { get; set; }
		public string PartCode { get; set; } = string.Empty;
		public string PartName { get; set; } = string.Empty;
		public string Amount { get; set; } = string.Empty;
		public string Serial { get; set; } = string.Empty;
		public DateTime? AgreedDeliverDate { get; set; }
		public string AgreedDeliverShamsiDate { get; set; } = string.Empty;
		public DateTime? StandardDeliveryDate { get; set; }
		public string StandardDeliveryShamsiDate { get; set; } = string.Empty;
		public DateTime? PreparationDate { get; set; }
		public string PreparationShamsiDate { get; set; } = string.Empty;
		public DateTime? SendToIndustrialMiladiDate { get; set; }
		public int ItemDelayDays { get; set; }
		public bool IsDelayCause { get; set; }
		public bool IsDelayed { get; set; }
	}
}
