using System.Data;
using System.Globalization;
using Stimulsoft.Base.Drawing;
using Stimulsoft.Report;
using Stimulsoft.Report.Chart;
using Stimulsoft.Report.Components;
using SysColor = System.Drawing.Color;

namespace ReportBuilder.Services;

public static class CompressorSizingChartBinder
{
	public static void Bind(StiReport report, DataSet dataSet)
	{
		if (report == null || dataSet == null)
			return;

		var header = dataSet.Tables["Header"]?.Rows.Count > 0 ? dataSet.Tables["Header"].Rows[0] : null;
		var flowUnit = Cell(header, "FlowUnit");
		var pressureUnit = Cell(header, "PressureUnit");

		BindChart(
			report,
			"PerformanceChart",
			dataSet.Tables["ChartPressure"],
			Cell(header, "PressureChartTitle"),
			AxisTitle("mass flow", flowUnit),
			AxisTitle("discharge pressure", pressureUnit));
		BindChart(
			report,
			"PowerChart",
			dataSet.Tables["ChartPower"],
			Cell(header, "PowerChartTitle"),
			AxisTitle("mass flow", flowUnit),
			"required power (kw)");
	}

	private static void BindChart(
		StiReport report,
		string chartName,
		DataTable? table,
		string? chartTitle,
		string axisX,
		string axisY)
	{
		if (report.GetComponentByName(chartName) is not StiChart chart)
			return;

		chart.DataSourceName = string.Empty;
		chart.Series.Clear();
		chart.SeriesLabels = new StiNoneLabels();
		if (!string.IsNullOrWhiteSpace(chartTitle))
			chart.Title.Text = chartTitle;
		if (chart.Area is not StiScatterArea)
			chart.Area = new StiScatterArea();

		if (chart.Area is StiAxisArea axisArea)
		{
			axisArea.XAxis.Title.Text = axisX;
			axisArea.YAxis.Title.Text = axisY;
			axisArea.GridLinesHor.Visible = true;
			axisArea.GridLinesVert.Visible = true;
		}

		if (table == null || table.Rows.Count == 0)
			return;

		foreach (var group in table.Rows.Cast<DataRow>()
			.GroupBy(row => Convert.ToString(row["Series"]) ?? string.Empty)
			.Where(group => group.Key.Length > 0))
		{
			var xs = new List<double>();
			var ys = new List<double>();
			foreach (var row in group)
			{
				if (row["X"] == DBNull.Value || row["Y"] == DBNull.Value)
					continue;
				xs.Add(Convert.ToDouble(row["X"], CultureInfo.InvariantCulture));
				ys.Add(Convert.ToDouble(row["Y"], CultureInfo.InvariantCulture));
			}

			if (xs.Count == 0)
				continue;
			if (xs.Count == 1)
			{
				xs.Add(xs[0]);
				ys.Add(ys[0]);
			}

			var color = ColorFor(group.Key);
			var series = new StiScatterLineSeries
			{
				CoreTitle = group.Key,
				ShowInLegend = true,
				ShowShadow = false,
				Lighting = false,
				LineColor = color,
				LineWidth = 2,
				ArgumentDataColumn = string.Empty,
				ValueDataColumn = string.Empty,
				AutoSeriesKeyDataColumn = string.Empty,
				AutoSeriesColorDataColumn = string.Empty,
				AutoSeriesTitleDataColumn = string.Empty
			};
			series.Title.Value = group.Key;
			series.Values = ys.Select(value => (double?)value).ToArray();
			series.Arguments = xs.Cast<object>().ToArray();
			series.ListOfValues.Value = string.Join(";", ys.Select(v => v.ToString(CultureInfo.InvariantCulture)));
			series.ListOfArguments.Value = string.Join(";", xs.Select(v => v.ToString(CultureInfo.InvariantCulture)));
			series.SeriesLabels = new StiNoneLabels();
			if (series.Marker != null)
			{
				series.Marker.Visible = true;
				series.Marker.Size = 7;
				series.Marker.Brush = new StiSolidBrush(color);
			}

			if (series.LineMarker != null)
				series.LineMarker.Visible = false;

			chart.Series.Add(series);
		}
	}

	private static SysColor ColorFor(string seriesName)
	{
		return seriesName switch
		{
			"SurgeLine" => SysColor.Black,
			"Turndown" => SysColor.Red,
			"DesignPoint" => SysColor.LimeGreen,
			_ => SysColor.FromArgb(0, 112, 192)
		};
	}

	private static string AxisTitle(string label, string unit)
	{
		return string.IsNullOrWhiteSpace(unit) ? label : label + " (" + unit + ")";
	}

	private static string Cell(DataRow? header, string column)
	{
		if (header == null || !header.Table.Columns.Contains(column) || header[column] == DBNull.Value)
			return string.Empty;
		return Convert.ToString(header[column]) ?? string.Empty;
	}
}
