namespace BackupConfigurator.Core.Entities;

/// <summary>
/// Estados posibles de ejecución de un backup
/// </summary>
public enum BackupStatus
{
    /// <summary>
    /// Backup en ejecución
    /// </summary>
    Running = 1,
    
    /// <summary>
    /// Backup completado exitosamente
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Backup fallido
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Backup cancelado
    /// </summary>
    Cancelled = 4
}
