# Pushes Stimulsoft JSON from Data/Seed/CngReports into dbo.ReportBuilderReport.Content.
param(
    [string]$Name = "*"
)
$ErrorActionPreference = "Stop"
$seedDir = Join-Path $PSScriptRoot "..\Seed\CngReports"
$cs = 'Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$; TrustServerCertificate=True'

Add-Type -AssemblyName System.Data
$conn = New-Object System.Data.SqlClient.SqlConnection $cs
$conn.Open()
try {
    $files = @(Get-ChildItem -Path $seedDir -Filter "$Name.json")
    if (-not $files) { throw "No seed JSON matching $Name.json in $seedDir" }
    foreach ($f in $files) {
        $json = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = @"
UPDATE dbo.ReportBuilderReport
SET Content = @content,
    ModifiedById = 1,
    ModifiedByName = N'seed-cng-repairs-reports',
    ModifiedDateMiladiDateTime = SYSDATETIME()
WHERE Name = @name;
SELECT @@ROWCOUNT AS RowsUpdated, @name AS ReportName, LEN(@content) AS ContentLen,
       (SELECT Title FROM dbo.ReportBuilderReport WHERE Name = @name) AS Title;
"@
        $pContent = $cmd.Parameters.Add("@content", [Data.SqlDbType]::NVarChar, -1)
        $pContent.Value = $json
        $pName = $cmd.Parameters.Add("@name", [Data.SqlDbType]::NVarChar, 200)
        $pName.Value = $f.BaseName
        $r = $cmd.ExecuteReader()
        if ($r.Read()) {
            Write-Output ("{0}: rows={1} len={2} title={3}" -f $r["ReportName"], $r["RowsUpdated"], $r["ContentLen"], $r["Title"])
        }
        $r.Close()
    }
}
finally {
    $conn.Close()
}
