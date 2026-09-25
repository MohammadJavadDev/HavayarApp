using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Hts.Inv
{
	/// <summary>
	/// TotalSystem dbo.Inv_PartBookSection — used to read PictureContent blobs for migration into FileEntity.
	/// </summary>
	[Table("Inv_PartBookSection")]
	public class Hts_Inv_PartBookSection
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[StringLength(255)]
		public string Title { get; set; } = string.Empty;

		public int Order { get; set; }

		[StringLength(255)]
		public string? PictureTitle { get; set; }

		public byte[]? PictureContent { get; set; }

		public bool IsForCustomBom { get; set; }

		[StringLength(2048)]
		public string? Comment { get; set; }
	}
}
