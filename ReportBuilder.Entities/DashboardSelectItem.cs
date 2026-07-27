namespace ReportBuilder.Entities
{
	public class DashboardSelectItem
	{
		public long? Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string Url { get; set; } = string.Empty;
		/// <summary>
		/// "stimulsoft" or "page"
		/// </summary>
		public string Type { get; set; } = "stimulsoft";
	}
}
