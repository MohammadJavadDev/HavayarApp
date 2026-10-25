using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entities.App.Edms
{
	/// <summary>
	/// مدرک اختیاری است تا رزرو شماره بدون مدرک ممکن باشد.
	/// حذف مدرک همچنان ترانسمیتال‌های همان مدرک را حذف می‌کند.
	/// </summary>
	public class TransmitalConfiguration : IEntityTypeConfiguration<Transmital>
	{
		public void Configure(EntityTypeBuilder<Transmital> builder)
		{
			builder.HasOne(t => t.Document)
				.WithMany()
				.HasForeignKey(t => t.DocumentId)
				.OnDelete(DeleteBehavior.Cascade)
				.IsRequired(false);
		}
	}
}
