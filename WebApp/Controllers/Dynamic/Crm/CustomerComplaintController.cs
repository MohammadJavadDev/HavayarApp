using Common.Attributes;
using Common.Utilities;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Crm;
using Entities.App.Hcm;
using Entities.App.Hrm;
using Entities.Base.DataTable;
using Entities.Base.Sms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Sms;
using System.Text.RegularExpressions;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.Dynamic
{
	[Route("Panel/Crm/[controller]")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("شکایات مشتری", typeof(CustomerComplaint))]
	public class CustomerComplaintController(IUnitOfWork unitOfWork, IWebHostEnvironment env, ISmsService sms) : BaseController
	{
		[HttpPost("[action]")]
		[ActionDisplayName("ذخیره", ActionAccessType.Api, ActionAccessItemType.Save)]
		public async Task<IActionResult> Save(CustomerComplaint model, CancellationToken cn)
		{
			Detach(model);
			var error = await Validate(model, cn);
			if (error != null)
				return BadRequest(error);
			ApplyFirstIds(model);
			await FillReceiverName(model, cn);
			if (model.Id == null || model.Id == 0)
			{
				model.ModuleTypeId = ComplaintRules.ModuleCustomerComplaint;
				return Ok(await unitOfWork.Repository<CustomerComplaint>().SaveAsync(model, cn, true));
			}
			if (await unitOfWork.Repository<CustomerComplaint>().TableNoTracking.AnyAsync(c => c.Id == model.Id, cn))
			{
				await KeepHiddenFields(model, cn);
				return Ok(await unitOfWork.Repository<CustomerComplaint>().UpdateAsync(model, cn, true));
			}
			model.ModuleTypeId = ComplaintRules.ModuleCustomerComplaint;
			return Ok(await unitOfWork.Repository<CustomerComplaint>().SaveAsync(model, cn, true));
		}

		[HttpPost("[action]")]
		[ActionDisplayName("درج", ActionAccessType.Api, ActionAccessItemType.Create)]
		public Task<IActionResult> Add(CustomerComplaint model, CancellationToken cn) => Save(model, cn);

		[HttpPost("[action]")]
		[ActionDisplayName("ویرایش", ActionAccessType.Api, ActionAccessItemType.Update)]
		public Task<IActionResult> Update(CustomerComplaint model, CancellationToken cn) => Save(model, cn);

		[HttpGet("[action]")]
		[ActionDisplayName("حذف", ActionAccessType.Api, ActionAccessItemType.Delete)]
		public async Task<IActionResult> Delete(long id, CancellationToken cn)
		{
			var entity = await unitOfWork.Repository<CustomerComplaint>().Table.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (entity != null)
				await unitOfWork.Repository<CustomerComplaint>().DeleteAsync(entity, cn, true);
			return Ok();
		}

		[HttpGet("[action]")]
		[ActionDisplayName("ویرایش اطلاعات", ActionAccessType.View, ActionAccessItemType.Update)]
		public async Task<IActionResult> Edit(long? id, CancellationToken cn)
		{
			await FillLookups(cn);
			var entity = (id != null && id != 0)
				? await unitOfWork.Repository<CustomerComplaint>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn)
				: new CustomerComplaint();
			return View(@"\Views\Panel\Crm\CustomerComplaint\Edit.cshtml", entity ?? new CustomerComplaint());
		}

		[HttpGet("[action]")]
		[ActionDisplayName("درج اطلاعات", ActionAccessType.View, ActionAccessItemType.Create)]
		public async Task<IActionResult> New(CancellationToken cn)
		{
			await FillLookups(cn);
			var today = DateTime.Today;
			return View(@"\Views\Panel\Crm\CustomerComplaint\Edit.cshtml", new CustomerComplaint
			{
				CompletionMiladiDate = today,
				CompletionShamsiDate = today.ToShamsiDate()
			});
		}

		[HttpGet("[action]")]
		[ActionDisplayName("لیست اطلاعات", ActionAccessType.View, ActionAccessItemType.List)]
		public IActionResult List() => View(@"\Views\Panel\Crm\CustomerComplaint\List.cshtml");

		[HttpPost("[action]")]
		[ActionDisplayName("خروجی اکسل", ActionAccessType.Api)]
		public async Task<IActionResult> ExportToExcel(DataTableRequest request, CancellationToken cn)
		{
			var licensePath = env.WebRootPath + "\\Aspose.Total.NET.lic";
			var memoryStream = new MemoryStream();
			try
			{
				await unitOfWork.Repository<CustomerComplaint>().ExportLargeDataToExcelAsync(request, memoryStream, licensePath);
				memoryStream.Position = 0;
				return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "exportExcel.xlsx");
			}
			catch (Exception ex)
			{
				return StatusCode(500, "خطا در زمان ایجاد فایل اکسل: " + ex.Message);
			}
		}

		[HttpPost("[action]")]
		[ActionDisplayName("دریافت اطلاعات", ActionAccessType.Api, ActionAccessItemType.FetchData)]
		public async Task<IActionResult> FetchData(DataTableRequest request, CancellationToken cn)
			=> Ok(await unitOfWork.Repository<CustomerComplaint>().FetchDataAsync(request, cn));

		[HttpPost("[action]")]
		[ActionDisplayName("ارسال پیام ثبت شکایت", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public Task<IActionResult> SendReceiptSms(long id, CancellationToken cn)
			=> SendCustomerSms(id, ComplaintRules.FinalNotClosed, SmsEventKeys.CrmComplaintReceipt, SmsTemplates.ComplaintReceipt, cn);

		[HttpPost("[action]")]
		[ActionDisplayName("ارسال پیام پایان شکایت", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public Task<IActionResult> SendClosedSms(long id, CancellationToken cn)
			=> SendCustomerSms(id, ComplaintRules.FinalClosed, SmsEventKeys.CrmComplaintClosed, SmsTemplates.ComplaintClosed, cn);

		[HttpGet("[action]")]
		public async Task<IActionResult> Lookups(short typeId, CancellationToken cn)
		{
			var rows = await unitOfWork.Repository<ComplaintLookupItem>().TableNoTracking
				.Where(c => c.LookupType == typeId && c.IsActive != Entities.Base.IsActiveEnum.DeActive)
				.OrderBy(c => c.Title)
				.Select(c => new { c.Id, c.HtsId, c.Title, c.LookupType })
				.ToListAsync(cn);
			return Ok(rows);
		}

		[HttpPost("[action]")]
		public async Task<IActionResult> AddLookup(short lookupType, string title, long? orgUnitId, CancellationToken cn)
		{
			if (lookupType is not (ComplaintRules.LookupChannel or ComplaintRules.LookupSubject or ComplaintRules.LookupInformReceive or ComplaintRules.LookupHandlingResult))
				return BadRequest("این نوع قابل افزودن نیست");
			title = (title ?? "").Trim();
			if (title.Length == 0)
				return BadRequest("عنوان را وارد کنید");
			if (lookupType == ComplaintRules.LookupSubject && orgUnitId is not > 0)
				return BadRequest("واحد سازمانی را انتخاب کنید");

			var item = new ComplaintLookupItem
			{
				LookupType = lookupType,
				Title = title,
				HtsId = 0,
				IsActive = Entities.Base.IsActiveEnum.Active
			};
			await unitOfWork.Repository<ComplaintLookupItem>().SaveAsync(item, cn, true);
			if (lookupType == ComplaintRules.LookupSubject)
			{
				await unitOfWork.Repository<ComplaintSubjectOrganizationUnit>().SaveAsync(new ComplaintSubjectOrganizationUnit
				{
					SubjectId = item.Id,
					OrgUnitId = orgUnitId,
					HtsId = 0,
					IsActive = Entities.Base.IsActiveEnum.Active
				}, cn, true);
			}
			return Ok(new { item.Id, item.HtsId, item.Title, item.LookupType });
		}

		private async Task<IActionResult> SendCustomerSms(long id, short requiredStatus, string eventKey, Func<string?, string?, string> body, CancellationToken cn)
		{
			var complaint = await unitOfWork.Repository<CustomerComplaint>().TableNoTracking.FirstOrDefaultAsync(c => c.Id == id, cn);
			if (complaint == null)
				return BadRequest("شکایت یافت نشد");
			if ((short?)complaint.FinalComplaintStatus != requiredStatus)
				return BadRequest(requiredStatus == ComplaintRules.FinalClosed
					? "ارسال پیام پایان فقط برای وضعیت بسته شد مجاز است"
					: "ارسال پیام ثبت فقط برای وضعیت بسته نشد مجاز است");

			var contact = await unitOfWork.Repository<CustomerComplaintContact>().TableNoTracking
				.Where(c => c.ComplaintId == id)
				.OrderBy(c => c.Id)
				.FirstOrDefaultAsync(cn);
			var mobile = NormalizeMobile(contact?.MobileNumber);
			if (string.IsNullOrWhiteSpace(mobile))
				return BadRequest("برای این شکایت شماره موبایل تعریف نشده");
			if (!Regex.IsMatch(mobile, @"^09\d{9}$"))
				return BadRequest("شماره " + contact?.MobileNumber + " نا معتبر است");

			var sent = await sms.TryEnqueueAsync(eventKey, mobile, body(contact?.ResponserFullName, complaint.TrackingCode), nameof(CustomerComplaint), id, ct: cn);
			if (!sent)
				return BadRequest("خطایی در حین ارسال پیامک بوجود آمد");
			return Ok(true);
		}

		private async Task<string?> Validate(CustomerComplaint model, CancellationToken cn)
		{
			if (model.CustomerId is not > 0 && string.IsNullOrWhiteSpace(model.CustomerTitle))
				return "مشتری را انتخاب کنید";
			if (string.IsNullOrWhiteSpace(model.CompletionShamsiDate)) return "تاریخ تکمیل را وارد کنید";
			if (string.IsNullOrWhiteSpace(model.OrganizationUnitIds) && model.OrgUnitId is not > 0)
				model.OrganizationUnitIds = model.OrgUnitId.ToString();
			if (string.IsNullOrWhiteSpace(model.OrganizationUnitIds)) return "واحد سازمانی را انتخاب کنید";
			if (string.IsNullOrWhiteSpace(model.ExpertOrManagerIds) && model.ExpertPersonelId is not > 0)
				model.ExpertOrManagerIds = model.ExpertPersonelId.ToString();
			if (string.IsNullOrWhiteSpace(model.ExpertOrManagerIds)) return "کارشناس یا مدیر را انتخاب کنید";
			if (model.Priority == null) return "اولویت را انتخاب کنید";
			if (model.CommunicationChannelId is not > 0) return "کانال ارتباط را انتخاب کنید";
			if (model.ComplaintSubjectId is not > 0) return "موضوع شکایت را انتخاب کنید";
			if (model.ComplaintStatus == null) return "وضعیت شکایت را انتخاب کنید";
			if (model.FinalComplaintStatus == null) return "وضعیت نهایی را انتخاب کنید";

			var subject = await unitOfWork.Repository<ComplaintLookupItem>().TableNoTracking
				.Where(c => c.Id == model.ComplaintSubjectId)
				.Select(c => new { c.HtsId })
				.FirstOrDefaultAsync(cn);
			if (subject != null && ComplaintRules.WrongdoerAgencySubjectHtsIds.Contains(subject.HtsId) && model.AgencyId is not > 0)
				return "نمایندگی خاطی را انتخاب کنید";
			if (subject != null && subject.HtsId == ComplaintRules.CompressorFailureSubjectHtsId && model.CompressorFailure == null)
				return "خرابی کمپرسور را انتخاب کنید";
			return null;
		}

		private async Task KeepHiddenFields(CustomerComplaint model, CancellationToken cn)
		{
			var stored = await unitOfWork.Repository<CustomerComplaint>().TableNoTracking
				.FirstOrDefaultAsync(c => c.Id == model.Id, cn);
			if (stored == null)
				return;
			model.PhoneNumber = stored.PhoneNumber;
			model.MobileNumber = stored.MobileNumber;
			model.Fax = stored.Fax;
			model.ComplaintPresenterJob = stored.ComplaintPresenterJob;
			model.Comment = stored.Comment;
			model.ComplaintMainSubject = stored.ComplaintMainSubject;
			model.ParentId = stored.ParentId;
			model.HasComplaintSurvey = stored.HasComplaintSurvey;
			model.IsSurveyLinkSended = stored.IsSurveyLinkSended;
			model.SurveyCompletionShamsiDate = stored.SurveyCompletionShamsiDate;
			model.SurveyInterviewerPersonelId = stored.SurveyInterviewerPersonelId;
			model.SurveyResponder = stored.SurveyResponder;
			model.SurveyDescription = stored.SurveyDescription;
			model.SurveyLinkSentShamsiDateTime = stored.SurveyLinkSentShamsiDateTime;
			model.SurveyLinkSentMiladiDateTime = stored.SurveyLinkSentMiladiDateTime;
			model.SurveyResponseShamsiDateTime = stored.SurveyResponseShamsiDateTime;
			model.SurveyResponseMiladiDateTime = stored.SurveyResponseMiladiDateTime;
			model.SurveySubjectsIds = stored.SurveySubjectsIds;
			model.SurveySubjectsInText = stored.SurveySubjectsInText;
			model.SurveyStatusId = stored.SurveyStatusId;
		}

		private static string? NormalizeMobile(string? mobile)
		{
			if (string.IsNullOrWhiteSpace(mobile))
				return null;
			return mobile.Trim().Fa2En().Replace(" ", "");
		}

		private async Task FillReceiverName(CustomerComplaint model, CancellationToken cn)
		{
			if (model.ComplaintReceiverPersonelId is not > 0 || !string.IsNullOrWhiteSpace(model.ComplaintReceiver))
				return;
			var person = await unitOfWork.Repository<Personel>().TableNoTracking
				.Where(c => c.Id == model.ComplaintReceiverPersonelId)
				.Select(c => new { c.Name, c.Family })
				.FirstOrDefaultAsync(cn);
			if (person != null)
				model.ComplaintReceiver = $"{person.Name} {person.Family}".Trim();
		}

		private static void ApplyFirstIds(CustomerComplaint model)
		{
			model.OrgUnitId ??= FirstId(model.OrganizationUnitIds);
			model.ExpertPersonelId ??= FirstId(model.ExpertOrManagerIds);
		}

		private static long? FirstId(string? csv)
		{
			if (string.IsNullOrWhiteSpace(csv))
				return null;
			var first = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
			return long.TryParse(first, out var id) && id > 0 ? id : null;
		}

		private static void Detach(CustomerComplaint model)
		{
			model.Customer = null;
			model.OrgUnit = null;
			model.ExpertPersonel = null;
			model.ComplaintReceiverPersonel = null;
			model.SurveyInterviewerPersonel = null;
			model.Part = null;
			model.Agency = null;
			model.CommunicationChannel = null;
			model.ComplaintSubject = null;
			model.InformForReceive = null;
			model.Parent = null;
			model.Actions = new List<CustomerComplaintAction>();
			model.Contacts = new List<CustomerComplaintContact>();
			model.Attachments = new List<CustomerComplaintAttachment>();
		}

		private async Task FillLookups(CancellationToken cn)
		{
			var items = await unitOfWork.Repository<ComplaintLookupItem>().TableNoTracking
				.Where(c => c.LookupType == ComplaintRules.LookupChannel
					|| c.LookupType == ComplaintRules.LookupSubject
					|| c.LookupType == ComplaintRules.LookupInformReceive
					|| c.LookupType == ComplaintRules.LookupHandlingResult)
				.OrderBy(c => c.Title)
				.Select(c => new ComplaintLookupOption(c.Id ?? 0, c.HtsId, c.LookupType, c.Title))
				.ToListAsync(cn);
			ViewBag.Channels = items.Where(c => c.LookupType == ComplaintRules.LookupChannel).ToList();
			ViewBag.Subjects = await SubjectOptions(cn);
			ViewBag.InformReceives = items.Where(c => c.LookupType == ComplaintRules.LookupInformReceive).ToList();
			ViewBag.HandlingResults = items.Where(c => c.LookupType == ComplaintRules.LookupHandlingResult).ToList();
			ViewBag.Agencies = await unitOfWork.Repository<ComplaintAgency>().TableNoTracking
				.OrderBy(c => c.Title)
				.Select(c => new ComplaintLookupOption(c.Id ?? 0, c.HtsId, 0, c.Title))
				.ToListAsync(cn);
			ViewBag.OrgUnits = await unitOfWork.Repository<OrgUnit>().TableNoTracking
				.Where(c => c.IsActive != Entities.Base.IsActiveEnum.DeActive)
				.OrderBy(c => c.Title)
				.Select(c => new ComplaintLookupOption(c.Id ?? 0, 0, 0, c.Title))
				.ToListAsync(cn);
		}

		private async Task<List<ComplaintLookupOption>> SubjectOptions(CancellationToken cn)
		{
			var subjects = await unitOfWork.Repository<ComplaintLookupItem>().TableNoTracking
				.Where(c => c.LookupType == ComplaintRules.LookupSubject)
				.Select(c => new { c.Id, c.HtsId, c.Title })
				.ToListAsync(cn);
			var links = await (
				from link in unitOfWork.Repository<ComplaintSubjectOrganizationUnit>().TableNoTracking
				join org in unitOfWork.Repository<OrgUnit>().TableNoTracking on link.OrgUnitId equals org.Id
				select new { link.SubjectId, org.Title }
			).ToListAsync(cn);
			var groupBySubject = links
				.Where(c => c.SubjectId is > 0)
				.GroupBy(c => c.SubjectId!.Value)
				.ToDictionary(g => g.Key, g => g.Select(x => x.Title).FirstOrDefault() ?? "");
			return subjects
				.Select(c => new ComplaintLookupOption(c.Id ?? 0, c.HtsId, ComplaintRules.LookupSubject, c.Title,
					groupBySubject.TryGetValue(c.Id ?? 0, out var group) ? group : ""))
				.OrderBy(c => c.Group).ThenBy(c => c.Title)
				.ToList();
		}

		public sealed record ComplaintLookupOption(long Id, long HtsId, short LookupType, string Title, string? Group = null);
	}
}
