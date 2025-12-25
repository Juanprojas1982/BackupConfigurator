using BackupConfigurator.Core.Entities;
using BackupConfigurator.Core.Interfaces;

namespace BackupConfigurator.Core.Services;

/// <summary>
/// Servicio de gestión de configuraciones de backup
/// </summary>
public class BackupConfigurationService : IBackupConfigurationService
{
    private readonly IBackupConfigurationRepository _repository;

    public BackupConfigurationService(IBackupConfigurationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<BackupConfiguration?> GetConfigurationAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<BackupConfiguration>> GetAllConfigurationsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<BackupConfiguration>> GetActiveConfigurationsAsync()
    {
        return await _repository.GetActiveConfigurationsAsync();
    }

    public async Task<int> CreateConfigurationAsync(BackupConfiguration configuration)
    {
        ValidateConfiguration(configuration);
        configuration.CreatedDate = DateTime.UtcNow;
        configuration.IsActive = true;
        return await _repository.CreateAsync(configuration);
    }

    public async Task<bool> UpdateConfigurationAsync(BackupConfiguration configuration)
    {
        ValidateConfiguration(configuration);
        configuration.LastModifiedDate = DateTime.UtcNow;
        return await _repository.UpdateAsync(configuration);
    }

    public async Task<bool> DeleteConfigurationAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<bool> ActivateConfigurationAsync(int id)
    {
        var config = await _repository.GetByIdAsync(id);
        if (config == null) return false;
        
        config.IsActive = true;
        config.LastModifiedDate = DateTime.UtcNow;
        return await _repository.UpdateAsync(config);
    }

    public async Task<bool> DeactivateConfigurationAsync(int id)
    {
        var config = await _repository.GetByIdAsync(id);
        if (config == null) return false;
        
        config.IsActive = false;
        config.LastModifiedDate = DateTime.UtcNow;
        return await _repository.UpdateAsync(config);
    }

    private void ValidateConfiguration(BackupConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.Name))
            throw new ArgumentException("El nombre de la configuración es requerido.", nameof(configuration.Name));
        
        if (string.IsNullOrWhiteSpace(configuration.DatabaseName))
            throw new ArgumentException("El nombre de la base de datos es requerido.", nameof(configuration.DatabaseName));
        
        if (string.IsNullOrWhiteSpace(configuration.ServerName))
            throw new ArgumentException("El nombre del servidor es requerido.", nameof(configuration.ServerName));
        
        if (string.IsNullOrWhiteSpace(configuration.BackupPath))
            throw new ArgumentException("La ruta de backup es requerida.", nameof(configuration.BackupPath));
    }
}
