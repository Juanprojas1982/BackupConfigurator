using System.Text.Json;
using BackupConfigurator.Core.Models;
using Serilog;

namespace BackupConfigurator.Core.Services;

public class ConfigurationManager
{
    private readonly ILogger _logger;
    private const string AppDataFolder = @"C:\ProgramData\BackupConfigurator";
    private const string ConfigFileName = "config.json";

    public ConfigurationManager(ILogger logger)
    {
        _logger = logger;
    }

    public string ConfigFilePath => Path.Combine(AppDataFolder, ConfigFileName);

    public ValidationResult SaveConfiguration(BackupConfiguration config)
    {
        try
        {
            _logger.Information("Saving configuration to {ConfigPath}", ConfigFilePath);

            Directory.CreateDirectory(AppDataFolder);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ConfigFilePath, json);

            _logger.Information("Configuration saved successfully");
            return ValidationResult.Ok($"Configuration saved to: {ConfigFilePath}");
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
            if (!File.Exists(ConfigFilePath))
            {
                _logger.Information("Configuration file not found");
                return (null, ValidationResult.Fail("Configuration file not found"));
            }

            _logger.Information("Loading configuration from {ConfigPath}", ConfigFilePath);

            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<BackupConfiguration>(json);

            if (config == null)
            {
                return (null, ValidationResult.Fail("Failed to deserialize configuration"));
            }

            _logger.Information("Configuration loaded successfully");
            return (config, ValidationResult.Ok("Configuration loaded successfully"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load configuration");
            return (null, ValidationResult.Fail($"Failed to load configuration: {ex.Message}"));
        }
    }
}
