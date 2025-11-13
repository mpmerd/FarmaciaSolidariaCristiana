-- =====================================================================================
-- SCRIPT DE MIGRACIÓN COMPLETO PARA SOMEE
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Última actualización: 13 de noviembre de 2025
-- 
-- INCLUYE TODAS LAS MIGRACIONES:
-- ✅ 20251023213325_AddPatientIdentificationRequired
-- ✅ 20251023225202_AddDeliveryFieldsEnhancement
-- ✅ 20251025212114_AddCreatedAtToDeliveries
-- ✅ 20251027160229_AddSuppliesTable
-- ✅ 20251027164041_AddSupplyToDeliveries
-- ✅ 20251027171452_AddSupplyToDonations
-- ✅ 20251028000000_AddTurnosSystem
-- ✅ 20251031224145_AddTurnoInsumos
-- ✅ 20251103000000_AddFechasBloqueadas
-- ✅ 20251104004321_AddTurnoIdToDeliveries
-- ✅ 20251113150644_AddNavbarDecorations
-- 
-- IMPORTANTE: Ejecutar en el panel SQL de Somee.com
-- =====================================================================================

PRINT '========================================================================='
PRINT 'INICIANDO MIGRACIONES COMPLETAS'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- MIGRACIÓN 1: AddPatientIdentificationRequired (23/10/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 1: Campo de Identificación Obligatorio...'
PRINT ''

-- Modificar tabla Patients: Hacer IdentificationDocument obligatorio
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Patients' 
           AND COLUMN_NAME = 'IdentificationDocument'
           AND IS_NULLABLE = 'YES')
BEGIN
    BEGIN TRY
        -- Actualizar registros existentes que tengan NULL
        UPDATE Patients
        SET IdentificationDocument = 'TEMP' + CAST(Id AS VARCHAR)
        WHERE IdentificationDocument IS NULL OR IdentificationDocument = '';
        
        -- Alterar la columna para hacerla NOT NULL
        ALTER TABLE Patients
        ALTER COLUMN IdentificationDocument nvarchar(20) NOT NULL;
        
        PRINT '✓ IdentificationDocument ahora es obligatorio'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ IdentificationDocument ya es obligatorio'
END

-- Agregar columna PatientIdentification a Deliveries
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Deliveries' 
               AND COLUMN_NAME = 'PatientIdentification')
BEGIN
    BEGIN TRY
        ALTER TABLE Deliveries
        ADD PatientIdentification nvarchar(20) NOT NULL DEFAULT '';
        
        -- Actualizar entregas existentes
        UPDATE d
        SET d.PatientIdentification = p.IdentificationDocument
        FROM Deliveries d
        INNER JOIN Patients p ON d.PatientId = p.Id
        WHERE d.PatientId IS NOT NULL;
        
        PRINT '✓ PatientIdentification agregada a Deliveries'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ PatientIdentification ya existe'
END

-- Registrar migración
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251023213325_AddPatientIdentificationRequired')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20251023213325_AddPatientIdentificationRequired', '8.0.11');
    PRINT '✓ Migración 1 registrada'
END

PRINT ''

-- =====================================================================================
-- MIGRACIÓN 2: AddDeliveryFieldsEnhancement (23/10/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 2: Mejoras en campos de Entregas...'
PRINT ''

-- Agregar LocationDetails
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Deliveries' 
               AND COLUMN_NAME = 'LocationDetails')
BEGIN
    BEGIN TRY
        ALTER TABLE Deliveries
        ADD LocationDetails nvarchar(500) NULL;
        PRINT '✓ LocationDetails agregada'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ LocationDetails ya existe'
END

-- Agregar Observations
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Deliveries' 
               AND COLUMN_NAME = 'Observations')
BEGIN
    BEGIN TRY
        ALTER TABLE Deliveries
        ADD Observations nvarchar(1000) NULL;
        PRINT '✓ Observations agregada'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Observations ya existe'
END

-- Registrar migración
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251023225202_AddDeliveryFieldsEnhancement')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20251023225202_AddDeliveryFieldsEnhancement', '8.0.11');
    PRINT '✓ Migración 2 registrada'
END

PRINT ''

