-- =====================================================================================
-- LIMPIAR DATOS DE PRUEBA - PRESERVA MEDICAMENTOS, USUARIOS Y PATROCINADORES
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Fecha: 25 de octubre de 2025
-- 
-- ⚠️ ADVERTENCIA: Este script ELIMINARÁ:
--    - Pacientes de prueba
--    - Entregas de prueba
--    - Donaciones de prueba
--    - Documentos de pacientes
-- 
-- ✅ PRESERVARÁ: 
--    - TODOS los medicamentos (datos reales)
--    - Todos los usuarios registrados
--    - Todos los patrocinadores
--    - Roles y configuración de Identity
-- 
-- IMPORTANTE: Ejecutar en el panel SQL de Somee.com
-- =====================================================================================

PRINT '========================================================================='
PRINT 'LIMPIEZA DE DATOS DE PRUEBA'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- PARTE 1: CONTAR DATOS ACTUALES
-- =====================================================================================

DECLARE @PatientCount INT
DECLARE @DeliveryCount INT
DECLARE @DonationCount INT
DECLARE @DocumentCount INT
DECLARE @MedicineCount INT
DECLARE @UserCount INT

SELECT @PatientCount = COUNT(*) FROM Patients
SELECT @DeliveryCount = COUNT(*) FROM Deliveries
SELECT @DonationCount = COUNT(*) FROM Donations
SELECT @DocumentCount = COUNT(*) FROM PatientDocuments
SELECT @MedicineCount = COUNT(*) FROM Medicines
SELECT @UserCount = COUNT(*) FROM AspNetUsers

PRINT 'DATOS ACTUALES:'
PRINT '  • Pacientes: ' + CAST(@PatientCount AS VARCHAR)
PRINT '  • Entregas: ' + CAST(@DeliveryCount AS VARCHAR)
PRINT '  • Donaciones: ' + CAST(@DonationCount AS VARCHAR)
PRINT '  • Documentos: ' + CAST(@DocumentCount AS VARCHAR)
PRINT '  • Medicamentos: ' + CAST(@MedicineCount AS VARCHAR) + ' (NO se eliminarán)'
PRINT '  • Usuarios: ' + CAST(@UserCount AS VARCHAR) + ' (NO se eliminarán)'
PRINT ''

-- Confirmación de seguridad
IF @MedicineCount = 0
BEGIN
    PRINT '⚠️ ADVERTENCIA: No hay medicamentos en la base de datos'
    PRINT '   Si continúas, la aplicación quedará sin medicamentos'
    PRINT ''
END

-- =====================================================================================
-- PARTE 2: ELIMINAR DATOS DE PRUEBA
-- =====================================================================================

PRINT '-- PARTE 2: Eliminando datos de prueba...'
PRINT ''

BEGIN TRY
    -- Paso 1: Eliminar documentos de pacientes
    IF EXISTS (SELECT 1 FROM PatientDocuments)
    BEGIN
        DELETE FROM PatientDocuments
        PRINT '  ✓ Documentos de pacientes eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    ELSE
    BEGIN
        PRINT '  ℹ No hay documentos de pacientes'
    END

    -- Paso 2: Eliminar entregas (libera el stock)
    IF EXISTS (SELECT 1 FROM Deliveries)
    BEGIN
        -- Restaurar stock de medicamentos antes de eliminar entregas
        UPDATE m
        SET m.StockQuantity = m.StockQuantity + d.Quantity
        FROM Medicines m
        INNER JOIN Deliveries d ON m.Id = d.MedicineId
        
        DECLARE @RestoredStock INT = @@ROWCOUNT
        
        DELETE FROM Deliveries
        PRINT '  ✓ Entregas eliminadas: ' + CAST(@@ROWCOUNT AS VARCHAR)
        PRINT '    (Stock restaurado en ' + CAST(@RestoredStock AS VARCHAR) + ' medicamentos)'
    END
    ELSE
    BEGIN
        PRINT '  ℹ No hay entregas'
    END

    -- Paso 3: Eliminar donaciones (descuenta del stock)
    IF EXISTS (SELECT 1 FROM Donations)
    BEGIN
        -- Descontar stock de medicamentos antes de eliminar donaciones
        UPDATE m
        SET m.StockQuantity = m.StockQuantity - d.Quantity
        FROM Medicines m
        INNER JOIN Donations d ON m.Id = d.MedicineId
        WHERE m.StockQuantity >= d.Quantity  -- Solo si hay suficiente stock
        
        DECLARE @AdjustedStock INT = @@ROWCOUNT
        
        DELETE FROM Donations
        PRINT '  ✓ Donaciones eliminadas: ' + CAST(@@ROWCOUNT AS VARCHAR)
        PRINT '    (Stock ajustado en ' + CAST(@AdjustedStock AS VARCHAR) + ' medicamentos)'
    END
    ELSE
    BEGIN
        PRINT '  ℹ No hay donaciones'
    END

    -- Paso 4: Eliminar pacientes
    IF EXISTS (SELECT 1 FROM Patients)
    BEGIN
        DELETE FROM Patients
        PRINT '  ✓ Pacientes eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    ELSE
    BEGIN
        PRINT '  ℹ No hay pacientes'
    END

    PRINT ''
    PRINT '  ✓ Limpieza completada exitosamente'
    
