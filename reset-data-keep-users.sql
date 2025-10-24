-- =====================================================================================
-- SCRIPT DE LIMPIEZA COMPLETA - PRESERVA SOLO USUARIOS
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Fecha: 23 de octubre de 2025
-- 
-- ⚠️ ADVERTENCIA: Este script ELIMINARÁ TODOS los datos de:
--    - Pacientes
--    - Medicamentos
--    - Donaciones
--    - Entregas
--    - Documentos de pacientes
-- 
-- ✅ PRESERVARÁ: 
--    - Todos los usuarios registrados
--    - Todos los patrocinadores (son datos reales)
-- 
-- IMPORTANTE: Ejecuta este script en el panel de Somee.com cuando estés listo
--             para empezar con datos reales.
-- =====================================================================================

PRINT '========================================================================='
PRINT 'INICIANDO LIMPIEZA COMPLETA DE DATOS'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- PARTE 1: VERIFICAR USUARIOS ANTES DE ELIMINAR
-- =====================================================================================

PRINT '-- PARTE 1: Verificando usuarios existentes...'
PRINT ''

DECLARE @UserCount INT
SELECT @UserCount = COUNT(*) FROM AspNetUsers

PRINT 'Usuarios registrados actualmente: ' + CAST(@UserCount AS VARCHAR)
PRINT ''

IF @UserCount = 0
BEGIN
    PRINT '⚠️ ADVERTENCIA: No hay usuarios en el sistema'
    PRINT '   Se creará el usuario admin por defecto'
END
ELSE
BEGIN
    PRINT 'Usuarios que se PRESERVARÁN:'
    SELECT UserName, Email FROM AspNetUsers ORDER BY UserName
END

PRINT ''
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- PARTE 2: ELIMINAR DATOS EN ORDEN (RESPETANDO FOREIGN KEYS)
-- =====================================================================================

PRINT '-- PARTE 2: Eliminando datos de prueba...'
PRINT ''

-- 2.1: Eliminar Documentos de Pacientes
BEGIN TRY
    DECLARE @DocumentCount INT
    SELECT @DocumentCount = COUNT(*) FROM PatientDocuments
    
    DELETE FROM PatientDocuments
    
    PRINT '✓ Documentos de pacientes eliminados: ' + CAST(@DocumentCount AS VARCHAR)
END TRY
BEGIN CATCH
    PRINT '✗ ERROR eliminando documentos: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.2: Eliminar Entregas
BEGIN TRY
    DECLARE @DeliveryCount INT
    SELECT @DeliveryCount = COUNT(*) FROM Deliveries
    
    DELETE FROM Deliveries
    
    PRINT '✓ Entregas eliminadas: ' + CAST(@DeliveryCount AS VARCHAR)
END TRY
BEGIN CATCH
    PRINT '✗ ERROR eliminando entregas: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.3: Eliminar Donaciones
BEGIN TRY
    DECLARE @DonationCount INT
    SELECT @DonationCount = COUNT(*) FROM Donations
    
    DELETE FROM Donations
    
    PRINT '✓ Donaciones eliminadas: ' + CAST(@DonationCount AS VARCHAR)
END TRY
BEGIN CATCH
    PRINT '✗ ERROR eliminando donaciones: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.4: Eliminar Medicamentos
BEGIN TRY
    DECLARE @MedicineCount INT
    SELECT @MedicineCount = COUNT(*) FROM Medicines
    
    DELETE FROM Medicines
    
    PRINT '✓ Medicamentos eliminados: ' + CAST(@MedicineCount AS VARCHAR)
END TRY
BEGIN CATCH
    PRINT '✗ ERROR eliminando medicamentos: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.5: Eliminar Pacientes
BEGIN TRY
    DECLARE @PatientCount INT
    SELECT @PatientCount = COUNT(*) FROM Patients
    
    DELETE FROM Patients
    
    PRINT '✓ Pacientes eliminados: ' + CAST(@PatientCount AS VARCHAR)
END TRY
BEGIN CATCH
    PRINT '✗ ERROR eliminando pacientes: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.6: Patrocinadores - NO ELIMINAR (Son datos reales)
