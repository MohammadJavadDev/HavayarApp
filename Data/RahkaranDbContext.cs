using Entities.Auth;
using Entities.Rahkaran.FIN3;
using Entities.Rahkaran.GNR3;
using Entities.Rahkaran.HCM3;
using Entities.Rahkaran.LGS3;
using Entities.Rahkaran.SLS3;
using Entities.Rahkaran.SYS3;
using Entities.Rahkaran.USR3;
using Entities.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
	public class RahkaranDbContext(DbContextOptions<RahkaranDbContext> options) : DbContext(options)
	{

		public DbSet<RahkaranPart> RahkaranParts { get; set; }
		public DbSet<RahkaranUnit> RahkaranUnits { get; set; }
		public DbSet<RahkaranParty> RahkaranParties { get; set; }
		public DbSet<RahkaranDepartment> RahkaranDepartments { get; set; }
		public DbSet<RahkaranGnr_PartyIndustry> RahkaranGnr_PartyIndustries { get; set; }
		public DbSet<RahkaranGnr_PartyIndustryItem> RahkaranGnr_PartyIndustryItems { get; set; }
		public DbSet<RahkaranRegionalDivision> RahkaranRegionalDivision { get; set; }
		public DbSet<Sale_RequirmentAdvertise> Sale_RequirmentAdvertise { get; set; }
		public DbSet<Sale_RequirmentAdvertiseItem> Sale_RequirmentAdvertiseItem { get; set; }
		public DbSet<RahkaranSale_ProductionOrder> RahkaranSale_ProductionOrder { get; set; }
		public DbSet<RahkaranSale_ProductionOrderItem> RahkaranSale_ProductionOrderItem { get; set; }
		public DbSet<RahkaranSale_ProductionOrderItemHistory> RahkaranSale_ProductionOrderItemHistory { get; set; }
		public DbSet<RahkaranDLType> RahkaranDLType { get; set; }
		public DbSet<RahkaranDL> RahkaranDL { get; set; }
		public DbSet<RahkaranContract> RahkaranContract { get; set; }
		public DbSet<RahkaranUser> RahkaranUsers { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

		}
	}
}
