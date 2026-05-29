namespace ReportBuilder.WebApp.ViewModels
{
    public class ReportColumnConfig
    {
        public string Data { get; set; } = "";
        public string Type { get; set; } = "string";
        public string Name { get; set; } = "";
        public string Title { get; set; } = "";
        public bool ShowInRelationData { get; set; }
        public object[] Options { get; set; } = Array.Empty<object>();
        public string TableName { get; set; } = "";
    }
}
