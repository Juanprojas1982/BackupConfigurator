using BackupConfigurator.Core.Entities;
using BackupConfigurator.Core.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace BackupConfigurator.Data.Repositories;

/// <summary>
/// Repositorio para gestionar configuraciones de backup en SQL Server
/// </summary>
public class BackupConfigurationRepository : IBackupConfigurationRepository
{
    private readonly string _connectionString;

    public BackupConfigurationRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' not found.");
    }

    public async Task<BackupConfiguration?> GetByIdAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT Id, Name, DatabaseName, ServerName, BackupType, BackupPath, 
                   IsCompressed, IsEncrypted, RetentionDays, Schedule, IsActive, 
                   CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy
            FROM BackupConfigurations
            WHERE Id = @Id";
        
        return await connection.QueryFirstOrDefaultAsync<BackupConfiguration>(query, new { Id = id });
    }

    public async Task<IEnumerable<BackupConfiguration>> GetAllAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT Id, Name, DatabaseName, ServerName, BackupType, BackupPath, 
                   IsCompressed, IsEncrypted, RetentionDays, Schedule, IsActive, 
                   CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy
            FROM BackupConfigurations
            ORDER BY CreatedDate DESC";
        
        return await connection.QueryAsync<BackupConfiguration>(query);
    }

    public async Task<IEnumerable<BackupConfiguration>> GetActiveConfigurationsAsync()
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            SELECT Id, Name, DatabaseName, ServerName, BackupType, BackupPath, 
                   IsCompressed, IsEncrypted, RetentionDays, Schedule, IsActive, 
                   CreatedDate, LastModifiedDate, CreatedBy, LastModifiedBy
            FROM BackupConfigurations
            WHERE IsActive = 1
            ORDER BY CreatedDate DESC";
        
        return await connection.QueryAsync<BackupConfiguration>(query);
    }

    public async Task<int> CreateAsync(BackupConfiguration configuration)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            INSERT INTO BackupConfigurations 
                (Name, DatabaseName, ServerName, BackupType, BackupPath, IsCompressed, 
                 IsEncrypted, RetentionDays, Schedule, IsActive, CreatedDate, CreatedBy)
            VALUES 
                (@Name, @DatabaseName, @ServerName, @BackupType, @BackupPath, @IsCompressed, 
                 @IsEncrypted, @RetentionDays, @Schedule, @IsActive, @CreatedDate, @CreatedBy);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        return await connection.ExecuteScalarAsync<int>(query, configuration);
    }

    public async Task<bool> UpdateAsync(BackupConfiguration configuration)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = @"
            UPDATE BackupConfigurations
            SET Name = @Name,
                DatabaseName = @DatabaseName,
                ServerName = @ServerName,
                BackupType = @BackupType,
                BackupPath = @BackupPath,
                IsCompressed = @IsCompressed,
                IsEncrypted = @IsEncrypted,
                RetentionDays = @RetentionDays,
                Schedule = @Schedule,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE Id = @Id";
        
        var result = await connection.ExecuteAsync(query, configuration);
        return result > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        var query = "DELETE FROM BackupConfigurations WHERE Id = @Id";
        var result = await connection.ExecuteAsync(query, new { Id = id });
        return result > 0;
    }
}
