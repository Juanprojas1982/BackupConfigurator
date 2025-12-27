using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace BackupConfigurator.Core.Models;

public class BackupConfiguration
{
    [JsonPropertyName("institutionNIT")]
    public string InstitutionNIT { get; set; } = string.Empty;

    [JsonPropertyName("sqlServer")]
    public string SqlServer { get; set; } = string.Empty;

    [JsonPropertyName("sqlUser")]
    public string SqlUser { get; set; } = string.Empty;

    [JsonPropertyName("sqlPassword")]
    public string SqlPassword { get; set; } = string.Empty;

    [JsonPropertyName("databaseName")]
    public string DatabaseName { get; set; } = string.Empty;

    [JsonPropertyName("differentialIntervalHours")]
    public int DifferentialIntervalHours { get; set; } = 6;

    [JsonPropertyName("fullBackupDayOfWeek")]
    public DayOfWeek FullBackupDayOfWeek { get; set; } = DayOfWeek.Sunday;

    [JsonPropertyName("fullBackupTime")]
    public string FullBackupTime { get; set; } = "02:00";

    [JsonPropertyName("localBasePath")]
    public string LocalBasePath { get; set; } = string.Empty;

    [JsonPropertyName("localRetentionDays")]
    public int LocalRetentionDays { get; set; } = 14;

    [JsonPropertyName("azureContainerUrl")]
    public string AzureContainerUrl { get; set; } = string.Empty;

    [JsonPropertyName("azureSasToken")]
    public string AzureSasToken { get; set; } = string.Empty;

    [JsonPropertyName("azCopyPath")]
    public string AzCopyPath { get; set; } = @"C:\Program Files\Microsoft\AzCopy\azcopy.exe";

    [JsonPropertyName("installationKeyHash")]
    public string InstallationKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Sanitizes a string to be safe for use in file paths and job names
    /// </summary>
    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var result = new System.Text.StringBuilder();
        foreach (char c in input)
        {
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                result.Append(c);
            else
                result.Append('-');
        }
        return result.ToString();
    }

    /// <summary>
    /// Generates SHA256 hash of the installation key
    /// </summary>
    public static string HashInstallationKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(key);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Validates an installation key against the stored hash
    /// </summary>
    public bool ValidateInstallationKey(string key)
    {
        if (string.IsNullOrWhiteSpace(InstallationKeyHash))
            return true; // No key set, allow operation

        var inputHash = HashInstallationKey(key);
        return inputHash == InstallationKeyHash;
    }

    /// <summary>
    /// Sets the installation key (stores the hash)
    /// </summary>
    public void SetInstallationKey(string key)
    {
        InstallationKeyHash = HashInstallationKey(key);
    }

    /// <summary>
    /// Gets the sanitized NIT for use in paths and job names
    /// </summary>
    public string SanitizedNIT => Sanitize(InstitutionNIT);

    /// <summary>
    /// Gets the sanitized database name for use in paths and job names
    /// </summary>
    public string SanitizedDatabaseName => Sanitize(DatabaseName);

    /// <summary>
    /// Gets the normalized SAS token (ensures it starts with ? and removes all whitespace)
    /// </summary>
    public string NormalizedSasToken
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AzureSasToken))
                return string.Empty;
            
            // Remove ALL whitespace characters (spaces, tabs, newlines, carriage returns)
            var cleaned = new string(AzureSasToken.Where(c => !char.IsWhiteSpace(c)).ToArray());
            
            // Ensure it starts with ?
            return cleaned.StartsWith('?') ? cleaned : $"?{cleaned}";
        }
    }

    /// <summary>
    /// Called after deserialization to clean up any invalid data
    /// </summary>
    public void CleanOnLoad()
    {
        // Clean the SAS token by removing all whitespace
        if (!string.IsNullOrWhiteSpace(AzureSasToken))
        {
            AzureSasToken = new string(AzureSasToken.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }
        
        // Clean the Container URL by removing trailing whitespace
        if (!string.IsNullOrWhiteSpace(AzureContainerUrl))
        {
            AzureContainerUrl = AzureContainerUrl.Trim().TrimEnd('/');
        }
    }
}
