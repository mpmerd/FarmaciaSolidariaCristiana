-- =====================================================================================
-- SCRIPT DE MIGRACIÓN: SISTEMA DE TURNOS PARA SOMEE
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Fecha: 31 de octubre de 2025
-- 
-- MIGRACIÓN:
-- ✅ 20251031141709_AddTurnosSystem
-- 
-- IMPORTANTE: Ejecutar en el panel SQL de Somee.com
-- PREREQUISITO: Las 6 migraciones anteriores deben estar aplicadas
-- =====================================================================================

PRINT '========================================================================='
PRINT 'MIGRACIÓN: SISTEMA DE TURNOS'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- MIGRACIÓN 7: AddTurnosSystem (31/10/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 7: Sistema de Turnos...'
PRINT ''

-- Crear tabla Turnos
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Turnos')
BEGIN
    BEGIN TRY
        CREATE TABLE Turnos (
            Id INT PRIMARY KEY IDENTITY(1,1),
            UserId NVARCHAR(450) NOT NULL,
            DocumentoIdentidadHash NVARCHAR(100) NOT NULL,
            FechaPreferida DATETIME2 NOT NULL,
            FechaSolicitud DATETIME2 NOT NULL,
            Estado NVARCHAR(20) NOT NULL,
            RecetaMedicaPath NVARCHAR(500) NULL,
            TarjetonPath NVARCHAR(500) NULL,
            NotasSolicitante NVARCHAR(1000) NULL,
            ComentariosFarmaceutico NVARCHAR(1000) NULL,
            RevisadoPorId NVARCHAR(450) NULL,
            FechaRevision DATETIME2 NULL,
            FechaEntrega DATETIME2 NULL,
            TurnoPdfPath NVARCHAR(500) NULL,
            NumeroTurno INT NULL,
            EmailEnviado BIT NOT NULL DEFAULT 0,
            
            -- Foreign Keys
            CONSTRAINT FK_Turnos_AspNetUsers_UserId 
                FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE NO ACTION,
            CONSTRAINT FK_Turnos_AspNetUsers_RevisadoPorId 
                FOREIGN KEY (RevisadoPorId) REFERENCES AspNetUsers(Id) ON DELETE NO ACTION
        );
        
        PRINT '✓ Tabla Turnos creada exitosamente'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear tabla Turnos: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Tabla Turnos ya existe'
END

-- Crear tabla TurnoMedicamentos
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TurnoMedicamentos')
BEGIN
    BEGIN TRY
        CREATE TABLE TurnoMedicamentos (
            Id INT PRIMARY KEY IDENTITY(1,1),
            TurnoId INT NOT NULL,
            MedicineId INT NOT NULL,
            CantidadSolicitada INT NOT NULL,
            DisponibleAlSolicitar BIT NOT NULL,
            CantidadAprobada INT NULL,
            Notas NVARCHAR(500) NULL,
            
            -- Foreign Keys
            CONSTRAINT FK_TurnoMedicamentos_Turnos_TurnoId 
                FOREIGN KEY (TurnoId) REFERENCES Turnos(Id) ON DELETE CASCADE,
            CONSTRAINT FK_TurnoMedicamentos_Medicines_MedicineId 
                FOREIGN KEY (MedicineId) REFERENCES Medicines(Id) ON DELETE NO ACTION
        );
        
        PRINT '✓ Tabla TurnoMedicamentos creada exitosamente'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear tabla TurnoMedicamentos: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Tabla TurnoMedicamentos ya existe'
END

-- Crear índices en Turnos
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Turnos_UserId' 
               AND object_id = OBJECT_ID('Turnos'))
BEGIN
    BEGIN TRY
        CREATE INDEX IX_Turnos_UserId ON Turnos(UserId);
        PRINT '✓ Índice IX_Turnos_UserId creado'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear índice IX_Turnos_UserId: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Índice IX_Turnos_UserId ya existe'
END

IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Turnos_RevisadoPorId' 
               AND object_id = OBJECT_ID('Turnos'))
BEGIN
    BEGIN TRY
        CREATE INDEX IX_Turnos_RevisadoPorId ON Turnos(RevisadoPorId);
        PRINT '✓ Índice IX_Turnos_RevisadoPorId creado'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear índice IX_Turnos_RevisadoPorId: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Índice IX_Turnos_RevisadoPorId ya existe'
END

-- Crear índices en TurnoMedicamentos
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_TurnoMedicamentos_TurnoId' 
               AND object_id = OBJECT_ID('TurnoMedicamentos'))
