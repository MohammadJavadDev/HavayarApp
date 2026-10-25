using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Law;
using Entities.Base.Sms;
using Microsoft.EntityFrameworkCore;
using Services.Sms;

namespace WebApp.Actions.Law
{
	/// <summary>
	/// معادل منطق ذخیره پرونده حقوقی HTS (JudicialFileManagementController.DoOperation):
	/// کد پرونده = ۲ رقم سال شمسی + شمارنده سراسری (در HTS از Gnr_MainConfiguration شماره 20 خوانده می‌شد).
	/// </summary>
	public class JudicialFileAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(JudicialFile), EntityActionTrigger.BeforeAdd, "AssignJudicialFileCode", "تولید کد پرونده حقوقی", Priority = 1)]
		public async Task AssignCode(JudicialFile entity, CancellationToken ct)
		{
			if (!string.IsNullOrWhiteSpace(entity.JudicialFileCode))
				return;

			// HTS: counter = مقدار جاری کانفیگ؛ code = سال(۲ رقم) + (counter < 10 ? "0" + counter : counter)؛ بعد counter + 1.
			// هم‌ارز: بزرگ‌ترین شماره ثبت‌شده + 1 (شماره بعد از سال، مثل 05125 → 125).
			var codes = await unitOfWork.Repository<JudicialFile>().TableNoTracking
				.Where(c => c.JudicialFileCode != null)
				.Select(c => c.JudicialFileCode!)
				.ToListAsync(ct);

			var max = 0;
			foreach (var code in codes)
			{
				if (code.Length > 2 && int.TryParse(code[2..], out var n) && n > max)
					max = n;
			}

			var counter = max + 1;
			var shamsi = DateTime.Now.ToShamsiDate();
			var year = shamsi.Length >= 4 ? shamsi[2..4] : shamsi;
			entity.JudicialFileCode = year + (counter < 10 ? "0" + counter : counter.ToString());
		}

		[EntityAction(typeof(JudicialFile), EntityActionTrigger.BeforeUpdate, "KeepJudicialFileIdentity", "حفظ کد و شناسه HTS پرونده", Priority = 1)]
		public Task KeepIdentity(JudicialFile entity, JudicialFile? oldEntity, CancellationToken ct)
		{
			if (oldEntity == null)
				return Task.CompletedTask;
			entity.HtsId = oldEntity.HtsId;
			entity.JudicialFileCode = oldEntity.JudicialFileCode;
			return Task.CompletedTask;
		}
	}

	/// <summary>
	/// HTS: برای هر «تاریخ مهم» جدید، همان لحظه ذخیره پیامک برای گیرندگان (Recipients) ارسال می‌شد.
	/// یادآوری در خود تاریخ در HTS وجود نداشت و اینجا هم اضافه نشده است.
	/// </summary>
	public class JudicialImportantDateAction(IUnitOfWork unitOfWork, ISmsService sms)
	{
		[EntityAction(typeof(JudicialImportantDate), EntityActionTrigger.BeforeAdd, "MarkImportantDateSent", "علامت ارسال تاریخ مهم", Priority = 1)]
		public Task MarkSent(JudicialImportantDate entity, CancellationToken ct)
		{
			entity.IsSend = true;
			return Task.CompletedTask;
		}

		[EntityAction(typeof(JudicialImportantDate), EntityActionTrigger.AfterAdd, "SendImportantDateSms", "پیامک تاریخ مهم پرونده حقوقی", Priority = 20)]
		public async Task SendSms(JudicialImportantDate entity, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(entity.Recipients) || entity.JudicialFileId is not > 0)
				return;

			var file = await unitOfWork.Repository<JudicialFile>().TableNoTracking
				.Where(f => f.Id == entity.JudicialFileId)
				.Select(f => new { f.JudicialFileCode, f.Subject })
				.FirstOrDefaultAsync(ct);
			if (file == null)
				return;

			await sms.TryEnqueueAsync(
				SmsEventKeys.LawImportantDate,
				entity.Recipients,
				SmsTemplates.LawImportantDate(file.JudicialFileCode, file.Subject, entity.Title, entity.DateShamsiDate),
				nameof(JudicialImportantDate),
				entity.Id,
				ct: ct);
		}
	}
}
