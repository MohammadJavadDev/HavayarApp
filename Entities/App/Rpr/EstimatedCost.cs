using Common.Attributes;
using Entities.App.Hcm;
using Entities.App.Inv;
using Entities.App.SLS;
using Entities.Base;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.App.Rpr
{
    [Display(Name = "هزینه تخمینی")]
    [Table("EstimatedCost", Schema = "Rpr")]
    public class EstimatedCost : BaseEntity
    {

        [DisplayName("تعمیرات")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public RepairRequest? RepairRequest { get; set; }
        public long? RepairRequestId { get; set; }

        [DisplayName("مشتری")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Customer? Customer { get; set; }
        public long? CustomerId { get; set; }

        [DisplayName("نام مشتری")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? CustomerTitle { get; set; }

        [DisplayName("نمایندگی")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Customer? Agency { get; set; }
        public long? AgencyId { get; set; }



        [DisplayName("مشتری")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Personel? ZoneSupervisor { get; set; }
        public long? ZoneSupervisorId { get; set; }

        /// <summary>
        /// After impliment CrmZone,HRM_Personel
        /// </summary>
        //public short? Zone_FK { get; set; }

        //public short? ZoneSupervisor_FK { get; set; }



        [DisplayName("کالا")]
        [DisplayInfo(null, true, type: SystemType.Entity)]
        public Part? Product { get; set; }
        public long? ProductId { get; set; }

        [DisplayName("نفر ساعت(دقیقه)")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public int? ManHour { get; set; }

        [DisplayName("نفر ساعت")]
        [DisplayInfo(null, true, type: SystemType.Int)]
        public string? ManMinutes { get; set; }


        [DisplayName("توضیحات")]
        [DisplayInfo(null, true, type: SystemType.String)]
        public string? Comment { get; set; }

    }
}