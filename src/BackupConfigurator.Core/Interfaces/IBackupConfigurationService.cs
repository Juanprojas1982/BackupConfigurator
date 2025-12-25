using BackupConfigurator.Core.Entities;

namespace BackupConfigurator.Core.Interfaces;

/// <summary>
/// Interfaz para el servicio de gestión de configuraciones de backup
/// </summary>
public interface IBackupConfigurationService
{
    Task<BackupConfiguration?> GetConfigurationAsync(int id);
    Task<IEnumerable<BackupConfiguration>> GetAllConfigurationsAsync();
    Task<IEnumerable<BackupConfiguration>> GetActiveConfigurationsAsync();
    Task<int> CreateConfigurationAsync(BackupConfiguration configuration);
    Task<bool> UpdateConfigurationAsync(BackupConfiguration configuration);
    Task<bool> DeleteConfigurationAsync(int id);
    Task<bool> ActivateConfigurationAsync(int id);
    Task<bool> DeactivateConfigurationAsync(int id);
}
