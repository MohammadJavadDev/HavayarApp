namespace Data.Services.Eng.CompressorSizing;

public interface ICompressorSizingService
{
	CompressorSelectionResult SelectByEfficiency(CompressorSizingRequest request);
	CompressorSelectionResult SelectByReliability(CompressorSizingRequest request);
	CompressorSizingAnalysisResult Analyze(CompressorSizingRequest request);
	CompressorSizingDatasheetFile ExportDatasheet(CompressorSizingDatasheetRequest request, string licensePath);
	System.Data.DataSet BuildReportDataSet(CompressorSizingRequest request);
	string ResolveReportTemplatePath();
	string? ResolveWatermarkImagePath();
}
