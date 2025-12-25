namespace BackupConfigurator.Core.Entities;

/// <summary>
/// Tipos de backup disponibles en SQL Server
/// </summary>
public enum BackupType
{
    /// <summary>
    /// Backup completo de la base de datos
    /// </summary>
    Full = 1,
    
    /// <summary>
    /// Backup diferencial
    /// </summary>
    Differential = 2,
    
    /// <summary>
    /// Backup del log de transacciones
    /// </summary>
    TransactionLog = 3
}
