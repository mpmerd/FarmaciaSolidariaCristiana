-- =====================================================
-- Migración: Agregar tabla UserDeviceTokens para OneSignal
-- Fecha: 2026-01-25
-- Descripción: Crea la tabla para almacenar tokens de 
--              dispositivos OneSignal para notificaciones push
-- =====================================================

-- Crear tabla UserDeviceTokens si no existe
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='UserDeviceTokens' AND xtype='U')
BEGIN
    CREATE TABLE [dbo].[UserDeviceTokens] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] NVARCHAR(450) NOT NULL,
        [OneSignalPlayerId] NVARCHAR(100) NOT NULL,
        [DeviceToken] NVARCHAR(500) NULL,
        [DeviceType] NVARCHAR(20) NOT NULL DEFAULT 'Unknown',
        [DeviceName] NVARCHAR(100) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastUsedAt] DATETIME2 NULL,
        CONSTRAINT [PK_UserDeviceTokens] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_UserDeviceTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[AspNetUsers] ([Id]) ON DELETE CASCADE
    );
    
    PRINT 'Tabla UserDeviceTokens creada exitosamente';
END
ELSE
BEGIN
    PRINT 'La tabla UserDeviceTokens ya existe';
END
GO

-- Crear índice único para UserId + OneSignalPlayerId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserDeviceTokens_UserId_OneSignalPlayerId')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_UserDeviceTokens_UserId_OneSignalPlayerId] 
        ON [dbo].[UserDeviceTokens] ([UserId] ASC, [OneSignalPlayerId] ASC);
    PRINT 'Índice IX_UserDeviceTokens_UserId_OneSignalPlayerId creado';
END
GO

-- Crear índice para búsqueda por OneSignalPlayerId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserDeviceTokens_OneSignalPlayerId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserDeviceTokens_OneSignalPlayerId] 
        ON [dbo].[UserDeviceTokens] ([OneSignalPlayerId] ASC);
    PRINT 'Índice IX_UserDeviceTokens_OneSignalPlayerId creado';
END
GO

-- Crear índice para búsqueda de tokens activos por usuario
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserDeviceTokens_UserId_IsActive')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserDeviceTokens_UserId_IsActive] 
        ON [dbo].[UserDeviceTokens] ([UserId] ASC, [IsActive] ASC)
        INCLUDE ([OneSignalPlayerId], [DeviceType]);
    PRINT 'Índice IX_UserDeviceTokens_UserId_IsActive creado';
END
GO

PRINT '=== Migración OneSignal completada ===';
