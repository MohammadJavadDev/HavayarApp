namespace ReportBuilder.Entities;

public class FetchDataReportStoreProcViewModelReq
{
    public class FetchDataReportStoreProcViewModelReqFilters
    {
        public string ColumnName { get; set; }
        public string Value { get; set; }
        public string Type { get; set; }
   
    }
    public string? StoreProcName { get; set; }

    public List<FetchDataReportStoreProcViewModelReqFilters> Filters { get; set; }
}

public class FetchDataReportTableViewModelReq
{
    public class FetchDataReportTableViewModelReqFilters
    {
        public string ColumnName { get; set; }
        public string[] Values { get; set; }
        public string Type { get; set; }
        public string Condition { get; set; } = "=";

    }
    public string? TableName { get; set; }

    public List<FetchDataReportTableViewModelReqFilters> Filters { get; set; }
}