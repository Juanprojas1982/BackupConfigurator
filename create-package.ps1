# Create distribution package
# Builds portable executable and creates a ZIP file ready for distribution

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "BackupConfigurator - Create Package" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Build portable executable
Write-Host "Step 1: Building portable executable..." -ForegroundColor Yellow
& .\publish-portable.ps1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed. Aborting package creation." -ForegroundColor Red
    exit 1
}

# Step 2: Create distribution folder
Write-Host ""
Write-Host "Step 2: Creating distribution package..." -ForegroundColor Yellow

$distFolder = ".\publish\distribution"
if (Test-Path $distFolder) {
    Remove-Item $distFolder -Recurse -Force
}
New-Item -ItemType Directory -Path $distFolder -Force | Out-Null

# Copy executable
Copy-Item ".\publish\portable\BackupConfigurator.UI.exe" -Destination $distFolder

# Create README for end users
$readmeContent = @"
# BackupConfigurator

## Installation

No installation required!

## How to Use

1. Double-click **BackupConfigurator.UI.exe**
2. Fill in your SQL Server and Azure configuration
3. Click "Install/Configure" to set up backup jobs

## System Requirements

- Windows 10/11 or Windows Server 2016 or later
- Administrator privileges (required for SQL Server Agent job creation)
- Network access to SQL Server and Azure Storage

## Notes

- This is a portable application - no .NET installation required
- Configuration files are saved in: %LOCALAPPDATA%\BackupConfigurator
- Log files are saved in the configured backup folder

## Support

For issues or questions, please contact your system administrator.

Version: 1.0
Built with .NET 9
"@

$readmeContent | Out-File -FilePath "$distFolder\README.txt" -Encoding UTF8

# Step 3: Create ZIP file
Write-Host ""
Write-Host "Step 3: Creating ZIP archive..." -ForegroundColor Yellow

$version = "1.0.0"
$timestamp = Get-Date -Format "yyyyMMdd"
$zipFileName = "BackupConfigurator-v$version-$timestamp.zip"
$zipPath = ".\publish\$zipFileName"

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

Compress-Archive -Path "$distFolder\*" -DestinationPath $zipPath -CompressionLevel Optimal

# Step 4: Show results
Write-Host ""
Write-Host "=====================================" -ForegroundColor Green
Write-Host "PACKAGE CREATED SUCCESSFULLY!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green
Write-Host ""

if (Test-Path $zipPath) {
    $zipSize = (Get-Item $zipPath).Length / 1MB
    Write-Host "Distribution package:" -ForegroundColor Cyan
    Write-Host "  $zipPath" -ForegroundColor White
    Write-Host ""
    Write-Host "Package size: $([math]::Round($zipSize, 2)) MB" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Contents:" -ForegroundColor Cyan
    Write-Host "  - BackupConfigurator.UI.exe (portable executable)" -ForegroundColor White
    Write-Host "  - README.txt (user instructions)" -ForegroundColor White
    Write-Host ""
    Write-Host "This ZIP file is ready to distribute!" -ForegroundColor Green
    Write-Host ""
    
    # Open folder
    explorer.exe (Resolve-Path ".\publish")
}
