namespace ReportBuilder.Entities
{
    public class DbObjectItems
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string? Title { get; set; }
        public bool Show { get; set; }
        public bool Filter { get; set; } = false;
        public string? TableTitle { get; set; }
        public string? TableType { get; set; }
        public Guid? TableItemId { get; set; }

    }

    public class DbObjectSelectItems
    {
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public List<DbObjectItems> Items { get; set; }
        public List<DbObjectItems> Filters { get; set; }
    }
}
