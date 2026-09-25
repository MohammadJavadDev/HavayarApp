# Sync_CngWorkReport_CopyFiles.ps1
# LOCAL/legacy helper only — NOT the production import path.
# Production: run App.BackgroundJob job CngWorkReportFileImportJob.ImportFilesFromUnc on the server
# (JobId: App.BackgroundJob.Jobs.Cng.CngWorkReportFileImportJob.ImportFilesFromUnc) so files go through IFileService.
# After Sync_CngWorkReport_FromTotalSystem.sql: this script used to copy UNC files into FileEntity by hand.
# Source share: \\172.20.40.27\Uploads\Cng\WorkReport\
# Target: relative path under Storage:UploadsPath (wwwroot\Uploads) -> cng/workreport/{HtsId}_{fileName}
# If copy fails after source is found, FileEntity.PhysicalPath keeps the UNC and FileId is still set.
# If source file is missing, FileId stays null and WorkReportFileAddress is kept (per plan).
#
# Examples:
#   .\Sync_CngWorkReport_CopyFiles.ps1
#   .\Sync_CngWorkReport_CopyFiles.ps1 -Server 172.20.40.42 -Database HavayarApp -User sa -Password '***'
#   .\Sync_CngWorkReport_CopyFiles.ps1 -WhatIf

