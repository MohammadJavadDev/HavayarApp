using Entities.App.Pln.Enums;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Pln
{
	[Display(Name = "کاربران تاخیرات سفارش ساخت")]
	[Table("ProductionOrderDelayResponsibleUser", Schema = "Pln")]
	[Index(nameof(DelayResponsible), IsUnique = true)]
	public class ProductionOrderDelayResponsibleUser:BaseEntity
	{
		 
		[Display(Name = "عامل تاخیرات")]
		public DelayResponsibleEnum DelayResponsible { get; set; }
		[Display(Name = "شناسه کاربران")]
		public List<long> UserIds { get; set; } = new();
		[Display(Name = "نام کاربران")]
		public List<string> UserNames { get; set; } = new();

		[Display(Name = "ایمیل کاربران")]
		public List<string> UsersEmail { get; set; } = new();
	}
}
