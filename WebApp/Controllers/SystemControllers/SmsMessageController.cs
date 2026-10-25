using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.Base;
using Entities.Base.DataTable;
using Entities.Base.Sms;
using Entities.Base.Sms.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Sms;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
	/// <summary>
	/// رهگیری پیامک‌ها: لیست (Data Profile روی همین موجودیت)، صفحه جزئیات با سابقه تلاش‌ها، ارسال مجدد و لغو.
	/// ثبت پیامک جدید فقط از <see cref="ISmsService"/> انجام می‌شود، نه از این صفحه.
	/// </summary>
	[Route("Panel/System/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("پیامک‌ها", typeof(SmsMessage))]
	public class SmsMessageController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List()
		{
			return View(@"\Views\Panel\System\SmsMessage\List.cshtml");
		}

		[HttpGet("[action]")]
		[ActionDisplayName("جزئیات پیامک", ActionAccessType.View, ActionAccessItemType.Show)]
		public async Task<IActionResult> Detail(long id, CancellationToken cn)
		{
			var message = await unitOfWork.Repository<SmsMessage>().TableNoTracking
				.Include(x => x.Attempts)
				.FirstOrDefaultAsync(x => x.Id == id, cn);
			if (message == null)
				return NotFound();

			var eventInfo = SmsEventKeys.Find(message.EventKey);
			var model = new SmsMessageDetailViewModel
			{
				Message = message,
				EventTitle = eventInfo?.Title ?? message.EventKey,
				StatusTitle = EnumTitle(message.Status),
				CategoryTitle = EnumTitle(message.Category),
				DeliveryStateTitle = message.DeliveryStateCode == null ? null : SmsDeliveryStates.Text(message.DeliveryStateCode),
				Attempts = message.Attempts
					.OrderBy(a => a.AttemptNumber)
					.Select(a => new SmsAttemptRow
					{
						AttemptNumber = a.AttemptNumber,
						ResultTitle = EnumTitle(a.ResultStatus),
						ProviderMessageId = a.ProviderMessageId,
						ProviderStatusCode = a.ProviderStatusCode,
						ErrorMessage = a.ErrorMessage,
						DurationMs = a.DurationMs,
						CreatedOnShamsiDateTime = a.CreatedOnShamsiDateTime
					})
					.ToList()
			};

			return View(@"\Views\Panel\System\SmsMessage\Detail.cshtml", model);
		}

		/// <summary>ارسال مجدد: فقط پیامک‌های ناموفق، تحویل‌نشده یا لغوشده که متنشان هنوز موجود است.</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("ارسال مجدد پیامک", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Resend(long id, CancellationToken cn)
		{
			var message = await unitOfWork.Repository<SmsMessage>().Table.FirstOrDefaultAsync(x => x.Id == id, cn);
			if (message == null)
				return NotFound("پیامک یافت نشد.");

			if (message.Status is not (SmsStatusEnum.Failed or SmsStatusEnum.NotDelivered or SmsStatusEnum.Cancelled))
				return BadRequest("فقط پیامک‌های ناموفق، تحویل‌نشده یا لغوشده قابل ارسال مجدد هستند.");

			if (message.IsSensitive && message.SentOnMiladiDateTime != null)
				return BadRequest("متن این پیامک (کد تأیید/رمز) بعد از ارسال حذف شده و قابل ارسال مجدد نیست.");

			message.Status = SmsStatusEnum.Pending;
			message.AttemptCount = 0;
			message.ErrorMessage = null;
			message.ScheduledOnMiladiDateTime = null;
			message.ScheduledOnShamsiDateTime = null;
			message.ProviderMessageId = null;
			message.SentOnMiladiDateTime = null;
			message.SentOnShamsiDateTime = null;
			message.DeliveryStateCode = null;
			message.DeliveryCheckCount = 0;
			message.LastDeliveryCheckOnMiladiDateTime = null;

			await unitOfWork.Repository<SmsMessage>().UpdateAsync(message, cn, saveAudit: true);
			return Ok(new { id = message.Id, status = (int)message.Status });
		}

		/// <summary>لغو: فقط پیامک‌هایی که هنوز در صف‌اند.</summary>
		[HttpPost("[action]")]
		[ActionDisplayName("لغو پیامک", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Cancel(long id, CancellationToken cn)
		{
			var message = await unitOfWork.Repository<SmsMessage>().Table.FirstOrDefaultAsync(x => x.Id == id, cn);
			if (message == null)
				return NotFound("پیامک یافت نشد.");

			if (message.Status != SmsStatusEnum.Pending)
				return BadRequest("فقط پیامک‌های در صف قابل لغو هستند.");

			message.Status = SmsStatusEnum.Cancelled;
			await unitOfWork.Repository<SmsMessage>().UpdateAsync(message, cn, saveAudit: true);
			return Ok(new { id = message.Id, status = (int)message.Status });
		}

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<SmsMessage>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
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
			return Ok(await unitOfWork.Repository<SmsMessage>().FetchDataAsync(request, cn));
		}

		private static string EnumTitle(Enum value)
		{
			var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
			return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
		}
	}

	public class SmsMessageDetailViewModel
	{
		public SmsMessage Message { get; set; } = new();
		public string EventTitle { get; set; } = string.Empty;
		public string StatusTitle { get; set; } = string.Empty;
		public string CategoryTitle { get; set; } = string.Empty;
		public string? DeliveryStateTitle { get; set; }
		public List<SmsAttemptRow> Attempts { get; set; } = [];
	}

	public class SmsAttemptRow
	{
		public int AttemptNumber { get; set; }
		public string ResultTitle { get; set; } = string.Empty;
		public string? ProviderMessageId { get; set; }
		public string? ProviderStatusCode { get; set; }
		public string? ErrorMessage { get; set; }
		public int? DurationMs { get; set; }
		public string? CreatedOnShamsiDateTime { get; set; }
	}
}
