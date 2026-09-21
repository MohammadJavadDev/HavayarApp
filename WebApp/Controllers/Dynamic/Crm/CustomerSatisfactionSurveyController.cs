using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Crm;
using Entities.App.SLS;
using Entities.Base.DataTable;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Crm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("رضایت‌سنجی بعد از فروش", typeof(CustomerSatisfactionSurvey))]
	public class CustomerSatisfactionSurveyController(IUnitOfWork unitOfWork, IWebHostEnvironment env) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CustomerSatisfactionSurvey model, CancellationToken cn)
		{
			if (model.Id == null || model.Id == 0) return await Add(model, cn);
			if (await unitOfWork.Repository<CustomerSatisfactionSurvey>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
				return await Update(model, cn);
			return await Add(model, cn);
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public async Task<IActionResult> Add(CustomerSatisfactionSurvey model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<CustomerSatisfactionSurvey>().SaveAsync(model, cn, true));

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public async Task<IActionResult> Update(CustomerSatisfactionSurvey model, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<CustomerSatisfactionSurvey>().UpdateAsync(model, cn, true));

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = unitOfWork.Repository<CustomerSatisfactionSurvey>().TableNoTracking.FirstOrDefault(c => c.Id == id);
			if (entity != null) await unitOfWork.Repository<CustomerSatisfactionSurvey>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public IActionResult Edit(long? id)
		{
			var entity = (id != null && id != 0)
				? unitOfWork.Repository<CustomerSatisfactionSurvey>().TableNoTracking.FirstOrDefault(c => c.Id == id)
				: new CustomerSatisfactionSurvey();
			return View(@"\Views\Panel\Crm\CustomerSatisfactionSurvey\Edit.cshtml", entity ?? new CustomerSatisfactionSurvey());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public IActionResult New() => View(@"\Views\Panel\Crm\CustomerSatisfactionSurvey\Edit.cshtml", new CustomerSatisfactionSurvey());

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Crm\CustomerSatisfactionSurvey\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<CustomerSatisfactionSurvey>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex) { return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message); }
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<CustomerSatisfactionSurvey>().FetchDataAsync(request, cn));

		[HttpGet("[action]")]
		[ActionDisplayName("ارسال لینک رضایت", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult SendingSatisfactionLink() => Ok();

		[HttpPost("[action]")]
		[ActionDisplayName("ارسال پیامک و ایمیل رضایت", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> SendSatisfactionLink(long id, CancellationToken cn)
		{
			var survey = await unitOfWork.Repository<CustomerSatisfactionSurvey>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (survey == null) return BadRequest("فرم رضایت‌سنجی یافت نشد");

			var surveyUrl = "/Panel/Crm/CustomerSatisfactionSurvey/Edit?id=" + survey.Id;
			var smsBody =
				"\"گروه صنعتی هوایار\"" + "\nرضایت سنجی از خدمات ارائه شده"
				+ "\nمشتری گرامی"
				+ "\nبا سلام و احترام"
				+ "\nبا توجه به دریافت خدمات از \"گروه صنعتی هوایار\"، خواهشمند است با ثبت نظر خود ما را در بهبود ارائه خدمات یاری فرمایید."
				+ "\nبرای ثبت نظر، خواهشمند است بر روی لینک ذیل کلیک نمایید:"
				+ "\n" + surveyUrl
				+ "\nواحد ارتباط با مشتریان"
				+ "\nتلفن: 41976-021"
				+ "\n09129590195"
				+ "\n09128077599"
				+ "\nCRM@havayar.com";

			var emailHtml =
				"<div style='direction:rtl;text-align:right'>مشتری گرامی<br/>با سلام و احترام<br/>"
				+ "با توجه به دریافت خدمات از گروه صنعتی هوایار، خواهشمند است با ثبت نظر خود ما را در بهبود ارائه خدمات یاری فرمایید.<br/>"
				+ "<a href='" + surveyUrl + "'>لینک نظر سنجی از خدمات ارائه شده</a><br/>"
				+ "واحد ارتباط با مشتریان<br/>تلفن: 41976-021</div>";

			string? toEmail = null;
			if (survey.CustomerId != null)
			{
				toEmail = await unitOfWork.Repository<Customer>().TableNoTracking
					.Where(c => c.Id == survey.CustomerId)
					.Select(c => c.Party != null ? c.Party.Email : null)
					.FirstOrDefaultAsync(cn);
			}

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = "رضایت سنجی از خدمات",
				Body = emailHtml + "<pre style='direction:rtl;white-space:pre-wrap'>" + smsBody + "</pre>",
				EntityId = survey.Id,
				OwnerId = CurrentUserId ?? 0,
				ViewPath = surveyUrl,
				IsRead = false,
				IsSend = false,
				ToEmails = string.IsNullOrWhiteSpace(toEmail) ? null : new List<string> { toEmail }
			}, cn);

			return Ok(new { smsBody, surveyUrl });
		}

		[HttpGet("[action]")]
		[ActionDisplayName("رضایت‌سنجی خدمات هوای فشرده", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult CompressedAirServices()
			=> View(@"\Views\Panel\Crm\CustomerSatisfactionSurvey\List.cshtml");

		[HttpGet("[action]")]
		[ActionDisplayName("رضایت‌سنجی بعد از فروش CNG", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult CngAfterSales()
			=> View(@"\Views\Panel\Crm\CustomerSatisfactionSurvey\List.cshtml");
	}
}
