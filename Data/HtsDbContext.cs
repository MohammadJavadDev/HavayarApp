
using Entities.Hts.Edms;
using Entities.Hts.Gnr;
using Entities.Hts.Hrm;
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
 		public DbSet<Hts_Inv_Part_Company> Hts_Inv_Part_Companies { get; set; }
 		public DbSet<Hts_Gnr_ManCompany> Hts_Gnr_ManCompanies { get; set; }
 		public DbSet<Hts_Gnr_User> Hts_Gnr_Users { get; set; }
 		public DbSet<Hts_HRM_Personel> Hts_HRM_Personels { get; set; }
 		public DbSet<Hts_HRM_OrgUnit> Hts_HRM_OrgUnits { get; set; }
 		public DbSet<Hts_Edms_Project> Hts_Edms_Projects { get; set; }
 		public DbSet<Hts_Edms_Project_Progress> Hts_Edms_Project_Progresses { get; set; }
 		public DbSet<Hts_Edms_Project_Attachment> Hts_Edms_Project_Attachments { get; set; }
 		public DbSet<Hts_Edms_ConfidentialProject_User> Hts_Edms_ConfidentialProject_Users { get; set; }
 		public DbSet<Hts_Pln_ProductionOrder> Hts_Pln_ProductionOrders { get; set; }
 		public DbSet<Hts_Pln_ProductionOrderItem> Hts_Pln_ProductionOrderItems { get; set; }
 		public DbSet<Hts_Pln_ProductionOrderItemBom> Hts_Pln_ProductionOrderItemBoms { get; set; }
 		public DbSet<Hts_Pln_ProductionOrderComment> Hts_Pln_ProductionOrderComments { get; set; }
 		public DbSet<Hts_Pln_ProductionOrderItemComment> Hts_Pln_ProductionOrderItemComments { get; set; }
 		public DbSet<Hts_Sup_OpenOrderRequest> Hts_Sup_OpenOrderRequests { get; set; }
 		public DbSet<Hts_Sup_OpenOrderRequest_Attachment> Hts_Sup_OpenOrderRequest_Attachments { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

		}
	}
}
