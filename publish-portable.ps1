# BackupConfigurator - Build Portable Single-File Executable
# This script creates a self-contained single .exe file that includes .NET 9 runtime

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "BackupConfigurator - Portable Build" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
if (Test-Path ".\publish\portable") {
    Remove-Item ".\publish\portable" -Recurse -Force
}

# Build the portable executable
Write-Host "Building portable executable (this may take a few minutes)..." -ForegroundColor Yellow
dotnet publish BackupConfigurator.UI\BackupConfigurator.UI.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    --output .\publish\portable

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "BUILD SUCCESSFUL!" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    
    $exePath = ".\publish\portable\BackupConfigurator.UI.exe"
    if (Test-Path $exePath) {
        $fileSize = (Get-Item $exePath).Length / 1MB
        Write-Host "Executable created at:" -ForegroundColor Cyan
        Write-Host "  $exePath" -ForegroundColor White
        Write-Host ""
        Write-Host "File size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "You can now copy this single .exe file to any Windows machine" -ForegroundColor Green
        Write-Host "No .NET installation required on target machine!" -ForegroundColor Green
        Write-Host ""
        
        # Open the folder
        Write-Host "Opening output folder..." -ForegroundColor Yellow
        explorer.exe (Resolve-Path ".\publish\portable")
    }
} else {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Red
    Write-Host "BUILD FAILED!" -ForegroundColor Red
    Write-Host "=====================================" -ForegroundColor Red
    Write-Host "Please check the error messages above." -ForegroundColor Red
}