[CmdletBinding()]
param(
    [string]$Server = '',
    [string]$Database = 'HavayarApp',
    [string]$User = '',
    [string]$Password = '',
    [string]$ConnectionString = '',
    [string]$UploadsRoot = '',
    [string]$SourceShare = '\\172.20.40.27\Uploads\Cng\WorkReport',
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
$scriptDir = $PSScriptRoot
$repoRoot = (Resolve-Path (Join-Path $scriptDir '..\..')).Path

function Get-ContentType([string]$name) {
    $ext = [IO.Path]::GetExtension($name).ToLowerInvariant()
    switch ($ext) {
        '.pdf'  { return 'application/pdf' }
        '.png'  { return 'image/png' }
        '.jpg'  { return 'image/jpeg' }
        '.jpeg' { return 'image/jpeg' }
        '.zip'  { return 'application/zip' }
        '.rar'  { return 'application/x-rar-compressed' }
        '.doc'  { return 'application/msword' }
        '.docx' { return 'application/vnd.openxmlformats-officedocument.wordprocessingml.document' }
        '.xls'  { return 'application/vnd.ms-excel' }
        '.xlsx' { return 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }
        default { return 'application/octet-stream' }
    }
}

function Read-AppSettingsConnection {
    $candidates = @(
        (Join-Path $repoRoot 'WebApp\appsettings.Development.json'),
        (Join-Path $repoRoot 'WebApp\appsettings.json')
    )
    foreach ($path in $candidates) {
        if (-not (Test-Path -LiteralPath $path)) { continue }
        try {
            $json = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
            $db = $json.ConnectionStrings.Db
            if ($db) { return [string]$db }
        } catch { }
    }
    return $null
}

function Read-UploadsRoot {
    $candidates = @(
        (Join-Path $repoRoot 'WebApp\appsettings.Development.json'),
        (Join-Path $repoRoot 'WebApp\appsettings.json')
    )
    foreach ($path in $candidates) {
        if (-not (Test-Path -LiteralPath $path)) { continue }
        try {
            $json = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
            $up = $json.Storage.UploadsPath
            if ($up) {
                if ([IO.Path]::IsPathRooted($up)) { return $up }
                return (Join-Path (Join-Path $repoRoot 'WebApp') $up)
            }
        } catch { }
    }
    return (Join-Path $repoRoot 'WebApp\wwwroot\Uploads')
}

if (-not $ConnectionString) {
    if ($Server -and $User) {
        $ConnectionString = "Data Source=$Server; Initial Catalog=$Database; User Id=$User; Password=$Password; TrustServerCertificate=True"
    } else {
        $ConnectionString = Read-AppSettingsConnection
        if (-not $ConnectionString) {
            $ConnectionString = 'Data Source=172.20.40.42; Initial Catalog=HavayarApp; User Id=sa; Password=Sql123456$$; TrustServerCertificate=True'
        }
    }
}

if (-not $UploadsRoot) {
    $UploadsRoot = Read-UploadsRoot
}

Write-Host "Connection: $($ConnectionString -replace 'Password=[^;]+','Password=***')"
Write-Host "UploadsRoot: $UploadsRoot"
Write-Host "SourceShare: $SourceShare"

Add-Type -AssemblyName System.Data
$conn = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$conn.Open()

$selectSql = "SELECT wr.Id, wr.HtsId, wr.WorkReportFileAddress, wr.WorkReportFileType FROM Cng.WorkReport wr WHERE wr.FileId IS NULL AND wr.WorkReportFileAddress IS NOT NULL AND LTRIM(RTRIM(wr.WorkReportFileAddress)) <> N'' ORDER BY wr.Id"

$cmd = $conn.CreateCommand()
$cmd.CommandText = $selectSql
$cmd.CommandTimeout = 120
$reader = $cmd.ExecuteReader()
$rows = New-Object System.Collections.Generic.List[object]
while ($reader.Read()) {
    $rows.Add([pscustomobject]@{
        Id                     = [long]$reader['Id']
        HtsId                  = [long]$reader['HtsId']
        WorkReportFileAddress  = [string]$reader['WorkReportFileAddress']
        WorkReportFileType     = if ($reader['WorkReportFileType'] -is [DBNull]) { $null } else { [string]$reader['WorkReportFileType'] }
    })
}
$reader.Close()

Write-Host ("Pending FileId rows: {0}" -f $rows.Count)

$copied = 0
$uncFallback = 0
$missing = 0
$errors = 0
$seed = 'sync-cng-workreport-files'
$now = [DateTime]::Now
$nowShamsi = $now.ToString('yyyy/MM/dd')

$relDir = 'cng/workreport'
$absDir = Join-Path $UploadsRoot ($relDir -replace '/', [IO.Path]::DirectorySeparatorChar)
if (-not $WhatIf) {
    New-Item -ItemType Directory -Force -Path $absDir | Out-Null
}

$invalidChars = [IO.Path]::GetInvalidFileNameChars() -join ''
$invalidPattern = "[{0}]" -f [regex]::Escape($invalidChars)

foreach ($row in $rows) {
    $src = $row.WorkReportFileAddress
    if (-not (Test-Path -LiteralPath $src)) {
        $leaf = Split-Path $src -Leaf
        $alt = Join-Path $SourceShare $leaf
        if (Test-Path -LiteralPath $alt) { $src = $alt }
    }

    $safeName = Split-Path $src -Leaf
    if ([string]::IsNullOrWhiteSpace($safeName)) { $safeName = 'file.bin' }
    if ($row.WorkReportFileType -and -not [IO.Path]::HasExtension($safeName)) {
        $safeName = $safeName + $row.WorkReportFileType
    }
    $safeName = [regex]::Replace([string]$safeName, $invalidPattern, '_')

    $relPath = "$relDir/$($row.HtsId)_$safeName"
    $destFull = Join-Path $UploadsRoot ($relPath -replace '/', [IO.Path]::DirectorySeparatorChar)
    $physicalPath = $relPath
    $size = [long]0
    $found = Test-Path -LiteralPath $src

    if ($found) {
        try {
            if (-not $WhatIf) {
                Copy-Item -LiteralPath $src -Destination $destFull -Force
                $size = (Get-Item -LiteralPath $destFull).Length
            }
            $copied++
        } catch {
            Write-Warning ("Copy failed HtsId={0}: {1} - storing UNC" -f $row.HtsId, $_.Exception.Message)
            $physicalPath = $row.WorkReportFileAddress
            $uncFallback++
        }
    } else {
        Write-Warning ("Missing file HtsId={0} path={1}" -f $row.HtsId, $row.WorkReportFileAddress)
        $missing++
        continue
    }

    if ($WhatIf) { continue }

    $tx = $conn.BeginTransaction()
    try {
        $ins = $conn.CreateCommand()
        $ins.Transaction = $tx
        $ins.CommandText = @'
INSERT INTO dbo.FileEntity
    (PhysicalPath, OriginalName, ContentType, Size, EntityType, EntityPropName, EntityId,
     CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime,
     ModifiedById, ModifiedByName, ModifiedDateMiladiDateTime, ModifiedDateShamsiDateTime, IsActive)
OUTPUT INSERTED.Id
VALUES
    (@PhysicalPath, @OriginalName, @ContentType, @Size,
     N'Entities.App.Cng.WorkReport', N'File', @EntityId,
     1, @Seed, @Now, @NowShamsi, 1, @Seed, @Now, @NowShamsi, 1);
'@
        [void]$ins.Parameters.AddWithValue('@PhysicalPath', $physicalPath)
        [void]$ins.Parameters.AddWithValue('@OriginalName', $safeName)
        [void]$ins.Parameters.AddWithValue('@ContentType', (Get-ContentType $safeName))
        [void]$ins.Parameters.AddWithValue('@Size', $size)
        [void]$ins.Parameters.AddWithValue('@EntityId', $row.Id)
        [void]$ins.Parameters.AddWithValue('@Seed', $seed)
        [void]$ins.Parameters.AddWithValue('@Now', $now)
        [void]$ins.Parameters.AddWithValue('@NowShamsi', $nowShamsi)
        $fileId = [long]$ins.ExecuteScalar()

        $upd = $conn.CreateCommand()
        $upd.Transaction = $tx
        $upd.CommandText = 'UPDATE Cng.WorkReport SET FileId = @FileId WHERE Id = @Id AND FileId IS NULL'
        [void]$upd.Parameters.AddWithValue('@FileId', $fileId)
        [void]$upd.Parameters.AddWithValue('@Id', $row.Id)
        [void]$upd.ExecuteNonQuery()

        $tx.Commit()
    } catch {
        $tx.Rollback()
        $errors++
        Write-Warning ("DB write failed Id={0}: {1}" -f $row.Id, $_.Exception.Message)
    }
}

$conn.Close()

Write-Host ''
Write-Host '=== Sync_CngWorkReport_CopyFiles summary ==='
Write-Host ("  Copied locally : {0}" -f $copied)
Write-Host ("  UNC fallback   : {0}" -f $uncFallback)
Write-Host ("  Missing files  : {0}" -f $missing)
Write-Host ("  DB errors      : {0}" -f $errors)
Write-Host ("  Pending input  : {0}" -f $rows.Count)
