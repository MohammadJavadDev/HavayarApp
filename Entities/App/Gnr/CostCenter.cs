using Common.Attributes;
using Entities.App.FIN;
using Entities.App.Gnr.Enums;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Entities.App.Gnr
{
	[Display(Name = "مرکز هزینه")]
	[Table("CostCenter", Schema = "Gnr")]
	public class CostCenter:BaseEntity
    {

		[DisplayName("شماره")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long Number { get; set; }

		[DisplayName("عنوان")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(100)]

		public string Title { get; set; }

		[DisplayName("نوع")]
		[DisplayInfo(null, false, type: SystemType.Select)]

		public CostCenterTypeEnum Type { get; set; }
		 
		[DisplayName("تفصیل")]
		[DisplayInfo(null, false, type: SystemType.Entity)]
		public DL? Dl { get; set; }

		public long? DlId { get; set; }


		public long? HamkaranId { get; set; }
	}
}
