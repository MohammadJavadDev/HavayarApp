using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Auth;

[Table(name: "UserDashboardCard", Schema = "system")]
public class UserDashboardCard : BaseEntity
{
	[Required]
	[DisplayName("کاربر")]
	public long UserId { get; set; }

	[Required]
	[Range(1, 4)]
	[DisplayName("جایگاه")]
	public int Slot { get; set; }

	[Required]
	[DisplayName("نمایه داده")]
	public long SavedQueryId { get; set; }

	[Required]
	[MaxLength(200)]
	[DisplayName("عنوان")]
	public string Title { get; set; } = string.Empty;

	[Required]
	[MaxLength(100)]
	[DisplayName("آیکن")]
	public string IconClass { get; set; } = string.Empty;

	[Required]
	[MaxLength(100)]
	[DisplayName("رنگ")]
	public string ColorClass { get; set; } = string.Empty;

	[Required]
	[MaxLength(500)]
	[DisplayName("مسیر لیست")]
	public string ListPath { get; set; } = string.Empty;

	public User? User { get; set; }
}

public class UserDashboardCardConfiguration : IEntityTypeConfiguration<UserDashboardCard>
{
	public void Configure(EntityTypeBuilder<UserDashboardCard> builder)
	{
		builder.ToTable("UserDashboardCard", "system");
		builder.HasKey(t => t.Id);

		builder.HasIndex(x => new { x.UserId, x.Slot })
			.IsUnique()
			.HasDatabaseName("IX_system_UserDashboardCard_UserId_Slot");

		builder.HasOne(x => x.User)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
