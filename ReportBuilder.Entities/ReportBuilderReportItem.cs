using Entities.Base;

namespace ReportBuilder.Entities
{
    public class ReportBuilderReportItem :BaseEntity
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string? Title { get; set; }
        public bool Show { get; set; } = false;
        public bool CanFilter { get; set; } = false;
        public bool Required { get; set; } = false;
        public ReportBuilderReport? ReportBuilderReport { get; set; }
        public long? ReportBuilderReportId { get; set; }
    }

    public class ReportBuilderReportSelect:BaseEntity
    {
        public string Title { get; set; }
        public ReportBuilderReport? ReportBuilderReport { get; set; }
        public long ReportBuilderReportId { get; set; }
        public List<ReportBuilderReportItem> ReportBuilderReportItems { get; set; }
    }
}