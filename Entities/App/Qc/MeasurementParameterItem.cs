using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Qc
{
	[Display(Name = "پارامتر اندازه گیری")]
	[Table("MeasurementParameterItem", Schema = "Qc")]
	public class MeasurementParameterItem : BaseEntity
	{
		[DisplayName("گروه")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]
		[ForeignKey(nameof(MeasurementParameterId))]
		public virtual MeasurementParameter? MeasurementParameter { get; set; }

		public long MeasurementParameterId { get; set; }

		[DisplayName("اقلام پارامتر اندازه گیری")]
		[MaxLength(2048)]
		[DisplayInfo("Title", true, type: SystemType.String, required: true, showInRelationData: true, regexInvalidError: "کاراکتر وارد شده غیر مجاز میباشد. ")]
		public string Title { get; set; } = string.Empty;

		[DisplayName("نماد رایج")]
		[MaxLength(10)]
		[DisplayInfo(null, true, type: SystemType.String)]
		public string? CommonSymbol { get; set; }

		public virtual ICollection<MeasurementParameterSubItem> SubItems { get; set; } = new List<MeasurementParameterSubItem>();
	}

	public class MeasurementParameterItemConfiguration : IEntityTypeConfiguration<MeasurementParameterItem>
	{
		public void Configure(EntityTypeBuilder<MeasurementParameterItem> builder)
		{
			builder.HasOne(x => x.MeasurementParameter)
				.WithMany()
				.HasForeignKey(x => x.MeasurementParameterId)
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired();

			builder.HasMany(x => x.SubItems)
				.WithOne(x => x.MeasurementParameterItem)
				.HasForeignKey(x => x.MeasurementParameterItemId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