-- =====================================================================================
-- MIGRACIÓN 3: AddCreatedAtToDeliveries (25/10/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 3: Campo CreatedAt para control de eliminación...'
PRINT ''

-- Agregar CreatedAt
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Deliveries' 
               AND COLUMN_NAME = 'CreatedAt')
BEGIN
    BEGIN TRY
        ALTER TABLE Deliveries ADD CreatedAt DATETIME2 NULL;
        PRINT '✓ CreatedAt agregada (nullable para preservar datos existentes)'
        PRINT '  ℹ Registros antiguos usarán DeliveryDate como referencia'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ CreatedAt ya existe'
END

-- Registrar migración
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251025212114_AddCreatedAtToDeliveries')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20251025212114_AddCreatedAtToDeliveries', '8.0.11');
    PRINT '✓ Migración 3 registrada'
END

PRINT ''

-- =====================================================================================
-- MIGRACIÓN 4: AddSuppliesTable (27/10/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 4: Tabla de Insumos...'
PRINT ''

-- Crear tabla Supplies
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Supplies')
BEGIN
    BEGIN TRY
        CREATE TABLE Supplies (
            Id INT PRIMARY KEY IDENTITY(1,1),
            Name NVARCHAR(MAX) NOT NULL,
            Description NVARCHAR(MAX) NULL,
            StockQuantity INT NOT NULL,
            Unit NVARCHAR(MAX) NOT NULL
        );
        PRINT '✓ Tabla Supplies creada exitosamente'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Tabla Supplies ya existe'
END

-- Registrar migración
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251027160229_AddSuppliesTable')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20251027160229_AddSuppliesTable', '8.0.11');
    PRINT '✓ Migración 4 registrada'
END

PRINT ''

-- =====================================================================================
-- MIGRACIÓN 5: AddSupplyToDeliveries (27/10/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 5: Entregas de Insumos...'
PRINT ''

-- Hacer MedicineId nullable (permitir NULL)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Deliveries' 
           AND COLUMN_NAME = 'MedicineId'
           AND IS_NULLABLE = 'NO')
BEGIN
    BEGIN TRY
        ALTER TABLE Deliveries
        ALTER COLUMN MedicineId INT NULL;
        PRINT '✓ MedicineId ahora acepta NULL'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al modificar MedicineId: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ MedicineId ya acepta NULL'
END

-- Agregar columna SupplyId
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Deliveries' 
               AND COLUMN_NAME = 'SupplyId')
BEGIN
    BEGIN TRY
        ALTER TABLE Deliveries
        ADD SupplyId INT NULL;
        PRINT '✓ SupplyId agregada'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al agregar SupplyId: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ SupplyId ya existe'
END

-- Crear índice en SupplyId
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Deliveries_SupplyId' 
               AND object_id = OBJECT_ID('Deliveries'))
BEGIN
    BEGIN TRY
        CREATE INDEX IX_Deliveries_SupplyId ON Deliveries(SupplyId);
        PRINT '✓ Índice IX_Deliveries_SupplyId creado'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear índice: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Índice IX_Deliveries_SupplyId ya existe'
END

-- Crear Foreign Key a Supplies
IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
               WHERE name = 'FK_Deliveries_Supplies_SupplyId')
BEGIN
    BEGIN TRY
        ALTER TABLE Deliveries
        ADD CONSTRAINT FK_Deliveries_Supplies_SupplyId
        FOREIGN KEY (SupplyId) REFERENCES Supplies(Id);
        PRINT '✓ Foreign Key FK_Deliveries_Supplies_SupplyId creada'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear Foreign Key: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Foreign Key FK_Deliveries_Supplies_SupplyId ya existe'
END

-- Registrar migración
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251027164041_AddSupplyToDeliveries')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20251027164041_AddSupplyToDeliveries', '8.0.11');
    PRINT '✓ Migración 5 registrada'
END

PRINT ''

-- =====================================================================================
-- MIGRACIÓN 6: AddSupplyToDonations (27/10/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 6: Donaciones de Insumos...'
PRINT ''

-- Hacer MedicineId nullable (permitir NULL)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Donations' 
           AND COLUMN_NAME = 'MedicineId'
           AND IS_NULLABLE = 'NO')
