using BackupConfigurator.Core.Entities;
using BackupConfigurator.Core.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BackupConfigurator.Data.Repositories;

/// <summary>
/// Repositorio para gestionar el historial de backups
/// </summary>
public class BackupHistoryRepository : IBackupHistoryRepository
{
    private readonly string _connectionString;

    public BackupHistoryRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' not found.");
    }

    public async Task<BackupHistory?> GetByIdAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT Id, BackupConfigurationId, StartTime, EndTime, Status, 
                   ErrorMessage, BackupSizeBytes, BackupFilePath, ExecutedBy
            FROM BackupHistory
            WHERE Id = @Id";
        
        return await connection.QueryFirstOrDefaultAsync<BackupHistory>(query, new { Id = id });
    }

    public async Task<IEnumerable<BackupHistory>> GetByConfigurationIdAsync(int configurationId)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT Id, BackupConfigurationId, StartTime, EndTime, Status, 
                   ErrorMessage, BackupSizeBytes, BackupFilePath, ExecutedBy
            FROM BackupHistory
            WHERE BackupConfigurationId = @ConfigurationId
            ORDER BY StartTime DESC";
        
        return await connection.QueryAsync<BackupHistory>(query, new { ConfigurationId = configurationId });
    }

    public async Task<IEnumerable<BackupHistory>> GetRecentHistoryAsync(int count)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT TOP (@Count) Id, BackupConfigurationId, StartTime, EndTime, Status, 
                   ErrorMessage, BackupSizeBytes, BackupFilePath, ExecutedBy
            FROM BackupHistory
            ORDER BY StartTime DESC";
        
        return await connection.QueryAsync<BackupHistory>(query, new { Count = count });
    }

    public async Task<int> CreateAsync(BackupHistory history)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            INSERT INTO BackupHistory 
                (BackupConfigurationId, StartTime, EndTime, Status, ErrorMessage, 
                 BackupSizeBytes, BackupFilePath, ExecutedBy)
            VALUES 
                (@BackupConfigurationId, @StartTime, @EndTime, @Status, @ErrorMessage, 
                 @BackupSizeBytes, @BackupFilePath, @ExecutedBy);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        return await connection.ExecuteScalarAsync<int>(query, history);
    }

    public async Task<bool> UpdateAsync(BackupHistory history)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            UPDATE BackupHistory
            SET EndTime = @EndTime,
                Status = @Status,
                ErrorMessage = @ErrorMessage,
                BackupSizeBytes = @BackupSizeBytes,
                BackupFilePath = @BackupFilePath
            WHERE Id = @Id";
        
        var result = await connection.ExecuteAsync(query, history);
        return result > 0;
    }
}
