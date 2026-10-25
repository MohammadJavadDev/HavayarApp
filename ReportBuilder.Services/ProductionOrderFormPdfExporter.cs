using System.Data;
using Data.Contracts;
using Data.Services.QueryBuilderServices;
using Entities.Base;
using Microsoft.EntityFrameworkCore;
using ReportBuilder.Entities;
using Stimulsoft.Base;
using Stimulsoft.Report;
using Stimulsoft.Report.Export;

namespace ReportBuilder.Services;

/// <summary>
/// PDF همان قالب چاپی ProductionOrderForm (دکمه «چاپ سفارش ساخت») را می‌سازد.
/// </summary>
public interface IProductionOrderFormPdfExporter
{
	Task<byte[]?> TryExportAsync(long productionOrderId, CancellationToken ct);
}

public sealed class ProductionOrderFormPdfExporter(IUnitOfWork unitOfWork, IQueryService queryService) : IProductionOrderFormPdfExporter
{
	public const string ReportName = "ProductionOrderForm";

	private const string LicenseKey =
		"6vJhGtLLLz2GNviWmUTrhSqnOItdDwjBylQzQcAOiHkO46nMQvol4ASeg91in+mGJLnn2KMIpg3eSXQSgaFOm15+0l" +
		"hekKip+wRGMwXsKpHAkTvorOFqnpF9rchcYoxHXtjNDLiDHZGTIWq6D/2q4k/eiJm9fV6FdaJIUbWGS3whFWRLPHWC" +
		"BsWnalqTdZlP9knjaWclfjmUKf2Ksc5btMD6pmR7ZHQfHXfdgYK7tLR1rqtxYxBzOPq3LIBvd3spkQhKb07LTZQoyQ" +
		"3vmRSMALmJSS6ovIS59XPS+oSm8wgvuRFqE1im111GROa7Ww3tNJTA45lkbXX+SocdwXvEZyaaq61Uc1dBg+4uFRxv" +
		"yRWvX5WDmJz1X0VLIbHpcIjdEDJUvVAN7Z+FW5xKsV5ySPs8aegsY9ndn4DmoZ1kWvzUaz+E1mxMbOd3tyaNnmVhPZ" +
		"eIBILmKJGN0BwnnI5fu6JHMM/9QR2tMO1Z4pIwae4P92gKBrt0MqhvnU1Q6kIaPPuG2XBIvAWykVeH2a9EP6064e11" +
		"PFCBX4gEpJ3XFD0peE5+ddZh+h495qUc1H2B";

	public async Task<byte[]?> TryExportAsync(long productionOrderId, CancellationToken ct)
	{
		if (productionOrderId <= 0)
			return null;

		var stored = await unitOfWork.Repository<ReportBuilderReport>()
			.TableNoTracking
			.FirstOrDefaultAsync(r => r.Name == ReportName, ct);

		if (stored?.SavedQueryId is not > 0 || string.IsNullOrWhiteSpace(stored.Content))
			return null;

		StiLicense.Key = LicenseKey;
		StiOptions.Engine.ForceInterpretationMode = true;

		var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["productionOrderId"] = productionOrderId.ToString()
		};
		var data = await queryService.ExecuteReportMultiAsync(stored.SavedQueryId.Value, parameters, "system", "system");

		using var dataSet = BuildDataSet(data);
		using var report = StiReport.CreateNewReport();
		report.LoadFromJson(stored.Content);
		report.CalculationMode = StiCalculationMode.Interpretation;
		if (dataSet.Tables.Count > 0)
			report.RegData(dataSet);
		report.Dictionary.Synchronize();
		report.Render(false);

		using var stream = new MemoryStream();
		report.ExportDocument(StiExportFormat.Pdf, stream);
		return stream.Length == 0 ? null : stream.ToArray();
	}

	private static DataSet BuildDataSet(QueryMultiResult data)
	{
		var dataSet = new DataSet();
		var resultSets = data.ResultSets ?? [];
		for (var i = 0; i < resultSets.Count; i++)
		{
			var select = data.Selects?.FirstOrDefault(s => s.Index == i);
			var tableName = string.IsNullOrWhiteSpace(select?.Name)
				? i == 0 ? "dtMaster" : i == 1 ? "dtDetail" : $"Select{i + 1}"
				: select.Name;
			var table = BuildTable(resultSets[i], tableName);
			if (table.Columns.Count > 0)
				dataSet.Tables.Add(table);
		}

		return dataSet;
	}

	private static DataTable BuildTable(QueryResult? result, string tableName)
	{
		var table = new DataTable(tableName);
		if (result?.ColumnNames == null)
			return table;

		foreach (var column in result.ColumnNames)
		{
			if (!table.Columns.Contains(column))
				table.Columns.Add(column);
		}

		if (result.Rows == null)
			return table;

		foreach (var record in result.Rows)
		{
			var row = table.NewRow();
			foreach (var pair in record)
			{
				if (!table.Columns.Contains(pair.Key))
					continue;
				row[pair.Key] = pair.Value ?? DBNull.Value;
			}
			table.Rows.Add(row);
		}

		return table;
	}
}
