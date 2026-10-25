using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.Rahkaran.HCM3
{
	/// <summary>پست راهکاران. کوئری قدیمی: <c>SELECT PostID, Title FROM HCM3.Post</c>.</summary>
	[Table("Post", Schema = "HCM3")]
	public class RahkaranPost
	{
		[Key]
		public long PostID { get; set; }

		[MaxLength(500)]
		public string? Title { get; set; }
	}
}
