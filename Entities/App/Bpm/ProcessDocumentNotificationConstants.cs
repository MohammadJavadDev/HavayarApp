namespace Entities.App.Bpm
{
	/// <summary>
	/// رشته‌ها و نقش‌های مشترک ابلاغ/ایمیل اسناد فرآیندی (فاز ۷).
	/// EmailJob هم از روی Title و ViewPath همین ثابت‌ها تشخیص می‌دهد که ایمیل ابلاغ است.
	/// </summary>
	public static class ProcessDocumentNotificationConstants
	{
		public const string SystemOwnerComment = "Created by system owner";

		public const string AnnouncementEmailTitle = "ابلاغیه سیستم مدیریت یکپارچه اسناد";
		public const string AnnouncementViewPath = "/panel/bpm/processdocument/published";
		public const string RequestEditViewPath = "/panel/bpm/processdocument/edit";
		public const string ManageEditViewPath = "/panel/bpm/processdocument/manageedit";

		public const string RoleShowAll = "Bpm.ProcessDocument.ShowAll";
		public const string RoleSupervisor = "Bpm.ProcessDocument.Supervisor";
		public const string RoleExpert = "Bpm.ProcessDocument.Expert";
		public const string RoleViewManage = "Bpm.ProcessDocument.ViewManage";
		public const string RolePublishedList = "Bpm.ProcessDocument.PublishedList";
		public const string RoleViewAllAttachments = "Bpm.ProcessDocument.ViewAllAttachments";

		public const string DefaultEmailDomain = "@havayar.com";
		public const string DefaultEmailSuffixKey = "Email:DefaultDomain";
	}
}
