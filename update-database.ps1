# Script helper برای اعمال Migrations به دیتابیس
# استفاده: .\update-database.ps1

param(
    [string]$Migration = ""  # خالی = آخرین migration
)

Write-Host "🔧 Updating database..." -ForegroundColor Cyan

# اجرا از پوشه Data
Push-Location Data

try {
    if ([string]::IsNullOrEmpty($Migration)) {
        Write-Host "📦 Applying all pending migrations..." -ForegroundColor Gray
        dotnet ef database update --startup-project ..\WebApp --verbose
    }
    else {
        Write-Host "📦 Applying migration: $Migration" -ForegroundColor Gray
        dotnet ef database update $Migration --startup-project ..\WebApp --verbose
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✅ Database updated successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "💡 Verify in SQL Server Management Studio or Azure Data Studio" -ForegroundColor Yellow
    }
    else {
        Write-Host ""
        Write-Host "❌ Database update failed!" -ForegroundColor Red
    }
}
finally {
    Pop-Location
}

