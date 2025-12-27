# BackupConfigurator - Advanced Build Script
# Provides multiple build options

param(
    [Parameter(Position=0)]
    [ValidateSet('portable', 'framework-dependent', 'trimmed', 'all')]
    [string]$BuildType = 'portable'
)

function Show-Menu {
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host "BackupConfigurator - Build Options" -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "1. Portable (Single .exe with runtime) - Recommended" -ForegroundColor Green
    Write-Host "   Size: ~70-90 MB | No .NET required on target" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Framework-dependent (Requires .NET 9)" -ForegroundColor Yellow
    Write-Host "   Size: ~500 KB | .NET 9 must be installed" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Trimmed (Optimized size)" -ForegroundColor Magenta
    Write-Host "   Size: ~40-50 MB | Advanced option" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Build all versions" -ForegroundColor Cyan
    Write-Host ""
    
    $choice = Read-Host "Select option (1-4)"
    
    switch ($choice) {
        "1" { return "portable" }
        "2" { return "framework-dependent" }
        "3" { return "trimmed" }
        "4" { return "all" }
        default { 
            Write-Host "Invalid choice. Using portable." -ForegroundColor Red
            return "portable" 
        }
    }
}

function Build-Portable {
    Write-Host ""
    Write-Host "Building PORTABLE executable..." -ForegroundColor Cyan
    Write-Host "This creates a single .exe with everything included" -ForegroundColor Gray
    
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
        Show-BuildSuccess ".\publish\portable\BackupConfigurator.UI.exe"
    }
}

function Build-FrameworkDependent {
    Write-Host ""
    Write-Host "Building FRAMEWORK-DEPENDENT executable..." -ForegroundColor Cyan
    Write-Host "Target machine must have .NET 9 installed" -ForegroundColor Gray
    
    dotnet publish BackupConfigurator.UI\BackupConfigurator.UI.csproj `
        --configuration Release `
        --runtime win-x64 `
        --self-contained false `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        --output .\publish\framework-dependent
    
    if ($LASTEXITCODE -eq 0) {
        Show-BuildSuccess ".\publish\framework-dependent\BackupConfigurator.UI.exe"
    }
}

function Build-Trimmed {
    Write-Host ""
    Write-Host "Building TRIMMED executable..." -ForegroundColor Cyan
    Write-Host "Optimized size with IL trimming (advanced)" -ForegroundColor Gray
    
    dotnet publish BackupConfigurator.UI\BackupConfigurator.UI.csproj `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:PublishTrimmed=true `
        -p:TrimMode=partial `
        -p:EnableCompressionInSingleFile=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        --output .\publish\trimmed
    
    if ($LASTEXITCODE -eq 0) {
        Show-BuildSuccess ".\publish\trimmed\BackupConfigurator.UI.exe"
    }
}

function Show-BuildSuccess {
    param([string]$exePath)
    
    if (Test-Path $exePath) {
        $fileSize = (Get-Item $exePath).Length / 1MB
        Write-Host ""
        Write-Host "? SUCCESS!" -ForegroundColor Green
        Write-Host "  Location: $exePath" -ForegroundColor White
        Write-Host "  Size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Cyan
    }
}

# Main execution
Clear-Host

if ($BuildType -eq '') {
    $BuildType = Show-Menu
}

Write-Host ""
Write-Host "Starting build process..." -ForegroundColor Yellow
Write-Host ""

# Clean old builds
if (Test-Path ".\publish") {
    Write-Host "Cleaning old builds..." -ForegroundColor Gray
    Remove-Item ".\publish" -Recurse -Force -ErrorAction SilentlyContinue
}

# Execute builds
switch ($BuildType) {
    'portable' { Build-Portable }
    'framework-dependent' { Build-FrameworkDependent }
    'trimmed' { Build-Trimmed }
    'all' { 
        Build-Portable
        Build-FrameworkDependent
        Build-Trimmed
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Build process completed!" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Open the publish folder
if (Test-Path ".\publish") {
    explorer.exe (Resolve-Path ".\publish")
}
