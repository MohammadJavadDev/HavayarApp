# Pushes Stimulsoft JSON from Data/Seed/TrnReports into dbo.ReportBuilderReport.Content.
# Usage: .\Update_TrnReport_Content.ps1 [-Name Cng]
param(
    [Parameter(Mandatory = $true)]
    [string]$Name
)

$ErrorActionPreference = "Stop"
$seedDir = Join-Path $PSScriptRoot "..\Seed\TrnReports"
$cs = 'Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$; TrustServerCertificate=True'

Add-Type -AssemblyName System.Data
$conn = New-Object System.Data.SqlClient.SqlConnection $cs
$conn.Open()
try {
    $files = @(Get-ChildItem -Path $seedDir -Filter "$Name.json")
    if (-not $files) { throw "No seed JSON named $Name.json in $seedDir" }

    foreach ($f in $files) {
        $json = [IO.File]::ReadAllText($f.FullName, [Text.Encoding]::UTF8)
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = @"
UPDATE dbo.ReportBuilderReport
SET Content = @content,
    ModifiedById = 1,
    ModifiedByName = N'seed-certificate-reports',
    ModifiedDateMiladiDateTime = SYSDATETIME()
WHERE Name = @name;
SELECT @@ROWCOUNT AS RowsUpdated, @name AS ReportName, LEN(@content) AS ContentLen;
"@
        $pContent = $cmd.Parameters.Add("@content", [Data.SqlDbType]::NVarChar, -1)
        $pContent.Value = $json
        $pName = $cmd.Parameters.Add("@name", [Data.SqlDbType]::NVarChar, 200)
        $pName.Value = $f.BaseName
        $r = $cmd.ExecuteReader()
        if ($r.Read()) {
            Write-Output ("{0}: rows={1} len={2}" -f $r["ReportName"], $r["RowsUpdated"], $r["ContentLen"])
        }
        $r.Close()
    }
}
finally {
    $conn.Close()
}