BEGIN
    BEGIN TRY
        ALTER TABLE Donations
        ALTER COLUMN MedicineId INT NULL;
        PRINT '✓ MedicineId ahora acepta NULL'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al modificar MedicineId: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ MedicineId ya acepta NULL'
END

-- Agregar columna SupplyId
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Donations' 
               AND COLUMN_NAME = 'SupplyId')
BEGIN
    BEGIN TRY
        ALTER TABLE Donations
        ADD SupplyId INT NULL;
        PRINT '✓ SupplyId agregada'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al agregar SupplyId: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ SupplyId ya existe'
END

-- Crear índice en SupplyId
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Donations_SupplyId' 
               AND object_id = OBJECT_ID('Donations'))
BEGIN
    BEGIN TRY
        CREATE INDEX IX_Donations_SupplyId ON Donations(SupplyId);
        PRINT '✓ Índice IX_Donations_SupplyId creado'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear índice: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Índice IX_Donations_SupplyId ya existe'
END

-- Crear Foreign Key a Supplies
IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
               WHERE name = 'FK_Donations_Supplies_SupplyId')
BEGIN
    BEGIN TRY
        ALTER TABLE Donations
        ADD CONSTRAINT FK_Donations_Supplies_SupplyId
        FOREIGN KEY (SupplyId) REFERENCES Supplies(Id);
        PRINT '✓ Foreign Key FK_Donations_Supplies_SupplyId creada'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear Foreign Key: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Foreign Key FK_Donations_Supplies_SupplyId ya existe'
END

-- Registrar migración
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251027171452_AddSupplyToDonations')
BEGIN
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20251027171452_AddSupplyToDonations', '8.0.11');
    PRINT '✓ Migración 6 registrada'
END

PRINT ''

-- =====================================================================================
-- VERIFICACIÓN FINAL
-- =====================================================================================

PRINT '========================================================================='
PRINT 'VERIFICACIÓN DE MIGRACIONES'
PRINT '========================================================================='
PRINT ''

-- Verificar migraciones registradas
PRINT 'Migraciones registradas:'
SELECT MigrationId, ProductVersion 
FROM __EFMigrationsHistory 
ORDER BY MigrationId;

PRINT ''

-- Verificar estructura de Patients
PRINT 'Estructura de Patients:'
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Patients'
AND COLUMN_NAME IN ('IdentificationDocument');

PRINT ''

-- Verificar estructura de Deliveries
PRINT 'Estructura de Deliveries (nuevas columnas):'
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Deliveries'
AND COLUMN_NAME IN ('PatientIdentification', 'LocationDetails', 'Observations', 'CreatedAt', 'MedicineId', 'SupplyId');

PRINT ''

-- Verificar estructura de Donations
PRINT 'Estructura de Donations (nuevas columnas):'
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Donations'
AND COLUMN_NAME IN ('MedicineId', 'SupplyId');

PRINT ''

