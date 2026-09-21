# Applies Seed_Certificate_Reports.sql with UTF-8 (sqlcmd without -f 65001 corrupts Persian).
$ErrorActionPreference = "Stop"
$path = Join-Path $PSScriptRoot "Seed_Certificate_Reports.sql"
$cs = 'Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$; TrustServerCertificate=True'
$sql = [IO.File]::ReadAllText($path, [Text.Encoding]::UTF8)

Add-Type -AssemblyName System.Data
$conn = New-Object System.Data.SqlClient.SqlConnection $cs
$conn.Open()
try {
    $cmd = $conn.CreateCommand()
    $cmd.CommandTimeout = 120
    $cmd.CommandText = $sql
    $r = $cmd.ExecuteReader()
    if ($r.Read()) {
        Write-Output ("SavedQueryId={0} QueryName={1} CertificateReportCount={2}" -f $r["SavedQueryId"], $r["QueryName"], $r["CertificateReportCount"])
    }
    $r.Close()
}
finally {
    $conn.Close()
}
