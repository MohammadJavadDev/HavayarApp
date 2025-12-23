using Entities.Auth;
using Entities.Rahkaran;
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

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{

		}
	}
}