-- =====================================================================================
-- MIGRACIÓN 8: AddTurnoInsumos (31/10/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 8: Soporte de Insumos en Turnos...'
PRINT ''

-- Verificar si la tabla TurnoInsumos ya existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TurnoInsumos')
BEGIN
    BEGIN TRY
        -- Crear tabla TurnoInsumos
        CREATE TABLE [TurnoInsumos] (
            [Id] int IDENTITY(1,1) NOT NULL,
            [TurnoId] int NOT NULL,
            [SupplyId] int NOT NULL,
            [CantidadSolicitada] int NOT NULL,
            [DisponibleAlSolicitar] bit NOT NULL,
            [CantidadAprobada] int NULL,
            [Notas] nvarchar(500) NULL,
            CONSTRAINT [PK_TurnoInsumos] PRIMARY KEY ([Id])
        );
        PRINT '✓ Tabla TurnoInsumos creada'
        
        -- Crear foreign key hacia Supplies (RESTRICT)
        ALTER TABLE [TurnoInsumos] ADD CONSTRAINT [FK_TurnoInsumos_Supplies_SupplyId] 
            FOREIGN KEY ([SupplyId]) REFERENCES [Supplies] ([Id]);
        PRINT '✓ Foreign key TurnoInsumos -> Supplies creada (RESTRICT)'
        
        -- Crear foreign key hacia Turnos (CASCADE)
        ALTER TABLE [TurnoInsumos] ADD CONSTRAINT [FK_TurnoInsumos_Turnos_TurnoId] 
            FOREIGN KEY ([TurnoId]) REFERENCES [Turnos] ([Id]) ON DELETE CASCADE;
        PRINT '✓ Foreign key TurnoInsumos -> Turnos creada (CASCADE)'
        
        -- Crear índice en SupplyId
        CREATE INDEX [IX_TurnoInsumos_SupplyId] ON [TurnoInsumos] ([SupplyId]);
        PRINT '✓ Índice IX_TurnoInsumos_SupplyId creado'
        
        -- Crear índice en TurnoId
        CREATE INDEX [IX_TurnoInsumos_TurnoId] ON [TurnoInsumos] ([TurnoId]);
        PRINT '✓ Índice IX_TurnoInsumos_TurnoId creado'
        
        PRINT ''
        PRINT '✅ Migración AddTurnoInsumos completada exitosamente'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR en AddTurnoInsumos: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '⚠ Tabla TurnoInsumos ya existe, omitiendo migración'
END

PRINT ''

-- =====================================================================================
-- MIGRACIÓN 9: AddFechasBloqueadas (03/11/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 9: Sistema de Bloqueo de Fechas...'
PRINT ''

-- Verificar si la tabla FechasBloqueadas ya existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FechasBloqueadas')
BEGIN
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
        PRINT '✅ Migración AddFechasBloqueadas completada exitosamente'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR en AddFechasBloqueadas: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '⚠ Tabla FechasBloqueadas ya existe, omitiendo creación'
END

-- Registrar migración (SIEMPRE, independientemente de si la tabla ya existía)
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
    PRINT '✓ Migración 9 ya estaba registrada en historial'
END

PRINT ''

-- =====================================================================================
-- MIGRACIÓN 10: AddTurnoIdToDeliveries (04/11/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 10: Relación TurnoId en Deliveries...'
PRINT ''

-- Agregar columna TurnoId a Deliveries
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Deliveries' 
               AND COLUMN_NAME = 'TurnoId')
BEGIN
    BEGIN TRY
        ALTER TABLE [Deliveries] 
        ADD [TurnoId] int NULL;
        PRINT '✓ TurnoId agregada a Deliveries (nullable)'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al agregar TurnoId: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ TurnoId ya existe en Deliveries'
END

-- Crear índice en TurnoId
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_Deliveries_TurnoId' 
               AND object_id = OBJECT_ID('Deliveries'))
BEGIN
    BEGIN TRY
        CREATE NONCLUSTERED INDEX [IX_Deliveries_TurnoId] 
            ON [Deliveries]([TurnoId] ASC);
        PRINT '✓ Índice IX_Deliveries_TurnoId creado'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear índice: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Índice IX_Deliveries_TurnoId ya existe'
END

-- Crear Foreign Key a Turnos (RESTRICT)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
               WHERE name = 'FK_Deliveries_Turnos_TurnoId')
BEGIN
    BEGIN TRY
        ALTER TABLE [Deliveries] 
        ADD CONSTRAINT [FK_Deliveries_Turnos_TurnoId] 
        FOREIGN KEY ([TurnoId]) REFERENCES [Turnos] ([Id]);
        PRINT '✓ Foreign key Deliveries -> Turnos creada (RESTRICT)'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al crear Foreign Key: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Foreign Key FK_Deliveries_Turnos_TurnoId ya existe'
END

-- Registrar migración
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251104004321_AddTurnoIdToDeliveries')
BEGIN
    BEGIN TRY
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20251104004321_AddTurnoIdToDeliveries', '8.0.11');
        PRINT '✓ Migración 10 registrada en __EFMigrationsHistory'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al registrar migración: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Migración 10 ya estaba registrada en historial'
END

PRINT ''
PRINT '✅ Migración AddTurnoIdToDeliveries completada exitosamente'
PRINT ''

-- =====================================================================================
-- MIGRACIÓN 11: AddNavbarDecorations (13/11/2025)
-- =====================================================================================

PRINT '-- MIGRACIÓN 11: Sistema de decoraciones del navbar...'
PRINT ''

