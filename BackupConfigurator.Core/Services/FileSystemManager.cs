using BackupConfigurator.Core.Models;
using Serilog;

namespace BackupConfigurator.Core.Services;

public class FileSystemManager
{
    private readonly ILogger _logger;
    private const string AppDataFolder = @"C:\ProgramData\BackupConfigurator";

    public FileSystemManager(ILogger logger)
    {
        _logger = logger;
    }

    public ValidationResult CreateBackupFolders(BackupConfiguration config)
    {
        try
        {
            _logger.Information("Creating backup folders for {NIT}/{Database}", config.SanitizedNIT, config.SanitizedDatabaseName);

            var basePath = Path.Combine(config.LocalBasePath, config.SanitizedNIT, config.SanitizedDatabaseName);
            var fullPath = Path.Combine(basePath, "FULL");
            var diffPath = Path.Combine(basePath, "DIFF");

            Directory.CreateDirectory(fullPath);
            Directory.CreateDirectory(diffPath);

            _logger.Information("Created backup folders: {FullPath}, {DiffPath}", fullPath, diffPath);

            var result = ValidationResult.Ok("Backup folders created successfully");
            result.AddDetail($"FULL: {fullPath}");
            result.AddDetail($"DIFF: {diffPath}");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create backup folders");
            return ValidationResult.Fail($"Failed to create backup folders: {ex.Message}");
        }
    }

    public ValidationResult CreateScriptsFolder()
    {
        try
        {
            var scriptsPath = Path.Combine(AppDataFolder, "scripts");
            Directory.CreateDirectory(scriptsPath);
            _logger.Information("Created scripts folder: {ScriptsPath}", scriptsPath);
            return ValidationResult.Ok($"Scripts folder created: {scriptsPath}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create scripts folder");
            return ValidationResult.Fail($"Failed to create scripts folder: {ex.Message}");
        }
    }

    public ValidationResult CreateUploadScripts(BackupConfiguration config)
    {
        try
        {
            _logger.Information("Creating upload scripts");

            var scriptsPath = Path.Combine(AppDataFolder, "scripts");
            Directory.CreateDirectory(scriptsPath);

            var basePath = Path.Combine(config.LocalBasePath, config.SanitizedNIT, config.SanitizedDatabaseName);
            var fullLocalPath = Path.Combine(basePath, "FULL");
            var diffLocalPath = Path.Combine(basePath, "DIFF");

            var containerUrl = config.AzureContainerUrl.TrimEnd('/');
            var sasToken = config.NormalizedSasToken;

            // FULL backup upload script
            var fullScript = $@"@echo off
REM Upload FULL backups to Azure Blob Storage
""{config.AzCopyPath}"" copy ""{fullLocalPath}\*.bak"" ""{containerUrl}/{config.SanitizedNIT}/{config.SanitizedDatabaseName}/FULL{sasToken}"" --overwrite=true --recursive=false
exit /b %ERRORLEVEL%
";

            var fullScriptPath = Path.Combine(scriptsPath, "upload_full.cmd");
            File.WriteAllText(fullScriptPath, fullScript);

            // DIFF backup upload script
            var diffScript = $@"@echo off
REM Upload DIFF backups to Azure Blob Storage
""{config.AzCopyPath}"" copy ""{diffLocalPath}\*.dif"" ""{containerUrl}/{config.SanitizedNIT}/{config.SanitizedDatabaseName}/DIFF{sasToken}"" --overwrite=true --recursive=false
exit /b %ERRORLEVEL%
";

            var diffScriptPath = Path.Combine(scriptsPath, "upload_diff.cmd");
            File.WriteAllText(diffScriptPath, diffScript);

            _logger.Information("Created upload scripts: {FullScript}, {DiffScript}", fullScriptPath, diffScriptPath);

            var result = ValidationResult.Ok("Upload scripts created successfully");
            result.AddDetail($"FULL upload: {fullScriptPath}");
            result.AddDetail($"DIFF upload: {diffScriptPath}");
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

            var scriptsPath = Path.Combine(AppDataFolder, "scripts");
            Directory.CreateDirectory(scriptsPath);

            var basePath = Path.Combine(config.LocalBasePath, config.SanitizedNIT, config.SanitizedDatabaseName);

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

    public ValidationResult RemoveBackupFolders(BackupConfiguration config, bool confirmed)
    {
        if (!confirmed)
        {
            return ValidationResult.Fail("Deletion not confirmed");
        }

        try
        {
            var basePath = Path.Combine(config.LocalBasePath, config.SanitizedNIT, config.SanitizedDatabaseName);

            if (Directory.Exists(basePath))
            {
                Directory.Delete(basePath, recursive: true);
                _logger.Information("Deleted backup folders: {BasePath}", basePath);
                return ValidationResult.Ok($"Deleted: {basePath}");
            }
            else
            {
                return ValidationResult.Ok("Backup folders not found (already deleted)");
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to remove backup folders");
            return ValidationResult.Fail($"Failed to remove backup folders: {ex.Message}");
        }
    }
}
