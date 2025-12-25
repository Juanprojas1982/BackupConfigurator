namespace BackupConfigurator.Core.Entities;

/// <summary>
/// Representa la configuración de un backup de base de datos SQL Server
/// </summary>
public class BackupConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public BackupType BackupType { get; set; }
    public string BackupPath { get; set; } = string.Empty;
    public bool IsCompressed { get; set; }
    public bool IsEncrypted { get; set; }
    public int? RetentionDays { get; set; }
    public string? Schedule { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string? LastModifiedBy { get; set; }
}
