using Common.Utilities;
using Data.Contracts;
using Entities.App.Hrm;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using Services.Auth;
using System.Net;
using System.Text;

namespace WebApp.Actions.Hrm
{
	/// <summary>
	/// ایمیل فرم ورود اضطراری — فقط مسیری که در HTS برای فرم ورود واقعاً اجرا می‌شود:
	/// ثبت اولیه و تایید نهایی، گیرنده سازنده فرم ورود، بدون CC.
	/// </summary>
	public static class EnexEntryFormEmail
	{
		public static async Task QueueCreatedAsync(IUnitOfWork unitOfWork, IUserService userService, long entryFormId, long? ownerUserId, CancellationToken ct)
			=> await QueueAsync(unitOfWork, userService, entryFormId, ownerUserId, created: true, ct);

		public static async Task QueueFinalAsync(IUnitOfWork unitOfWork, IUserService userService, long entryFormId, long? ownerUserId, CancellationToken ct)
			=> await QueueAsync(unitOfWork, userService, entryFormId, ownerUserId, created: false, ct);

		private static async Task QueueAsync(IUnitOfWork unitOfWork, IUserService userService, long entryFormId, long? ownerUserId, bool created, CancellationToken ct)
		{
			var entity = await unitOfWork.Repository<EnexEntryForm>().TableNoTracking
				.Include(x => x.ExitForm)
				.FirstOrDefaultAsync(x => x.Id == entryFormId, ct);
			if (entity == null || entity.CreatedById == null) return;

			var username = await userService.TableNoTracking
				.Where(u => u.Id == entity.CreatedById)
				.Select(u => u.Username)
				.FirstOrDefaultAsync(ct);
			var to = ToEmail(username);
			if (string.IsNullOrWhiteSpace(to)) return;

			var parts = await unitOfWork.Repository<EnexFormPart>().TableNoTracking
				.Include(p => p.EnexPart)
				.Include(p => p.Part)
				.Where(p => p.EntryFormId == entryFormId)
				.OrderBy(p => p.Id)
				.ToListAsync(ct);

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = $"اعلان فرم ورود اضطراری شماره {entity.FormNumber}",
				Body = BuildBody(entity, parts, created),
				EntityId = entity.Id,
				OwnerId = ownerUserId ?? entity.CreatedById ?? 1,
				ViewPath = "/Panel/Hrm/EnexEntryForm/Edit?id=" + entity.Id,
				IsRead = false,
				IsSend = false,
				ToEmails = [to],
				CcEmails = null
			}, ct);
			await unitOfWork.SaveChangesAsync(ct);
		}

		private static string? ToEmail(string? username)
		{
			if (string.IsNullOrWhiteSpace(username)) return null;
			var value = username.Trim();
			return value.Contains('@') ? value : value + "@havayar.com";
		}

		private static string H(string? s) => WebUtility.HtmlEncode(s ?? "");

		private static string BuildBody(EnexEntryForm entity, List<EnexFormPart> parts, bool created)
		{
			var status = entity.LastStatusId.ToDisplay();
			var who = H(entity.CreatedByName);
			var partsInfo = string.Join("<br />", parts.Select(p =>
			{
				var code = p.EnexPart?.Code ?? p.Part?.Code;
				var title = p.EnexPart?.Title ?? p.Part?.Name;
				var qty = p.EnterAmount ?? p.Mount;
				return H($"{code} - {title}") + " مقدار/ تعداد : " + H(qty.ToString("0.##"));
			}));

			var intro = created
				? $"احتراماَ فرم <strong>ورود اضطراری</strong>  شماره <strong>{entity.FormNumber}</strong> با مشخصات ذیل توسط <strong>{who}</strong> ایجاد گردید"
				: $"احتراماَ وضعیت فرم <strong>ورود اضطراری</strong>  شماره <strong>{entity.FormNumber}</strong> با مشخصات ذیل توسط <strong>{who}</strong>  تایید گردید";

			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='100%' >");
			sb.AppendLine("<tr style='width: 7.0in; background: #000aa0'><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار</div>");
			sb.AppendLine("</td></tr><tr><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			sb.Append(intro);
			sb.Append("<br /><br /><ul>");
			sb.Append("<li>نوع فرم : <strong> ورود اضطراری </strong></li>");
			sb.Append($"<li style='margin-top:7px'>شماره فرم ورود اضطراری : <strong> {entity.FormNumber} </strong></li>");
			sb.Append($"<li style='margin-top:7px'>شماره فرم خروج اضطراری : <strong> {entity.ExitForm?.FormNumber} </strong></li>");
			sb.Append($"<li style='margin-top:7px'>اقلام : <br /><strong> {partsInfo} </strong></li>");
			sb.Append($"<li style='margin-top:7px'>آخرین وضعیت : <strong> {H(status)} </strong></li>");
			sb.Append($"<li style='margin-top:40px'>درخواست کننده : <strong> {who} </strong></li>");
			sb.Append($"<li style='margin-top:7px'>زمان ثبت : <strong style='direction:ltr'> {H(entity.CreatedOnShamsiDateTime)} </strong></li>");
			sb.Append("</ul></div></td></tr></table></div>");
			return sb.ToString();
		}
	}
}
