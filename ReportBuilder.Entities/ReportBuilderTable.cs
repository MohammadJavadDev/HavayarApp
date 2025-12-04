
using System.ComponentModel.DataAnnotations.Schema;
using Entities.Base;

namespace ReportBuilder.Entities
{
    public class ReportBuilderTable:BaseEntity
    {
        public string Title { get; set; }
        public string ObjectName { get; set; }
        public string ObjectSchema { get; set; }
        public string? ObjectType { get; set; }
        public string FiltersContent { get; set; } = "";
        public string SelectsContent { get; set; } = "";
        [NotMapped]
        public List<ReportBuilderTableItem> ReportBuilderTableFilters { get; set; }
        [NotMapped]
        public List<ReportBuilderTableSelect> ReportBuilderTableSelects { get; set; }

    }
    public class ReportBuilderTableSelect  
    {
        public string Title { get; set; }
        public List<ReportBuilderTableItem> ReportBuilderReportItems { get; set; }
    }
}
