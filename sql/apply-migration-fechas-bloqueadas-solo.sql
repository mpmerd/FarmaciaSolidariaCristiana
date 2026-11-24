-- =====================================================================================
-- MIGRACIÓN 9: AddFechasBloqueadas (03/11/2025)
-- Sistema de Bloqueo de Fechas para Turnos
-- =====================================================================================

PRINT '========================================================================='
PRINT 'APLICANDO MIGRACIÓN 9: AddFechasBloqueadas'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

-- Verificar si la migración ya está registrada
IF EXISTS (SELECT * FROM __EFMigrationsHistory 
           WHERE MigrationId = '20251103000000_AddFechasBloqueadas')
BEGIN
    PRINT '⚠ Migración 9 ya está registrada en el historial'
    PRINT 'Verificando si la tabla existe...'
    PRINT ''
END

-- Verificar si la tabla FechasBloqueadas ya existe
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FechasBloqueadas')
BEGIN
    PRINT '⚠ La tabla FechasBloqueadas ya existe'
    PRINT ''
    
    -- Mostrar estructura
    PRINT 'Estructura actual de la tabla:'
    SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'FechasBloqueadas'
    ORDER BY ORDINAL_POSITION;
    
    PRINT ''
    PRINT '✓ Tabla ya está lista para usar'
END
ELSE
BEGIN
    PRINT 'Creando tabla FechasBloqueadas...'
    PRINT ''
    
    BEGIN TRY
        -- Crear tabla FechasBloqueadas
        CREATE TABLE [FechasBloqueadas] (
            [Id] int IDENTITY(1,1) NOT NULL,
            [Fecha] date NOT NULL,
            [Motivo] nvarchar(500) NOT NULL,
            [UsuarioId] nvarchar(450) NOT NULL,
            [FechaCreacion] datetime2 NOT NULL DEFAULT GETDATE(),
            CONSTRAINT [PK_FechasBloqueadas] PRIMARY KEY ([Id])
        );
        PRINT '✓ Tabla FechasBloqueadas creada'
        
        -- Crear índice único en Fecha
        CREATE UNIQUE NONCLUSTERED INDEX [IX_FechasBloqueadas_Fecha] 
            ON [FechasBloqueadas]([Fecha] ASC);
        PRINT '✓ Índice único IX_FechasBloqueadas_Fecha creado'
        
        -- Crear índice en UsuarioId
        CREATE NONCLUSTERED INDEX [IX_FechasBloqueadas_UsuarioId] 
            ON [FechasBloqueadas]([UsuarioId] ASC);
        PRINT '✓ Índice IX_FechasBloqueadas_UsuarioId creado'
        
        -- Crear foreign key hacia AspNetUsers (RESTRICT)
        ALTER TABLE [FechasBloqueadas] ADD CONSTRAINT [FK_FechasBloqueadas_AspNetUsers_UsuarioId] 
            FOREIGN KEY ([UsuarioId]) REFERENCES [AspNetUsers] ([Id]);
        PRINT '✓ Foreign key FechasBloqueadas -> AspNetUsers creada (RESTRICT)'
        
        PRINT ''
        PRINT '✅ Tabla FechasBloqueadas creada exitosamente'
        
    END TRY
    BEGIN CATCH
        PRINT ''
        PRINT '✗ ERROR al crear tabla FechasBloqueadas:'
        PRINT '  Mensaje: ' + ERROR_MESSAGE()
        PRINT '  Número: ' + CAST(ERROR_NUMBER() AS VARCHAR)
        PRINT '  Línea: ' + CAST(ERROR_LINE() AS VARCHAR)
        PRINT ''
    END CATCH
END

PRINT ''

-- Registrar migración en historial (si no existe)
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251103000000_AddFechasBloqueadas')
BEGIN
    BEGIN TRY
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20251103000000_AddFechasBloqueadas', '8.0.11');
        PRINT '✓ Migración 9 registrada en __EFMigrationsHistory'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al registrar migración: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Migración 9 ya estaba registrada'
END

PRINT ''

-- Verificación final
PRINT '========================================================================='
PRINT 'VERIFICACIÓN FINAL'
PRINT '========================================================================='
PRINT ''

-- Verificar que la tabla existe
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FechasBloqueadas')
BEGIN
    PRINT '✅ Tabla FechasBloqueadas: EXISTE'
    
    -- Contar registros
    DECLARE @Count INT
    SELECT @Count = COUNT(*) FROM FechasBloqueadas
    PRINT '   Fechas bloqueadas actuales: ' + CAST(@Count AS VARCHAR)
    
    -- Verificar índices
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FechasBloqueadas_Fecha')
        PRINT '✅ Índice IX_FechasBloqueadas_Fecha: OK'
    ELSE
        PRINT '⚠ Índice IX_FechasBloqueadas_Fecha: FALTA'
    
    IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FechasBloqueadas_UsuarioId')
        PRINT '✅ Índice IX_FechasBloqueadas_UsuarioId: OK'
    ELSE
        PRINT '⚠ Índice IX_FechasBloqueadas_UsuarioId: FALTA'
    
    -- Verificar foreign key
    IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_FechasBloqueadas_AspNetUsers_UsuarioId')
        PRINT '✅ Foreign Key a AspNetUsers: OK'
    ELSE
        PRINT '⚠ Foreign Key a AspNetUsers: FALTA'
END
ELSE
BEGIN
    PRINT '❌ Tabla FechasBloqueadas: NO EXISTE'
END

PRINT ''

-- Verificar migración registrada
IF EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20251103000000_AddFechasBloqueadas')
    PRINT '✅ Migración registrada en historial: OK'
ELSE
    PRINT '⚠ Migración NO registrada en historial'

PRINT ''
PRINT '========================================================================='
PRINT 'MIGRACIÓN 9 COMPLETADA'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''
PRINT '📌 FUNCIONALIDAD:'
PRINT '  • Admins pueden bloquear fechas específicas para turnos'
PRINT '  • Bloqueo individual o por rango (máx 30 días)'
PRINT '  • Sistema verifica fechas bloqueadas al asignar turnos'
PRINT '  • Útil para días festivos, emergencias, mantenimiento'
PRINT '  • Acceso desde: /FechasBloqueadas (solo Admin)'
PRINT ''
