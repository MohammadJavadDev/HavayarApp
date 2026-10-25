using Common.Utilities;
using Data.Contracts;
using Entities.App.Hrm;
using Services.Auth;
using Entities.Base.Enums;
using Entities.Base.Notification;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;

namespace WebApp.Actions.Hrm
{
	/// <summary>
	/// ایمیل «اعلان تایید ورود کالا» — معادل HTS <c>GuardPartIoService.SendEmailNotification</c>.
	/// گیرندگان و موضوع دقیقاً مثل HTS هستند.
	/// </summary>
	public static class GuardPartIoEmail
	{
		/// <summary>شناسه کالای «نامشخص» در HTS (HRM_Guard_Part.Id = 676).</summary>
		public const long HtsUnknownPartId = 676;

		private static readonly string[] Receivers = ["Daryaft", "Mousavi.a", "Fazeli.e", "Kamandar.m"];

		private static readonly string[] Cc =
		[
			"Kargar.m", "guard", "Ahsani.e", "Tafakor.s", "Kamandar.m",
			"Pouya.r", "Khodabandeh.f", "Asadpour.h"
		];

		private const string Subject = "اعلان تایید ورود کالا";

		private static string ToEmail(string value)
			=> value.Contains('@') ? value : value + "@havayar.com";

		/// <summary>ایمیل را در صف Notification (system.Notification) می‌گذارد؛ ارسال با EmailJob انجام می‌شود.</summary>
		public static async Task QueueAsync(IUnitOfWork unitOfWork, IUserService userService, long partIoId, long? ownerUserId, CancellationToken ct)
		{
			var entity = await unitOfWork.Repository<GuardPartIo>().TableNoTracking
				.Include(p => p.IoSource)
				.FirstOrDefaultAsync(p => p.Id == partIoId, ct);
			if (entity == null) return;

			var detail = await unitOfWork.Repository<GuardPartIoDetail>().TableNoTracking
				.Include(d => d.Part)
				.Include(d => d.RelatedUnit)
				.Where(d => d.PartIoId == partIoId)
				.OrderBy(d => d.Id)
				.FirstOrDefaultAsync(ct);

			string? creatorUsername = null;
			if (entity.CreatedById != null)
				creatorUsername = await userService.TableNoTracking
					.Where(u => u.Id == entity.CreatedById)
					.Select(u => u.Username)
					.FirstOrDefaultAsync(ct);

			var cc = Cc.Select(ToEmail).ToList();
			if (!string.IsNullOrWhiteSpace(creatorUsername))
			{
				cc.Add(ToEmail(creatorUsername.Trim()));
				if (string.Equals(creatorUsername.Trim(), "papi", StringComparison.OrdinalIgnoreCase))
					cc.Add("khederzadeh.s@havayar.com");
			}

			await unitOfWork.Repository<Notification>().AddAsync(new Notification
			{
				Type = NotificationType.Email,
				Title = Subject,
				Body = BuildBody(entity, detail),
				EntityId = entity.Id,
				OwnerId = ownerUserId ?? 1,
				ViewPath = "/Panel/Hrm/GuardPartIo/Edit?id=" + entity.Id,
				IsRead = false,
				IsSend = false,
				ToEmails = Receivers.Select(ToEmail).ToList(),
				CcEmails = cc
			}, ct);
			await unitOfWork.SaveChangesAsync(ct);
		}

		private static string H(string? s) => WebUtility.HtmlEncode(s ?? string.Empty);

		private static string BuildBody(GuardPartIo entity, GuardPartIoDetail? detail)
		{
			var sb = new StringBuilder();
			sb.AppendLine("<div style='text-align:center;direction:rtl'>");
			sb.AppendLine("<table border='1' cellspacing='0' cellpadding='5' style='text-align:right; direction: rtl'  width='672' >");
			sb.AppendLine("<tr style='width: 7.0in; background: #000aa0'><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#FFFFFF;text-align:center;direction:rtl'>گروه صنعتی هوایار</div>");
			sb.AppendLine("</td></tr>");
			sb.AppendLine("<tr><td colspan='2'>");
			sb.AppendLine("<div style='font-size:14.0pt;font-family:Zar;color:#1F497D;text-align:right;direction:rtl'>");
			sb.AppendLine("با سلام و احترام <br />");
			sb.AppendLine("بدینوسیله به استحضار می رساند کالا با مشخصات ذیل وارد کارخانه شد، لذا خواهشمند است در خصوص تحویل آن مساعدت فرمایید ");
			sb.AppendLine("</br></br><ul>");

			if (detail != null)
			{
				sb.AppendLine($"<li>کالا : <strong>{H(detail.Part?.Title)}</strong></li>");
				sb.AppendLine($"<li>تعداد/مقدار : <strong>{detail.Mount}</strong></li>");
				sb.AppendLine($"<li>منشاء اعلان ورود : <strong>{H(detail.IoReason.ToDisplay())}</strong></li>");
				sb.AppendLine($"<li>سفارش دهنده : <strong>{H(detail.RelatedUnit?.Title)}</strong></li>");
			}

			sb.AppendLine($"<li>تامین کننده/ارسال کننده : <strong>{H(entity.IoSource?.Title)}</strong></li>");
			sb.AppendLine($"<li>درخواست دهنده : <strong>{H(entity.CreatedByName)}</strong></li>");
			sb.AppendLine($"<li>توضیحات : <strong>{H(entity.Comment)}</strong></li>");
			sb.AppendLine("</ul></div></td></tr></table></div>");
			return sb.ToString();
		}
	}
}
