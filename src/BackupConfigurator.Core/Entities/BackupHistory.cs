namespace BackupConfigurator.Core.Entities;

/// <summary>
/// Historial de ejecución de backups
/// </summary>
public class BackupHistory
{
    public int Id { get; set; }
    public int BackupConfigurationId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public BackupStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public long? BackupSizeBytes { get; set; }
    public string? BackupFilePath { get; set; }
    public string ExecutedBy { get; set; } = string.Empty;
}
