using Common.Attributes;
using Entities.App.Hrm;
using Entities.Auth;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Bpm
{
	[Display(Name = "واحد ویرایش‌کننده سند فرآیندی")]
	[Table("ProcessDocumentEditingOrgUnit", Schema = "Bpm")]
	public class ProcessDocumentEditingOrgUnit : BaseEntity
	{
		public long ProcessDocumentId { get; set; }
		public virtual ProcessDocument? ProcessDocument { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual OrgUnit? OrgUnit { get; set; }
		public long OrgUnitId { get; set; }
	}

	[Display(Name = "واحد مجری سند فرآیندی")]
	[Table("ProcessDocumentExecuterOrgUnit", Schema = "Bpm")]
	public class ProcessDocumentExecuterOrgUnit : BaseEntity
	{
		public long ProcessDocumentId { get; set; }
		public virtual ProcessDocument? ProcessDocument { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual OrgUnit? OrgUnit { get; set; }
		public long OrgUnitId { get; set; }
	}

	[Display(Name = "مجری فرآیند سند فرآیندی")]
	[Table("ProcessDocumentProcessExecuter", Schema = "Bpm")]
	public class ProcessDocumentProcessExecuter : BaseEntity
	{
		public long ProcessDocumentId { get; set; }
		public virtual ProcessDocument? ProcessDocument { get; set; }

		[DisplayName("کاربر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual User? User { get; set; }
		public long UserId { get; set; }
	}

	[Display(Name = "واحد ذینفع سند فرآیندی")]
	[Table("ProcessDocumentBeneficiaryOrgUnit", Schema = "Bpm")]
	public class ProcessDocumentBeneficiaryOrgUnit : BaseEntity
	{
		public long ProcessDocumentId { get; set; }
		public virtual ProcessDocument? ProcessDocument { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual OrgUnit? OrgUnit { get; set; }
		public long OrgUnitId { get; set; }
	}

	[Display(Name = "گیرنده ابلاغ سند فرآیندی")]
	[Table("ProcessDocumentNotificationRecipient", Schema = "Bpm")]
	public class ProcessDocumentNotificationRecipient : BaseEntity
	{
		public long ProcessDocumentId { get; set; }
		public virtual ProcessDocument? ProcessDocument { get; set; }

		[DisplayName("کاربر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual User? User { get; set; }
		public long UserId { get; set; }
	}

	[Display(Name = "واحد مرتبط سند فرآیندی")]
	[Table("ProcessDocumentRelatedOrgUnit", Schema = "Bpm")]
	public class ProcessDocumentRelatedOrgUnit : BaseEntity
	{
		public long ProcessDocumentId { get; set; }
		public virtual ProcessDocument? ProcessDocument { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual OrgUnit? OrgUnit { get; set; }
		public long OrgUnitId { get; set; }
	}

	/// <summary>
	/// «تیم بازنگری/تدوین» در HTS (EditingOrganizationUnitIds) شناسهٔ کاربران است، نه واحد سازمانی.
	/// جایگزین ProcessDocumentEditingOrgUnit (که معکوس مدل شده بود).
	/// </summary>
	[Display(Name = "تیم بازنگری/تدوین سند فرآیندی")]
	[Table("ProcessDocumentEditingUser", Schema = "Bpm")]
	public class ProcessDocumentEditingUser : BaseEntity
	{
		public long ProcessDocumentId { get; set; }
		public virtual ProcessDocument? ProcessDocument { get; set; }

		[DisplayName("کاربر")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual User? User { get; set; }
		public long UserId { get; set; }
	}

	/// <summary>
	/// «مجری فرآیند» در HTS (ProcessExecuterIds) شناسهٔ واحد سازمانی است، نه کاربر.
	/// جایگزین ProcessDocumentProcessExecuter (که معکوس مدل شده بود).
	/// </summary>
	[Display(Name = "مجری فرآیند سند فرآیندی")]
	[Table("ProcessDocumentProcessExecuterOrgUnit", Schema = "Bpm")]
	public class ProcessDocumentProcessExecuterOrgUnit : BaseEntity
	{
		public long ProcessDocumentId { get; set; }
		public virtual ProcessDocument? ProcessDocument { get; set; }

		[DisplayName("واحد سازمانی")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		public virtual OrgUnit? OrgUnit { get; set; }
		public long OrgUnitId { get; set; }
	}

	public class ProcessDocumentEditingUserConfiguration : IEntityTypeConfiguration<ProcessDocumentEditingUser>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentEditingUser> builder)
		{
			builder.HasIndex(x => new { x.ProcessDocumentId, x.UserId }).IsUnique();
			builder.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}

	public class ProcessDocumentProcessExecuterOrgUnitConfiguration : IEntityTypeConfiguration<ProcessDocumentProcessExecuterOrgUnit>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentProcessExecuterOrgUnit> builder)
		{
			builder.HasIndex(x => new { x.ProcessDocumentId, x.OrgUnitId }).IsUnique();
			builder.HasOne(x => x.OrgUnit)
				.WithMany()
				.HasForeignKey(x => x.OrgUnitId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}

	public class ProcessDocumentEditingOrgUnitConfiguration : IEntityTypeConfiguration<ProcessDocumentEditingOrgUnit>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentEditingOrgUnit> builder)
		{
			builder.HasIndex(x => new { x.ProcessDocumentId, x.OrgUnitId }).IsUnique();
			builder.HasOne(x => x.OrgUnit)
				.WithMany()
				.HasForeignKey(x => x.OrgUnitId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}

	public class ProcessDocumentExecuterOrgUnitConfiguration : IEntityTypeConfiguration<ProcessDocumentExecuterOrgUnit>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentExecuterOrgUnit> builder)
		{
			builder.HasIndex(x => new { x.ProcessDocumentId, x.OrgUnitId }).IsUnique();
			builder.HasOne(x => x.OrgUnit)
				.WithMany()
				.HasForeignKey(x => x.OrgUnitId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}

	public class ProcessDocumentProcessExecuterConfiguration : IEntityTypeConfiguration<ProcessDocumentProcessExecuter>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentProcessExecuter> builder)
		{
			builder.HasIndex(x => new { x.ProcessDocumentId, x.UserId }).IsUnique();
			builder.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}

	public class ProcessDocumentBeneficiaryOrgUnitConfiguration : IEntityTypeConfiguration<ProcessDocumentBeneficiaryOrgUnit>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentBeneficiaryOrgUnit> builder)
		{
			builder.HasIndex(x => new { x.ProcessDocumentId, x.OrgUnitId }).IsUnique();
			builder.HasOne(x => x.OrgUnit)
				.WithMany()
				.HasForeignKey(x => x.OrgUnitId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}

	public class ProcessDocumentNotificationRecipientConfiguration : IEntityTypeConfiguration<ProcessDocumentNotificationRecipient>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentNotificationRecipient> builder)
		{
			builder.HasIndex(x => new { x.ProcessDocumentId, x.UserId }).IsUnique();
			builder.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}

	public class ProcessDocumentRelatedOrgUnitConfiguration : IEntityTypeConfiguration<ProcessDocumentRelatedOrgUnit>
	{
		public void Configure(EntityTypeBuilder<ProcessDocumentRelatedOrgUnit> builder)
		{
			builder.HasIndex(x => new { x.ProcessDocumentId, x.OrgUnitId }).IsUnique();
			builder.HasOne(x => x.OrgUnit)
				.WithMany()
				.HasForeignKey(x => x.OrgUnitId)
				.OnDelete(DeleteBehavior.NoAction);
		}
	}
}
