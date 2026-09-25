using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.Base.DataTable
{
    public class DataTableRequest
    {
        public int draw { get; set; }
        public int start { get; set; }
        public int length { get; set; }
        public string? TableName { get; set; }
        public SearchTable? search { get; set; }
        public OrderTable[]? order { get; set; }
        public List<ColumnTable>? columns { get; set; }
        public DataTableSearchBuilder? searchBuilder { get; set; }

        public bool UserDataFillter { get; set; } = false;
        public long? profileId { get; set; }

        // Display values are rendered in the browser; never execute JavaScript on the server.
        public DataTableDisplayExport? displayExport { get; set; }
    }

    public class DataTableDisplayExport
    {
        public List<string> Headers { get; set; } = [];
        public List<List<DataTableDisplayExportCell>> Rows { get; set; } = [];
    }

    public class DataTableDisplayExportCell
    {
        public string? Text { get; set; }
        public double? Number { get; set; }
    }

    public class DataTableSearchBuilder
    {
        public List<DataTableCriterion>? criteria { get; set; }
        public string? logic { get; set; }
    }

    public class DataTableCriterion
    {
        public string? condition { get; set; }
        public string? data { get; set; }
        public string? origData { get; set; }
        public string? name { get; set; }
        public string? type { get; set; }
        public List<string>? value { get; set; }
        public List<DataTableCriterion>? criteria { get; set; }
        public string? logic { get; set; }
    }

    public class SearchTable
    {
        public string[]? value { get; set; } = [];
        public bool regex { get; set; } = false;
        /// <summary>
        /// عملگر فیلتر ستون: contains, !contains, starts, ends, =, !=, null, !null, between, &gt;, &lt;, none
        /// </summary>
        public string? condition { get; set; }
        /// <summary>ترکیب شرط‌های همین ستون: and یا or</summary>
        public string? logic { get; set; }
        /// <summary>شرط‌های بعد از شرط اول همین ستون</summary>
        public List<SearchRule>? rules { get; set; }
    }

    public class SearchRule
    {
        public string? condition { get; set; }
        public string[]? value { get; set; }
    }

    public class OrderTable
    {
        public string? column { get; set; }
        public string? dir { get; set; }
    }

    public class ColumnTable
    {
        public string? data { get; set; }
        public string? name { get; set; }
        public string type { get; set; } = "string";
        public string title { get; set; } = "نامشخص";
          public string? RelatedEntityTypeFullName { get; set; }
          public bool searchable { get; set; } = false;
        public bool orderable { get; set; } = false;
        public SearchTable? Search { get; set; }
        public string? tableName { get; set; }
        public List<DataTableOptions>? options { get; set; }
    }

    public class DataTableOptions
    {
        public string name { get; set; }
        public string value { get; set; }

    }

    public class DataTableResponse
    {
        public int Draw { get; set; }
        public object RecordsTotal { get; set; }
        public object RecordsFiltered { get; set; }
        public object Data { get; set;}
     
    }
}
