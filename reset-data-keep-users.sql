-- =====================================================================================
-- SCRIPT DE LIMPIEZA COMPLETA - PRESERVA SOLO USUARIOS Y PATROCINADORES
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Versión 2 - Optimizado para Somee.com
-- Fecha: 24 de octubre de 2025
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
--    - Roles y configuración de Identity
-- =====================================================================================

-- Declarar TODAS las variables al inicio para evitar errores
DECLARE @UserCount INT
DECLARE @DocumentCount INT
DECLARE @DeliveryCount INT
DECLARE @DonationCount INT
DECLARE @MedicineCount INT
DECLARE @PatientCount INT
DECLARE @SponsorCount INT
DECLARE @UsersAfter INT
DECLARE @HasPatients INT
DECLARE @HasMedicines INT
DECLARE @HasDonations INT
DECLARE @HasDeliveries INT
DECLARE @HasDocuments INT
DECLARE @FinalPatients INT
DECLARE @FinalMedicines INT
DECLARE @FinalDonations INT
DECLARE @FinalDeliveries INT
DECLARE @FinalDocuments INT
DECLARE @FinalSponsors INT
DECLARE @FinalUsers INT
DECLARE @FinalRoles INT
DECLARE @FinalUserRoles INT
DECLARE @PatientsCheck INT
DECLARE @MedicinesCheck INT
DECLARE @DonationsCheck INT
DECLARE @DeliveriesCheck INT
DECLARE @DocumentsCheck INT

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
-- PARTE 2: ELIMINAR DATOS EN ORDEN
-- =====================================================================================

PRINT '-- PARTE 2: Eliminando datos de prueba...'
PRINT ''

-- Deshabilitar constraints
PRINT 'Deshabilitando constraints de foreign keys...'
BEGIN TRY
    ALTER TABLE Deliveries NOCHECK CONSTRAINT ALL
    ALTER TABLE Donations NOCHECK CONSTRAINT ALL
    ALTER TABLE PatientDocuments NOCHECK CONSTRAINT ALL
    PRINT '  ✓ Constraints deshabilitadas'
END TRY
BEGIN CATCH
    PRINT '  ⚠️ ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.1: PatientDocuments
