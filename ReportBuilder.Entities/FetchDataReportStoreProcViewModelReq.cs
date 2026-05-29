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

    /// <summary>
    /// کوئری سفارشی از SavedQuery (QueryDesigner) - در صورت وجود، به جای ساخت از TableName استفاده می‌شود
    /// </summary>
    public string? BaseQuery { get; set; }

    /// <summary>
    /// مقادیر پارامترها برای BaseQuery - نام پارامتر به مقدار
    /// </summary>
    public Dictionary<string, string>? ParameterValues { get; set; }

    public List<FetchDataReportTableViewModelReqFilters> Filters { get; set; }
}