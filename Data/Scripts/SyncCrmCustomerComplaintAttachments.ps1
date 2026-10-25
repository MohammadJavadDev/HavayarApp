# پیوست‌های شکایت را از TotalSystem روی دیسک می‌نویسد و به FileEntity وصل می‌کند.
# اتصال و مسیر آپلود از WebApp\appsettings.Development.json خوانده می‌شود.
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Test-Path (Join-Path $root 'WebApp'))) { $root = Split-Path $PSScriptRoot -Parent | Split-Path -Parent }
$web = Join-Path (Split-Path $PSScriptRoot -Parent | Split-Path -Parent) 'WebApp'
$settingsPath = Join-Path $web 'appsettings.Development.json'
$text = Get-Content $settingsPath -Raw
$db = [regex]::Match($text, '(?m)^\s*"Db":\s*"(?<c>[^"]+)"').Groups['c'].Value
if (-not $db) { throw 'Connection string Db not found' }
$uploadsRel = [regex]::Match($text, '"UploadsPath":\s*"(?<p>[^"]+)"').Groups['p'].Value
if (-not $uploadsRel) { $uploadsRel = 'wwwroot\Uploads' }
$uploads = if ([IO.Path]::IsPathRooted($uploadsRel)) { $uploadsRel } else { Join-Path $web $uploadsRel }

Add-Type -AssemblyName System.Data
$con = New-Object System.Data.SqlClient.SqlConnection $db
$con.Open()

$cmd = $con.CreateCommand()
$cmd.CommandTimeout = 0
$cmd.CommandText = @"
SELECT a.Id AS HtsId, a.CustomerComplaintId, a.Attachment_FileName, a.Attachment_FileContent, a.CreatedDate, a.CreatedDateInText, a.Comment, a.CreatedUserId
FROM [TMS].[TotalSystem].[dbo].[Crm_Customer_ComplaintAttachment] a
WHERE a.Attachment_FileContent IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Crm.CustomerComplaintAttachment x WHERE x.HtsId = a.Id AND x.FileId IS NOT NULL)
"@
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter $cmd
$table = New-Object System.Data.DataTable
[void]$adapter.Fill($table)
Write-Host ("attachments to copy: " + $table.Rows.Count)

$copied = 0
foreach ($row in $table.Rows) {
    $htsId = [int64]$row.HtsId
    $name = [string]$row.Attachment_FileName
    if ([string]::IsNullOrWhiteSpace($name)) { $name = "$htsId.bin" }
    $safe = [IO.Path]::GetFileName($name)
    $dir = Join-Path $uploads ("crm-complaint\" + $htsId)
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
    $full = Join-Path $dir $safe
    [IO.File]::WriteAllBytes($full, [byte[]]$row.Attachment_FileContent)
    $relative = ("crm-complaint/" + $htsId + "/" + $safe)
    $ext = [IO.Path]::GetExtension($safe).ToLowerInvariant()
    $contentType = switch ($ext) {
        '.pdf' { 'application/pdf' }
        '.png' { 'image/png' }
        '.jpg' { 'image/jpeg' }
        '.jpeg' { 'image/jpeg' }
        '.zip' { 'application/zip' }
        '.rar' { 'application/vnd.rar' }
        default { 'application/octet-stream' }
    }

    $lookup = $con.CreateCommand()
    $lookup.CommandText = "SELECT Id FROM Crm.CustomerComplaint WHERE HtsId = @h AND HtsId <> 0"
    [void]$lookup.Parameters.AddWithValue('@h', $htsId)
    # complaint hts id is the parent complaint id, not the attachment id
    $lookup.Parameters.Clear()
    [void]$lookup.Parameters.AddWithValue('@h', [int64]$row.CustomerComplaintId)
    $complaintId = $lookup.ExecuteScalar()
    if ($null -eq $complaintId) { Write-Host "skip attachment $htsId; complaint missing"; continue }

    $ins = $con.CreateCommand()
    $ins.CommandText = @"
DECLARE @AttId BIGINT = (SELECT Id FROM Crm.CustomerComplaintAttachment WHERE HtsId = @HtsId);
IF @AttId IS NULL
BEGIN
    INSERT INTO Crm.CustomerComplaintAttachment (HtsId, ComplaintId, Comment, IsActive, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
    VALUES (@HtsId, @ComplaintId, @Comment, 1, 1, N'sync-crm-complaint', @Created, @CreatedText);
    SET @AttId = SCOPE_IDENTITY();
END
DECLARE @FileId BIGINT;
INSERT INTO dbo.FileEntity (PhysicalPath, OriginalName, ContentType, Size, EntityType, EntityPropName, EntityId, IsActive, CreatedById, CreatedByName, CreatedOnMiladiDateTime, CreatedOnShamsiDateTime)
VALUES (@Path, @Name, @Type, @Size, N'Entities.App.Crm.CustomerComplaintAttachment', N'File', @AttId, 1, 1, N'sync-crm-complaint', @Created, @CreatedText);
SET @FileId = SCOPE_IDENTITY();
UPDATE Crm.CustomerComplaintAttachment SET FileId = @FileId WHERE Id = @AttId;
"@
    [void]$ins.Parameters.AddWithValue('@HtsId', $htsId)
    [void]$ins.Parameters.AddWithValue('@ComplaintId', [int64]$complaintId)
    $comment = $row.Comment
    if ($comment -is [DBNull]) { $comment = [DBNull]::Value }
    [void]$ins.Parameters.AddWithValue('@Comment', $comment)
    [void]$ins.Parameters.AddWithValue('@Created', $row.CreatedDate)
    [void]$ins.Parameters.AddWithValue('@CreatedText', [string]$row.CreatedDateInText)
    [void]$ins.Parameters.AddWithValue('@Path', $relative)
    [void]$ins.Parameters.AddWithValue('@Name', $safe)
    [void]$ins.Parameters.AddWithValue('@Type', $contentType)
    [void]$ins.Parameters.AddWithValue('@Size', [int64]([byte[]]$row.Attachment_FileContent).Length)
    [void]$ins.ExecuteNonQuery()
    $copied++
}
$con.Close()
Write-Host "copied $copied"
