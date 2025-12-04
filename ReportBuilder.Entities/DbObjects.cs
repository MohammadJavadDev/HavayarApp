namespace ReportBuilder.Entities
{
    public class DbObjects
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Schema { get; set; }
        public long? TableItemId { get; set; }
        public string? TableTitle { get; set; }
        public string? TableType { get; set; }

        public List<DbObjectSelectItems>? Selects { get; set; } = new();
        public List<DbObjectItems>? Filters { get; set; } = new();

    }
}