-- Verificar si la tabla NavbarDecorations ya existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NavbarDecorations')
BEGIN
    BEGIN TRY
        -- Crear tabla NavbarDecorations
        CREATE TABLE [NavbarDecorations] (
            [Id] int IDENTITY(1,1) NOT NULL,
            [Name] nvarchar(max) NOT NULL,
            [Type] int NOT NULL,
            [PresetKey] nvarchar(max) NULL,
            [DisplayText] nvarchar(max) NULL,
            [TextColor] nvarchar(max) NULL,
            [CustomIconPath] nvarchar(max) NULL,
            [IconClass] nvarchar(max) NULL,
            [IconColor] nvarchar(max) NULL,
            [IsActive] bit NOT NULL DEFAULT 0,
            [ActivatedAt] datetime2 NULL,
            [ActivatedBy] nvarchar(max) NULL,
            [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
            CONSTRAINT [PK_NavbarDecorations] PRIMARY KEY ([Id])
        );
        PRINT '✓ Tabla NavbarDecorations creada'
        
        -- Crear índice en IsActive para consultas rápidas
        CREATE NONCLUSTERED INDEX [IX_NavbarDecorations_IsActive] 
            ON [NavbarDecorations]([IsActive] ASC);
        PRINT '✓ Índice IX_NavbarDecorations_IsActive creado'
        
        PRINT ''
        PRINT '✅ Migración AddNavbarDecorations completada exitosamente'
        PRINT ''
        PRINT '📝 Nueva funcionalidad habilitada:'
        PRINT '  • Decoraciones predefinidas: Navidad, Epifanía, Semana Santa, Aldersgate, Pentecostés'
        PRINT '  • Decoraciones personalizadas con iconos propios'
        PRINT '  • Actualización dinámica sin reiniciar aplicación'
        PRINT '  • Solo una decoración activa a la vez'
        PRINT ''
        PRINT '🎨 Administradores: Avanzado > Decoraciones del Navbar'
        
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR en AddNavbarDecorations: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '⚠ Tabla NavbarDecorations ya existe, omitiendo creación'
END

-- Registrar migración
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId = '20251113150644_AddNavbarDecorations')
BEGIN
    BEGIN TRY
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20251113150644_AddNavbarDecorations', '8.0.11');
        PRINT '✓ Migración 11 registrada en __EFMigrationsHistory'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR al registrar migración: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '✓ Migración 11 ya estaba registrada en historial'
END

PRINT ''

-- =====================================================================================
-- VERIFICACIONES Y ESTADÍSTICAS FINALES
-- =====================================================================================

-- Estadísticas de datos
PRINT 'Estadísticas:'

-- Estadísticas básicas (sin columnas que pueden no existir)
SELECT 
    (SELECT COUNT(*) FROM Patients) AS TotalPacientes,
    (SELECT COUNT(*) FROM Medicines) AS TotalMedicamentos,
    (SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Supplies') 
                 THEN (SELECT COUNT(*) FROM Supplies) 
                 ELSE 0 END) AS TotalInsumos,
    (SELECT COUNT(*) FROM Sponsors) AS TotalPatrocinadores,
    (SELECT COUNT(*) FROM Deliveries) AS TotalEntregas,
    (SELECT COUNT(*) FROM Deliveries WHERE CreatedAt IS NOT NULL) AS EntregasConCreatedAt,
    (SELECT COUNT(*) FROM Deliveries WHERE CreatedAt IS NULL) AS EntregasAntiguasSinCreatedAt,
    (SELECT COUNT(*) FROM Deliveries WHERE MedicineId IS NOT NULL) AS EntregasMedicamentos,
    (SELECT COUNT(*) FROM Donations) AS TotalDonaciones,
    (SELECT COUNT(*) FROM Donations WHERE MedicineId IS NOT NULL) AS DonacionesMedicamentos,
    (SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Turnos') 
                 THEN (SELECT COUNT(*) FROM Turnos) 
                 ELSE 0 END) AS TotalTurnos,
    (SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TurnoMedicamentos') 
                 THEN (SELECT COUNT(*) FROM TurnoMedicamentos) 
                 ELSE 0 END) AS TotalTurnoMedicamentos,
    (SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TurnoInsumos') 
                 THEN (SELECT COUNT(*) FROM TurnoInsumos) 
                 ELSE 0 END) AS TotalTurnoInsumos,
    (SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FechasBloqueadas') 
                 THEN (SELECT COUNT(*) FROM FechasBloqueadas) 
                 ELSE 0 END) AS TotalFechasBloqueadas,
    (SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NavbarDecorations') 
                 THEN (SELECT COUNT(*) FROM NavbarDecorations) 
                 ELSE 0 END) AS TotalDecoraciones,
    (SELECT CASE WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'NavbarDecorations') 
                 THEN (SELECT COUNT(*) FROM NavbarDecorations WHERE IsActive = 1) 
                 ELSE 0 END) AS DecoracionesActivas;

