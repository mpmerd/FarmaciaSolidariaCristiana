-- =====================================================================================
-- SCRIPT DE MIGRACIÓN COMPLETO PARA SOMEE
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Fecha: 27 de octubre de 2025
-- 
-- INCLUYE TODAS LAS MIGRACIONES:
-- ✅ 20251023213325_AddPatientIdentificationRequired
-- ✅ 20251023225202_AddDeliveryFieldsEnhancement
-- ✅ 20251025212114_AddCreatedAtToDeliveries
-- ✅ 20251027160229_AddSuppliesTable
-- ✅ 20251027164041_AddSupplyToDeliveries
-- ✅ 20251027171452_AddSupplyToDonations
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
    (SELECT COUNT(*) FROM Donations WHERE MedicineId IS NOT NULL) AS DonacionesMedicamentos;

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
PRINT ''
PRINT 'Finalizado: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