PRINT 'Paso 1/5: Eliminando Documentos de Pacientes...'
BEGIN TRY
    SELECT @DocumentCount = COUNT(*) FROM PatientDocuments
    IF @DocumentCount > 0
    BEGIN
        DELETE FROM PatientDocuments
        PRINT '  ✓ Eliminados: ' + CAST(@DocumentCount AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ No hay registros'
END TRY
BEGIN CATCH
    PRINT '  ✗ ERROR: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.2: Deliveries
PRINT 'Paso 2/5: Eliminando Entregas...'
BEGIN TRY
    SELECT @DeliveryCount = COUNT(*) FROM Deliveries
    IF @DeliveryCount > 0
    BEGIN
        DELETE FROM Deliveries
        PRINT '  ✓ Eliminadas: ' + CAST(@DeliveryCount AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ No hay registros'
END TRY
BEGIN CATCH
    PRINT '  ✗ ERROR: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.3: Donations
PRINT 'Paso 3/5: Eliminando Donaciones...'
BEGIN TRY
    SELECT @DonationCount = COUNT(*) FROM Donations
    IF @DonationCount > 0
    BEGIN
        DELETE FROM Donations
        PRINT '  ✓ Eliminadas: ' + CAST(@DonationCount AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ No hay registros'
END TRY
BEGIN CATCH
    PRINT '  ✗ ERROR: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.4: Medicines
PRINT 'Paso 4/5: Eliminando Medicamentos...'
BEGIN TRY
    SELECT @MedicineCount = COUNT(*) FROM Medicines
    IF @MedicineCount > 0
    BEGIN
        DELETE FROM Medicines
        PRINT '  ✓ Eliminados: ' + CAST(@MedicineCount AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ No hay registros'
END TRY
BEGIN CATCH
    PRINT '  ✗ ERROR: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.5: Patients
PRINT 'Paso 5/5: Eliminando Pacientes...'
BEGIN TRY
    SELECT @PatientCount = COUNT(*) FROM Patients
    IF @PatientCount > 0
    BEGIN
        DELETE FROM Patients
        PRINT '  ✓ Eliminados: ' + CAST(@PatientCount AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ No hay registros'
END TRY
BEGIN CATCH
    PRINT '  ✗ ERROR: ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- Rehabilitar constraints
PRINT 'Rehabilitando constraints de foreign keys...'
BEGIN TRY
    ALTER TABLE Deliveries WITH CHECK CHECK CONSTRAINT ALL
    ALTER TABLE Donations WITH CHECK CHECK CONSTRAINT ALL
    ALTER TABLE PatientDocuments WITH CHECK CHECK CONSTRAINT ALL
    PRINT '  ✓ Constraints rehabilitadas'
END TRY
BEGIN CATCH
    PRINT '  ⚠️ ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- 2.6: Sponsors - PRESERVAR
PRINT '-----------------------------------------------------------------------'
PRINT 'PRESERVANDO PATROCINADORES...'
BEGIN TRY
    SELECT @SponsorCount = COUNT(*) FROM Sponsors
    PRINT '  ⚠️ Patrocinadores preservados: ' + CAST(@SponsorCount AS VARCHAR)
    IF @SponsorCount > 0
    BEGIN
        PRINT '  Lista de patrocinadores:'
        SELECT '    - ' + Name AS Patrocinador FROM Sponsors ORDER BY Name
    END
END TRY
BEGIN CATCH
    PRINT '  ✗ ERROR: ' + ERROR_MESSAGE()
END CATCH
PRINT '-----------------------------------------------------------------------'
PRINT ''

-- =====================================================================================
-- PARTE 3: RESETEAR CONTADORES DE IDENTIDAD
-- =====================================================================================

PRINT '-- PARTE 3: Reseteando contadores...'
PRINT ''

BEGIN TRY
    SELECT @HasPatients = COUNT(*) FROM Patients
    SELECT @HasMedicines = COUNT(*) FROM Medicines
    SELECT @HasDonations = COUNT(*) FROM Donations
    SELECT @HasDeliveries = COUNT(*) FROM Deliveries
    SELECT @HasDocuments = COUNT(*) FROM PatientDocuments
    
    IF @HasPatients = 0
    BEGIN
        DBCC CHECKIDENT ('Patients', RESEED, 0)
        PRINT '  ✓ Patients reiniciado'
    END
    
    IF @HasMedicines = 0
    BEGIN
        DBCC CHECKIDENT ('Medicines', RESEED, 0)
        PRINT '  ✓ Medicines reiniciado'
    END
    
    IF @HasDonations = 0
    BEGIN
        DBCC CHECKIDENT ('Donations', RESEED, 0)
        PRINT '  ✓ Donations reiniciado'
    END
    
    IF @HasDeliveries = 0
    BEGIN
        DBCC CHECKIDENT ('Deliveries', RESEED, 0)
        PRINT '  ✓ Deliveries reiniciado'
    END
    
    IF @HasDocuments = 0
    BEGIN
        DBCC CHECKIDENT ('PatientDocuments', RESEED, 0)
        PRINT '  ✓ PatientDocuments reiniciado'
    END
    
    PRINT '  ⚠️ Sponsors NO reiniciado (datos reales)'
END TRY
BEGIN CATCH
    PRINT '  ⚠️ ' + ERROR_MESSAGE()
END CATCH

PRINT ''

-- =====================================================================================
-- PARTE 4: VERIFICAR USUARIOS DESPUÉS
-- =====================================================================================

PRINT '-- PARTE 4: Verificación final...'
PRINT ''

SELECT @UsersAfter = COUNT(*) FROM AspNetUsers
PRINT 'Usuarios preservados: ' + CAST(@UsersAfter AS VARCHAR)

IF @UsersAfter > 0
BEGIN
    PRINT ''
    PRINT 'Lista de usuarios preservados:'
    SELECT '  - ' + UserName + ' (' + Email + ')' AS Usuario 
    FROM AspNetUsers 
    ORDER BY UserName
END

PRINT ''

-- =====================================================================================
-- PARTE 5: RESUMEN FINAL
-- =====================================================================================

PRINT '========================================================================='
PRINT 'RESUMEN FINAL'
PRINT '========================================================================='
PRINT ''

SELECT @FinalPatients = COUNT(*) FROM Patients
SELECT @FinalMedicines = COUNT(*) FROM Medicines
SELECT @FinalDonations = COUNT(*) FROM Donations
SELECT @FinalDeliveries = COUNT(*) FROM Deliveries
SELECT @FinalDocuments = COUNT(*) FROM PatientDocuments
SELECT @FinalSponsors = COUNT(*) FROM Sponsors
SELECT @FinalUsers = COUNT(*) FROM AspNetUsers
SELECT @FinalRoles = COUNT(*) FROM AspNetRoles
SELECT @FinalUserRoles = COUNT(*) FROM AspNetUserRoles

PRINT 'TABLAS OPERACIONALES (Eliminadas):'
PRINT '  • Pacientes: ' + CAST(@FinalPatients AS VARCHAR)
PRINT '  • Medicamentos: ' + CAST(@FinalMedicines AS VARCHAR)
PRINT '  • Donaciones: ' + CAST(@FinalDonations AS VARCHAR)
PRINT '  • Entregas: ' + CAST(@FinalDeliveries AS VARCHAR)
PRINT '  • Documentos: ' + CAST(@FinalDocuments AS VARCHAR)
PRINT ''
PRINT 'DATOS PRESERVADOS:'
PRINT '  • Patrocinadores: ' + CAST(@FinalSponsors AS VARCHAR)
PRINT '  • Usuarios: ' + CAST(@FinalUsers AS VARCHAR)
PRINT '  • Roles: ' + CAST(@FinalRoles AS VARCHAR)
PRINT '  • Usuario-Roles: ' + CAST(@FinalUserRoles AS VARCHAR)
PRINT ''

-- Verificación
SELECT @PatientsCheck = COUNT(*) FROM Patients
SELECT @MedicinesCheck = COUNT(*) FROM Medicines
SELECT @DonationsCheck = COUNT(*) FROM Donations
SELECT @DeliveriesCheck = COUNT(*) FROM Deliveries
SELECT @DocumentsCheck = COUNT(*) FROM PatientDocuments

IF @PatientsCheck = 0 AND @MedicinesCheck = 0 AND @DonationsCheck = 0 AND @DeliveriesCheck = 0 AND @DocumentsCheck = 0
BEGIN
    PRINT '✅ VERIFICACIÓN: Todas las tablas operacionales vacías'
    PRINT '✅ Base de datos lista para producción'
END
ELSE
BEGIN
    PRINT '⚠️ ADVERTENCIA: Algunas tablas tienen datos:'
    IF @PatientsCheck > 0 PRINT '  - Patients: ' + CAST(@PatientsCheck AS VARCHAR)
    IF @MedicinesCheck > 0 PRINT '  - Medicines: ' + CAST(@MedicinesCheck AS VARCHAR)
    IF @DonationsCheck > 0 PRINT '  - Donations: ' + CAST(@DonationsCheck AS VARCHAR)
    IF @DeliveriesCheck > 0 PRINT '  - Deliveries: ' + CAST(@DeliveriesCheck AS VARCHAR)
    IF @DocumentsCheck > 0 PRINT '  - PatientDocuments: ' + CAST(@DocumentsCheck AS VARCHAR)
END

PRINT ''
PRINT '========================================================================='
PRINT 'Finalizado: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
