using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hcm
{
	/// <summary>شغل پرسنل — معادل HTS <c>HRM_Job</c>.</summary>
	[Display(Name = "شغل")]
	[Table("Job", Schema = "Hcm")]
	public class Job : BaseEntity
	{
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(480)]
		public string? Title { get; set; }

		[DisplayName("شناسه همکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HamkaranId { get; set; }

		[DisplayName("شناسه شرکت")]
		[DisplayInfo(null, false, type: SystemType.Int)]
		public short? CompanyId { get; set; }

		[DisplayName("نمایش عمومی")]
		[DisplayInfo(null, false, type: SystemType.Boolean)]
		public bool? ShowPublic { get; set; }

		[DisplayName("شناسه راهکاران")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? RahkaranId { get; set; }
	}

	public class JobConfiguration : IEntityTypeConfiguration<Job>
	{
		public void Configure(EntityTypeBuilder<Job> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] <> CAST(0 AS bigint)")
				.HasDatabaseName("IX_Hcm_Job_HtsId");
		}
	}
}
