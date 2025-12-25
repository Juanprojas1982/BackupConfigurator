-- =============================================
-- Script de inicialización de base de datos
-- BackupConfigurator Database
-- =============================================

USE master;
GO

-- Crear la base de datos si no existe
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'BackupConfiguratorDB')
BEGIN
    CREATE DATABASE BackupConfiguratorDB;
    PRINT 'Base de datos BackupConfiguratorDB creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La base de datos BackupConfiguratorDB ya existe.';
END
GO

USE BackupConfiguratorDB;
GO

-- =============================================
-- Tabla: BackupConfigurations
-- Descripción: Almacena configuraciones de backup
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BackupConfigurations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BackupConfigurations] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [DatabaseName] NVARCHAR(128) NOT NULL,
        [ServerName] NVARCHAR(128) NOT NULL,
        [BackupType] INT NOT NULL,
        [BackupPath] NVARCHAR(500) NOT NULL,
        [IsCompressed] BIT NOT NULL DEFAULT 1,
        [IsEncrypted] BIT NOT NULL DEFAULT 0,
        [RetentionDays] INT NULL,
        [Schedule] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(100) NOT NULL,
        [LastModifiedBy] NVARCHAR(100) NULL,
        
        CONSTRAINT [CK_BackupConfigurations_BackupType] CHECK ([BackupType] IN (1, 2, 3)),
        CONSTRAINT [CK_BackupConfigurations_RetentionDays] CHECK ([RetentionDays] IS NULL OR [RetentionDays] > 0)
    );

    CREATE INDEX [IX_BackupConfigurations_DatabaseName] ON [dbo].[BackupConfigurations]([DatabaseName]);
    CREATE INDEX [IX_BackupConfigurations_IsActive] ON [dbo].[BackupConfigurations]([IsActive]);
    CREATE INDEX [IX_BackupConfigurations_CreatedDate] ON [dbo].[BackupConfigurations]([CreatedDate]);

    PRINT 'Tabla BackupConfigurations creada exitosamente.';
END
GO

-- =============================================
-- Tabla: BackupHistory
-- Descripción: Almacena historial de ejecuciones de backup
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BackupHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BackupHistory] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [BackupConfigurationId] INT NOT NULL,
        [StartTime] DATETIME2 NOT NULL,
        [EndTime] DATETIME2 NULL,
        [Status] INT NOT NULL,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [BackupSizeBytes] BIGINT NULL,
        [BackupFilePath] NVARCHAR(500) NULL,
        [ExecutedBy] NVARCHAR(100) NOT NULL,
        
        CONSTRAINT [FK_BackupHistory_BackupConfigurations] 
            FOREIGN KEY ([BackupConfigurationId]) 
            REFERENCES [dbo].[BackupConfigurations]([Id])
            ON DELETE CASCADE,
        
        CONSTRAINT [CK_BackupHistory_Status] CHECK ([Status] IN (1, 2, 3, 4)),
        CONSTRAINT [CK_BackupHistory_BackupSizeBytes] CHECK ([BackupSizeBytes] IS NULL OR [BackupSizeBytes] >= 0)
    );

    CREATE INDEX [IX_BackupHistory_BackupConfigurationId] ON [dbo].[BackupHistory]([BackupConfigurationId]);
    CREATE INDEX [IX_BackupHistory_StartTime] ON [dbo].[BackupHistory]([StartTime] DESC);
    CREATE INDEX [IX_BackupHistory_Status] ON [dbo].[BackupHistory]([Status]);

    PRINT 'Tabla BackupHistory creada exitosamente.';
END
GO

PRINT 'Inicialización de base de datos completada.';
GO
