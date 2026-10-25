using Common.Attributes;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Base
{
	/// <summary>
	/// آدرس‌هایی که نباید از صندوق IT Support ایمیل بگیرند (ترک سازمان یا غیرفعال شدن صندوق).
	/// فقط ردیف‌های فعال در <c>EmailJob</c> از To و CC حذف می‌شوند.
	/// </summary>
	[Display(Name = "ایمیل‌های عدم ارسال")]
	[Table("BlockedEmail", Schema = "system")]
	[Index(nameof(Email), IsUnique = true)]
	public class BlockedEmail : BaseEntity
	{
		[DisplayName("ایمیل")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(320)]
		public string Email { get; set; } = string.Empty;

		[DisplayName("نام شخص")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(200)]
		public string? PersonName { get; set; }

		[DisplayName("توضیح")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(1000)]
		public string? Description { get; set; }
	}
}
