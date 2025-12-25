using BackupConfigurator.Core.Entities;
using BackupConfigurator.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BackupConfigurator.Data.Services;

/// <summary>
/// Servicio para ejecutar backups de SQL Server
/// </summary>
public class BackupExecutionService : IBackupExecutionService
{
    private readonly IConfiguration _configuration;
    private readonly IBackupHistoryRepository _historyRepository;
    private readonly ILogger<BackupExecutionService> _logger;

    public BackupExecutionService(
        IConfiguration configuration,
        IBackupHistoryRepository historyRepository,
        ILogger<BackupExecutionService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _historyRepository = historyRepository ?? throw new ArgumentNullException(nameof(historyRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BackupHistory> ExecuteBackupAsync(BackupConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var history = new BackupHistory
        {
            BackupConfigurationId = configuration.Id,
            StartTime = DateTime.UtcNow,
            Status = BackupStatus.Running,
            ExecutedBy = Environment.UserName
        };

        var historyId = await _historyRepository.CreateAsync(history);
        history.Id = historyId;

        try
        {
            _logger.LogInformation("Iniciando backup de {DatabaseName} en {ServerName}", 
                configuration.DatabaseName, configuration.ServerName);

            var connectionString = BuildConnectionString(configuration.ServerName);
            var backupFileName = GenerateBackupFileName(configuration);
            var backupFullPath = Path.Combine(configuration.BackupPath, backupFileName);

            await ExecuteSqlBackupAsync(connectionString, configuration, backupFullPath, cancellationToken);

            var fileInfo = new FileInfo(backupFullPath);
            history.EndTime = DateTime.UtcNow;
            history.Status = BackupStatus.Completed;
            history.BackupFilePath = backupFullPath;
            history.BackupSizeBytes = fileInfo.Exists ? fileInfo.Length : 0;

            _logger.LogInformation("Backup completado exitosamente: {BackupPath}", backupFullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando backup de {DatabaseName}", configuration.DatabaseName);
            history.EndTime = DateTime.UtcNow;
            history.Status = BackupStatus.Failed;
            history.ErrorMessage = ex.Message;
        }

        await _historyRepository.UpdateAsync(history);
        return history;
    }

    public async Task<bool> ValidateConfigurationAsync(BackupConfiguration configuration)
    {
        try
        {
            var connectionString = BuildConnectionString(configuration.ServerName);
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = "SELECT COUNT(*) FROM sys.databases WHERE name = @DatabaseName";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@DatabaseName", configuration.DatabaseName);

            var result = (int)(await command.ExecuteScalarAsync() ?? 0);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validando configuración para {DatabaseName}", configuration.DatabaseName);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetDatabasesAsync(string serverName, string? connectionString = null)
    {
        var databases = new List<string>();
        
        try
        {
            var connString = connectionString ?? BuildConnectionString(serverName);
            using var connection = new SqlConnection(connString);
            await connection.OpenAsync();

            var query = "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name";
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                databases.Add(reader.GetString(0));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo lista de bases de datos de {ServerName}", serverName);
        }

        return databases;
    }

    private async Task ExecuteSqlBackupAsync(string connectionString, BackupConfiguration config, 
        string backupPath, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var backupTypeString = config.BackupType switch
        {
            BackupType.Full => "DATABASE",
            BackupType.Differential => "DATABASE",
            BackupType.TransactionLog => "LOG",
            _ => "DATABASE"
        };

        var backupCommand = $@"
            BACKUP {backupTypeString} [{config.DatabaseName}]
            TO DISK = @BackupPath
            WITH 
                NAME = @BackupName,
                {(config.BackupType == BackupType.Differential ? "DIFFERENTIAL," : "")}
                {(config.IsCompressed ? "COMPRESSION," : "NO_COMPRESSION,")}
                STATS = 10,
                CHECKSUM";

        using var command = new SqlCommand(backupCommand, connection);
        command.CommandTimeout = 0; // Sin timeout para backups grandes
        command.Parameters.AddWithValue("@BackupPath", backupPath);
        command.Parameters.AddWithValue("@BackupName", $"{config.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}");

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private string BuildConnectionString(string serverName)
    {
        var baseConnectionString = _configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(baseConnectionString))
        {
            return $"Server={serverName};Database=master;Integrated Security=true;TrustServerCertificate=true;";
        }

        var builder = new SqlConnectionStringBuilder(baseConnectionString)
        {
            DataSource = serverName,
            InitialCatalog = "master"
        };

        return builder.ConnectionString;
    }

    private string GenerateBackupFileName(BackupConfiguration configuration)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var backupTypePrefix = configuration.BackupType switch
        {
            BackupType.Full => "FULL",
            BackupType.Differential => "DIFF",
            BackupType.TransactionLog => "LOG",
            _ => "FULL"
        };

        return $"{configuration.DatabaseName}_{backupTypePrefix}_{timestamp}.bak";
    }
}
