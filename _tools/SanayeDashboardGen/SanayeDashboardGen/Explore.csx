using System;
using System.Linq;
using System.Reflection;
var asm = Assembly.LoadFrom(typeof(Stimulsoft.Report.StiReport).Assembly.Location);
foreach (var t in asm.GetTypes().Where(x => x.Name.Contains("Indicator") || x.Name.Contains("Gauge") || x.Name.Contains("TreeMap") || x.Name.Contains("ComboBox") || x.Name.Contains("Progress") || x.Name.Contains("ChartElement") || x.Name.Contains("TableElement") || x.Name.Contains("Dashboard") || x.Name.Contains("FilterElement") || x.Name.Contains("ListBox")).OrderBy(x => x.FullName))
  Console.WriteLine(t.FullName);
