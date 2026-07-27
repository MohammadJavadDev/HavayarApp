using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

var cs = "Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$;TrustServerCertificate=True";
await using var conn = new SqlConnection(cs);
await conn.OpenAsync();
await using var cmd = new SqlCommand("SELECT Content FROM dbo.ReportBuilderReport WHERE Id=14", conn);
var content = (string)(await cmd.ExecuteScalarAsync())!;
File.WriteAllText(@"D:\Projects\Havayar\HavayarApp\_tools\SanayeDashboardGen\report14.json", content);
var root = JsonNode.Parse(content)!;
Console.WriteLine("Pages=" + root["Pages"]!.AsObject().Count);
Console.WriteLine("IRANSansWeb count=" + (content.Split("IRANSansWeb").Length - 1));
Console.WriteLine("Turquoise count=" + (content.Split("Turquoise").Length - 1));
Console.WriteLine("Blue style mentions=" + (content.Split("\"Style\": \"Blue\"").Length - 1 + content.Split("\"Style\":\"Blue\"").Length - 1));

// spot-check page0 first indicator
var ind = root["Pages"]!["0"]!["Components"]!["0"]!;
Console.WriteLine("Ind0 Name=" + ind["Name"] + " Style=" + ind["Style"] + " Font=" + ind["Font"] + " Title=" + ind["Title"]?["Text"] + " TitleFont=" + ind["Title"]?["Font"]);
Console.WriteLine("Ind0 Value Label=" + ind["Value"]?["Label"]);

// table page waiting
var tbl = root["Pages"]!["1"]!["Components"]!["0"]!;
Console.WriteLine("Table Style=" + tbl["Style"] + " Font=" + tbl["Font"] + " Title=" + tbl["Title"]?["Text"]);
var cols = tbl["Columns"]!.AsObject();
foreach (var c in cols.Take(4))
  Console.WriteLine("  Col Label=" + c.Value?["Label"] + " Expr=" + c.Value?["Expression"]);

// English leftovers heuristic
var englishHits = new[] { "Serial", "PartCode", "PartName", "BranchTitle", "CustomerName", "ItemCount", "PurchaseRequestNumber", "WeekLabel", "RequestType", "Status", "Comment" };
foreach (var e in englishHits)
{
  // Label":"English" would be bad; Label":"Persian" good
  var bad = content.Contains($"\"Label\": \"{e}\"") || content.Contains($"\"Label\":\"{e}\"");
  Console.WriteLine($"Label still English '{e}'? {bad}");
}
