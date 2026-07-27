using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;
using Stimulsoft.Base;
using Stimulsoft.Base.Drawing;
using Stimulsoft.Dashboard.Components;
using Stimulsoft.Dashboard.Components.Chart;
using Stimulsoft.Dashboard.Components.ComboBox;
using Stimulsoft.Dashboard.Components.Gauge;
using Stimulsoft.Dashboard.Components.Indicator;
using Stimulsoft.Dashboard.Components.Progress;
using Stimulsoft.Dashboard.Components.Table;
using Stimulsoft.Report;
using Stimulsoft.Report.Dashboard;
using Stimulsoft.Report.Dictionary;
using StiFont = Stimulsoft.Drawing.Font;
using SysColor = System.Drawing.Color;
using SysFontStyle = System.Drawing.FontStyle;

/// <summary>
/// Polish ReportId=14: IRANSansWeb font, teal/blue corporate theme, Persian labels.
/// </summary>
static class Program
{
	const string Cs = "Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$;TrustServerCertificate=True";
	const long ReportId = 14;
	// نام واقعی خانواده داخل فایل TTF در wwwroot/panelLib/fonts
	const string FontName = "IRANSansWeb(FaNum)";

	static readonly SysColor PageBg = SysColor.FromArgb(240, 247, 250);
	static readonly SysColor CardBg = SysColor.White;
	static readonly SysColor Accent = SysColor.FromArgb(14, 116, 144);      // teal-700
	static readonly SysColor AccentSoft = SysColor.FromArgb(207, 250, 254); // cyan-100
	static readonly SysColor TitleColor = SysColor.FromArgb(30, 58, 78);
	static readonly SysColor TextMuted = SysColor.FromArgb(71, 85, 105);
	static readonly SysColor BorderColor = SysColor.FromArgb(165, 243, 252);
	static readonly SysColor KpiValue = SysColor.FromArgb(8, 47, 73);

	static readonly Dictionary<string, string> FaLabels = new(StringComparer.OrdinalIgnoreCase)
	{
		["Serial"] = "سریال",
		["PartCode"] = "کد کالا",
		["PartName"] = "نام کالا",
		["BranchTitle"] = "دپارتمان فروش",
		["CustomerName"] = "مشتری",
		["EquipmentCategory"] = "گروه تجهیز",
		["ProductionStep"] = "کد مرحله",
		["ProductionStepName"] = "مرحله ساخت",
		["ShamsiYear"] = "سال",
		["ItemCount"] = "تعداد",
		["ProductionStartShamsiDate"] = "تاریخ شروع تولید",
		["CreatedOnShamsiDateTime"] = "تاریخ ایجاد",
		["ProductionEndShamsiDate"] = "تاریخ پایان تولید",
		["PreparationShamsiDate"] = "تاریخ آماده‌سازی",
		["TestingEndShamsiDate"] = "تاریخ پایان تست",
		["PurchaseRequestNumber"] = "شماره درخواست خرید",
		["RequiredQty"] = "تعداد نیاز",
		["OrderQty"] = "تعداد سفارش",
		["StopShamsiDate"] = "تاریخ توقف",
		["Status"] = "وضعیت",
		["RequestedPersonel"] = "پرسنل درخواست‌کننده",
		["Comment"] = "توضیحات",
		["PurchaseRequestShamsiDate"] = "تاریخ درخواست خرید",
		["IsRoutineRequest"] = "درخواست روتین",
		["WeekLabel"] = "هفته",
		["WeekStartShamsi"] = "شروع هفته",
		["PassedCount"] = "گذشته از تولید",
		["RemainCount"] = "مانده",
		["RequestType"] = "نوع درخواست",
		["Cnt"] = "تعداد",
		["Id"] = "شناسه",
		["Z_MontazereTolid"] = "منتظر تولید",
		["Z_DarHaleTolid"] = "در حال تولید",
		["Z_Test"] = "تست",
		["Z_Bastebandi"] = "بسته‌بندی",
		["Z_AmadeyeErsal"] = "آماده ارسال",
		["Z_TahvilForosh"] = "تحویل فروش",
		["Z_TadarokatDarRah"] = "تدارکات در راه",
		["Z_SumOfPassedProductionSteps"] = "گذشته از تولید",
		["Z_SumOfProductionSteps"] = "جمع مراحل",
		["Z_RemainedCount"] = "مانده",
		["Z_RemainedPercent"] = "درصد مانده",
	};

