#Requires -Version 5.1
<#
.SYNOPSIS
  Publish App.BackgroundJob exactly like FolderProfile.pubxml, then sync only changed files to the server.

.DESCRIPTION
  1) dotnet publish with PublishProfile=FolderProfile (no minify / obfuscate / recompress)
  2) Compare local publish output with server by MD5 (not timestamp)
  3) Copy only new/changed files to \\172.20.40.42\D$\Application\BackgroundJob
  4) Optionally recycle IIS AppPool

.EXAMPLE
  .\DeployeBackgroundJob.ps1
  .\DeployeBackgroundJob.ps1 -SkipAppPool
  .\DeployeBackgroundJob.ps1 -WhatIf
#>

[CmdletBinding()]
param(
    [switch]$SkipAppPool,
    [switch]$WhatIf,
    [switch]$SkipPublish
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------
# Configuration
# ---------------------------------------------------------
$remoteServer = "172.20.40.42"
$remoteUsername = "mirlohi.m"
$remotePassword = "Mj@09876"   # بهتر است بعداً از Windows Credential Manager بخوانید
$psExecPath = "C:\Tools\PsExec.exe"
$logRoot = "D:\Temp\DeployLogs"

$project = @{
    Name           = "App.BackgroundJob"
    ProjectFile    = "D:\Projects\Havayar\HavayarApp\App.BackgroundJob\App.BackgroundJob.csproj"
    PublishProfile = "FolderProfile"
    # باید با PublishUrl داخل FolderProfile.pubxml یکی باشد
    PublishFolder  = "D:\Projects\Havayar\HavayarAppPublishBackgroundJob"
    RemoteShare    = "\\172.20.40.42\D$"
    # مسیر نهایی روی سرور از طریق UNC
    RemoteUncPath  = "\\172.20.40.42\D$\Application\\HavayarAppPublishBackgroundJob"
    AppPool        = "Backgroundjob"
}

# فقط فایل‌هایی که در N روز اخیر در خروجی publish تغییر کرده‌اند مقایسه/کپی می‌شوند
$compareMaxAgeDays = 7

# پوشه‌هایی که روی سرور دست نزن
$excludeDirNames = @(
    "logs",
    "App_Data"
)

# پسوندهایی که معمولاً لازم نیست روی سرور بروند
$excludeExtensions = @(".pdb", ".xml")  # docs XML؛ در صورت نیاز xml را از لیست بردارید

# ---------------------------------------------------------
# Helpers
# ---------------------------------------------------------
function Write-Step([string]$Message, [string]$Color = "Cyan") {
    Write-Host "`n==> $Message" -ForegroundColor $Color
}

function Write-Info([string]$Message) {
    Write-Host "    $Message" -ForegroundColor Gray
}

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Test-ExcludedPath([string]$RelativePath) {
    $norm = $RelativePath.Replace("/", "\").TrimStart("\")
    foreach ($ex in $excludeDirNames) {
        $exNorm = $ex.Replace("/", "\").TrimStart("\")
        if ($norm.Equals($exNorm, [StringComparison]::OrdinalIgnoreCase) -or
            $norm.StartsWith($exNorm + "\", [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    $ext = [System.IO.Path]::GetExtension($norm)
    if ($ext -and ($excludeExtensions -contains $ext.ToLowerInvariant())) {
        return $true
    }
    return $false
}

function Invoke-NativeProcess {
    param(
        [string]$FileName,
        [string]$Arguments,
        [int]$TimeoutMs = 60000
    )

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FileName
    $psi.Arguments = $Arguments
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    $stdout = $p.StandardOutput.ReadToEnd()
    $stderr = $p.StandardError.ReadToEnd()
    if (-not $p.WaitForExit($TimeoutMs)) {
        try { $p.Kill() } catch {}
        return [PSCustomObject]@{ ExitCode = -1; StdOut = $stdout; StdErr = "Timeout: $stderr" }
    }
    return [PSCustomObject]@{
        ExitCode = $p.ExitCode
        StdOut   = $stdout
        StdErr   = $stderr
    }
}

function Invoke-NetUseSafe {
    param([string]$Arguments)
    return Invoke-NativeProcess -FileName "net.exe" -Arguments $Arguments
}

function Connect-RemoteShare {
    param(
        [string]$Share,
        [string]$User,
        [string]$Password
    )

    Write-Step "Connecting to $Share"

    # پاک کردن نشست قبلی (اگر نباشد خطا را نادیده می‌گیریم)
    [void](Invoke-NetUseSafe "use `"$Share`" /delete /y")
    [void](Invoke-NetUseSafe "use * /delete /y")

    $map = Invoke-NetUseSafe "use `"$Share`" `"$Password`" /user:`"$User`""
    if ($map.ExitCode -ne 0) {
        # بعضی محیط‌ها ترتیب آرگومان را این‌طور می‌خواهند
        $map = Invoke-NetUseSafe "use `"$Share`" /user:`"$User`" `"$Password`""
    }

    if ($map.ExitCode -ne 0) {
        throw ("Failed to connect to {0}`nExit={1}`nOut={2}`nErr={3}" -f `
                $Share, $map.ExitCode, $map.StdOut, $map.StdErr)
    }

    if (-not (Test-Path -LiteralPath $Share)) {
        throw "Share connected but path is not accessible: $Share"
    }

    Write-Info "Connected: $Share"
}

function Disconnect-RemoteShare {
    param([string]$Share)
    [void](Invoke-NetUseSafe "use `"$Share`" /delete /y")
}

function Invoke-AppPool {
    param(
        [string]$Action, # stop | start
        [string]$Server,
        [string]$PoolName,
        [string]$PsExec,
        [string]$User,
        [string]$Password
    )

    $appCmd = "C:\Windows\System32\inetsrv\appcmd.exe $Action apppool /apppool.name:$PoolName"

    try {
        if (Test-Path -LiteralPath $PsExec) {
            # PsExec روی stderr پیام Connecting... می‌نویسد؛ نباید با Stop اسکریپت بترکد
            $args = "\\$Server -accepteula -u `"$User`" -p `"$Password`" cmd /c $appCmd"
            $res = Invoke-NativeProcess -FileName $PsExec -Arguments $args -TimeoutMs 90000
            if ($res.ExitCode -eq 0) { return $true }

            # بدون یوزر/پسورد هم یک‌بار امتحان کن (اگر با همان اکانت لاگین هستی)
            $args2 = "\\$Server -accepteula cmd /c $appCmd"
            $res2 = Invoke-NativeProcess -FileName $PsExec -Arguments $args2 -TimeoutMs 90000
            if ($res2.ExitCode -eq 0) { return $true }

            Write-Warning ("AppPool {0} via PsExec failed. Exit={1} Err={2}" -f $Action, $res2.ExitCode, $res2.StdErr.Trim())
            return $false
        }

        $session = New-CimSession -ComputerName $Server -ErrorAction Stop
        $res = Invoke-CimMethod -CimSession $session -ClassName Win32_Process -MethodName Create -Arguments @{
            CommandLine = $appCmd
        }
        Remove-CimSession $session
        return ($res.ReturnValue -eq 0)
    }
    catch {
        Write-Warning "AppPool $Action failed: $_"
        return $false
    }
}

function Publish-WithProfile {
    param(
        [string]$ProjectFile,
        [string]$ProfileName,
        [string]$ExpectedPublishFolder
    )

    Write-Step "dotnet publish (profile: $ProfileName)"
    Write-Info "This uses FolderProfile.pubxml settings only (no minify/obfuscate)."

    if (-not (Test-Path -LiteralPath $ProjectFile)) {
        throw "Project not found: $ProjectFile"
    }

    # دقیقاً مطابق پروفایل Visual Studio:
    # - Configuration از LastUsedBuildConfiguration = Release
    # - PublishUrl از پروفایل خوانده می‌شود
    # - SelfContained=false
    #
    # نکته مهم: pubxml فقط <PublishUrl> دارد که فقط توسط Visual Studio خوانده می‌شود.
    # ابزار خط فرمان dotnet برای مسیر خروجی به <PublishDir> نیاز دارد و بدون آن،
    # خروجی را (بی‌صدا و بدون خطا) در مسیر پیش‌فرض bin\Release\<tfm>\publish می‌گذارد،
    # نه در PublishFolder موردنظر. برای همین صراحتاً PublishDir را هم پاس می‌دهیم.
    # (رجوع شود به: https://github.com/dotnet/sdk/issues/12490)
    $publishArgs = @(
        "publish",
        $ProjectFile,
        "-c", "Release",
        "/p:PublishProfile=$ProfileName",
        "/p:PublishDir=$ExpectedPublishFolder\"
    )

    & dotnet @publishArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed (exit $LASTEXITCODE)"
    }

    if (-not (Test-Path -LiteralPath $ExpectedPublishFolder)) {
        throw "Publish folder not found after publish: $ExpectedPublishFolder`nCheck PublishUrl in FolderProfile.pubxml"
    }

    Write-Info "Publish output: $ExpectedPublishFolder"
}

function Sync-ChangedFilesByHash {
    param(
        [string]$SourceRoot,
        [string]$DestRoot,
        [int]$MaxAgeDays = 7,
        [switch]$WhatIf
    )

    $cutoff = (Get-Date).AddDays( - [Math]::Abs($MaxAgeDays))

    Write-Step "Comparing recent files with server (last $MaxAgeDays days, MD5)"
    Write-Info "Source : $SourceRoot"
    Write-Info "Dest   : $DestRoot"
    Write-Info "Cutoff : $cutoff (older files are skipped)"

    if (-not (Test-Path -LiteralPath $DestRoot)) {
        if ($WhatIf) {
            Write-Info "[WhatIf] Would create destination: $DestRoot"
        }
        else {
            Ensure-Directory $DestRoot
        }
    }

    $sourceRootFull = (Resolve-Path -LiteralPath $SourceRoot).Path.TrimEnd("\")
    $allFiles = Get-ChildItem -LiteralPath $sourceRootFull -Recurse -File -Force
    $files = $allFiles | Where-Object { $_.LastWriteTime -ge $cutoff }

    $added = New-Object System.Collections.Generic.List[string]
    $modified = New-Object System.Collections.Generic.List[string]
    $skipped = 0
    $excluded = 0
    $ageSkipped = $allFiles.Count - $files.Count
    $copiedBytes = [int64]0

    Write-Info ("Candidates: {0} of {1} files (age-skipped: {2})" -f $files.Count, $allFiles.Count, $ageSkipped)

    foreach ($file in $files) {
        $relative = $file.FullName.Substring($sourceRootFull.Length).TrimStart("\")

        if (Test-ExcludedPath $relative) {
            $excluded++
            continue
        }

        $destPath = Join-Path $DestRoot $relative
        $needCopy = $false
        $changeType = "MOD"

        if (-not (Test-Path -LiteralPath $destPath)) {
            $needCopy = $true
            $changeType = "NEW"
        }
        else {
            $destItem = Get-Item -LiteralPath $destPath -Force
            if ($file.Length -ne $destItem.Length) {
                $needCopy = $true
            }
            else {
                $srcHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm MD5).Hash
                $dstHash = (Get-FileHash -LiteralPath $destPath -Algorithm MD5).Hash
                if ($srcHash -ne $dstHash) {
                    $needCopy = $true
                }
            }
        }

        if (-not $needCopy) {
            $skipped++
            continue
        }

        if ($changeType -eq "NEW") { $added.Add($relative) } else { $modified.Add($relative) }

        if ($WhatIf) {
            Write-Host "    [WhatIf] $changeType  $relative" -ForegroundColor Yellow
            continue
        }

        $destDir = Split-Path -Parent $destPath
        Ensure-Directory $destDir
        Copy-Item -LiteralPath $file.FullName -Destination $destPath -Force
        $copiedBytes += $file.Length
        Write-Host "    $changeType  $relative" -ForegroundColor DarkGreen
    }

    [PSCustomObject]@{
        Added       = $added
        Modified    = $modified
        Skipped     = $skipped
        Excluded    = $excluded
        AgeSkipped  = $ageSkipped
        CopiedBytes = $copiedBytes
    }
}

function Write-HtmlReport {
    param(
        $Result,
        [string]$LogRoot,
        [string]$ProjectName
    )

    Ensure-Directory $LogRoot
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $htmlFile = Join-Path $LogRoot "DeployReport_$ProjectName`_$stamp.html"
    $reportTime = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $mb = [math]::Round($Result.CopiedBytes / 1MB, 2)

    $rows = ""
    foreach ($f in $Result.Added) {
        $rows += "<li class='file-item'><span class='tag tag-new'>NEW</span> $([System.Net.WebUtility]::HtmlEncode($f))</li>"
    }
    foreach ($f in $Result.Modified) {
        $rows += "<li class='file-item'><span class='tag tag-mod'>MOD</span> $([System.Net.WebUtility]::HtmlEncode($f))</li>"
    }
    if (-not $rows) {
        $rows = "<li class='empty'>هیچ فایلی برای انتقال لازم نبود (همه یکسان بودند).</li>"
    }

    @"
<!DOCTYPE html>
<html lang="fa" dir="rtl">
<head>
<meta charset="UTF-8" />
<title>Deploy Report</title>
<style>
body{font-family:'Segoe UI',Tahoma;background:#f0f2f5;padding:20px}
.card{background:#fff;border-radius:8px;box-shadow:0 2px 10px rgba(0,0,0,.08);overflow:hidden}
.card-header{background:#1a73e8;color:#fff;padding:14px 16px;font-weight:700}
.stats{display:flex;background:#e8f0fe;padding:10px}
.stat{flex:1;text-align:center}
.file-list{list-style:none;margin:0;padding:10px;max-height:420px;overflow:auto}
.file-item{padding:6px 8px;border-bottom:1px solid #eee;font-family:Consolas;font-size:12px;direction:ltr;text-align:left}
.tag{display:inline-block;padding:2px 6px;border-radius:4px;font-size:11px;margin-right:8px;color:#fff}
.tag-new{background:#28a745}.tag-mod{background:#ffc107;color:#000}
.empty{color:#888;text-align:center;padding:12px}
</style>
</head>
<body>
  <div class="card">
    <div class="card-header">گزارش استقرار — $ProjectName</div>
    <div class="stats">
      <div class="stat">جدید: $($Result.Added.Count)</div>
      <div class="stat">تغییر: $($Result.Modified.Count)</div>
      <div class="stat">بدون تغییر: $($Result.Skipped)</div>
      <div class="stat">حجم کپی: $mb MB</div>
    </div>
    <ul class="file-list">$rows</ul>
    <div style="padding:10px;color:#666;text-align:center">زمان: $reportTime — بدون minify/obfuscate — مقایسه با MD5</div>
  </div>
</body>
</html>
"@ | Out-File -FilePath $htmlFile -Encoding UTF8

    return $htmlFile
}

# ---------------------------------------------------------
# Main
# ---------------------------------------------------------
$sw = [System.Diagnostics.Stopwatch]::StartNew()
Ensure-Directory $logRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Havayar BackgroundJob Deploy (FolderProfile)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Info "Profile : $($project.PublishProfile)"
Write-Info "Publish : $($project.PublishFolder)"
Write-Info "Server  : $($project.RemoteUncPath)"
if ($WhatIf) { Write-Host "MODE: WhatIf (no files will be copied)" -ForegroundColor Yellow }

try {
    if (-not $SkipPublish) {
        Publish-WithProfile `
            -ProjectFile $project.ProjectFile `
            -ProfileName $project.PublishProfile `
            -ExpectedPublishFolder $project.PublishFolder
    }
    else {
        Write-Step "Skipping publish (-SkipPublish)"
        if (-not (Test-Path -LiteralPath $project.PublishFolder)) {
            throw "Publish folder missing: $($project.PublishFolder)"
        }
    }

    Connect-RemoteShare -Share $project.RemoteShare -User $remoteUsername -Password $remotePassword

    $remoteRoot = $project.RemoteUncPath
    Write-Info "Remote root: $remoteRoot"

    if (-not $SkipAppPool -and -not $WhatIf) {
        Write-Step "Stopping AppPool '$($project.AppPool)'" "Yellow"
        try {
            if (Invoke-AppPool -Action stop -Server $remoteServer -PoolName $project.AppPool -PsExec $psExecPath -User $remoteUsername -Password $remotePassword) {
                Start-Sleep -Seconds 2
                Write-Info "AppPool stopped"
            }
            else {
                Write-Warning "Could not stop AppPool (continuing anyway)"
            }
        }
        catch {
            Write-Warning "AppPool stop error ignored: $_"
        }
    }

    $syncResult = Sync-ChangedFilesByHash `
        -SourceRoot $project.PublishFolder `
        -DestRoot $remoteRoot `
        -MaxAgeDays $compareMaxAgeDays `
        -WhatIf:$WhatIf

    Write-Step "Sync summary" "Green"
    Write-Info ("New         : {0}" -f $syncResult.Added.Count)
    Write-Info ("Modified    : {0}" -f $syncResult.Modified.Count)
    Write-Info ("Unchanged   : {0}" -f $syncResult.Skipped)
    Write-Info ("Age-skipped : {0} (older than {1} days)" -f $syncResult.AgeSkipped, $compareMaxAgeDays)
    Write-Info ("Excluded    : {0}" -f $syncResult.Excluded)
    Write-Info ("Bytes       : {0:N0}" -f $syncResult.CopiedBytes)

    if (-not $SkipAppPool -and -not $WhatIf) {
        Write-Step "Starting AppPool '$($project.AppPool)'" "Green"
        try {
            if (Invoke-AppPool -Action start -Server $remoteServer -PoolName $project.AppPool -PsExec $psExecPath -User $remoteUsername -Password $remotePassword) {
                Write-Info "AppPool started"
            }
            else {
                Write-Warning "Could not start AppPool — start it manually in IIS"
            }
        }
        catch {
            Write-Warning "AppPool start error ignored: $_"
        }
    }

    $report = Write-HtmlReport -Result $syncResult -LogRoot $logRoot -ProjectName $project.Name
    $sw.Stop()
    Write-Host "`nDONE in $([math]::Round($sw.Elapsed.TotalSeconds,1))s" -ForegroundColor Cyan
    Write-Host "Report: $report" -ForegroundColor Cyan
    if (-not $WhatIf) {
        Start-Process $report
    }
}
catch {
    Write-Host "`nDEPLOY FAILED: $_" -ForegroundColor Red
    exit 1
}
finally {
    Disconnect-RemoteShare -Share $project.RemoteShare
}
