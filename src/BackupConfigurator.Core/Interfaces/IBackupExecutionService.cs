using BackupConfigurator.Core.Entities;

namespace BackupConfigurator.Core.Interfaces;

/// <summary>
/// Interfaz para el servicio de ejecución de backups
/// </summary>
public interface IBackupExecutionService
{
    Task<BackupHistory> ExecuteBackupAsync(BackupConfiguration configuration, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigurationAsync(BackupConfiguration configuration);
    Task<IEnumerable<string>> GetDatabasesAsync(string serverName, string? connectionString = null);
}
