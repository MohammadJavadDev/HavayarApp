# Applies Seed_CngRepairs_Reports.sql with UTF-8 (sqlcmd without -f 65001 corrupts Persian).
# Then pushes Stimulsoft JSON Content from Data/Seed/CngReports.
$ErrorActionPreference = "Stop"
$scriptDir = $PSScriptRoot
$path = Join-Path $scriptDir "Seed_CngRepairs_Reports.sql"
$cs = 'Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$; TrustServerCertificate=True'

# Prefer sqlcmd -f 65001 when available
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if ($sqlcmd) {
    & sqlcmd -S 172.20.40.42 -d HavayarApp -U sa -P 'Sql123456$$' -C -b -I -f 65001 -i $path
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit $LASTEXITCODE" }
    Write-Output "sqlcmd apply OK"
} else {
    $sql = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)
    # Split on GO batches
    $batches = [regex]::Split($sql, '(?im)^\s*GO\s*$')
    Add-Type -AssemblyName System.Data
    $conn = New-Object System.Data.SqlClient.SqlConnection $cs
    $conn.Open()
    try {
        foreach ($batch in $batches) {
            $b = $batch.Trim()
            if (-not $b) { continue }
            $cmd = $conn.CreateCommand()
            $cmd.CommandTimeout = 180
            $cmd.CommandText = $b
            $r = $cmd.ExecuteReader()
            while ($r.Read()) {
                $vals = @()
                for ($i=0; $i -lt $r.FieldCount; $i++) { $vals += ("{0}={1}" -f $r.GetName($i), $r.GetValue($i)) }
                Write-Output ($vals -join "; ")
            }
            while ($r.NextResult()) {
                while ($r.Read()) {
                    $vals = @()
                    for ($i=0; $i -lt $r.FieldCount; $i++) { $vals += ("{0}={1}" -f $r.GetName($i), $r.GetValue($i)) }
                    Write-Output ($vals -join "; ")
                }
            }
            $r.Close()
        }
    } finally { $conn.Close() }
}

& (Join-Path $scriptDir "Update_CngRepairs_Report_Content.ps1")
