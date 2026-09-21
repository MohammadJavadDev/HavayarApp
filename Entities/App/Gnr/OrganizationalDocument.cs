using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Gnr
{
	[Display(Name = "اسناد سازمانی")]
	[Table("OrganizationalDocument", Schema = "Gnr")]
	public class OrganizationalDocument : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("پوشه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public long FolderId { get; set; }
		public virtual DocumentFolder? Folder { get; set; }

		[DisplayName("پوشه سطح ۲")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public long? SecondLevelFolderId { get; set; }
		public virtual DocumentFolder? SecondLevelFolder { get; set; }

		[DisplayName("پوشه سطح ۳")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public long? ThirdLevelFolderId { get; set; }
		public virtual DocumentFolder? ThirdLevelFolder { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(2048)]
		public string? Title { get; set; }

		[DisplayName("عنوان لاتین")]
		[DisplayInfo(null, true, type: SystemType.String, required: true)]
		[MaxLength(512)]
		public string? TitleInLatin { get; set; }

		[DisplayName("برند")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]
		public string? Brand { get; set; }

		[DisplayName("بازنگری")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public short Revision { get; set; }

		[DisplayName("سال")]
		[DisplayInfo(null, true, type: SystemType.Int)]
		public short? Year { get; set; }

		[DisplayName("فایل")]
		[DisplayInfo(null, true, type: SystemType.File, required: true, fileTypes: ".pdf,.zip,.rar,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png", maxFileSize: 50)]
		public long? FileId { get; set; }
		public virtual FileEntity? File { get; set; }

		[DisplayName("توضیحات")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(4000)]
		public string? Comment { get; set; }

		[NotMapped]
		[DisplayInfo(null, false, type: SystemType.ListLong)]
		public string? FileIds { get; set; }

		[NotMapped]
		public string? SaveWarning { get; set; }
	}

	public class OrganizationalDocumentConfiguration : IEntityTypeConfiguration<OrganizationalDocument>
	{
		public void Configure(EntityTypeBuilder<OrganizationalDocument> builder)
		{
			builder.HasOne(x => x.Folder)
				.WithMany()
				.HasForeignKey(x => x.FolderId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.SecondLevelFolder)
				.WithMany()
				.HasForeignKey(x => x.SecondLevelFolderId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.ThirdLevelFolder)
				.WithMany()
				.HasForeignKey(x => x.ThirdLevelFolderId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasOne(x => x.File)
				.WithMany()
				.HasForeignKey(x => x.FileId)
				.OnDelete(DeleteBehavior.Restrict);

			builder.HasIndex(x => x.HtsId)
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Gnr_OrganizationalDocument_HtsId");
		}
	}
}
