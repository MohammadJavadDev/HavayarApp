using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Hrm
{
	/// <summary>کانال ارتباطی جذب — معادل <c>Gnr_Lookup</c> با <c>LookupType_FK = 20</c>. شناسه ۰ (جابینجا) معتبر است.</summary>
	[Display(Name = "کانال ارتباطی")]
	[Table("IncomingSource", Schema = "Hrm")]
	public class IncomingSource : BaseEntity
	{
		/// <summary>معادل <c>Gnr_Lookup.Lookup_ID</c>. برای ردیف‌های ساخته‌شده در سیستم جدید null است تا با شناسه ۰ تداخل نکند.</summary>
		[DisplayName("شناسه HTS")]
		[DisplayInfo(null, false, type: SystemType.Long)]
		public long? HtsId { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(100)]
		public string? Title { get; set; }
	}

	public class IncomingSourceConfiguration : IEntityTypeConfiguration<IncomingSource>
	{
		public void Configure(EntityTypeBuilder<IncomingSource> builder)
		{
			builder.HasIndex(x => x.HtsId).IsUnique().HasFilter("[HtsId] IS NOT NULL")
				.HasDatabaseName("IX_Hrm_IncomingSource_HtsId");
		}
	}
}
