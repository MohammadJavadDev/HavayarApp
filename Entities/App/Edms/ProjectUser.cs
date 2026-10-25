using Common.Attributes;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Edms
{
	[Display(Name = "کاربر پروژه")]
	[Table("ProjectUser", Schema = "Edms")]
	public class ProjectUser : BaseEntity
	{
		[DisplayName("پروژه")]
		[DisplayInfo(null, true, SystemType.Entity, required: true)]
		public Project? Project { get; set; }

		[DisplayName("شناسه پروژه")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long ProjectId { get; set; }

		[DisplayName("کاربر")]
		[DisplayInfo(null, true, SystemType.Entity, required: true)]
		public User? User { get; set; }

		[DisplayName("شناسه کاربر")]
		[DisplayInfo(null, true, SystemType.Long)]
		public long UserId { get; set; }

		[DisplayName("سرپرست پروژه است")]
		[DisplayInfo(null, true, SystemType.Boolean)]
		public bool UserIsSupervisor { get; set; }

		/// <summary>
		/// شناسه ردیف در سیستم قدیم، فقط برای انتقال یک‌باره. نمایه‌ها از این ستون استفاده نمی‌کنند.
		/// </summary>
		public long HtsId { get; set; }
	}

	public class ProjectUserConfiguration : IEntityTypeConfiguration<ProjectUser>
	{
		public void Configure(EntityTypeBuilder<ProjectUser> builder)
		{
			builder.HasOne(x => x.Project)
				.WithMany()
				.HasForeignKey(x => x.ProjectId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => new { x.ProjectId, x.UserId })
				.IsUnique()
				.HasDatabaseName("IX_Edms_ProjectUser_Project_User");

			builder.HasIndex(x => x.UserId)
				.HasDatabaseName("IX_Edms_ProjectUser_UserId");

			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Edms_ProjectUser_HtsId");
		}
	}
}