BEGIN
    BEGIN TRY
        CREATE INDEX IX_TurnoMedicamentos_TurnoId ON TurnoMedicamentos(TurnoId);
        PRINT '✓ Índice IX_TurnoMedicamentos_TurnoId creado'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear índice IX_TurnoMedicamentos_TurnoId: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Índice IX_TurnoMedicamentos_TurnoId ya existe'
END

IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_TurnoMedicamentos_MedicineId' 
               AND object_id = OBJECT_ID('TurnoMedicamentos'))
BEGIN
    BEGIN TRY
        CREATE INDEX IX_TurnoMedicamentos_MedicineId ON TurnoMedicamentos(MedicineId);
        PRINT '✓ Índice IX_TurnoMedicamentos_MedicineId creado'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear índice IX_TurnoMedicamentos_MedicineId: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Índice IX_TurnoMedicamentos_MedicineId ya existe'
END

-- Registrar migración
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251031141709_AddTurnosSystem')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20251031141709_AddTurnosSystem', '8.0.11');
    PRINT '✓ Migración 7 registrada'
END
ELSE
BEGIN
    PRINT '✓ Migración 7 ya estaba registrada'
END

PRINT ''

-- =====================================================================================
-- VERIFICACIÓN FINAL
-- =====================================================================================

PRINT '========================================================================='
PRINT 'VERIFICACIÓN DE MIGRACIÓN'
PRINT '========================================================================='
PRINT ''

-- Verificar migración registrada
PRINT 'Migración Sistema de Turnos:'
SELECT MigrationId, ProductVersion 
FROM __EFMigrationsHistory 
WHERE MigrationId = '20251031141709_AddTurnosSystem';

PRINT ''

-- Verificar estructura de Turnos
PRINT 'Estructura de tabla Turnos:'
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Turnos'
ORDER BY ORDINAL_POSITION;

PRINT ''

-- Verificar estructura de TurnoMedicamentos
PRINT 'Estructura de tabla TurnoMedicamentos:'
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'TurnoMedicamentos'
ORDER BY ORDINAL_POSITION;

PRINT ''

-- Verificar Foreign Keys
PRINT 'Foreign Keys creadas:'
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.name LIKE '%Turno%'
ORDER BY TableName, ForeignKeyName;

PRINT ''

-- Verificar Índices
PRINT 'Índices creados:'
SELECT 
    i.name AS IndexName,
    OBJECT_NAME(i.object_id) AS TableName,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE OBJECT_NAME(i.object_id) IN ('Turnos', 'TurnoMedicamentos')
AND i.name IS NOT NULL
ORDER BY TableName, IndexName;

PRINT ''

-- Estadísticas
PRINT 'Estadísticas iniciales:'
SELECT 
    (SELECT COUNT(*) FROM Turnos) AS TotalTurnos,
    (SELECT COUNT(*) FROM TurnoMedicamentos) AS TotalTurnoMedicamentos;

PRINT ''
PRINT '========================================================================='
PRINT 'MIGRACIÓN SISTEMA DE TURNOS COMPLETADA EXITOSAMENTE'
PRINT '========================================================================='
PRINT ''
PRINT '✅ TABLAS CREADAS:'
PRINT '  • Turnos (16 columnas)'
PRINT '  • TurnoMedicamentos (7 columnas)'
PRINT ''
PRINT '✅ FOREIGN KEYS:'
PRINT '  • Turnos → AspNetUsers (UserId)'
PRINT '  • Turnos → AspNetUsers (RevisadoPorId)'
PRINT '  • TurnoMedicamentos → Turnos (TurnoId) CASCADE DELETE'
PRINT '  • TurnoMedicamentos → Medicines (MedicineId)'
PRINT ''
PRINT '✅ ÍNDICES:'
PRINT '  • IX_Turnos_UserId'
PRINT '  • IX_Turnos_RevisadoPorId'
PRINT '  • IX_TurnoMedicamentos_TurnoId'
PRINT '  • IX_TurnoMedicamentos_MedicineId'
PRINT ''
PRINT '📌 PRÓXIMOS PASOS:'
PRINT '  1. Ejecutar seed-turnos-test-data.sql (opcional, solo para testing)'
PRINT '  2. Crear directorios en servidor:'
PRINT '     - wwwroot/uploads/turnos/'
PRINT '     - wwwroot/pdfs/turnos/'
PRINT '  3. Subir logos:'
PRINT '     - wwwroot/images/logo-iglesia.png'
PRINT '     - wwwroot/images/logo-adriano.png'
PRINT '  4. Deploy de la aplicación con ./deploy-to-somee.sh'
PRINT ''
PRINT 'Finalizado: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
