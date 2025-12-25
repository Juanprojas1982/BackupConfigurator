using BackupConfigurator.Core.Entities;

namespace BackupConfigurator.Core.Interfaces;

/// <summary>
/// Interfaz para repositorio de historial de backups
/// </summary>
public interface IBackupHistoryRepository
{
    Task<BackupHistory?> GetByIdAsync(int id);
    Task<IEnumerable<BackupHistory>> GetByConfigurationIdAsync(int configurationId);
    Task<IEnumerable<BackupHistory>> GetRecentHistoryAsync(int count);
    Task<int> CreateAsync(BackupHistory history);
    Task<bool> UpdateAsync(BackupHistory history);
}
