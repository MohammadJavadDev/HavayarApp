# ASCII-only runner: execute UTF-8 SQL seed against HavayarApp.
param(
    [string]$SqlFile = (Join-Path $PSScriptRoot 'Seed_AfterSalesServiceSystem_DataProfilesAndAccess.sql'),
    [string]$ConnectionString = 'Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$; TrustServerCertificate=True'
)
$ErrorActionPreference = 'Stop'
$sql = [System.IO.File]::ReadAllText($SqlFile, [System.Text.Encoding]::UTF8)
$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.add_InfoMessage({ param($s,$e) Write-Host $e.Message })
$conn.FireInfoMessageEventOnUserErrors = $false
try {
    $conn.Open()
} catch {
    $ConnectionString = 'Data Source=PortalSRV\PORTAL; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$; TrustServerCertificate=True'
    $conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
    $conn.add_InfoMessage({ param($s,$e) Write-Host $e.Message })
    $conn.Open()
}
$cmd = $conn.CreateCommand()
$cmd.CommandTimeout = 180
$cmd.CommandText = $sql
try {
    $reader = $cmd.ExecuteReader()
    do {
        $table = New-Object System.Data.DataTable
        $table.Load($reader)
        if ($table.Columns.Count -gt 0 -and $table.Rows.Count -gt 0) {
            $table | Format-Table -AutoSize | Out-String -Width 220 | Write-Host
        }
    } while (-not $reader.IsClosed)
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
