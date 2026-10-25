using Entities.App.Bom.Enums;

namespace WebApp.Framework
{
	/// <summary>
	/// قواعد گردش‌کار «درخواست ویرایش اقلام فرمول» (معادل BomChangeRequestController در HTS):
	/// مراحل مجاز، نقش لازم برای هر مرحله و نقش دریافت‌کنندهٔ ایمیل پس از تغییر وضعیت.
	/// کنترلر (اعمال سمت سرور) و View (فیلتر لیست وضعیت) هر دو از همین کلاس استفاده می‌کنند.
	/// نقش‌ها همان نقش‌های گروه‌های کاربری HTS (۱۴۴ تا ۱۶۴) هستند که قبلاً در system.Role ساخته شده‌اند.
	/// </summary>
	public static class FormulChangeRequestWorkflow
	{
		#region Role names (system.Role.Name)

		/// <summary>HTS گروه 144 — ثبت/ویرایش/حذف درخواست</summary>
		public const string RoleGeneral = "کارشناسان درخواست ایجاد/تغییر قطعه";
		/// <summary>HTS گروه 145 / Permission 80 HasPlanningPermission</summary>
		public const string RolePlanning = "کارشناسان درخواست ایجاد/تغییر قطعه (برنامه ریزی)";
		/// <summary>HTS گروه 146 / Permission 81 HasQcApprovePermission</summary>
		public const string RoleQc = "کارشناسان درخواست ایجاد/تغییر قطعه (QC)";
		/// <summary>HTS گروه 147 / Permission 82 HasEngineeringPermission</summary>
		public const string RoleEngineering = "کارشناسان درخواست ایجاد/تغییر قطعه (مهندسی)";
		/// <summary>HTS گروه 150 / Permission 84 Bom_ChangeRequestOperationPermission</summary>
		public const string RoleFinal = "کارشناسان درخواست ایجاد/تغییر قطعه (اعمال نهایی)";
		/// <summary>HTS گروه 164 / Permission 7 ShowAll (فقط مشاهده)</summary>
		public const string RoleShowAll = "کارشناسان درخواست ایجاد/تغییر قطعه (مشاهده همه)";

		#endregion

		private static readonly FormulChangeRequestStatusEnum[] None = [];

		/// <summary>وضعیت‌های بعدی مجاز از وضعیت فعلی (همان نگاشت Edit.cshtml).</summary>
		public static FormulChangeRequestStatusEnum[] GetNextStatuses(FormulChangeRequestStatusEnum? current) => current switch
		{
			null or FormulChangeRequestStatusEnum.PlanningReject
				=> [FormulChangeRequestStatusEnum.InitialRegistration],
			FormulChangeRequestStatusEnum.InitialRegistration or FormulChangeRequestStatusEnum.QcReject
				=> [FormulChangeRequestStatusEnum.PlanningApproval, FormulChangeRequestStatusEnum.PlanningReject],
			FormulChangeRequestStatusEnum.PlanningApproval or FormulChangeRequestStatusEnum.EngineeringReject
				=> [FormulChangeRequestStatusEnum.QcApproval, FormulChangeRequestStatusEnum.QcReject],
			FormulChangeRequestStatusEnum.QcApproval
				=> [FormulChangeRequestStatusEnum.EngineeringApproval, FormulChangeRequestStatusEnum.EngineeringReject],
			FormulChangeRequestStatusEnum.EngineeringApproval
				=> [FormulChangeRequestStatusEnum.EndOperation],
			_ => None
		};

		/// <summary>نقشی که اجازهٔ خروج از وضعیت فعلی را دارد.</summary>
		public static string? GetRequiredRole(FormulChangeRequestStatusEnum? current) => current switch
		{
			null or FormulChangeRequestStatusEnum.PlanningReject => RoleGeneral,
			FormulChangeRequestStatusEnum.InitialRegistration or FormulChangeRequestStatusEnum.QcReject => RolePlanning,
			FormulChangeRequestStatusEnum.PlanningApproval or FormulChangeRequestStatusEnum.EngineeringReject => RoleQc,
			FormulChangeRequestStatusEnum.QcApproval => RoleEngineering,
			FormulChangeRequestStatusEnum.EngineeringApproval or FormulChangeRequestStatusEnum.EndOperation => RoleFinal,
			_ => null
		};

		/// <summary>آیا کاربر نقش لازم برای اقدام روی وضعیت فعلی را دارد (ادمین همیشه مجاز).</summary>
		public static bool HasStageRole(bool isAdministrator, Func<string, bool> hasRole, FormulChangeRequestStatusEnum? current)
		{
			if (isAdministrator)
				return true;
			var role = GetRequiredRole(current);
			return role != null && hasRole(role);
		}

		/// <summary>آیا کاربر می‌تواند وضعیت را از from به to ببرد.</summary>
		public static bool CanTransition(bool isAdministrator, Func<string, bool> hasRole,
			FormulChangeRequestStatusEnum? from, FormulChangeRequestStatusEnum? to)
		{
			if (!to.HasValue)
				return false;
			if (!GetNextStatuses(from).Contains(to.Value))
				return false;
			return HasStageRole(isAdministrator, hasRole, from);
		}

		/// <summary>وضعیت‌هایی که کاربر از وضعیت فعلی می‌تواند انتخاب کند (برای dropdown).</summary>
		public static FormulChangeRequestStatusEnum[] GetAllowedNextStatuses(bool isAdministrator, Func<string, bool> hasRole,
			FormulChangeRequestStatusEnum? current)
			=> HasStageRole(isAdministrator, hasRole, current) ? GetNextStatuses(current) : None;

		/// <summary>
		/// نقش دریافت‌کنندهٔ ایمیل پس از رفتن به وضعیت to (معادل SendCommentNotificationEmail در HTS).
		/// null یعنی گیرنده ایجادکنندهٔ درخواست است یا ایمیل جداگانه‌ای لازم نیست.
		/// </summary>
		public static string? GetNotifyRole(FormulChangeRequestStatusEnum to) => to switch
		{
			FormulChangeRequestStatusEnum.PlanningApproval => RoleQc,
			FormulChangeRequestStatusEnum.QcApproval => RoleEngineering,
			FormulChangeRequestStatusEnum.QcReject => RolePlanning,
			FormulChangeRequestStatusEnum.EngineeringApproval => RoleFinal,
			FormulChangeRequestStatusEnum.EngineeringReject => RoleQc,
			_ => null
		};
	}
}
