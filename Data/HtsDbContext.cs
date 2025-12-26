
using Entities.Hts.Inv;

using Microsoft.EntityFrameworkCore;


namespace Data
{
	public class HtsDbContext(DbContextOptions<HtsDbContext> options) : DbContext(options)
	{

 		public DbSet<Hts_Inv_Part> Hts_Inv_Parts { get; set; }
 		public DbSet<Hts_Inv_Part_Attachment> Hts_Inv_Part_Attachments { get; set; }
 		public DbSet<Hts_Inv_Part_Attachment_Permission> Hts_Inv_Part_Attachment_Permissions { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

		}
	}
}
