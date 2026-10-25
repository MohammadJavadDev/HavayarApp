using Common.Attributes;
using Common.Auth.Enums;
using Data.Contracts;
using Entities.App.Hcm;
using Entities.App.SLS;
using Entities.Base;
using Entities.Base.Sms;
using Entities.Base.Sms.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Sms;
using WebFramework.Filtters;
using WebFramework.Page;

namespace WebApp.Controllers.SystemControllers
{
	/// <summary>
	/// ارسال پیامک دستی (معادل ماژول «ارسال پیام» HTS): متن آزاد به پرسنل، مشتریان و شماره‌های خارجی.
	/// فقط در صف ثبت می‌شود (<see cref="ISmsService"/>)؛ ارسال و پیگیری از صفحه «پیامک‌ها».
	/// دسترسی از «گروه‌های کاربری» (نقش) به عملیات این کنترلر داده می‌شود.
	/// </summary>
	[Route("Panel/System/ManualSms")]
	[ApiController]
	[ApiResultFilter]
	[ControllerInfo("ارسال پیامک دستی")]
	public class ManualSmsController(IUnitOfWork unitOfWork, ISmsService sms) : BaseController
	{
		private const int MaxBodyLength = 1000;
		private const int MaxRecipients = 300;

		[HttpGet("[action]")]
		[ActionDisplayName("ارسال پیامک دستی", ActionAccessType.View, ActionAccessItemType.Custom)]
		public IActionResult Index()
		{
			return View(@"\Views\Panel\System\ManualSms\Index.cshtml");
		}

		[HttpPost("[action]")]
		[ActionDisplayName("ثبت ارسال پیامک دستی", ActionAccessType.Api, ActionAccessItemType.Custom)]
		public async Task<IActionResult> Send(ManualSmsRequest request, CancellationToken cn)
		{
			var body = request.Body?.Trim();
			if (string.IsNullOrWhiteSpace(body))
				return BadRequest("متن پیامک را وارد کنید.");
			if (body.Length > MaxBodyLength)
				return BadRequest($"متن پیامک نباید بیشتر از {MaxBodyLength} نویسه باشد.");

			var personelIds = ParseIds(request.PersonelIds);
			var customerIds = ParseIds(request.CustomerIds);
			var externalNumbers = SmsHelpers.SplitReceivers(request.ExternalMobiles);

			var total = personelIds.Count + customerIds.Count + externalNumbers.Count;
			if (total == 0)
				return BadRequest("حداقل یک گیرنده (پرسنل، مشتری یا شماره‌ی خارجی) انتخاب کنید.");
			if (total > MaxRecipients)
				return BadRequest($"حداکثر {MaxRecipients} گیرنده در هر ارسال مجاز است.");

			var createdById = CurrentUserId;
			var createdByName = CurrentUserFullNameFn ?? CurrentUserFullName;

			var queued = 0;
			var invalid = 0;
			var withoutMobile = new List<string>();

			void Count(IReadOnlyList<SmsMessage> items)
			{
				queued += items.Count(x => x.Status == SmsStatusEnum.Pending);
				invalid += items.Count(x => x.Status != SmsStatusEnum.Pending);
			}

			if (personelIds.Count > 0)
			{
				var people = await unitOfWork.Repository<Personel>().TableNoTracking
					.Where(p => p.Id != null && personelIds.Contains(p.Id.Value))
					.Select(p => new { p.Id, Name = ((p.Name ?? "") + " " + (p.Family ?? "")).Trim(), p.Mobile })
					.ToListAsync(cn);

				foreach (var p in people)
				{
					if (string.IsNullOrWhiteSpace(p.Mobile))
					{
						withoutMobile.Add(p.Name);
						continue;
					}

					Count(await sms.EnqueueAsync(new SmsEnqueueRequest
					{
						EventKey = SmsEventKeys.ManualPersonnel,
						Receivers = p.Mobile,
						Body = body,
						RecipientName = p.Name,
						SourceEntityType = nameof(Personel),
						SourceEntityId = p.Id,
						CreatedById = createdById,
						CreatedByName = createdByName
					}, cn));
				}
			}

			if (customerIds.Count > 0)
			{
				var customers = await unitOfWork.Repository<Customer>().TableNoTracking
					.Where(c => c.Id != null && customerIds.Contains(c.Id.Value))
					.Select(c => new
					{
						c.Id,
						Name = c.Party != null ? c.Party.FullName : null,
						Mobile = c.Party != null ? c.Party.Mobile : null
					})
					.ToListAsync(cn);

				foreach (var c in customers)
				{
					if (string.IsNullOrWhiteSpace(c.Mobile))
					{
						withoutMobile.Add(c.Name ?? ("مشتری " + c.Id));
						continue;
					}

					Count(await sms.EnqueueAsync(new SmsEnqueueRequest
					{
						EventKey = SmsEventKeys.ManualCustomer,
						Receivers = c.Mobile,
						Body = body,
						RecipientName = c.Name,
						SourceEntityType = nameof(Customer),
						SourceEntityId = c.Id,
						CreatedById = createdById,
						CreatedByName = createdByName
					}, cn));
				}
			}

			if (externalNumbers.Count > 0)
			{
				Count(await sms.EnqueueAsync(new SmsEnqueueRequest
				{
					EventKey = SmsEventKeys.ManualExternal,
					Receivers = string.Join(";", externalNumbers.Select(x => x.Mobile)),
					Body = body,
					CreatedById = createdById,
					CreatedByName = createdByName
				}, cn));
			}

			return Ok(new { queued, invalid, withoutMobile });
		}

		private static List<long> ParseIds(string? csv)
			=> (csv ?? string.Empty)
				.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(s => long.TryParse(s, out var v) ? v : 0)
				.Where(v => v > 0)
				.Distinct()
				.ToList();
	}

	public class ManualSmsRequest
	{
		public string? Body { get; set; }

		/// <summary>شناسه پرسنل منتخب، با ویرگول جدا شده (خروجی EntitySelector چندانتخابی).</summary>
		public string? PersonelIds { get; set; }

		/// <summary>شناسه مشتریان منتخب، با ویرگول جدا شده.</summary>
		public string? CustomerIds { get; set; }

		/// <summary>شماره‌های خارجی با ; یا ، یا خط جدید.</summary>
		public string? ExternalMobiles { get; set; }
	}
}
