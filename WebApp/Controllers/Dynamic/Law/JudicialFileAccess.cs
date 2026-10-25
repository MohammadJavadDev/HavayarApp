using Data.Contracts;
using Entities.App.Law;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Controllers.Dynamic
{
	/// <summary>نام نقش‌های سیستم حقوقی (Seed_Law_RoleAccess_Menu.sql).</summary>
	public static class LawRoles
	{
		/// <summary>HTS: صفحه پرونده حقوقی، «دسترسی کامل» (همه پرونده‌ها را می‌بیند).</summary>
		public const string JudicialFileFull = "Law.JudicialFile.Full";
		public const string JudicialFileView = "Law.JudicialFile.View";
		public const string JudicialFileEditor = "Law.JudicialFile.Editor";
		/// <summary>HTS: صفحه گزارش کلی پرونده‌ها، «دسترسی کامل».</summary>
		public const string JudicialFileReportFull = "Law.JudicialFileReport.Full";
		public const string JudicialFileReportView = "Law.JudicialFileReport.View";
	}

	/// <summary>
	/// HTS: فقط «دسترسی کامل» همه پرونده‌ها را می‌بیند؛ بقیه فقط پرونده‌هایی که خودشان ثبت کرده‌اند.
	/// همین قاعده روی ویرایش/حذف/ردیف‌های وابسته هم اعمال می‌شود.
	/// </summary>
	public static class JudicialFileAccess
	{
		public const string Denied = "شما به این پرونده دسترسی ندارید";

		public static async Task<bool> CanAccessAsync(IUnitOfWork unitOfWork, bool seeAll, long? userId, long? judicialFileId, CancellationToken ct)
		{
			if (judicialFileId is not > 0)
				return false;
			if (seeAll)
				return true;
			if (userId is not > 0)
				return false;
			return await unitOfWork.Repository<JudicialFile>().TableNoTracking
				.AnyAsync(f => f.Id == judicialFileId && f.CreatedById == userId, ct);
		}
	}
}
