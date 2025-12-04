namespace ReportBuilder.Entities;

public class ReportBuilderTableItem  
{
    public string? Title { get; set; }
    public string DataType { get; set; }
    public string ColumnName { get; set; }
    public bool Show { get; set; }

    public bool? Filter { get; set; }
    
}