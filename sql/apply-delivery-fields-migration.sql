-- =====================================================================================
-- SCRIPT DE MIGRACIÓN: Agregar Campos de Entrega Mejorados
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Fecha: 23 de octubre de 2025
-- 
-- IMPORTANTE: Este script agrega los campos:
--   - Dosage (Dosis)
--   - TreatmentDuration (Duración del Tratamiento)
--   - DeliveredBy (Entregado Por)
-- a la tabla Deliveries
--
-- Ejecuta este script en el panel de Somee.com DESPUÉS del script anterior
-- =====================================================================================

PRINT '========================================================================='
PRINT 'INICIANDO MIGRACIÓN - Campos de Entrega Mejorados'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- VERIFICAR SI LOS CAMPOS YA EXISTEN
-- =====================================================================================

PRINT '-- Verificando existencia de campos...'
PRINT ''

DECLARE @DosageExists BIT = 0
DECLARE @TreatmentDurationExists BIT = 0
DECLARE @DeliveredByExists BIT = 0

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Deliveries' AND COLUMN_NAME = 'Dosage')
    SET @DosageExists = 1

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Deliveries' AND COLUMN_NAME = 'TreatmentDuration')
    SET @TreatmentDurationExists = 1

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Deliveries' AND COLUMN_NAME = 'DeliveredBy')
    SET @DeliveredByExists = 1

PRINT 'Dosage existe: ' + CAST(@DosageExists AS VARCHAR)
PRINT 'TreatmentDuration existe: ' + CAST(@TreatmentDurationExists AS VARCHAR)
PRINT 'DeliveredBy existe: ' + CAST(@DeliveredByExists AS VARCHAR)
PRINT ''

-- =====================================================================================
-- AGREGAR CAMPO: Dosage (Dosis)
-- =====================================================================================

IF @DosageExists = 0
BEGIN
    PRINT '-- Agregando campo Dosage...'
    BEGIN TRY
        ALTER TABLE Deliveries
        ADD Dosage nvarchar(100) NULL;
        
        PRINT '✓ Campo Dosage agregado correctamente'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR agregando Dosage: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '⚠️ Campo Dosage ya existe'
END

PRINT ''

-- =====================================================================================
-- AGREGAR CAMPO: TreatmentDuration (Duración del Tratamiento)
-- =====================================================================================

IF @TreatmentDurationExists = 0
BEGIN
    PRINT '-- Agregando campo TreatmentDuration...'
    BEGIN TRY
        ALTER TABLE Deliveries
        ADD TreatmentDuration nvarchar(100) NULL;
        
        PRINT '✓ Campo TreatmentDuration agregado correctamente'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR agregando TreatmentDuration: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '⚠️ Campo TreatmentDuration ya existe'
END

PRINT ''

-- =====================================================================================
-- AGREGAR CAMPO: DeliveredBy (Entregado Por)
-- =====================================================================================

IF @DeliveredByExists = 0
BEGIN
    PRINT '-- Agregando campo DeliveredBy...'
    BEGIN TRY
        ALTER TABLE Deliveries
        ADD DeliveredBy nvarchar(200) NULL;
        
        PRINT '✓ Campo DeliveredBy agregado correctamente'
    END TRY
    BEGIN CATCH
        PRINT '✗ ERROR agregando DeliveredBy: ' + ERROR_MESSAGE()
    END CATCH
END
ELSE
BEGIN
    PRINT '⚠️ Campo DeliveredBy ya existe'
END

PRINT ''



-- =====================================================================================
-- ACTUALIZAR TABLA DE MIGRACIONES
-- =====================================================================================

PRINT '-- Actualizando tabla de migraciones...'

BEGIN TRY
    IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory WHERE MigrationId = '20251023225202_AddDeliveryFieldsEnhancement')
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20251023225202_AddDeliveryFieldsEnhancement', '8.0.11');
        
        PRINT '✓ Migración registrada en __EFMigrationsHistory'
    END
    ELSE
    BEGIN
        PRINT '⚠️ Migración ya estaba registrada'
    END
END TRY
BEGIN CATCH
    PRINT '✗ ERROR actualizando __EFMigrationsHistory: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- =====================================================================================
-- RESUMEN FINAL
-- =====================================================================================

PRINT '========================================================================='
PRINT 'MIGRACIÓN COMPLETADA EXITOSAMENTE'
PRINT '========================================================================='
PRINT ''
PRINT 'Campos agregados a la tabla Deliveries:'
PRINT '  - Dosage (nvarchar(100)): Para registrar la dosis del medicamento'
PRINT '  - TreatmentDuration (nvarchar(100)): Para registrar la duración del tratamiento'
PRINT '  - DeliveredBy (nvarchar(200)): Para registrar quién hizo la entrega'
PRINT ''
PRINT '✅ Sistema actualizado y listo para usar'
PRINT ''
PRINT 'Ahora puedes:'
PRINT '  1. Registrar dosis y duración en las entregas'
PRINT '  2. Ver historial completo con estos datos'
PRINT '  3. Saber quién entregó cada medicamento'
PRINT ''
PRINT '========================================================================='
