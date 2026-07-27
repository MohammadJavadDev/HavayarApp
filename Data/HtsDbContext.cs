
using Entities.Hts.Inv;
using Entities.Hts.Pln;
using Entities.Hts.Sup;
using Microsoft.EntityFrameworkCore;


namespace Data
{
	public class HtsDbContext(DbContextOptions<HtsDbContext> options) : DbContext(options)
	{

 		public DbSet<Hts_Inv_Part> Hts_Inv_Parts { get; set; }
 		public DbSet<Hts_Inv_Part_Attachment> Hts_Inv_Part_Attachments { get; set; }
 		public DbSet<Hts_Inv_Part_Attachment_Permission> Hts_Inv_Part_Attachment_Permissions { get; set; }
 		public DbSet<Hts_Pln_ProductionOrder> Hts_Pln_ProductionOrders { get; set; }
 		public DbSet<Hts_Pln_ProductionOrderItem> Hts_Pln_ProductionOrderItems { get; set; }
 		public DbSet<Hts_Pln_ProductionOrderItemBom> Hts_Pln_ProductionOrderItemBoms { get; set; }
 		public DbSet<Hts_Sup_OpenOrderRequest> Hts_Sup_OpenOrderRequests { get; set; }
 		public DbSet<Hts_Sup_OpenOrderRequest_Attachment> Hts_Sup_OpenOrderRequest_Attachments { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

		}
	}
}
