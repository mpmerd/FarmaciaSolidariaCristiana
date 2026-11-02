-- Migración: AddBroadcastEmailQueue (tabla BroadcastEmailQueueItems)
-- Fecha: 2026-10-19
-- Propósito: Cola persistente de emails de notificación masiva (broadcast).
--            La procesa BroadcastEmailProcessor (BackgroundService): 1 email cada 60s
--            con tope diario de 350 (cuota Gmail 500/día compartidos con transaccionales).
--            Sobrevive reciclados del hosting de Somee: la cola vive en la BD.
--
-- Aplicar en: Somee → Manage my DB → SQL Manager (pegar y ejecutar completo)
-- Idempotente: se puede re-ejecutar sin error.

-- =============================================
-- 1. Tabla de la cola
-- =============================================

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = 'BroadcastEmailQueueItems'
)
BEGIN
    CREATE TABLE BroadcastEmailQueueItems (
        Id INT IDENTITY (1, 1) NOT NULL,
        BroadcastId UNIQUEIDENTIFIER NOT NULL,
        UserId NVARCHAR(450) NOT NULL,
        Email NVARCHAR(256) NOT NULL,
        Title NVARCHAR(150) NOT NULL,
        Message NVARCHAR(2000) NOT NULL,
        Status INT NOT NULL,
        Attempts INT NOT NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        SentAtUtc DATETIME2 NULL,
        LastError NVARCHAR(500) NULL,

        CONSTRAINT PK_BroadcastEmailQueueItems PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_BroadcastEmailQueueItems_AspNetUsers_UserId
            FOREIGN KEY (UserId) REFERENCES AspNetUsers (Id) ON DELETE CASCADE
    );
    PRINT 'Tabla BroadcastEmailQueueItems creada exitosamente';
END
ELSE
BEGIN
    PRINT 'Tabla BroadcastEmailQueueItems ya existe';
END
GO

-- =============================================
-- 2. Índices (picker del processor + conteo diario)
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BroadcastEmailQueueItems_Status_Id'
      AND object_id = OBJECT_ID('BroadcastEmailQueueItems')
)
BEGIN
    CREATE INDEX IX_BroadcastEmailQueueItems_Status_Id
        ON BroadcastEmailQueueItems (Status, Id);
    PRINT 'Índice IX_BroadcastEmailQueueItems_Status_Id creado';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BroadcastEmailQueueItems_SentAtUtc'
      AND object_id = OBJECT_ID('BroadcastEmailQueueItems')
)
BEGIN
    CREATE INDEX IX_BroadcastEmailQueueItems_SentAtUtc
        ON BroadcastEmailQueueItems (SentAtUtc);
    PRINT 'Índice IX_BroadcastEmailQueueItems_SentAtUtc creado';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_BroadcastEmailQueueItems_UserId'
      AND object_id = OBJECT_ID('BroadcastEmailQueueItems')
)
BEGIN
    CREATE INDEX IX_BroadcastEmailQueueItems_UserId
        ON BroadcastEmailQueueItems (UserId);
    PRINT 'Índice IX_BroadcastEmailQueueItems_UserId creado';
END
GO

-- =============================================
-- 3. Historial de migraciones de EF
--    (OBLIGATORIO: si no, MigrateAsync del arranque intentaría
--     volver a crear la tabla y fallaría)
-- =============================================

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory
    WHERE MigrationId = '20261019161303_AddBroadcastEmailQueue'
)
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261019161303_AddBroadcastEmailQueue', '10.0.7');
    PRINT 'Migración registrada en __EFMigrationsHistory';
END
ELSE
BEGIN
    PRINT 'Migración ya registrada en __EFMigrationsHistory';
END
GO

-- =============================================
-- Verificación (opcional): ver la tabla creada
-- =============================================
-- SELECT * FROM BroadcastEmailQueueItems;
-- SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
