using Common.Attributes;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Base
{
	/// <summary>
	/// تنظیمات عمومی سیستم به‌صورت کلید/مقدار.
	/// مقدارها از دیتابیس خوانده می‌شوند (با کش کوتاه‌مدت)، پس تغییر آن‌ها نیاز به ری‌استارت ندارد.
	/// کلیدهای شناخته‌شده در <see cref="GeneralSettingKeys"/> تعریف شده‌اند.
	/// </summary>
	[Display(Name = "تنظیمات عمومی")]
	[Table("GeneralSetting", Schema = "system")]
	[Index(nameof(SettingKey), IsUnique = true)]
	public class GeneralSetting : BaseEntity
	{
		[DisplayName("کلید")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(150)]
		public string SettingKey { get; set; } = string.Empty;

		/// <summary>
		/// عمداً در گرید پیش‌فرض نیست (AddToTable=false)، چون مقدار محرمانه‌ها نباید نمایش داده شود.
		/// نمایه داده «GeneralSetting_List» مقدار را با پنهان‌سازی محرمانه‌ها نشان می‌دهد.
		/// </summary>
		[DisplayName("مقدار")]
		[DisplayInfo(null, false, type: SystemType.String)]
		public string? SettingValue { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? Title { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, false, type: SystemType.String)]
		[MaxLength(1000)]
		public string? Description { get; set; }

		/// <summary>اگر true باشد مقدار در لیست‌ها و لاگ‌ها نمایش داده نمی‌شود (رمز، ApiKey).</summary>
		[DisplayName("محرمانه")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsSecret { get; set; }
	}
}
