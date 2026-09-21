using Common.Utilities;
using Data.Contracts;
using Data.Contracts.Actions;
using Entities.App.Trn;
using Entities.App.Trn.Enums;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Actions.Trn
{
	/// <summary>
	/// قوانین صدور گواهی مطابق HTS:
	/// - R7 (BeforeAdd): شماره گواهی روی پیشوند سری (نه TemplateKey) + تاریخ صدور پیش‌فرض.
	/// - R8 (BeforeAdd): انتخاب و ذخیره کلید قالب چاپ تکی در لحظه صدور.
	/// </summary>
	public class CertificateAction(IUnitOfWork unitOfWork)
	{
		[EntityAction(typeof(Certificate), EntityActionTrigger.BeforeAdd, "GenerateCertificateNo", "تولید شماره گواهی و انتخاب قالب", Priority = 1)]
		public async Task GenerateBeforeAdd(Certificate entity, CancellationToken ct)
		{
			long? courseId = null;
			var isPrivateTrack = entity.CourseCompanyParticipantId != null;
			if (entity.CourseParticipantId != null)
			{
				courseId = unitOfWork.Repository<CourseParticipant>().TableNoTracking
					.Where(p => p.Id == entity.CourseParticipantId)
					.Select(p => (long?)p.CourseId)
					.FirstOrDefault();
			}
			else if (entity.CourseCompanyParticipantId != null)
			{
				courseId = unitOfWork.Repository<CourseCompanyParticipant>().TableNoTracking
					.Where(p => p.Id == entity.CourseCompanyParticipantId)
					.Select(p => (long?)p.CourseId)
					.FirstOrDefault();
			}

			if (courseId == null)
				throw new Exception("ردیف ثبت‌نام (عمومی/اختصاصی) برای صدور گواهی مشخص نشده است.");

			var course = unitOfWork.Repository<Course>().TableNoTracking
				.FirstOrDefault(c => c.Id == courseId);
			if (course == null)
				throw new Exception("دوره یافت نشد.");

			var duplicate =
				(entity.CourseParticipantId != null && await unitOfWork.Repository<Certificate>().TableNoTracking
					.AnyAsync(c => c.CourseParticipantId == entity.CourseParticipantId, ct))
				|| (entity.CourseCompanyParticipantId != null && await unitOfWork.Repository<Certificate>().TableNoTracking
					.AnyAsync(c => c.CourseCompanyParticipantId == entity.CourseCompanyParticipantId, ct));
			if (duplicate)
				throw new Exception("برای این ثبت‌نام قبلاً گواهی صادر شده است.");

			var bank = unitOfWork.Repository<CourseBank>().TableNoTracking
				.FirstOrDefault(b => b.Id == course.CourseBankId);

			entity.TemplateKey = ResolveTemplateKey(course, bank, isPrivateTrack);

			if (!entity.CertificateNo.HasValue())
				entity.CertificateNo = await GenerateCertificateNoAsync(bank, ct);

			if (entity.IssueMiladiDate == null)
			{
				var now = DateTime.Now;
				entity.IssueMiladiDate = now;
				entity.IssueShamsiDate = now.ToShamsiDate();
			}
		}

		/// <summary>
		/// نگاشت چاپ تکی HTS. مسیر از ردیف ثبت‌نام مشخص می‌شود (عمومی/اختصاصی).
		/// </summary>
		public static string ResolveTemplateKey(Course course, CourseBank? bank, bool isPrivateTrack)
		{
			var isCng = bank?.CourseField == CourseFieldEnum.Cng;
			var type = course.CngCourseType;

			if (isPrivateTrack)
			{
				if (type == CourseTypeEnum.LegalCustomersSeminar)
					return "Certificate9";
				if (type == CourseTypeEnum.RealCustomersSeminar)
					return "Certificate10";
				if (course.HasExam)
					return "Certificate3";
				return "Certificate1";
			}

			if (isCng)
				return "Cng";

			return type switch
			{
				CourseTypeEnum.Warranty => "Certificate5",
				CourseTypeEnum.Online => "Certificate6",
				CourseTypeEnum.TrainingSeminar => "Certificate7",
				CourseTypeEnum.HavayarFuel => "Certificate8",
				CourseTypeEnum.LegalCustomersSeminar => "Certificate10",
				CourseTypeEnum.RealCustomersSeminar => "Certificate9",
				_ => course.HasExam ? "Certificate4" : "Certificate2"
			};
		}

		/// <summary>
		/// CNG + اپراتوری → HAV/{n}؛ CNG غیراپراتوری → HAV/T/{n}؛ سایر → HY-{yy}-{n}.
		/// دنباله از max پسوند عددی همان پیشوند (بدون فیلتر TemplateKey).
		/// </summary>
		private async Task<string> GenerateCertificateNoAsync(CourseBank? bank, CancellationToken ct)
		{
			string prefix;
			if (bank?.CourseField == CourseFieldEnum.Cng)
				prefix = bank.Type == CourseBankTypeEnum.Operator ? "HAV/" : "HAV/T/";
			else
				prefix = "HY-" + DateTime.Now.ToShamsiDate().Substring(2, 2) + "-";

			var query = unitOfWork.Repository<Certificate>().TableNoTracking
				.Where(c => c.CertificateNo != null)
				.Select(c => c.CertificateNo!);

			List<string> numbers;
			if (prefix == "HAV/")
			{
				numbers = await query
					.Where(n => n.StartsWith("HAV/") && !n.StartsWith("HAV/T/"))
					.ToListAsync(ct);
			}
			else
			{
				numbers = await query
					.Where(n => n.StartsWith(prefix))
					.ToListAsync(ct);
			}

			var maxSeq = 0;
			foreach (var no in numbers)
			{
				var suffix = no.Length > prefix.Length ? no.Substring(prefix.Length) : string.Empty;
				if (int.TryParse(suffix, out var parsed) && parsed > maxSeq)
					maxSeq = parsed;
			}

			var seq = maxSeq + 1;
			string candidate;
			do
			{
				candidate = $"{prefix}{seq}";
				seq++;
			} while (await unitOfWork.Repository<Certificate>().TableNoTracking.AnyAsync(c => c.CertificateNo == candidate, ct));

			return candidate;
		}
	}
}
