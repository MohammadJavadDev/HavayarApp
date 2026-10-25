using Common.Attributes;
using Entities.App.Hrm;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Crm
{
	/// <summary>
	/// لوکاپ‌هایی که کاربر از خود فرم شکایت مقدار جدید اضافه می‌کند: کانال 173، موضوع 174، اطلاع دریافت 177، نتیجه رسیدگی 179.
	/// </summary>
	[Display(Name = "مقدار شکایت")]
	[Table("ComplaintLookupItem", Schema = "Crm")]
	public class ComplaintLookupItem : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("نوع")]
		[DisplayInfo(null, true, type: SystemType.Int, required: true)]
		public short LookupType { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(256)]
		public string Title { get; set; } = string.Empty;
	}

	public class ComplaintLookupItemConfiguration : IEntityTypeConfiguration<ComplaintLookupItem>
	{
		public void Configure(EntityTypeBuilder<ComplaintLookupItem> builder)
		{
			builder.HasIndex(x => new { x.LookupType, x.HtsId })
				.IsUnique()
				.HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_ComplaintLookupItem_Type_HtsId");
		}
	}

	[Display(Name = "واحد سازمانی موضوع شکایت")]
	[Table("ComplaintSubjectOrganizationUnit", Schema = "Crm")]
	public class ComplaintSubjectOrganizationUnit : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		public long? SubjectId { get; set; }
		public virtual ComplaintLookupItem? Subject { get; set; }

		public long? OrgUnitId { get; set; }
		public virtual OrgUnit? OrgUnit { get; set; }

		[DisplayName("شناسه واحد HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsOrgUnitId { get; set; }
	}

	public class ComplaintSubjectOrganizationUnitConfiguration : IEntityTypeConfiguration<ComplaintSubjectOrganizationUnit>
	{
		public void Configure(EntityTypeBuilder<ComplaintSubjectOrganizationUnit> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_ComplaintSubjectOrganizationUnit_HtsId");
			builder.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
			builder.HasOne(x => x.OrgUnit).WithMany().HasForeignKey(x => x.OrgUnitId).OnDelete(DeleteBehavior.Restrict);
		}
	}

	/// <summary>نمایندگی خاطی — همان فهرست فیلترشده Gnr_ManCompany در فرم شکایت.</summary>
	[Display(Name = "نمایندگی خاطی")]
	[Table("ComplaintAgency", Schema = "Crm")]
	public class ComplaintAgency : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(256)]
		public string Title { get; set; } = string.Empty;
	}

	public class ComplaintAgencyConfiguration : IEntityTypeConfiguration<ComplaintAgency>
	{
		public void Configure(EntityTypeBuilder<ComplaintAgency> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_ComplaintAgency_HtsId");
		}
	}

	/// <summary>جدول Crm_ComplaintType. فیلد نوع شکایت فرم به این جدول وصل نیست.</summary>
	[Display(Name = "نوع شکایت (جدول)")]
	[Table("ComplaintType", Schema = "Crm")]
	public class ComplaintType : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Title { get; set; }
	}

	public class ComplaintTypeConfiguration : IEntityTypeConfiguration<ComplaintType>
	{
		public void Configure(EntityTypeBuilder<ComplaintType> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_ComplaintType_HtsId");
		}
	}

	/// <summary>جدول Crm_Complaint_ReciveMethod. فرم شکایات مشتری از آن استفاده نمی‌کند.</summary>
	[Display(Name = "روش دریافت شکایت")]
	[Table("ComplaintReceiveMethod", Schema = "Crm")]
	public class ComplaintReceiveMethod : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(256)]
		public string? Title { get; set; }
	}

	public class ComplaintReceiveMethodConfiguration : IEntityTypeConfiguration<ComplaintReceiveMethod>
	{
		public void Configure(EntityTypeBuilder<ComplaintReceiveMethod> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Crm_ComplaintReceiveMethod_HtsId");
		}
	}
}
