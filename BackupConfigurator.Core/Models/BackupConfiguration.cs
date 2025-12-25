using System.Text.Json.Serialization;

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
    /// Gets the sanitized NIT for use in paths and job names
    /// </summary>
    public string SanitizedNIT => Sanitize(InstitutionNIT);

    /// <summary>
    /// Gets the sanitized database name for use in paths and job names
    /// </summary>
    public string SanitizedDatabaseName => Sanitize(DatabaseName);

    /// <summary>
    /// Gets the normalized SAS token (ensures it starts with ?)
    /// </summary>
    public string NormalizedSasToken
    {
        get
        {
            if (string.IsNullOrWhiteSpace(AzureSasToken))
                return string.Empty;
            var trimmed = AzureSasToken.Trim();
            return trimmed.StartsWith('?') ? trimmed : $"?{trimmed}";
        }
    }
}
