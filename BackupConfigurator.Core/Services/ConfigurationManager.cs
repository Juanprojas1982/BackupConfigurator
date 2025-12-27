using System.Text.Encodings.Web;
using System.Text.Json;
using BackupConfigurator.Core.Models;
using Serilog;

namespace BackupConfigurator.Core.Services;

public class ConfigurationManager
{
    private readonly ILogger _logger;

    public ConfigurationManager(ILogger logger)
    {
        _logger = logger;
    }

    private static string GetConfigFilePath(BackupConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.LocalBasePath) || 
            string.IsNullOrWhiteSpace(config.SanitizedDatabaseName))
        {
            throw new InvalidOperationException("LocalBasePath and DatabaseName must be set before saving configuration");
        }

        return Path.Combine(config.LocalBasePath, config.SanitizedDatabaseName, "config.json");
    }

    public ValidationResult SaveConfiguration(BackupConfiguration config)
    {
        try
        {
            var configPath = GetConfigFilePath(config);
            var directory = Path.GetDirectoryName(configPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create a copy without the SQL password (security)
            var configToSave = new BackupConfiguration
            {
                InstitutionNIT = config.InstitutionNIT,
                SqlServer = config.SqlServer,
                SqlUser = config.SqlUser,
                SqlPassword = string.Empty, // DO NOT SAVE PASSWORD
                DatabaseName = config.DatabaseName,
                DifferentialIntervalHours = config.DifferentialIntervalHours,
                FullBackupDayOfWeek = config.FullBackupDayOfWeek,
                FullBackupTime = config.FullBackupTime,
                LocalBasePath = config.LocalBasePath,
                LocalRetentionDays = config.LocalRetentionDays,
                AzureContainerUrl = config.AzureContainerUrl,
                AzureSasToken = config.AzureSasToken,
                AzCopyPath = config.AzCopyPath,
                InstallationKeyHash = config.InstallationKeyHash
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                // Keep ampersands and other characters unescaped for readability
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(configToSave, options);
            File.WriteAllText(configPath, json);

            _logger.Information("Configuration saved to {ConfigPath}", configPath);

            return ValidationResult.Ok($"Configuration saved to: {configPath}");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save configuration");
            return ValidationResult.Fail($"Failed to save configuration: {ex.Message}");
        }
    }

    public (BackupConfiguration? config, ValidationResult result) LoadConfiguration()
    {
        try
        {
            // Search in common backup locations
            var searchPaths = new List<string>();
            
            var commonDrives = new[] { "D:\\", "E:\\", "F:\\", "C:\\" };
            foreach (var drive in commonDrives.Where(Directory.Exists))
            {
                try
                {
                    var backupFolders = Directory.GetDirectories(drive, "*", SearchOption.TopDirectoryOnly)
                        .Where(d => d.Contains("Backup", StringComparison.OrdinalIgnoreCase) || 
                                    d.Contains("Respaldo", StringComparison.OrdinalIgnoreCase));

                    foreach (var folder in backupFolders.Take(5))
                    {
                        var possibleConfigs = Directory.GetFiles(folder, "config.json", SearchOption.AllDirectories).Take(5);
                        searchPaths.AddRange(possibleConfigs);
                    }
                }
                catch
                {
                    // Ignore access errors
                }
            }

            foreach (var configPath in searchPaths.Where(File.Exists))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<BackupConfiguration>(json);

                    if (config != null)
                    {
                        // Clean the configuration after loading
                        config.CleanOnLoad();
                        
                        _logger.Information("Configuration loaded from {ConfigPath}", configPath);
                        var result = ValidationResult.Ok($"Configuration loaded from: {configPath}");
                        result.AddDetail("? SQL Password not saved for security - will be requested when needed");
                        return (config, result);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to load configuration from {ConfigPath}", configPath);
                }
            }

            _logger.Warning("No configuration file found");
            return (null, ValidationResult.Fail("No configuration file found"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading configuration");
            return (null, ValidationResult.Fail($"Error loading configuration: {ex.Message}"));
        }
    }
}
