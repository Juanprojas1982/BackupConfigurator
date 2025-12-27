using BackupConfigurator.Core.Models;
using Serilog;
using System.IO.Compression;

namespace BackupConfigurator.Core.Services;

public class FileSystemManager
{
    private readonly ILogger _logger;
    private const string AzCopyDownloadUrl = "https://aka.ms/downloadazcopy-v10-windows";

    public FileSystemManager(ILogger logger)
    {
        _logger = logger;
    }

    private static string GetScriptsPath(BackupConfiguration config)
    {
        return Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName, "scripts");
    }

    private static string GetLogsPath(BackupConfiguration config)
    {
        return Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName, "logs");
    }

    public async Task<ValidationResult> EnsureAzCopyAvailableAsync(string configuredPath)
    {
        var result = new ValidationResult();
        
        // We'll determine the target path after we get the config, for now check if configured path is valid
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            _logger.Information("AzCopy found at configured path: {AzCopyPath}", configuredPath);
            result.Success = true;
            result.Message = $"AzCopy found: {configuredPath}";
            result.AddDetail($"Using: {configuredPath}");
            return result;
        }

        result.AddDetail("AzCopy not found at configured location, will search and install...");

        // Check common locations to find AzCopy
        var commonPaths = new[]
        {
            @"C:\Program Files\AzCopy\azcopy.exe",
            @"C:\Program Files (x86)\AzCopy\azcopy.exe",
            @"C:\Program Files\Microsoft\AzCopy\azcopy.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "azcopy.exe")
        };

        string? foundPath = null;
        foreach (var path in commonPaths)
        {
            if (File.Exists(path))
            {
                foundPath = path;
                result.AddDetail($"Found AzCopy at: {path}");
                break;
            }
        }

        if (foundPath != null)
        {
            result.Success = true;
            result.Message = $"AzCopy found: {foundPath}";
            result.AddDetail("Will be copied to local bin folder during job installation");
            return result;
        }

        // If not found, try to download
        try
        {
            result.AddDetail("Downloading AzCopy from Microsoft...");
            _logger.Information("Downloading AzCopy from {Url}", AzCopyDownloadUrl);

            var tempZipPath = Path.Combine(Path.GetTempPath(), $"azcopy_{Guid.NewGuid()}.zip");
            var tempExtractPath = Path.Combine(Path.GetTempPath(), $"azcopy_extract_{Guid.NewGuid()}");

            using (var httpClient = new HttpClient())
            {
                httpClient.Timeout = TimeSpan.FromMinutes(5);
                var response = await httpClient.GetAsync(AzCopyDownloadUrl);
                response.EnsureSuccessStatusCode();

                using var fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream);
            }

            result.AddDetail("Downloaded AzCopy package");

            // Extract ZIP
            Directory.CreateDirectory(tempExtractPath);
            ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath);

            // Find azcopy.exe in extracted folder
            var azcopyExe = Directory.GetFiles(tempExtractPath, "azcopy.exe", SearchOption.AllDirectories).FirstOrDefault();

            if (azcopyExe == null)
            {
                File.Delete(tempZipPath);
                Directory.Delete(tempExtractPath, true);
                result.Success = false;
                result.Message = "Failed to find azcopy.exe in downloaded package";
                return result;
            }

            // Copy to a temporary location accessible by current user
            var tempAzCopyPath = Path.Combine(Path.GetTempPath(), "azcopy.exe");
            File.Copy(azcopyExe, tempAzCopyPath, overwrite: true);

            // Cleanup
            File.Delete(tempZipPath);
            Directory.Delete(tempExtractPath, true);

            _logger.Information("AzCopy downloaded to temp location: {Path}", tempAzCopyPath);

            result.Success = true;
            result.Message = "AzCopy downloaded successfully";
            result.AddDetail($"Downloaded to: {tempAzCopyPath}");
            result.AddDetail("Will be copied to local bin folder during job installation");
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to download AzCopy");
            result.Success = false;
            result.Message = $"Failed to download AzCopy: {ex.Message}";
            result.AddDetail("Please download AzCopy manually from:");
            result.AddDetail("https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azcopy-v10");
            return result;
        }
    }

    private ValidationResult InstallAzCopyToLocalBin(BackupConfiguration config)
    {
        try
        {
            var binPath = Path.Combine(config.LocalBasePath, "bin");
            var targetAzCopyPath = Path.Combine(binPath, "azcopy.exe");

            // If already exists at target location, we're done
            if (File.Exists(targetAzCopyPath))
            {
                _logger.Information("AzCopy already installed at: {Path}", targetAzCopyPath);
                return ValidationResult.Ok($"AzCopy ready: {targetAzCopyPath}");
            }

            // Create bin directory
            Directory.CreateDirectory(binPath);

            // Find AzCopy in common locations
            var searchPaths = new[]
            {
                @"C:\Program Files\AzCopy\azcopy.exe",
                @"C:\Program Files (x86)\AzCopy\azcopy.exe",
                @"C:\Program Files\Microsoft\AzCopy\azcopy.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "azcopy.exe"),
                Path.Combine(Path.GetTempPath(), "azcopy.exe")
            };

            string? sourcePath = null;
            foreach (var path in searchPaths)
            {
                if (File.Exists(path))
                {
                    sourcePath = path;
                    break;
                }
            }

            if (sourcePath == null)
            {
                return ValidationResult.Fail("AzCopy not found. Please download it first or run Install/Configure Jobs.");
            }

            // Copy AzCopy to local bin folder
            File.Copy(sourcePath, targetAzCopyPath, overwrite: true);
            _logger.Information("Copied AzCopy from {Source} to {Target}", sourcePath, targetAzCopyPath);

            // Grant permissions to SQL Server Agent for the bin folder
            var icaclsBin = $"\"{binPath}\" /grant \"NT Service\\SQLSERVERAGENT:(OI)(CI)(RX)\" /T";
            RunIcacls(icaclsBin);

            var result = ValidationResult.Ok($"AzCopy installed to: {targetAzCopyPath}");
            result.AddDetail($"Source: {sourcePath}");
            result.AddDetail($"Target: {targetAzCopyPath}");
            result.AddDetail("? Permissions configured for SQL Server Agent");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to install AzCopy to local bin folder");
            return ValidationResult.Fail($"Failed to install AzCopy: {ex.Message}");
        }
    }

    public ValidationResult CreateBackupFolders(BackupConfiguration config)
    {
        try
        {
            _logger.Information("Creating backup folders for database {Database}", config.SanitizedDatabaseName);

            var basePath = Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName);
            var fullPath = Path.Combine(basePath, "FULL");
            var diffPath = Path.Combine(basePath, "DIFF");
            var scriptsPath = Path.Combine(basePath, "scripts");
            var logsPath = Path.Combine(basePath, "logs");
            var binPath = Path.Combine(config.LocalBasePath, "bin");

            Directory.CreateDirectory(fullPath);
            Directory.CreateDirectory(diffPath);
            Directory.CreateDirectory(scriptsPath);
            Directory.CreateDirectory(logsPath);
            Directory.CreateDirectory(binPath);

            _logger.Information("Created backup folders: {FullPath}, {DiffPath}, {ScriptsPath}, {LogsPath}, {BinPath}", 
                fullPath, diffPath, scriptsPath, logsPath, binPath);

            // Install AzCopy to local bin folder
            var azCopyResult = InstallAzCopyToLocalBin(config);
            
            // Grant permissions to SQL Server Agent
            var permissionsResult = GrantSqlAgentPermissions(basePath, Path.Combine(binPath, "azcopy.exe"));
            
            var result = ValidationResult.Ok("Backup folders created successfully");
            result.AddDetail($"FULL: {fullPath}");
            result.AddDetail($"DIFF: {diffPath}");
            result.AddDetail($"scripts: {scriptsPath}");
            result.AddDetail($"logs: {logsPath}");
            result.AddDetail($"bin: {binPath}");
            result.AddDetail("");
            
            if (azCopyResult.Success)
            {
                result.AddDetail("? AzCopy installed to bin folder");
            }
            else
            {
                result.AddDetail($"? AzCopy: {azCopyResult.Message}");
            }
            
            if (permissionsResult.Success)
            {
                result.AddDetail("? SQL Server Agent permissions configured");
            }
            else
            {
                result.AddDetail("? Warning: Could not configure SQL Server Agent permissions automatically");
                result.AddDetail(permissionsResult.Message);
                result.AddDetail("You may need to run the application as Administrator or configure permissions manually");
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create backup folders");
            return ValidationResult.Fail($"Failed to create backup folders: {ex.Message}");
        }
    }

    private ValidationResult GrantSqlAgentPermissions(string basePath, string azCopyPath)
    {
        try
        {
            _logger.Information("Configuring SQL Server Agent permissions");

            // Grant permissions to backup folder
            var icaclsBasePath = $"\"{basePath}\" /grant \"NT Service\\SQLSERVERAGENT:(OI)(CI)(RX)\" /T";
            var basePathResult = RunIcacls(icaclsBasePath);
            
            // Grant permissions to AzCopy if it exists
            if (!string.IsNullOrWhiteSpace(azCopyPath) && File.Exists(azCopyPath))
            {
                var icaclsAzCopy = $"\"{azCopyPath}\" /grant \"NT Service\\SQLSERVERAGENT:(RX)\"";
                var azCopyResult = RunIcacls(icaclsAzCopy);
                
                if (!azCopyResult.Success)
                {
                    _logger.Warning("Could not set AzCopy permissions: {Message}", azCopyResult.Message);
                }
            }

            if (basePathResult.Success)
            {
                _logger.Information("SQL Server Agent permissions configured successfully");
                return ValidationResult.Ok("Permissions configured for SQL Server Agent");
            }
            else
            {
                _logger.Warning("Could not set permissions: {Message}", basePathResult.Message);
                return ValidationResult.Fail($"Could not set permissions: {basePathResult.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error configuring SQL Server Agent permissions");
            return ValidationResult.Fail($"Error: {ex.Message}");
        }
    }

    private ValidationResult RunIcacls(string arguments)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "icacls.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
            {
                return ValidationResult.Fail("Failed to start icacls process");
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return ValidationResult.Ok("Permissions set successfully");
            }
            else
            {
                var message = !string.IsNullOrWhiteSpace(error) ? error : output;
                return ValidationResult.Fail($"icacls failed: {message}");
            }
        }
        catch (Exception ex)
        {
            return ValidationResult.Fail($"Exception: {ex.Message}");
        }
    }

    public ValidationResult CreateUploadScripts(BackupConfiguration config)
    {
        try
        {
            _logger.Information("Creating upload scripts");

            var scriptsPath = GetScriptsPath(config);
            Directory.CreateDirectory(scriptsPath);

            var basePath = Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName);
            var fullLocalPath = Path.Combine(basePath, "FULL");
            var diffLocalPath = Path.Combine(basePath, "DIFF");
            var configJsonPath = Path.Combine(basePath, "config.json");
            
            // AzCopy is now in the bin folder
            var azCopyPath = Path.Combine(config.LocalBasePath, "bin", "azcopy.exe");

            var containerUrl = config.AzureContainerUrl.TrimEnd('/');

            // FULL backup upload script - Load SAS from config.json dynamically
            var fullDestUrl = $"{containerUrl}/{config.SanitizedDatabaseName}/FULL";
            var fullScript = "@echo off\r\n" +
                "setlocal EnableExtensions EnableDelayedExpansion\r\n" +
                "\r\n" +
                "REM ===== Upload FULL backups to Azure Blob Storage =====\r\n" +
                "REM SAS token loaded from config.json to avoid command-line escaping issues\r\n" +
                "\r\n" +
                "REM ===== Paths =====\r\n" +
                $"set AZCOPY=\"{azCopyPath}\"\r\n" +
                $"set CFG=\"{configJsonPath}\"\r\n" +
                $"set SRC=\"{fullLocalPath}\\*.bak\"\r\n" +
                $"set DST_BASE=\"{fullDestUrl}\"\r\n" +
                "\r\n" +
                "REM ===== Load SAS from config.json =====\r\n" +
                "for /f \"usebackq delims=\" %%S in (`powershell -NoProfile -Command \"(Get-Content '%CFG%' -Raw | ConvertFrom-Json).azureSasToken.Trim()\"`) do (\r\n" +
                "  set SAS_TOKEN=%%S\r\n" +
                ")\r\n" +
                "\r\n" +
                "REM Remove leading ? if present\r\n" +
                "if \"!SAS_TOKEN:~0,1!\"==\"?\" set SAS_TOKEN=!SAS_TOKEN:~1!\r\n" +
                "\r\n" +
                "REM ===== Validate SAS token loaded =====\r\n" +
                "if \"!SAS_TOKEN!\"==\"\" (\r\n" +
                "  echo ERROR: SAS TOKEN NOT LOADED FROM CONFIG\r\n" +
                "  exit /b 9\r\n" +
                ")\r\n" +
                "\r\n" +
                "REM ===== Build complete destination URL =====\r\n" +
                "set DST=!DST_BASE!?!SAS_TOKEN!\r\n" +
                "\r\n" +
                "REM ===== Execute AzCopy =====\r\n" +
                "%AZCOPY% copy \"%SRC%\" \"!DST!\" --overwrite=true --recursive=false\r\n" +
                "\r\n" +
                "set EXIT_CODE=%errorlevel%\r\n" +
                "set SAS_TOKEN=\r\n" +
                "set DST=\r\n" +
                "\r\n" +
                "exit /b %EXIT_CODE%\r\n";

            var fullScriptPath = Path.Combine(scriptsPath, "upload_full.cmd");
            File.WriteAllText(fullScriptPath, fullScript);

            // DIFF backup upload script - Load SAS from config.json dynamically
            var diffDestUrl = $"{containerUrl}/{config.SanitizedDatabaseName}/DIFF";
            var diffScript = "@echo off\r\n" +
                "setlocal EnableExtensions EnableDelayedExpansion\r\n" +
                "\r\n" +
                "REM ===== Upload DIFF backups to Azure Blob Storage =====\r\n" +
                "REM SAS token loaded from config.json to avoid command-line escaping issues\r\n" +
                "\r\n" +
                "REM ===== Paths =====\r\n" +
                $"set AZCOPY=\"{azCopyPath}\"\r\n" +
                $"set CFG=\"{configJsonPath}\"\r\n" +
                $"set SRC=\"{diffLocalPath}\\*.dif\"\r\n" +
                $"set DST_BASE=\"{diffDestUrl}\"\r\n" +
                "\r\n" +
                "REM ===== Load SAS from config.json =====\r\n" +
                "for /f \"usebackq delims=\" %%S in (`powershell -NoProfile -Command \"(Get-Content '%CFG%' -Raw | ConvertFrom-Json).azureSasToken.Trim()\"`) do (\r\n" +
                "  set SAS_TOKEN=%%S\r\n" +
                ")\r\n" +
                "\r\n" +
                "REM Remove leading ? if present\r\n" +
                "if \"!SAS_TOKEN:~0,1!\"==\"?\" set SAS_TOKEN=!SAS_TOKEN:~1!\r\n" +
                "\r\n" +
                "REM ===== Validate SAS token loaded =====\r\n" +
                "if \"!SAS_TOKEN!\"==\"\" (\r\n" +
                "  echo ERROR: SAS TOKEN NOT LOADED FROM CONFIG\r\n" +
                "  exit /b 9\r\n" +
                ")\r\n" +
                "\r\n" +
                "REM ===== Build complete destination URL =====\r\n" +
                "set DST=!DST_BASE!?!SAS_TOKEN!\r\n" +
                "\r\n" +
                "REM ===== Execute AzCopy =====\r\n" +
                "%AZCOPY% copy \"%SRC%\" \"!DST!\" --overwrite=true --recursive=false\r\n" +
                "\r\n" +
                "set EXIT_CODE=%errorlevel%\r\n" +
                "set SAS_TOKEN=\r\n" +
                "set DST=\r\n" +
                "\r\n" +
                "exit /b %EXIT_CODE%\r\n";
            var diffScriptPath = Path.Combine(scriptsPath, "upload_diff.cmd");
            File.WriteAllText(diffScriptPath, diffScript);

            _logger.Information("Created upload scripts using AzCopy at: {AzCopyPath}", azCopyPath);

            var result = ValidationResult.Ok("Upload scripts created successfully");
            result.AddDetail($"FULL upload: {fullScriptPath}");
            result.AddDetail($"DIFF upload: {diffScriptPath}");
            result.AddDetail($"Using AzCopy: {azCopyPath}");
            result.AddDetail("? SAS token loaded dynamically from config.json");
            result.AddDetail("? SAS passed via AZCOPY_SAS_TOKEN environment variable");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create upload scripts");
            return ValidationResult.Fail($"Failed to create upload scripts: {ex.Message}");
        }
    }

    public ValidationResult CreateCleanupScript(BackupConfiguration config)
    {
        try
        {
            _logger.Information("Creating cleanup script");

            var scriptsPath = GetScriptsPath(config);
            Directory.CreateDirectory(scriptsPath);

            var basePath = Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName);

            var cleanupScript = $@"@echo off
REM Cleanup old backups older than {config.LocalRetentionDays} days
PowerShell.exe -NoProfile -ExecutionPolicy Bypass -Command ""& {{Get-ChildItem -Path '{basePath}\FULL' -Filter '*.bak' | Where-Object {{$_.LastWriteTime -lt (Get-Date).AddDays(-{config.LocalRetentionDays})}} | Remove-Item -Force; Get-ChildItem -Path '{basePath}\DIFF' -Filter '*.dif' | Where-Object {{$_.LastWriteTime -lt (Get-Date).AddDays(-{config.LocalRetentionDays})}} | Remove-Item -Force}}""
exit /b %ERRORLEVEL%
";

            var cleanupScriptPath = Path.Combine(scriptsPath, "cleanup_local.cmd");
            File.WriteAllText(cleanupScriptPath, cleanupScript);

            _logger.Information("Created cleanup script: {CleanupScript}", cleanupScriptPath);

            var result = ValidationResult.Ok("Cleanup script created successfully");
            result.AddDetail($"Cleanup: {cleanupScriptPath}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create cleanup script");
            return ValidationResult.Fail($"Failed to create cleanup script: {ex.Message}");
        }
    }

    public ValidationResult ValidateAzCopyExists(string azCopyPath)
    {
        if (File.Exists(azCopyPath))
        {
            _logger.Information("AzCopy found at {AzCopyPath}", azCopyPath);
            return ValidationResult.Ok($"AzCopy found: {azCopyPath}");
        }
        else
        {
            _logger.Warning("AzCopy not found at {AzCopyPath}", azCopyPath);
            return ValidationResult.Fail($"AzCopy not found at: {azCopyPath}. Please install AzCopy or specify correct path.");
        }
    }

    public async Task<ValidationResult> DownloadAllFromAzureAsync(BackupConfiguration config)
    {
        var result = new ValidationResult();
        try
        {
            _logger.Information("Starting download of all backups from Azure for database {Database}", config.SanitizedDatabaseName);

            // AzCopy is in the bin folder
            var azCopyPath = Path.Combine(config.LocalBasePath, "bin", "azcopy.exe");
            
            if (!File.Exists(azCopyPath))
            {
                result.Success = false;
                result.Message = "AzCopy not found";
                result.AddDetail($"AzCopy not found at: {azCopyPath}");
                result.AddDetail("Please run Install/Configure Jobs first to install AzCopy");
                return result;
            }

            var basePath = Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName);
            var downloadPath = Path.Combine(basePath, "AzureDownload");
            Directory.CreateDirectory(downloadPath);

            var containerUrl = config.AzureContainerUrl.TrimEnd('/');
            var sasToken = config.NormalizedSasToken;

            result.AddDetail("Downloading all backups for database from Azure...");
            var databaseSourceUrl = $"{containerUrl}/{config.SanitizedDatabaseName}{sasToken}";
            
            var databaseResult = await RunAzCopyDownloadAsync(azCopyPath, databaseSourceUrl, downloadPath);
            
            if (databaseResult.Success)
            {
                result.AddDetail($"? All backups downloaded to: {downloadPath}");
                
                // Count files in all subdirectories (AzCopy preserves Azure folder structure)
                var fullPath = Path.Combine(downloadPath, "FULL");
                var diffPath = Path.Combine(downloadPath, "DIFF");
                
                int fullFiles = 0;
                int diffFiles = 0;
                
                // Check if FULL and DIFF folders exist (AzCopy creates them from Azure structure)
                if (Directory.Exists(fullPath))
                {
                    fullFiles = Directory.GetFiles(fullPath, "*.bak", SearchOption.AllDirectories).Length;
                    _logger.Information("Found {Count} FULL backup files in {Path}", fullFiles, fullPath);
                }
                else
                {
                    _logger.Warning("FULL folder not found at {Path}", fullPath);
                }
                
                if (Directory.Exists(diffPath))
                {
                    diffFiles = Directory.GetFiles(diffPath, "*.dif", SearchOption.AllDirectories).Length;
                    _logger.Information("Found {Count} DIFF backup files in {Path}", diffFiles, diffPath);
                }
                else
                {
                    _logger.Warning("DIFF folder not found at {Path}", diffPath);
                }
                
                // Also count all .bak and .dif files recursively in case structure is different
                var allBakFiles = Directory.GetFiles(downloadPath, "*.bak", SearchOption.AllDirectories).Length;
                var allDifFiles = Directory.GetFiles(downloadPath, "*.dif", SearchOption.AllDirectories).Length;
                
                _logger.Information("Total files found: {BakCount} .bak, {DifCount} .dif in {Path}", allBakFiles, allDifFiles, downloadPath);
                
                // Use the higher count (in case folders exist but are structured differently)
                fullFiles = Math.Max(fullFiles, allBakFiles);
                diffFiles = Math.Max(diffFiles, allDifFiles);
                
                result.Success = true;
                result.Message = $"Downloaded {fullFiles} FULL backup(s) and {diffFiles} DIFF backup(s)";
                result.AddDetail($"Total files: {fullFiles + diffFiles}");
                result.AddDetail($"Download location: {downloadPath}");
                
                if (fullFiles > 0 || diffFiles > 0)
                {
                    result.AddDetail($"  • FULL backups (.bak): {fullFiles}");
                    result.AddDetail($"  • DIFF backups (.dif): {diffFiles}");
                }
                else
                {
                    result.AddDetail("? No backup files found in downloaded content");
                    result.AddDetail($"Check folder: {downloadPath}");
                }
                
                _logger.Information("Download completed: {FullFiles} FULL, {DiffFiles} DIFF", fullFiles, diffFiles);
            }
            else
            {
                result.Success = false;
                result.Message = "Download failed";
                result.AddDetail("No files were downloaded.");
                result.AddDetail("");
                result.AddDetail("Troubleshooting steps:");
                result.AddDetail("  1. Verify that backups have been uploaded to Azure");
                result.AddDetail("  2. Check Azure Portal to confirm folder structure");
                result.AddDetail($"  3. Expected path: {config.SanitizedDatabaseName}/FULL or {config.SanitizedDatabaseName}/DIFF");
                result.AddDetail("  4. Verify SAS token has READ and LIST permissions");
                result.AddDetail("  5. Check SAS token expiration date");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to download backups from Azure");
            return ValidationResult.Fail($"Failed to download from Azure: {ex.Message}");
        }
    }

    private async Task<ValidationResult> RunAzCopyDownloadAsync(string azCopyPath, string sourceUrl, string destinationPath)
    {
        var result = new ValidationResult();
        try
        {
            _logger.Information("Running AzCopy: {AzCopyPath}", azCopyPath);
            _logger.Information("Source URL: {SourceUrl}", sourceUrl.Contains("?") ? sourceUrl.Substring(0, sourceUrl.IndexOf('?')) + "?[SAS_TOKEN]" : sourceUrl);
            _logger.Information("Destination: {DestinationPath}", destinationPath);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = azCopyPath,
                Arguments = $"copy \"{sourceUrl}\" \"{destinationPath}\" --recursive=true --overwrite=true",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
            {
                return ValidationResult.Fail("Failed to start AzCopy process");
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            _logger.Information("AzCopy exit code: {ExitCode}", process.ExitCode);
            if (!string.IsNullOrWhiteSpace(output))
            {
                _logger.Information("AzCopy output: {Output}", output);
            }
            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.Warning("AzCopy error: {Error}", error);
            }

            if (process.ExitCode == 0)
            {
                _logger.Information("AzCopy download completed successfully");
                result.Success = true;
                result.Message = "Download successful";
                
                // Add output details if available
                if (!string.IsNullOrWhiteSpace(output))
                {
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines.Take(5)) // Show first 5 lines
                    {
                        result.AddDetail($"  {line}");
                    }
                }
                
                return result;
            }
            else
            {
                _logger.Warning("AzCopy download failed with exit code {ExitCode}", process.ExitCode);
                result.Success = false;
                result.Message = $"AzCopy failed with exit code {process.ExitCode}";
                
                // Add error details
                if (!string.IsNullOrWhiteSpace(error))
                {
                    result.AddDetail("AzCopy Error:");
                    var errorLines = error.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in errorLines.Take(10)) // Show first 10 error lines
                    {
                        result.AddDetail($"  {line}");
                    }
                }
                
                // Add output details if available
                if (!string.IsNullOrWhiteSpace(output))
                {
                    result.AddDetail("AzCopy Output:");
                    var outputLines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in outputLines.Take(10)) // Show first 10 output lines
                    {
                        result.AddDetail($"  {line}");
                    }
                }
                
                // Add helpful hints
                result.AddDetail("");
                result.AddDetail("Possible causes:");
                result.AddDetail("  • SAS token expired or invalid");
                result.AddDetail("  • SAS token missing required permissions (Read, List)");
                result.AddDetail("  • Container or folder path does not exist in Azure");
                result.AddDetail("  • Network connectivity issues");
                
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error running AzCopy download");
            result.Success = false;
            result.Message = $"Error running AzCopy: {ex.Message}";
            result.AddDetail($"Exception: {ex.GetType().Name}");
            result.AddDetail($"Message: {ex.Message}");
            if (ex.InnerException != null)
            {
                result.AddDetail($"Inner Exception: {ex.InnerException.Message}");
            }
            return result;
        }
    }

    public ValidationResult RemoveBackupFolders(BackupConfiguration config, bool confirmed)
    {
        if (!confirmed)
        {
            return ValidationResult.Fail("Deletion not confirmed");
        }

        try
        {
            var basePath = Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName);
            var scriptsPath = Path.Combine(basePath, "scripts");

            if (Directory.Exists(scriptsPath))
            {
                Directory.Delete(scriptsPath, recursive: true);
                _logger.Information("Deleted scripts folder: {ScriptsPath}", scriptsPath);
                return ValidationResult.Ok($"Deleted scripts folder: {scriptsPath}\n\nBackup files (FULL, DIFF), logs, and configuration were preserved.");
            }
            else
            {
                return ValidationResult.Ok("Scripts folder not found (already deleted or never created).\n\nBackup files (FULL, DIFF), logs, and configuration were preserved.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to remove scripts folder");
            return ValidationResult.Fail($"Failed to remove scripts folder: {ex.Message}");
        }
    }
}
