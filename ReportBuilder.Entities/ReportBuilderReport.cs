using System.ComponentModel;
using Common.Attributes;
using Entities.Base;

namespace ReportBuilder.Entities
{
    public class ReportBuilderReport:BaseEntity
    {
        [DisplayInfo(null, true, type: SystemType.String)]
        [DisplayName("نام ابجکت")]
        public string Name { get; set; }

        [DisplayInfo(null, true, type: SystemType.String)]
        [DisplayName("عنوان")]
        public string Title { get; set; }

        public string ObjectType { get; set; }
 
        public long? SavedQueryId { get; set; }

        public string? BaseQuery { get; set; }
        public ReportType Type { get; set; }
        public List<ReportBuilderReportSelect> ReportBuilderReportSelects { get; set; }
        public List<ReportBuilderReportItem> ReportBuilderReportFilters { get; set; }
        public string?  Content { get; set; }
    }

    public enum ReportType
    {
        Table,
        StimulSoft,
	   Dashboard
	}

}
