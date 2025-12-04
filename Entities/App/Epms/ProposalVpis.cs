using Common.Attributes;
using Entities.Auth;
using Entities.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.App.Epms
{

	[Display(Name = "پروپوزال Vpis")]
	[Table("ProposalVpis", Schema = "Epms")]
	public class ProposalVpis : BaseEntity
	{
		[DisplayName("پروپوزال")]
		[DisplayInfo(null, true, type: SystemType.Entity, required: true)]

		public Proposal Proposal { get; set; }

		[DisplayName("شناسه پروپوزال")]
		[DisplayInfo(null, true, type: SystemType.Long, required: true)]
		public long ProposalId { get; set; }

		[DisplayName("شناسه نوع مدرک  ")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public long? VpisTypeId { get; set; }

		[DisplayName(" نوع مدرک  ")]
		[DisplayInfo(null, true, type: SystemType.Long)]
		public VpisType? VpisType { get; set; }

		[DisplayName("عنوان مدرک")]
		[DisplayInfo(null, true, type: SystemType.String, required: true, showInRelationData: true)]
		[MaxLength(2000)]
		public string DocumentTitle { get; set; }
		[DisplayName("شماره مدرک")]
		[DisplayInfo(null, true, type: SystemType.String )]
		[MaxLength(2000)]
		public string DocumentNumber { get; set; }

		 
		public long? MainResponsibleId { get; set; }

		[DisplayName("مسئول اصلی")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? MainResponsible { get; set; }

 
	 
		public long? ControlResponsibleId { get; set; }

		[DisplayName("مسئول کنترل")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? ControlResponsible { get; set; }

		public long? ApprovedUserId { get; set; }

		[DisplayName("تایید کننده")]
		[DisplayInfo(null, true, type: SystemType.Entity)]
		public User? ApprovedUser { get; set; }

		[DisplayName("کامنت")]
		[DisplayInfo(null, true, type: SystemType.String)]
		[MaxLength(2000)]
		public string? Comment { get; set; }



	}
}