BEGIN TRY
    DECLARE @SponsorCount INT
    SELECT @SponsorCount = COUNT(*) FROM Sponsors
    
    PRINT '⚠️ Patrocinadores PRESERVADOS (datos reales): ' + CAST(@SponsorCount AS VARCHAR)
    
    IF @SponsorCount > 0
    BEGIN
        PRINT 'Lista de patrocinadores preservados:'
        SELECT Name FROM Sponsors ORDER BY Name
    END
END TRY
BEGIN CATCH
    PRINT '✗ ERROR verificando patrocinadores: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- =====================================================================================
-- PARTE 3: RESETEAR CONTADORES DE IDENTIDAD
-- =====================================================================================

PRINT '-- PARTE 3: Reseteando contadores de identidad...'
PRINT ''

-- Resetear contadores para que los próximos IDs empiecen en 1
BEGIN TRY
    DBCC CHECKIDENT ('Patients', RESEED, 0)
    PRINT '✓ Contador de Patients reiniciado'
    
    DBCC CHECKIDENT ('Medicines', RESEED, 0)
    PRINT '✓ Contador de Medicines reiniciado'
    
    DBCC CHECKIDENT ('Donations', RESEED, 0)
    PRINT '✓ Contador de Donations reiniciado'
    
    DBCC CHECKIDENT ('Deliveries', RESEED, 0)
    PRINT '✓ Contador de Deliveries reiniciado'
    
    DBCC CHECKIDENT ('PatientDocuments', RESEED, 0)
    PRINT '✓ Contador de PatientDocuments reiniciado'
    
    -- NO resetear Sponsors porque se preservan los datos reales
    -- DBCC CHECKIDENT ('Sponsors', RESEED, 0)
    PRINT '⚠️ Contador de Sponsors NO reiniciado (preservando datos reales)'
END TRY
BEGIN CATCH
    PRINT '⚠️ ADVERTENCIA reseteando contadores: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- =====================================================================================
-- PARTE 4: VERIFICAR USUARIOS DESPUÉS DE LIMPIEZA
-- =====================================================================================

PRINT '-- PARTE 4: Verificando usuarios después de limpieza...'
PRINT ''

SELECT @UserCount = COUNT(*) FROM AspNetUsers

PRINT 'Usuarios preservados: ' + CAST(@UserCount AS VARCHAR)
PRINT ''

IF @UserCount > 0
BEGIN
    PRINT 'Lista de usuarios preservados:'
    SELECT UserName, Email FROM AspNetUsers ORDER BY UserName
END
ELSE
BEGIN
    PRINT '⚠️ No hay usuarios. El sistema creará el admin automáticamente al iniciar.'
END

PRINT ''

-- =====================================================================================
-- PARTE 5: RESUMEN FINAL
-- =====================================================================================

PRINT '========================================================================='
PRINT 'LIMPIEZA COMPLETADA EXITOSAMENTE'
PRINT '========================================================================='
PRINT ''
PRINT 'Estado de la base de datos:'
PRINT '  - Pacientes: ' + CAST((SELECT COUNT(*) FROM Patients) AS VARCHAR) + ' (eliminados)'
PRINT '  - Medicamentos: ' + CAST((SELECT COUNT(*) FROM Medicines) AS VARCHAR) + ' (eliminados)'
PRINT '  - Donaciones: ' + CAST((SELECT COUNT(*) FROM Donations) AS VARCHAR) + ' (eliminados)'
PRINT '  - Entregas: ' + CAST((SELECT COUNT(*) FROM Deliveries) AS VARCHAR) + ' (eliminados)'
PRINT '  - Documentos: ' + CAST((SELECT COUNT(*) FROM PatientDocuments) AS VARCHAR) + ' (eliminados)'
PRINT '  - Patrocinadores: ' + CAST((SELECT COUNT(*) FROM Sponsors) AS VARCHAR) + ' ✅ PRESERVADOS'
PRINT '  - Usuarios: ' + CAST((SELECT COUNT(*) FROM AspNetUsers) AS VARCHAR) + ' ✅ PRESERVADOS'
PRINT ''
PRINT '✅ Base de datos lista para datos de producción'
PRINT ''
PRINT '========================================================================='
