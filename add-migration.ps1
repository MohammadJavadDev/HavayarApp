# Script helper برای اجرای EF Migrations
# استفاده: .\add-migration.ps1 -Name "AddNotificationBuilder"

param(
    [Parameter(Mandatory=$true)]
    [string]$Name
)

Write-Host "🔨 Adding migration: $Name" -ForegroundColor Cyan

# اجرا از پوشه Data با startup project WebApp
Push-Location Data

try {
    Write-Host "📦 Building Data project..." -ForegroundColor Gray
    dotnet build --nologo -v quiet
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful" -ForegroundColor Green
        Write-Host ""
        Write-Host "🔧 Creating migration..." -ForegroundColor Cyan
        
        # اجرای migration با startup project صحیح
        dotnet ef migrations add $Name --startup-project ..\WebApp --verbose
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "✅ Migration '$Name' created successfully!" -ForegroundColor Green
            Write-Host ""
            Write-Host "💡 Next steps:" -ForegroundColor Yellow
            Write-Host "   1. Review the migration file in Data/Migrations/" -ForegroundColor Gray
            Write-Host "   2. Run: .\update-database.ps1" -ForegroundColor Gray
        }
        else {
            Write-Host ""
            Write-Host "❌ Migration failed!" -ForegroundColor Red
        }
    }
    else {
        Write-Host "❌ Build failed!" -ForegroundColor Red
    }
}
finally {
    Pop-Location
}

