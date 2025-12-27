# BackupConfigurator - Build Instructions

## Quick Start (Recommended)

### Build Portable Executable (Single .exe)

Simply run:
```powershell
.\publish-portable.ps1
```

This will create a single executable at `.\publish\portable\BackupConfigurator.UI.exe`

**Characteristics:**
- ? Single .exe file (~70-90 MB)
- ? Includes .NET 9 runtime
- ? Works on any Windows machine
- ? No installation required
- ? Just copy and run!

---

## Advanced Options

### Interactive Build Menu

Run the advanced build script:
```powershell
.\build.ps1
```

This will show you a menu with options:

1. **Portable** - Single .exe with everything (~70-90 MB)
2. **Framework-dependent** - Small executable, requires .NET 9 (~500 KB)
3. **Trimmed** - Optimized size (~40-50 MB)
4. **Build all** - Creates all three versions

### Command-Line Build

You can also specify the build type directly:
```powershell
# Portable (recommended)
.\build.ps1 portable

# Framework-dependent
.\build.ps1 framework-dependent

# Trimmed (experimental)
.\build.ps1 trimmed

# Build all versions
.\build.ps1 all
```

---

## Manual Build Commands

If you prefer to run dotnet commands directly:

### Portable Single File
```powershell
dotnet publish BackupConfigurator.UI\BackupConfigurator.UI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o .\publish\portable
```

### Framework Dependent
```powershell
dotnet publish BackupConfigurator.UI\BackupConfigurator.UI.csproj `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o .\publish\framework-dependent
```

---

## Distribution

### For End Users (Recommended)
1. Run `.\publish-portable.ps1`
2. Copy `.\publish\portable\BackupConfigurator.UI.exe` to target machine
3. Run the .exe (no installation needed)

### For Machines with .NET 9
1. Run `.\build.ps1 framework-dependent`
2. Copy entire `.\publish\framework-dependent\` folder
3. Target machine must have .NET 9 Desktop Runtime installed

---

## File Size Comparison

| Build Type           | Size      | .NET Required | Best For                    |
|---------------------|-----------|---------------|----------------------------|
| Portable            | ~70-90 MB | No            | **Most users (recommended)** |
| Framework-dependent | ~500 KB   | Yes (.NET 9)  | Servers with .NET installed |
| Trimmed             | ~40-50 MB | No            | Advanced users             |

---

## Requirements

### Build Machine
- .NET 9 SDK installed
- Windows PowerShell 5.1+ or PowerShell 7+

### Target Machine (Portable build)
- Windows 10/11 or Windows Server 2016+
- x64 architecture
- No other requirements!

### Target Machine (Framework-dependent)
- Windows 10/11 or Windows Server 2016+
- .NET 9 Desktop Runtime installed

---

## Troubleshooting

### "Cannot run scripts" error
Run PowerShell as Administrator and execute:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Build fails
Make sure you're in the solution root directory (where the .sln file is located)

### Large file size
This is normal for self-contained builds. The .exe includes the entire .NET runtime.
Use the trimmed option to reduce size, or framework-dependent if target has .NET 9.

---

## Notes

- **Portable builds** are recommended for most use cases
- The executable is **digitally signed** if you have a code signing certificate
- Log files and configuration are stored in the user's local application data folder
- The executable requires administrator privileges to create SQL Server Agent jobs

---

## Next Steps

After building, you can:
1. Test the executable on a clean Windows VM
2. Create a ZIP file for distribution
3. Upload to your deployment system
4. Consider code signing for production use
