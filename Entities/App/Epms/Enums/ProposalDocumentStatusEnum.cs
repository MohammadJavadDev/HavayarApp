using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Epms.Enums
{
	public enum ProposalDocumentStatusEnum
	{
		[Display(Name = "ثبت اولیه")]
		Submit,
		[Display(Name = "تایید")]
		Approve ,
		[Display(Name = "نظر داده شده")]
		Commented ,
		[Display(Name = "منتظر")]
		Hold,
		[Display(Name = "ارسال برای مشتری")]
		IssueForClient 

	}
}
