-- =============================================
-- Script de datos de ejemplo
-- BackupConfigurator Database
-- =============================================

USE BackupConfiguratorDB;
GO

-- Insertar configuraciones de ejemplo
IF NOT EXISTS (SELECT * FROM BackupConfigurations WHERE Name = 'Daily Full Backup - ProductionDB')
BEGIN
    INSERT INTO BackupConfigurations 
        (Name, DatabaseName, ServerName, BackupType, BackupPath, IsCompressed, IsEncrypted, 
         RetentionDays, Schedule, IsActive, CreatedBy)
    VALUES 
        ('Daily Full Backup - ProductionDB', 'ProductionDB', 'localhost', 1, 'C:\Backups\Full', 1, 0, 30, 'Daily 2:00 AM', 1, 'System'),
        ('Hourly Log Backup - ProductionDB', 'ProductionDB', 'localhost', 3, 'C:\Backups\Log', 1, 0, 7, 'Hourly', 1, 'System'),
        ('Weekly Differential - TestDB', 'TestDB', 'localhost', 2, 'C:\Backups\Diff', 1, 0, 14, 'Weekly Sunday 3:00 AM', 1, 'System');

    PRINT 'Datos de ejemplo insertados exitosamente.';
END
ELSE
BEGIN
    PRINT 'Los datos de ejemplo ya existen.';
END
GO
