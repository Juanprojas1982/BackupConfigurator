-- =============================================
-- Stored Procedures para BackupConfigurator
-- =============================================

USE BackupConfiguratorDB;
GO

-- =============================================
-- SP: usp_GetBackupStatistics
-- Descripción: Obtiene estadísticas de backups
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetBackupStatistics]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_GetBackupStatistics];
GO

CREATE PROCEDURE [dbo].[usp_GetBackupStatistics]
    @Days INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        bc.Name AS ConfigurationName,
        bc.DatabaseName,
        COUNT(bh.Id) AS TotalBackups,
        SUM(CASE WHEN bh.Status = 2 THEN 1 ELSE 0 END) AS SuccessfulBackups,
        SUM(CASE WHEN bh.Status = 3 THEN 1 ELSE 0 END) AS FailedBackups,
        AVG(DATEDIFF(SECOND, bh.StartTime, bh.EndTime)) AS AvgDurationSeconds,
        SUM(bh.BackupSizeBytes) / 1024 / 1024 / 1024.0 AS TotalSizeGB,
        MAX(bh.StartTime) AS LastBackupTime
    FROM BackupConfigurations bc
    LEFT JOIN BackupHistory bh ON bc.Id = bh.BackupConfigurationId
        AND bh.StartTime >= DATEADD(DAY, -@Days, GETUTCDATE())
    WHERE bc.IsActive = 1
    GROUP BY bc.Id, bc.Name, bc.DatabaseName
    ORDER BY bc.Name;
END
GO

-- =============================================
-- SP: usp_CleanupOldBackupHistory
-- Descripción: Limpia historial antiguo de backups
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_CleanupOldBackupHistory]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_CleanupOldBackupHistory];
GO

CREATE PROCEDURE [dbo].[usp_CleanupOldBackupHistory]
    @RetentionDays INT = 90
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DeletedCount INT;
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE());

    DELETE FROM BackupHistory
    WHERE StartTime < @CutoffDate;

    SET @DeletedCount = @@ROWCOUNT;

    SELECT @DeletedCount AS DeletedRecords, @CutoffDate AS CutoffDate;
END
GO

-- =============================================
-- SP: usp_GetFailedBackups
-- Descripción: Obtiene backups fallidos recientes
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_GetFailedBackups]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_GetFailedBackups];
GO

CREATE PROCEDURE [dbo].[usp_GetFailedBackups]
    @Days INT = 7
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        bc.Name AS ConfigurationName,
        bc.DatabaseName,
        bc.ServerName,
        bh.StartTime,
        bh.EndTime,
        bh.ErrorMessage,
        bh.ExecutedBy
    FROM BackupHistory bh
    INNER JOIN BackupConfigurations bc ON bh.BackupConfigurationId = bc.Id
    WHERE bh.Status = 3 -- Failed
        AND bh.StartTime >= DATEADD(DAY, -@Days, GETUTCDATE())
    ORDER BY bh.StartTime DESC;
END
GO

-- =============================================
-- SP: usp_ExecuteBackup
-- Descripción: Ejecuta un backup basado en configuración
-- =============================================
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[usp_ExecuteBackup]') AND type in (N'P', N'PC'))
    DROP PROCEDURE [dbo].[usp_ExecuteBackup];
GO

CREATE PROCEDURE [dbo].[usp_ExecuteBackup]
    @ConfigurationId INT,
    @ExecutedBy NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DatabaseName NVARCHAR(128);
    DECLARE @BackupPath NVARCHAR(500);
    DECLARE @BackupType INT;
    DECLARE @IsCompressed BIT;
    DECLARE @HistoryId INT;
    DECLARE @ErrorMessage NVARCHAR(MAX);
    DECLARE @BackupCommand NVARCHAR(MAX);
    DECLARE @FileName NVARCHAR(500);

    -- Obtener configuración
    SELECT 
        @DatabaseName = DatabaseName,
        @BackupPath = BackupPath,
        @BackupType = BackupType,
        @IsCompressed = IsCompressed
    FROM BackupConfigurations
    WHERE Id = @ConfigurationId AND IsActive = 1;

    IF @DatabaseName IS NULL
    BEGIN
        RAISERROR('Configuración no encontrada o inactiva.', 16, 1);
        RETURN;
    END

    -- Crear registro de historial
    INSERT INTO BackupHistory (BackupConfigurationId, StartTime, Status, ExecutedBy)
    VALUES (@ConfigurationId, GETUTCDATE(), 1, @ExecutedBy);

    SET @HistoryId = SCOPE_IDENTITY();

    -- Generar nombre de archivo
    SET @FileName = @DatabaseName + '_' + 
                    CASE @BackupType 
                        WHEN 1 THEN 'FULL_'
                        WHEN 2 THEN 'DIFF_'
                        WHEN 3 THEN 'LOG_'
                    END +
                    REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR, GETUTCDATE(), 120), '-', ''), ' ', '_'), ':', '') + '.bak';

    -- Construir comando de backup
    SET @BackupCommand = 'BACKUP ' + 
                         CASE @BackupType 
                             WHEN 3 THEN 'LOG'
                             ELSE 'DATABASE'
                         END + ' [' + @DatabaseName + '] TO DISK = ''' + @BackupPath + '\' + @FileName + ''' WITH ' +
                         CASE @BackupType WHEN 2 THEN 'DIFFERENTIAL, ' ELSE '' END +
                         CASE @IsCompressed WHEN 1 THEN 'COMPRESSION, ' ELSE 'NO_COMPRESSION, ' END +
                         'CHECKSUM, STATS = 10';

    BEGIN TRY
        -- Ejecutar backup
        EXEC sp_executesql @BackupCommand;

        -- Actualizar historial como exitoso
        UPDATE BackupHistory
        SET EndTime = GETUTCDATE(),
            Status = 2,
            BackupFilePath = @BackupPath + '\' + @FileName
        WHERE Id = @HistoryId;

        SELECT @HistoryId AS HistoryId, 'Success' AS Result;
    END TRY
    BEGIN CATCH
        SET @ErrorMessage = ERROR_MESSAGE();

        -- Actualizar historial como fallido
        UPDATE BackupHistory
        SET EndTime = GETUTCDATE(),
            Status = 3,
            ErrorMessage = @ErrorMessage
        WHERE Id = @HistoryId;

        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END
GO

PRINT 'Stored Procedures creados exitosamente.';
GO