-- Estadísticas adicionales solo si las columnas existen (SQL dinámico)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Deliveries' AND COLUMN_NAME = 'SupplyId')
BEGIN
    DECLARE @SqlDeliveries NVARCHAR(MAX) = 'SELECT COUNT(*) AS EntregasInsumos FROM Deliveries WHERE SupplyId IS NOT NULL';
    EXEC sp_executesql @SqlDeliveries;
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Donations' AND COLUMN_NAME = 'SupplyId')
BEGIN
    DECLARE @SqlDonations NVARCHAR(MAX) = 'SELECT COUNT(*) AS DonacionesInsumos FROM Donations WHERE SupplyId IS NOT NULL';
    EXEC sp_executesql @SqlDonations;
END

PRINT ''
PRINT '========================================================================='
PRINT 'TODAS LAS MIGRACIONES COMPLETADAS EXITOSAMENTE'
PRINT '========================================================================='
PRINT ''
PRINT '✅ CAMBIOS APLICADOS:'
PRINT '  • Identificación de paciente obligatoria'
PRINT '  • Campos mejorados en entregas (LocationDetails, Observations)'
PRINT '  • Control de tiempo para eliminar entregas (CreatedAt)'
PRINT '  • Nueva tabla Supplies para gestión de insumos'
PRINT '  • Entregas ahora soportan medicamentos E insumos'
PRINT '  • Donaciones ahora soportan medicamentos E insumos'
PRINT '  • Sistema de Turnos implementado (Martes/Jueves 1-4 PM)'
PRINT '  • Turnos ahora soportan medicamentos E insumos médicos'
PRINT '  • Sistema de Bloqueo de Fechas para días sin turnos'
PRINT '  • Entregas vinculadas a Turnos (TurnoId) para mejor trazabilidad'
PRINT '  • Sistema de Decoraciones del Navbar para festividades cristianas'
PRINT ''
PRINT '📌 IMPORTANTE:'
PRINT '  • Entregas antiguas tienen CreatedAt = NULL (usan DeliveryDate)'
PRINT '  • Solo se pueden eliminar entregas dentro de 2 horas de creación'
PRINT '  • Medicamentos con entregas/donaciones NO se pueden eliminar'
PRINT '  • Insumos con entregas/donaciones NO se pueden eliminar'
PRINT '  • Pacientes con entregas NO se pueden eliminar'
PRINT '  • Datos de producción preservados: Medicamentos, Usuarios, Patrocinadores'
PRINT '  • Entregas existentes mantienen sus medicamentos (MedicineId)'
PRINT '  • Donaciones existentes mantienen sus medicamentos (MedicineId)'
PRINT '  • Nuevas entregas/donaciones pueden ser de medicamentos O insumos'
PRINT '  • Turnos permiten solicitar medicamentos O insumos (no ambos a la vez)'
PRINT '  • Límite: 30 turnos por día, horario Martes/Jueves 1-4 PM (slots de 6 min)'
PRINT '  • Fechas bloqueadas impiden solicitar turnos (días festivos, emergencias)'
PRINT '  • Admins pueden bloquear fechas individuales o rangos de hasta 30 días'
PRINT '  • Múltiples entregas por turno: se puede registrar varios items de un turno'
PRINT '  • Eliminación inteligente: turno vuelve a Pendiente solo cuando se eliminan TODAS sus entregas'
PRINT '  • Decoraciones del navbar: 5 festividades predefinidas + opción personalizada'
PRINT '  • Actualización dinámica: decoraciones se aplican sin reiniciar la aplicación'
PRINT ''
PRINT 'Finalizado: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
