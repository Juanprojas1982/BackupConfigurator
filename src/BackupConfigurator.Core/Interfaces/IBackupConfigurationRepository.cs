using BackupConfigurator.Core.Entities;

namespace BackupConfigurator.Core.Interfaces;

/// <summary>
/// Interfaz para repositorio de configuraciones de backup
/// </summary>
public interface IBackupConfigurationRepository
{
    Task<BackupConfiguration?> GetByIdAsync(int id);
    Task<IEnumerable<BackupConfiguration>> GetAllAsync();
    Task<IEnumerable<BackupConfiguration>> GetActiveConfigurationsAsync();
    Task<int> CreateAsync(BackupConfiguration configuration);
    Task<bool> UpdateAsync(BackupConfiguration configuration);
    Task<bool> DeleteAsync(int id);
}