END TRY
BEGIN CATCH
    PRINT ''
    PRINT '  ✗ ERROR durante la limpieza:'
    PRINT '    ' + ERROR_MESSAGE()
    PRINT ''
END CATCH

-- =====================================================================================
-- PARTE 3: RESETEAR CONTADORES (OPCIONAL)
-- =====================================================================================

PRINT ''
PRINT '-- PARTE 3: Reseteando contadores...'
PRINT ''

BEGIN TRY
    -- Solo resetear si las tablas están vacías
    IF NOT EXISTS (SELECT 1 FROM Patients)
    BEGIN
        DBCC CHECKIDENT ('Patients', RESEED, 0)
        PRINT '  ✓ Contador de Patients reiniciado'
    END
    
    IF NOT EXISTS (SELECT 1 FROM Deliveries)
    BEGIN
        DBCC CHECKIDENT ('Deliveries', RESEED, 0)
        PRINT '  ✓ Contador de Deliveries reiniciado'
    END
    
    IF NOT EXISTS (SELECT 1 FROM Donations)
    BEGIN
        DBCC CHECKIDENT ('Donations', RESEED, 0)
        PRINT '  ✓ Contador de Donations reiniciado'
    END
    
    IF NOT EXISTS (SELECT 1 FROM PatientDocuments)
    BEGIN
        DBCC CHECKIDENT ('PatientDocuments', RESEED, 0)
        PRINT '  ✓ Contador de PatientDocuments reiniciado'
    END
    
    -- NO resetear Medicines porque tiene datos reales
    
END TRY
BEGIN CATCH
    PRINT '  ⚠️ No se pudieron resetear algunos contadores'
    PRINT '    ' + ERROR_MESSAGE()
END CATCH

-- =====================================================================================
-- PARTE 4: VERIFICACIÓN FINAL
-- =====================================================================================

PRINT ''
PRINT '========================================================================='
PRINT 'VERIFICACIÓN FINAL'
PRINT '========================================================================='
PRINT ''

DECLARE @FinalPatients INT
DECLARE @FinalDeliveries INT
DECLARE @FinalDonations INT
DECLARE @FinalDocuments INT
DECLARE @FinalMedicines INT
DECLARE @FinalUsers INT

SELECT @FinalPatients = COUNT(*) FROM Patients
SELECT @FinalDeliveries = COUNT(*) FROM Deliveries
SELECT @FinalDonations = COUNT(*) FROM Donations
SELECT @FinalDocuments = COUNT(*) FROM PatientDocuments
SELECT @FinalMedicines = COUNT(*) FROM Medicines
SELECT @FinalUsers = COUNT(*) FROM AspNetUsers

PRINT 'DATOS DESPUÉS DE LA LIMPIEZA:'
PRINT ''
PRINT 'ELIMINADOS (Datos de prueba):'
PRINT '  • Pacientes: ' + CAST(@FinalPatients AS VARCHAR) + ' (debería ser 0)'
PRINT '  • Entregas: ' + CAST(@FinalDeliveries AS VARCHAR) + ' (debería ser 0)'
PRINT '  • Donaciones: ' + CAST(@FinalDonations AS VARCHAR) + ' (debería ser 0)'
PRINT '  • Documentos: ' + CAST(@FinalDocuments AS VARCHAR) + ' (debería ser 0)'
PRINT ''
PRINT 'PRESERVADOS (Datos reales):'
PRINT '  • Medicamentos: ' + CAST(@FinalMedicines AS VARCHAR)
PRINT '  • Usuarios: ' + CAST(@FinalUsers AS VARCHAR)
PRINT ''

-- Mostrar estado del stock de medicamentos
PRINT 'STOCK DE MEDICAMENTOS:'
SELECT 
    Name AS Medicamento,
    StockQuantity AS Stock,
    Unit AS Unidad
FROM Medicines
ORDER BY Name

PRINT ''
PRINT '========================================================================='
PRINT 'LIMPIEZA COMPLETADA'
PRINT '========================================================================='
PRINT ''
PRINT '✅ RESULTADO:'
PRINT '  • Datos de prueba eliminados'
PRINT '  • Medicamentos reales preservados'
PRINT '  • Usuarios preservados'
PRINT '  • Sistema listo para producción'
PRINT ''
PRINT 'Finalizado: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