	static void Main()
	{
		StiLicense.Key =
			"6vJhGtLLLz2GNviWmUTrhSqnOItdDwjBylQzQcAOiHkO46nMQvol4ASeg91in+mGJLnn2KMIpg3eSXQSgaFOm15+0l" +
			"hekKip+wRGMwXsKpHAkTvorOFqnpF9rchcYoxHXtjNDLiDHZGTIWq6D/2q4k/eiJm9fV6FdaJIUbWGS3whFWRLPHWC" +
			"BsWnalqTdZlP9knjaWclfjmUKf2Ksc5btMD6pmR7ZHQfHXfdgYK7tLR1rqtxYxBzOPq3LIBvd3spkQhKb07LTZQoyQ" +
			"3vmRSMALmJSS6ovIS59XPS+oSm8wgvuRFqE1im111GROa7Ww3tNJTA45lkbXX+SocdwXvEZyaaq61Uc1dBg+4uFRxv" +
			"yRWvX5WDmJz1X0VLIbHpcIjdEDJUvVAN7Z+FW5xKsV5ySPs8aegsY9ndn4DmoZ1kWvzUaz+E1mxMbOd3tyaNnmVhPZ" +
			"eIBILmKJGN0BwnnI5fu6JHMM/9QR2tMO1Z4pIwae4P92gKBrt0MqhvnU1Q6kIaPPuG2XBIvAWykVeH2a9EP6064e11" +
			"PFCBX4gEpJ3XFD0peE5+ddZh+h495qUc1H2B";

		var content = GenerateDashboardJson();
		var outPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SanayeDashboard.json"));
		File.WriteAllText(outPath, content, Encoding.UTF8);
		Console.WriteLine("JSON: " + outPath + " len=" + content.Length);
		Console.WriteLine("HasFont=" + content.Contains(FontName));
		Console.WriteLine("HasTurquoise=" + (content.Contains("Turquoise") || content.Contains("Blue")));
		Console.WriteLine("HasSerialFa=" + content.Contains("سریال"));
		Console.WriteLine("HasPartCodeFa=" + content.Contains("کد کالا"));
		if (!content.Contains(FontName)) throw new Exception("Font not applied");
		if (!content.Contains("سریال")) throw new Exception("Persian labels missing");

		using var conn = new SqlConnection(Cs);
		conn.Open();
		using var cmd = new SqlCommand(@"
UPDATE dbo.ReportBuilderReport
SET Content=@Content, ModifiedDateMiladiDateTime=SYSUTCDATETIME(), ModifiedByName=N'system-polish-ui'
WHERE Id=@Id;
SELECT Id, LEN(Content) AS CLen,
  CASE WHEN Content LIKE N'%IRANSansWeb%' THEN 1 ELSE 0 END AS HasFont,
  CASE WHEN Content LIKE N'%سریال%' THEN 1 ELSE 0 END AS HasFa,
  CASE WHEN Content LIKE N'%Turquoise%' OR Content LIKE N'%""Style"": ""Blue""%' OR Content LIKE N'%""Style"":""Blue""%' THEN 1 ELSE 0 END AS HasStyle
FROM dbo.ReportBuilderReport WHERE Id=@Id;", conn);
		cmd.Parameters.Add("@Content", SqlDbType.NVarChar, -1).Value = content;
		cmd.Parameters.Add("@Id", SqlDbType.BigInt).Value = ReportId;
		using var r = cmd.ExecuteReader();
		r.Read();
		Console.WriteLine($"Updated Id={r.GetInt64(0)} CLen={Convert.ToInt64(r.GetValue(1))} Font={r.GetValue(2)} Fa={r.GetValue(3)} Style={r.GetValue(4)}");
	}

	static StiFont F(float size, SysFontStyle style = SysFontStyle.Regular) => new(FontName, size, style);

	static void StyleTitle(IStiTitle title, string text, float size = 11f, bool bold = true)
	{
		title.Text = text;
		title.Visible = true;
		var t = (StiTitle)title;
		t.Font = F(size, bold ? SysFontStyle.Bold : SysFontStyle.Regular);
		t.ForeColor = TitleColor;
		t.BackColor = SysColor.Transparent;
	}

	static void StyleCard(dynamic el, StiElementStyleIdent style)
	{
		el.Style = style;
		el.BackColor = CardBg;
		el.Border = new StiSimpleBorder(StiBorderSides.All, BorderColor, 1, StiPenStyle.Solid);
		el.CornerRadius = new StiCornerRadius(10);
		try { el.Font = F(13f, SysFontStyle.Bold); } catch { /* chart has no Font */ }
		try { el.ForeColor = KpiValue; } catch { }
	}

	static void SetMeterLabel(object? meter, string columnName)
	{
		if (meter == null) return;
		var label = FaLabels.TryGetValue(columnName, out var fa) ? fa : columnName;
		meter.GetType().GetProperty("Label")?.SetValue(meter, label);
	}

	static string GenerateDashboardJson()
	{
		var report = StiReport.CreateNewDashboard();
		report.ReportName = "SanayeTaahodatMoavenatEjraee";
		report.ReportAlias = "صنایع - تعهدات معاونت اجرایی";
		report.RegData(BuildSampleDataSet());
		report.Dictionary.Synchronize();
		report.Pages.Clear();

		BuildMainPage(CreatePage(report, "PageMain", "۱ صفحه اصلی", 1200, 1100), report);
		BuildTablePage(CreatePage(report, "PageWaitingEngineering", "۲ سفارشات در انتظار مهندسی", 1200, 800), report, "WaitingEngineering",
			new[] { "Serial", "PartCode", "PartName", "BranchTitle", "CustomerName", "ProductionStepName", "ProductionStartShamsiDate", "CreatedOnShamsiDateTime" });
		BuildTablePage(CreatePage(report, "PageReadyToSend", "۳ سفارشات آماده به ارسال", 1200, 800), report, "ReadyToSend",
			new[] { "Serial", "PartCode", "PartName", "BranchTitle", "CustomerName", "ProductionEndShamsiDate", "PreparationShamsiDate", "TestingEndShamsiDate" });
		BuildTablePage(CreatePage(report, "PageStoppedPurchases", "۴ خریدهای متوقف شده", 1200, 800), report, "StoppedPurchases",
			new[] { "PurchaseRequestNumber", "PartCode", "PartName", "RequiredQty", "OrderQty", "StopShamsiDate", "Status", "RequestedPersonel", "Comment" });
		BuildOpenWithoutEngPage(CreatePage(report, "PageOpenWithoutEngAccept", "۵ درخواست‌های باز بدون تایید مهندسی", 1200, 900), report);
		BuildWeeklyPage(CreatePage(report, "PageWeekly", "۶ نمودار هفتگی", 1200, 900), report);
		return report.SaveToJsonString();
	}

	static StiDashboard CreatePage(StiReport report, string name, string alias, double width, double height)
	{
		var page = new StiDashboard
		{
			Name = name,
			Alias = alias,
			Width = width,
			Height = height,
			Style = StiElementStyleIdent.Turquoise,
			Brush = new StiSolidBrush(PageBg),
			BackColor = PageBg
		};
		report.Pages.Add(page);
		return page;
	}

	static void BuildMainPage(StiDashboard page, StiReport report)
	{
		var kpi = report.Dictionary.DataSources["KpiSummary"];
		var orders = report.Dictionary.DataSources["OrdersMain"];
		double x = 20, y = 20, cardW = 220, cardH = 140, gap = 12;

		AddIndicator(page, "IndMontazer", "منتظر تولید", kpi.Columns["Z_MontazereTolid"], x, y, cardW, cardH, StiElementStyleIdent.Blue); x += cardW + gap;
		AddIndicator(page, "IndDarHal", "در حال تولید", kpi.Columns["Z_DarHaleTolid"], x, y, cardW, cardH, StiElementStyleIdent.Turquoise); x += cardW + gap;
		AddIndicator(page, "IndTest", "تست", kpi.Columns["Z_Test"], x, y, cardW, cardH, StiElementStyleIdent.DarkTurquoise); x += cardW + gap;
		AddIndicator(page, "IndBaste", "بسته‌بندی", kpi.Columns["Z_Bastebandi"], x, y, cardW, cardH, StiElementStyleIdent.Blue); x += cardW + gap;
		AddIndicator(page, "IndAmade", "آماده ارسال", kpi.Columns["Z_AmadeyeErsal"], x, y, cardW, cardH, StiElementStyleIdent.Green);

		x = 20; y += cardH + gap;
		AddIndicator(page, "IndTahvil", "تحویل فروش", kpi.Columns["Z_TahvilForosh"], x, y, cardW, cardH, StiElementStyleIdent.DarkBlue); x += cardW + gap;
		AddIndicator(page, "IndTadarokat", "تدارکات در راه", kpi.Columns["Z_TadarokatDarRah"], x, y, cardW, cardH, StiElementStyleIdent.Orange); x += cardW + gap;
		AddIndicator(page, "IndPassed", "گذشته از تولید", kpi.Columns["Z_SumOfPassedProductionSteps"], x, y, cardW, cardH, StiElementStyleIdent.Turquoise); x += cardW + gap;
		AddIndicator(page, "IndSum", "جمع مراحل", kpi.Columns["Z_SumOfProductionSteps"], x, y, cardW, cardH, StiElementStyleIdent.Blue); x += cardW + gap;
		AddIndicator(page, "IndRemain", "مانده", kpi.Columns["Z_RemainedCount"], x, y, cardW, cardH, StiElementStyleIdent.Sienna);

		y += cardH + gap + 8;

		var gauge = new StiGaugeElement { Name = "GaugeRemain", ClientRectangle = new RectangleD(20, y, 320, 260), Maximum = 100 };
		StyleCard(gauge, StiElementStyleIdent.Turquoise);
		StyleTitle(gauge.Title, "درصد مانده", 12f);
		gauge.Font = F(12f);
		gauge.AddValue(kpi.Columns["Z_RemainedPercent"]);
		SetMeterLabel(gauge.GetValue(), "Z_RemainedPercent");
		page.Components.Add(gauge);

		var progress = new StiProgressElement { Name = "ProgressPassed", ClientRectangle = new RectangleD(360, y, 320, 260) };
		StyleCard(progress, StiElementStyleIdent.Green);
		StyleTitle(progress.Title, "پیشرفت تولید", 12f);
		progress.Font = F(12f);
		progress.AddValue(kpi.Columns["Z_SumOfPassedProductionSteps"]);
		progress.AddTarget(kpi.Columns["Z_SumOfProductionSteps"]);
		SetMeterLabel(progress.GetValue(), "Z_SumOfPassedProductionSteps");
		page.Components.Add(progress);

		var donut = new StiChartElement { Name = "DonutSteps", ClientRectangle = new RectangleD(700, y, 480, 260) };
		StyleCard(donut, StiElementStyleIdent.DarkTurquoise);
		StyleTitle(donut.Title, "توزیع مراحل ساخت", 12f);
		donut.AddArgument(orders.Columns["ProductionStepName"]);
		donut.AddValue(orders.Columns["ItemCount"]);
		TrySetSeriesType(donut, "Doughnut");
		LabelChart(donut, "ProductionStepName", "ItemCount");
		page.Components.Add(donut);

		y += 280;
		AddCombo(page, "FilterYear", orders.Columns["ShamsiYear"], 20, y, 280, 70, "سال");
		AddCombo(page, "FilterBranch", orders.Columns["BranchTitle"], 320, y, 320, 70, "دپارتمان فروش");
		AddCombo(page, "FilterCustomer", orders.Columns["CustomerName"], 660, y, 340, 70, "مشتری");
		y += 90;

		var treemap = new StiChartElement { Name = "TreeMapEquipment", ClientRectangle = new RectangleD(20, y, 560, 300) };
		StyleCard(treemap, StiElementStyleIdent.Blue);
		StyleTitle(treemap.Title, "گروه تجهیز", 12f);
		treemap.AddArgument(orders.Columns["EquipmentCategory"]);
		treemap.AddValue(orders.Columns["ItemCount"]);
		TrySetSeriesType(treemap, "Treemap");
		LabelChart(treemap, "EquipmentCategory", "ItemCount");
		page.Components.Add(treemap);

		var byBranch = new StiChartElement { Name = "BarByBranch", ClientRectangle = new RectangleD(600, y, 580, 300) };
		StyleCard(byBranch, StiElementStyleIdent.Turquoise);
		StyleTitle(byBranch.Title, "تعداد بر اساس دپارتمان فروش", 12f);
		byBranch.AddArgument(orders.Columns["BranchTitle"]);
		byBranch.AddValue(orders.Columns["ItemCount"]);
		LabelChart(byBranch, "BranchTitle", "ItemCount");
		page.Components.Add(byBranch);
	}

	static void BuildTablePage(StiDashboard page, StiReport report, string sourceName, string[] columns)
	{
		var src = report.Dictionary.DataSources[sourceName];
		var table = new StiTableElement { Name = "Table_" + sourceName, ClientRectangle = new RectangleD(20, 20, 1160, 740) };
		StyleCard(table, StiElementStyleIdent.AliceBlue);
		StyleTitle(table.Title, page.Alias, 13f);
		table.Font = F(10f);
		table.ForeColor = TextMuted;
		foreach (var colName in columns)
		{
			if (!src.Columns.Contains(colName)) continue;
			table.CreateMeter(src.Columns[colName]);
		}
		LabelTableColumns(table, columns);
		page.Components.Add(table);
	}

	static void BuildOpenWithoutEngPage(StiDashboard page, StiReport report)
	{
		var open = report.Dictionary.DataSources["OpenWithoutEngAccept"];
		var byType = report.Dictionary.DataSources["OpenOrdersByRequestType"];

		var donut = new StiChartElement { Name = "DonutRequestType", ClientRectangle = new RectangleD(20, 20, 450, 300) };
		StyleCard(donut, StiElementStyleIdent.Turquoise);
		StyleTitle(donut.Title, "تفکیک نوع درخواست", 12f);
		donut.AddArgument(byType.Columns["RequestType"]);
		donut.AddValue(byType.Columns["Cnt"]);
		TrySetSeriesType(donut, "Doughnut");
		LabelChart(donut, "RequestType", "Cnt");
		page.Components.Add(donut);

		var table = new StiTableElement { Name = "TableOpenWithoutEng", ClientRectangle = new RectangleD(20, 340, 1160, 520) };
		StyleCard(table, StiElementStyleIdent.AliceBlue);
		StyleTitle(table.Title, "درخواست‌های باز بدون تایید مهندسی", 13f);
		table.Font = F(10f);
		var cols = new[] { "PurchaseRequestNumber", "PartCode", "PartName", "RequiredQty", "OrderQty", "PurchaseRequestShamsiDate", "RequestedPersonel", "IsRoutineRequest", "Status" };
		foreach (var colName in cols)
			if (open.Columns.Contains(colName)) table.CreateMeter(open.Columns[colName]);
		LabelTableColumns(table, cols);
		page.Components.Add(table);
	}

	static void BuildWeeklyPage(StiDashboard page, StiReport report)
	{
		var weekly = report.Dictionary.DataSources["WeeklyCommitments"];
		var line = new StiChartElement { Name = "LineWeekly", ClientRectangle = new RectangleD(20, 20, 1160, 520) };
		StyleCard(line, StiElementStyleIdent.DarkTurquoise);
		StyleTitle(line.Title, "روند تعهدات هفتگی", 13f);
		line.AddArgument(weekly.Columns["WeekLabel"]);
		line.AddValue(weekly.Columns["ItemCount"]);
		TrySetSeriesType(line, "Spline");
		LabelChart(line, "WeekLabel", "ItemCount");
		page.Components.Add(line);

		var table = new StiTableElement { Name = "TableWeekly", ClientRectangle = new RectangleD(20, 560, 1160, 300) };
		StyleCard(table, StiElementStyleIdent.AliceBlue);
		StyleTitle(table.Title, "جزئیات هفتگی", 12f);
		table.Font = F(10f);
		var cols = new[] { "WeekLabel", "WeekStartShamsi", "ItemCount", "PassedCount", "RemainCount" };
		foreach (var colName in cols)
			if (weekly.Columns.Contains(colName)) table.CreateMeter(weekly.Columns[colName]);
		LabelTableColumns(table, cols);
		page.Components.Add(table);
	}

	static void AddIndicator(StiDashboard page, string name, string title, StiDataColumn column, double x, double y, double w, double h, StiElementStyleIdent style)
	{
		var ind = new StiIndicatorElement { Name = name, ClientRectangle = new RectangleD(x, y, w, h) };
		StyleCard(ind, style);
		StyleTitle(ind.Title, title, 11f);
		ind.Font = F(18f, SysFontStyle.Bold);
		ind.ForeColor = KpiValue;
		ind.AddValue(column);
		SetMeterLabel(ind.GetValue(), column.Name);
		page.Components.Add(ind);
	}

	static void AddCombo(StiDashboard page, string name, StiDataColumn column, double x, double y, double w, double h, string title)
	{
		var combo = new StiComboBoxElement { Name = name, ClientRectangle = new RectangleD(x, y, w, h) };
		combo.Style = StiElementStyleIdent.Turquoise;
		combo.BackColor = CardBg;
		combo.Border = new StiSimpleBorder(StiBorderSides.All, BorderColor, 1, StiPenStyle.Solid);
		combo.CornerRadius = new StiCornerRadius(8);
		combo.AddKeyMeter(column);
		combo.AddNameMeter(column);
		SetMeterLabel(combo.GetKeyMeter(), column.Name);
		SetMeterLabel(combo.GetNameMeter(), column.Name);
		// Combo has no Title API in this build; name encodes purpose
		page.Components.Add(combo);
	}

	static void LabelChart(StiChartElement chart, string argCol, string valCol)
	{
		foreach (var m in chart.FetchAllArguments()) SetMeterLabel(m, argCol);
		foreach (var m in chart.FetchAllValues()) SetMeterLabel(m, valCol);
	}

	static void LabelTableColumns(StiTableElement table, string[] columns)
	{
		var meters = table.GetType().GetProperty("Columns")?.GetValue(table) as System.Collections.IEnumerable;
		if (meters == null) return;
		var i = 0;
		foreach (var meter in meters)
		{
			if (i >= columns.Length) break;
			SetMeterLabel(meter, columns[i]);
			// also try Font on meter if present
			var fontProp = meter.GetType().GetProperty("Font");
			if (fontProp != null && fontProp.CanWrite)
				try { fontProp.SetValue(meter, F(10f)); } catch { }
			i++;
		}
	}

	static void TrySetSeriesType(StiChartElement chart, string token)
	{
		try
		{
			foreach (var meter in chart.FetchAllValues())
			{
				var prop = meter.GetType().GetProperty("SeriesType");
				if (prop == null || !prop.PropertyType.IsEnum) continue;
				foreach (var v in Enum.GetValues(prop.PropertyType))
					if (v.ToString()!.Contains(token, StringComparison.OrdinalIgnoreCase))
					{ prop.SetValue(meter, v); return; }
			}
		}
		catch { }
	}

	static DataSet BuildSampleDataSet()
	{
		var ds = new DataSet();
		var kpi = new DataTable("KpiSummary");
		foreach (var c in new[] { "Z_MontazereTolid", "Z_DarHaleTolid", "Z_Test", "Z_Bastebandi", "Z_AmadeyeErsal", "Z_TahvilForosh", "Z_TadarokatDarRah", "Z_SumOfPassedProductionSteps", "Z_SumOfProductionSteps", "Z_RemainedCount", "Z_RemainedPercent" })
			kpi.Columns.Add(c, typeof(decimal));
		kpi.Rows.Add(1, 2, 3, 4, 5, 6, 7, 18, 28, 10, 35.7m);
		ds.Tables.Add(kpi);

		var orders = new DataTable("OrdersMain");
		foreach (var c in new (string n, Type t)[] {
			("Id", typeof(long)), ("Serial", typeof(string)), ("PartCode", typeof(string)), ("PartName", typeof(string)),
			("BranchTitle", typeof(string)), ("CustomerName", typeof(string)), ("EquipmentCategory", typeof(string)),
			("ProductionStep", typeof(int)), ("ProductionStepName", typeof(string)), ("ShamsiYear", typeof(string)),
			("ItemCount", typeof(int)), ("ProductionStartShamsiDate", typeof(string)), ("CreatedOnShamsiDateTime", typeof(string))
		}) orders.Columns.Add(c.n, c.t);
		orders.Rows.Add(1L, "S1", "P1", "کالا", "دفتر مرکزی", "مشتری ۱", "تجهیز اصلی", 215, "در حال تولید", "1404", 5, "1404/01/01", "1404/01/01 10:00");
		ds.Tables.Add(orders);

		var waiting = orders.Clone(); waiting.TableName = "WaitingEngineering"; waiting.ImportRow(orders.Rows[0]); ds.Tables.Add(waiting);

		var ready = new DataTable("ReadyToSend");
		foreach (var c in new[] { "Id", "Serial", "PartCode", "PartName", "BranchTitle", "CustomerName", "ProductionEndShamsiDate", "PreparationShamsiDate", "TestingEndShamsiDate", "ItemCount" })
			ready.Columns.Add(c, c is "Id" or "ItemCount" ? typeof(long) : typeof(string));
		ready.Rows.Add(1L, "S2", "P2", "کالا۲", "کارخانه", "مشتری ۲", "1404/03/01", "1404/02/20", "1404/02/25", 1L);
		ds.Tables.Add(ready);

		var stopped = new DataTable("StoppedPurchases");
		foreach (var c in new[] { "Id", "PurchaseRequestNumber", "PartCode", "PartName", "RequiredQty", "OrderQty", "StopShamsiDate", "Status", "RequestedPersonel", "Comment" })
			stopped.Columns.Add(c, c is "Id" or "PurchaseRequestNumber" or "RequiredQty" or "OrderQty" ? typeof(long) : typeof(string));
		stopped.Rows.Add(1L, 100L, "P9", "کالا۹", 2L, 2L, "1404/01/10", "متوقف", "کاربر", "توقف");
		ds.Tables.Add(stopped);

		var openEng = new DataTable("OpenWithoutEngAccept");
		foreach (var c in new[] { "Id", "PurchaseRequestNumber", "PartCode", "PartName", "RequiredQty", "OrderQty", "PurchaseRequestShamsiDate", "RequestedPersonel", "IsRoutineRequest", "Status" })
			openEng.Columns.Add(c, c is "Id" or "PurchaseRequestNumber" or "RequiredQty" or "OrderQty" ? typeof(long) : (c == "IsRoutineRequest" ? typeof(bool) : typeof(string)));
		openEng.Rows.Add(1L, 200L, "P8", "کالا۸", 1L, 1L, "1404/01/05", "کاربر", false, "باز");
		ds.Tables.Add(openEng);

		var byType = new DataTable("OpenOrdersByRequestType");
		byType.Columns.Add("RequestType", typeof(string)); byType.Columns.Add("Cnt", typeof(int));
		byType.Rows.Add("روتین", 4); byType.Rows.Add("غیر روتین", 7); ds.Tables.Add(byType);

		var weekly = new DataTable("WeeklyCommitments");
		foreach (var c in new[] { "WeekLabel", "WeekStartShamsi", "ItemCount", "PassedCount", "RemainCount" })
			weekly.Columns.Add(c, c.Contains("Count") ? typeof(int) : typeof(string));
		weekly.Rows.Add("هفته ۱", "1404/01/01", 10, 4, 6); ds.Tables.Add(weekly);
		return ds;
	}
}
