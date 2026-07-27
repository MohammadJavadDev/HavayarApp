using System;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;

var cs = "Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$;TrustServerCertificate=True";
var outDir = @"D:\Projects\Havayar\HavayarApp\_tools\SanayeDashboardGen";
Directory.CreateDirectory(outDir);
await using var conn = new SqlConnection(cs);
await conn.OpenAsync();
foreach (var id in new[] { 13, 14 })
{
    await using var cmd = new SqlCommand("SELECT Content FROM dbo.ReportBuilderReport WHERE Id=@id", conn);
    cmd.Parameters.AddWithValue("@id", id);
    var content = (string)(await cmd.ExecuteScalarAsync())!;
    File.WriteAllText(Path.Combine(outDir, $"report{id}.json"), content);
    var root = JsonNode.Parse(content)!;
    Console.WriteLine($"=== Report {id} ===");
    Console.WriteLine("ReportUnit=" + root["ReportUnit"]);
    Console.WriteLine("Pages keys=" + string.Join(",", ((JsonObject)root["Pages"]!).Select(kv => kv.Key)));
    var dict = root["Dictionary"];
    var ds = dict?["DataSources"];
    if (ds is JsonObject dso)
    {
        Console.WriteLine("DataSources count=" + dso.Count);
        foreach (var kv in dso)
        {
            var name = kv.Value?["Name"]?.ToString();
            var alias = kv.Value?["Alias"]?.ToString();
            var cols = kv.Value?["Columns"] as JsonObject;
            Console.WriteLine($"  DS Name={name} Alias={alias} Cols={cols?.Count}");
        }
    }
    var pages = root["Pages"] as JsonObject;
    foreach (var pk in pages!)
    {
        var page = pk.Value!;
        var comps = page["Components"] as JsonObject;
        Console.WriteLine($"Page[{pk.Key}] Ident={page["Ident"]} Name={page["Name"]} Alias={page["Alias"]} CompCount={comps?.Count}");
        if (comps != null)
        {
            var i = 0;
            foreach (var c in comps)
            {
                if (i++ > 4) { Console.WriteLine("  ..."); break; }
                var el = c.Value!;
                var rect = el["ClientRectangle"]?.ToString();
                var exprSample = "";
                // dig for Expression
                var json = el.ToJsonString();
                var idx = json.IndexOf("\"Expression\"");
                if (idx >= 0) exprSample = json.Substring(idx, Math.Min(120, json.Length - idx));
                Console.WriteLine($"  Comp Ident={el["Ident"]} Name={el["Name"]} Rect={rect}");
                if (!string.IsNullOrEmpty(exprSample)) Console.WriteLine("    " + exprSample.Replace("\r","").Replace("\n"," "));
            }
        }
    }
}
