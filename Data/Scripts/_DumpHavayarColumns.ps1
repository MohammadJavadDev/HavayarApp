# ASCII-only: dump INFORMATION_SCHEMA.COLUMNS to JSON for the Python seed generator.
param(
    [string]$ConnectionString = 'Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$; TrustServerCertificate=True',
    [string]$OutFile = ''
)
$ErrorActionPreference = 'Stop'
if (-not $OutFile) { $OutFile = Join-Path $PSScriptRoot '_havayar_columns.json' }

function Dump([string]$cs) {
    $conn = New-Object System.Data.SqlClient.SqlConnection $cs
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS"
    $r = $cmd.ExecuteReader()
    $map = @{}
    while ($r.Read()) {
        $key = ($r.GetString(0) + '.' + $r.GetString(1))
        if (-not $map.ContainsKey($key)) { $map[$key] = New-Object System.Collections.Generic.List[string] }
        [void]$map[$key].Add($r.GetString(2))
    }
    $r.Close(); $conn.Close()
    return $map
}

try {
    $map = Dump $ConnectionString
} catch {
    Write-Host "Primary CS failed: $($_.Exception.Message)"
    $ConnectionString = 'Data Source=PortalSRV\PORTAL; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$; TrustServerCertificate=True'
    $map = Dump $ConnectionString
}

$obj = [ordered]@{ connection = $ConnectionString; tables = [ordered]@{} }
foreach ($k in ($map.Keys | Sort-Object)) { $obj.tables[$k] = @($map[$k]) }
$json = $obj | ConvertTo-Json -Depth 4 -Compress
[System.IO.File]::WriteAllText($OutFile, $json, [System.Text.UTF8Encoding]::new($false))
Write-Host "Wrote $OutFile tables=$($map.Count)"
Write-Output $ConnectionString
