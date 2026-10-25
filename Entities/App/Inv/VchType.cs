using Common.Attributes;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Inv
{
	/// <summary>
	/// نوع رسید انبار — معادل HTS <c>dbo.Inv_VchType</c>.
	/// <c>Id</c> همان <c>VchType_ID</c> است و <c>HamkaranId</c> همان <c>Hamkaran_InvVchType_FK</c>.
	/// </summary>
	[Display(Name = "نوع رسید انبار")]
	[Table("VchType", Schema = "Inv")]
	public class VchType : BaseEntity
	{
		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(400)]
		public string Title { get; set; } = string.Empty;

		[DisplayName("شناسه راهکاران")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long HamkaranId { get; set; }
	}

	public class VchTypeConfiguration : IEntityTypeConfiguration<VchType>
	{
		public void Configure(EntityTypeBuilder<VchType> builder)
		{
			builder.Property(x => x.Title).IsRequired();
			builder.HasIndex(x => x.HamkaranId)
				.IsUnique()
				.HasDatabaseName("IX_Inv_VchType_HamkaranId");
		}
	}
}
