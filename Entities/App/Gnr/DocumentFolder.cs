using Common.Attributes;
using Entities.App.Hrm;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Gnr
{
	[Display(Name = "پوشه اسناد سازمانی")]
	[Table("DocumentFolder", Schema = "Gnr")]
	public class DocumentFolder : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		/// <summary>1 = Gnr_Document1stLevel, 2 = 2nd, 3 = 3rd. Used by sync and by OrganizationalDocument FKs.</summary>
		[DisplayName("سطح")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public int FolderLevel { get; set; } = 1;

		[DisplayName("پوشه والد")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public long? ParentId { get; set; }
		public virtual DocumentFolder? Parent { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public long? OrganizationUnitId { get; set; }
		public virtual OrgUnit? OrganizationUnit { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(2048)]
		public string? Title { get; set; }

		[DisplayName("عنوان پوشه (مسیر فایل)")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(256)]
		public string? FolderTitle { get; set; }

		[DisplayName("خصوصی")]
		[DisplayInfo(null, true, type: SystemType.Boolean)]
		public bool IsPrivate { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		public virtual ICollection<DocumentFolderUser> AllowedUsers { get; set; } = new List<DocumentFolderUser>();
		public virtual ICollection<DocumentFolder> Children { get; set; } = new List<DocumentFolder>();

		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListLong)]
		public string? AllowedUserIds { get; set; }

		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListString)]
		public string? AllowedUserNames { get; set; }
	}

	[Display(Name = "کاربر مجاز پوشه")]
	[Table("DocumentFolderUser", Schema = "Gnr")]
	public class DocumentFolderUser : BaseEntity
	{
		public long DocumentFolderId { get; set; }
		public virtual DocumentFolder? DocumentFolder { get; set; }

		public long UserId { get; set; }
		public virtual User? User { get; set; }
	}

	public class DocumentFolderConfiguration : IEntityTypeConfiguration<DocumentFolder>
	{
		public void Configure(EntityTypeBuilder<DocumentFolder> builder)
		{
			builder.HasOne(x => x.Parent)
				.WithMany(x => x.Children)
				.HasForeignKey(x => x.ParentId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.OrganizationUnit)
				.WithMany()
				.HasForeignKey(x => x.OrganizationUnitId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => new { x.FolderLevel, x.HtsId })
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Gnr_DocumentFolder_LevelHtsId");
		}
	}

	public class DocumentFolderUserConfiguration : IEntityTypeConfiguration<DocumentFolderUser>
	{
		public void Configure(EntityTypeBuilder<DocumentFolderUser> builder)
		{
			builder.HasOne(x => x.DocumentFolder)
				.WithMany(x => x.AllowedUsers)
				.HasForeignKey(x => x.DocumentFolderId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => new { x.DocumentFolderId, x.UserId }).IsUnique();
		}
	}
}
