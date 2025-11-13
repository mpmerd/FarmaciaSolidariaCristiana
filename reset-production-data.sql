-- =====================================================================================
-- SCRIPT DE LIMPIEZA COMPLETA PARA PRUEBAS - PRODUCCIÓN
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Versión 1.0 - Preparación para inicio de operaciones
-- Fecha: 3 de noviembre de 2025
-- 
-- ⚠️ ADVERTENCIA: Este script ELIMINARÁ TODOS los datos transaccionales:
--    - Pacientes (ALL)
--    - Turnos (ALL) y relaciones TurnoMedicamentos, TurnoInsumos
--    - Fechas Bloqueadas (ALL)
--    - Entregas (ALL) de medicamentos e insumos
--    - Donaciones (ALL) de medicamentos e insumos
--    - Documentos de pacientes (ALL)
--    - Decoraciones del Navbar PERSONALIZADAS (Custom solamente)
-- 
-- ✅ PRESERVARÁ (Datos maestros reales): 
--    - MEDICAMENTOS (datos reales cargados)
--    - INSUMOS (datos reales cargados)
--    - PATROCINADORES (datos reales)
--    - USUARIOS (Admin, Farmacéuticos, ViewerPublic reales)
--    - ROLES y configuración de Identity
-- 
-- 🎯 PROPÓSITO: 
--    Limpiar la base de datos de producción ANTES del lanzamiento oficial
--    para poder hacer pruebas finales sin afectar datos maestros reales.
-- 
-- ⚡ EJECUCIÓN: Panel SQL de Somee.com
-- =====================================================================================

PRINT '========================================================================='
PRINT 'LIMPIEZA COMPLETA DE DATOS TRANSACCIONALES'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''
PRINT '⚠️  ESTE SCRIPT ELIMINARÁ TODOS LOS DATOS DE:'
PRINT '   • Pacientes'
PRINT '   • Turnos (con medicamentos e insumos)'
PRINT '   • Fechas Bloqueadas'
PRINT '   • Entregas (medicamentos e insumos)'
PRINT '   • Donaciones (medicamentos e insumos)'
PRINT '   • Documentos de pacientes'
PRINT '   • Decoraciones Navbar personalizadas (custom)'
PRINT ''
PRINT '✅ PRESERVARÁ:'
PRINT '   • Medicamentos (datos maestros)'
PRINT '   • Insumos (datos maestros)'
PRINT '   • Patrocinadores'
PRINT '   • Usuarios'
PRINT '   • Decoraciones predefinidas (Navidad, Epifanía, etc.)'
PRINT ''
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- PARTE 1: VERIFICAR Y REPORTAR ESTADO ACTUAL
-- =====================================================================================

PRINT '-- PARTE 1: Estado actual de la base de datos'
PRINT ''

DECLARE @PatientsCount INT
DECLARE @TurnosCount INT
DECLARE @TurnoMedicamentosCount INT
DECLARE @TurnoInsumosCount INT
DECLARE @FechasBloqueadasCount INT
DECLARE @DeliveriesCount INT
DECLARE @DonationsCount INT
DECLARE @DocumentsCount INT
DECLARE @NavbarDecorationsCustomCount INT
DECLARE @MedicinesCount INT
DECLARE @SuppliesCount INT
DECLARE @SponsorsCount INT
DECLARE @UsersCount INT
DECLARE @NavbarDecorationsPredefinedCount INT

-- Contar datos transaccionales (serán eliminados)
SELECT @PatientsCount = COUNT(*) FROM Patients
SELECT @TurnosCount = COUNT(*) FROM Turnos
SELECT @TurnoMedicamentosCount = COUNT(*) FROM TurnoMedicamentos
SELECT @TurnoInsumosCount = COUNT(*) FROM TurnoInsumos
SELECT @FechasBloqueadasCount = COUNT(*) FROM FechasBloqueadas
SELECT @DeliveriesCount = COUNT(*) FROM Deliveries
SELECT @DonationsCount = COUNT(*) FROM Donations
SELECT @DocumentsCount = COUNT(*) FROM PatientDocuments
SELECT @NavbarDecorationsCustomCount = COUNT(*) FROM NavbarDecorations WHERE Type = 1 -- Custom = 1

-- Contar datos maestros (serán preservados)
SELECT @MedicinesCount = COUNT(*) FROM Medicines
SELECT @SuppliesCount = COUNT(*) FROM Supplies
SELECT @SponsorsCount = COUNT(*) FROM Sponsors
SELECT @UsersCount = COUNT(*) FROM AspNetUsers
SELECT @NavbarDecorationsPredefinedCount = COUNT(*) FROM NavbarDecorations WHERE Type = 0 -- Predefined = 0

PRINT '📊 DATOS TRANSACCIONALES (SE ELIMINARÁN):'
PRINT '  • Pacientes: ' + CAST(@PatientsCount AS VARCHAR)
PRINT '  • Turnos: ' + CAST(@TurnosCount AS VARCHAR)
PRINT '  • Turno-Medicamentos: ' + CAST(@TurnoMedicamentosCount AS VARCHAR)
PRINT '  • Turno-Insumos: ' + CAST(@TurnoInsumosCount AS VARCHAR)
PRINT '  • Fechas Bloqueadas: ' + CAST(@FechasBloqueadasCount AS VARCHAR)
PRINT '  • Entregas: ' + CAST(@DeliveriesCount AS VARCHAR)
PRINT '  • Donaciones: ' + CAST(@DonationsCount AS VARCHAR)
PRINT '  • Documentos pacientes: ' + CAST(@DocumentsCount AS VARCHAR)
PRINT '  • Decoraciones Navbar Custom: ' + CAST(@NavbarDecorationsCustomCount AS VARCHAR)
PRINT ''
PRINT '📦 DATOS MAESTROS (SE PRESERVARÁN):'
PRINT '  • Medicamentos: ' + CAST(@MedicinesCount AS VARCHAR)
PRINT '  • Insumos: ' + CAST(@SuppliesCount AS VARCHAR)
PRINT '  • Patrocinadores: ' + CAST(@SponsorsCount AS VARCHAR)
PRINT '  • Usuarios: ' + CAST(@UsersCount AS VARCHAR)
PRINT '  • Decoraciones Predefinidas: ' + CAST(@NavbarDecorationsPredefinedCount AS VARCHAR)
PRINT ''

-- Verificación de seguridad
IF @MedicinesCount = 0
BEGIN
    PRINT '⚠️  ADVERTENCIA: No hay medicamentos en la base de datos!'
    PRINT '   Esto puede indicar un problema. Verifica antes de continuar.'
    PRINT ''
END

IF @SuppliesCount = 0
BEGIN
    PRINT '⚠️  ADVERTENCIA: No hay insumos en la base de datos!'
    PRINT '   Esto puede indicar un problema. Verifica antes de continuar.'
    PRINT ''
END

IF @UsersCount = 0
BEGIN
    PRINT '❌ ERROR CRÍTICO: No hay usuarios en la base de datos!'
    PRINT '   NO SE PUEDE CONTINUAR. Restaura usuarios primero.'
    PRINT ''
    RETURN
END

-- Mostrar usuarios que se preservarán
PRINT 'Usuarios que se PRESERVARÁN:'
SELECT UserName, Email, EmailConfirmed 
FROM AspNetUsers 
ORDER BY UserName
PRINT ''

PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- PARTE 2: ELIMINAR DATOS TRANSACCIONALES EN ORDEN
-- =====================================================================================

PRINT '-- PARTE 2: Eliminando datos transaccionales...'
PRINT ''

BEGIN TRANSACTION

BEGIN TRY

    -- 2.1: Deshabilitar constraints temporalmente para evitar problemas
    PRINT 'Paso 0: Deshabilitando constraints temporalmente...'
    ALTER TABLE Deliveries NOCHECK CONSTRAINT ALL
    ALTER TABLE Donations NOCHECK CONSTRAINT ALL
    ALTER TABLE PatientDocuments NOCHECK CONSTRAINT ALL
    ALTER TABLE Turnos NOCHECK CONSTRAINT ALL
    ALTER TABLE TurnoMedicamentos NOCHECK CONSTRAINT ALL
    ALTER TABLE TurnoInsumos NOCHECK CONSTRAINT ALL
    ALTER TABLE FechasBloqueadas NOCHECK CONSTRAINT ALL
    ALTER TABLE NavbarDecorations NOCHECK CONSTRAINT ALL
    PRINT '  ✓ Constraints deshabilitadas'
    PRINT ''

    -- 2.2: Eliminar TurnoInsumos
    PRINT 'Paso 1/9: Eliminando TurnoInsumos...'
    IF EXISTS (SELECT 1 FROM TurnoInsumos)
    BEGIN
        DELETE FROM TurnoInsumos
        PRINT '  ✓ Eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ  No hay registros'
    PRINT ''

    -- 2.3: Eliminar TurnoMedicamentos
    PRINT 'Paso 2/9: Eliminando TurnoMedicamentos...'
    IF EXISTS (SELECT 1 FROM TurnoMedicamentos)
    BEGIN
        DELETE FROM TurnoMedicamentos
        PRINT '  ✓ Eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ  No hay registros'
    PRINT ''

    -- 2.4: Eliminar Turnos
    PRINT 'Paso 3/9: Eliminando Turnos...'
    IF EXISTS (SELECT 1 FROM Turnos)
    BEGIN
        DELETE FROM Turnos
        PRINT '  ✓ Eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ  No hay registros'
    PRINT ''

    -- 2.5: Eliminar Fechas Bloqueadas
    PRINT 'Paso 4/9: Eliminando Fechas Bloqueadas...'
    IF EXISTS (SELECT 1 FROM FechasBloqueadas)
    BEGIN
        DELETE FROM FechasBloqueadas
        PRINT '  ✓ Eliminadas: ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ  No hay registros'
    PRINT ''

    -- 2.6: Eliminar Documentos de Pacientes
    PRINT 'Paso 5/9: Eliminando Documentos de Pacientes...'
    IF EXISTS (SELECT 1 FROM PatientDocuments)
    BEGIN
        DELETE FROM PatientDocuments
        PRINT '  ✓ Eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ  No hay registros'
    PRINT ''

    -- 2.7: Eliminar Entregas (libera stock)
    PRINT 'Paso 6/9: Eliminando Entregas...'
    IF EXISTS (SELECT 1 FROM Deliveries)
    BEGIN
        -- Restaurar stock de medicamentos
        UPDATE m
        SET m.StockQuantity = m.StockQuantity + d.Quantity
        FROM Medicines m
        INNER JOIN Deliveries d ON m.Id = d.MedicineId
        WHERE d.MedicineId IS NOT NULL
        
        DECLARE @MedicineStockRestored INT = @@ROWCOUNT
        
        -- Restaurar stock de insumos
        UPDATE s
        SET s.StockQuantity = s.StockQuantity + d.Quantity
        FROM Supplies s
        INNER JOIN Deliveries d ON s.Id = d.SupplyId
        WHERE d.SupplyId IS NOT NULL
        
        DECLARE @SupplyStockRestored INT = @@ROWCOUNT
        
        -- Eliminar entregas
        DELETE FROM Deliveries
        
        PRINT '  ✓ Entregas eliminadas: ' + CAST(@@ROWCOUNT AS VARCHAR)
        PRINT '    Stock medicamentos restaurado: ' + CAST(@MedicineStockRestored AS VARCHAR)
        PRINT '    Stock insumos restaurado: ' + CAST(@SupplyStockRestored AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ  No hay registros'
    PRINT ''

    -- 2.8: Eliminar Donaciones (ajusta stock)
    PRINT 'Paso 7/9: Eliminando Donaciones...'
    IF EXISTS (SELECT 1 FROM Donations)
    BEGIN
        -- Descontar stock de medicamentos
        UPDATE m
        SET m.StockQuantity = CASE 
            WHEN m.StockQuantity >= d.Quantity THEN m.StockQuantity - d.Quantity
            ELSE 0  -- Evitar valores negativos
        END
        FROM Medicines m
        INNER JOIN Donations d ON m.Id = d.MedicineId
        WHERE d.MedicineId IS NOT NULL
        
        DECLARE @MedicineStockAdjusted INT = @@ROWCOUNT
        
        -- Descontar stock de insumos
        UPDATE s
        SET s.StockQuantity = CASE 
            WHEN s.StockQuantity >= d.Quantity THEN s.StockQuantity - d.Quantity
            ELSE 0  -- Evitar valores negativos
        END
        FROM Supplies s
        INNER JOIN Donations d ON s.Id = d.SupplyId
        WHERE d.SupplyId IS NOT NULL
        
        DECLARE @SupplyStockAdjusted INT = @@ROWCOUNT
        
        -- Eliminar donaciones
        DELETE FROM Donations
        
        PRINT '  ✓ Donaciones eliminadas: ' + CAST(@@ROWCOUNT AS VARCHAR)
        PRINT '    Stock medicamentos ajustado: ' + CAST(@MedicineStockAdjusted AS VARCHAR)
        PRINT '    Stock insumos ajustado: ' + CAST(@SupplyStockAdjusted AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ  No hay registros'
    PRINT ''

    -- 2.9: Eliminar Pacientes
    PRINT 'Paso 8/10: Eliminando Pacientes...'
    IF EXISTS (SELECT 1 FROM Patients)
    BEGIN
        DELETE FROM Patients
        PRINT '  ✓ Eliminados: ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ  No hay registros'
    PRINT ''

    -- 2.10: Eliminar Decoraciones del Navbar
    PRINT 'Paso 9/10: Eliminando Decoraciones Personalizadas del Navbar...'
    IF EXISTS (SELECT 1 FROM NavbarDecorations WHERE Type = 1) -- Solo Custom
    BEGIN
        DELETE FROM NavbarDecorations WHERE Type = 1 -- Custom = 1
        PRINT '  ✓ Eliminadas (custom): ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    ELSE
        PRINT '  ℹ  No hay decoraciones personalizadas'
    
    -- Desactivar cualquier decoración predefinida que esté activa
    IF EXISTS (SELECT 1 FROM NavbarDecorations WHERE Type = 0 AND IsActive = 1)
    BEGIN
        UPDATE NavbarDecorations 
        SET IsActive = 0, ActivatedAt = NULL, ActivatedBy = NULL
        WHERE Type = 0 AND IsActive = 1
        PRINT '  ✓ Desactivadas (predefinidas): ' + CAST(@@ROWCOUNT AS VARCHAR)
    END
    PRINT ''

    -- 2.11: Rehabilitar constraints
    PRINT 'Paso 10/10: Rehabilitando constraints...'
    ALTER TABLE Deliveries WITH CHECK CHECK CONSTRAINT ALL
    ALTER TABLE Donations WITH CHECK CHECK CONSTRAINT ALL
    ALTER TABLE PatientDocuments WITH CHECK CHECK CONSTRAINT ALL
    ALTER TABLE Turnos WITH CHECK CHECK CONSTRAINT ALL
    ALTER TABLE TurnoMedicamentos WITH CHECK CHECK CONSTRAINT ALL
    ALTER TABLE TurnoInsumos WITH CHECK CHECK CONSTRAINT ALL
    ALTER TABLE FechasBloqueadas WITH CHECK CHECK CONSTRAINT ALL
    ALTER TABLE NavbarDecorations WITH CHECK CHECK CONSTRAINT ALL
    PRINT '  ✓ Constraints rehabilitadas'
    PRINT ''

    COMMIT TRANSACTION
    PRINT '✅ TRANSACCIÓN COMPLETADA EXITOSAMENTE'

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    
    PRINT ''
    PRINT '❌ ERROR DURANTE LA ELIMINACIÓN'
    PRINT '========================================================================='
    PRINT 'Error: ' + ERROR_MESSAGE()
    PRINT 'Línea: ' + CAST(ERROR_LINE() AS VARCHAR)
    PRINT ''
    PRINT '⚠️  TRANSACCIÓN REVERTIDA - NO SE ELIMINÓ NADA'
    PRINT ''
    
    -- Intentar rehabilitar constraints de todos modos
    BEGIN TRY
        ALTER TABLE Deliveries WITH CHECK CHECK CONSTRAINT ALL
        ALTER TABLE Donations WITH CHECK CHECK CONSTRAINT ALL
        ALTER TABLE PatientDocuments WITH CHECK CHECK CONSTRAINT ALL
        ALTER TABLE Turnos WITH CHECK CHECK CONSTRAINT ALL
        ALTER TABLE TurnoMedicamentos WITH CHECK CHECK CONSTRAINT ALL
        ALTER TABLE TurnoInsumos WITH CHECK CHECK CONSTRAINT ALL
        ALTER TABLE FechasBloqueadas WITH CHECK CHECK CONSTRAINT ALL
        ALTER TABLE NavbarDecorations WITH CHECK CHECK CONSTRAINT ALL
    END TRY
    BEGIN CATCH
        PRINT 'No se pudieron rehabilitar constraints'
    END CATCH
    
    RETURN
END CATCH

PRINT ''
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- PARTE 3: RESETEAR CONTADORES DE IDENTIDAD
-- =====================================================================================

PRINT '-- PARTE 3: Reseteando contadores de identidad...'
PRINT ''

BEGIN TRY
    -- Resetear solo tablas que están vacías
    DECLARE @EmptyPatients INT, @EmptyTurnos INT, @EmptyTM INT, @EmptyTI INT
    DECLARE @EmptyFechasBloqueadas INT
    DECLARE @EmptyDeliveries INT, @EmptyDonations INT, @EmptyDocs INT
    DECLARE @CustomDecorationsCount INT
    
    SELECT @EmptyPatients = COUNT(*) FROM Patients
    SELECT @EmptyTurnos = COUNT(*) FROM Turnos
    SELECT @EmptyTM = COUNT(*) FROM TurnoMedicamentos
    SELECT @EmptyTI = COUNT(*) FROM TurnoInsumos
    SELECT @EmptyFechasBloqueadas = COUNT(*) FROM FechasBloqueadas
    SELECT @EmptyDeliveries = COUNT(*) FROM Deliveries
    SELECT @EmptyDonations = COUNT(*) FROM Donations
    SELECT @EmptyDocs = COUNT(*) FROM PatientDocuments
    SELECT @CustomDecorationsCount = COUNT(*) FROM NavbarDecorations WHERE Type = 1
    
    IF @EmptyPatients = 0
    BEGIN
        DBCC CHECKIDENT ('Patients', RESEED, 0)
        PRINT '  ✓ Patients reiniciado a 0'
    END
    
    IF @EmptyTurnos = 0
    BEGIN
        DBCC CHECKIDENT ('Turnos', RESEED, 0)
        PRINT '  ✓ Turnos reiniciado a 0'
    END
    
    IF @EmptyTM = 0
    BEGIN
        DBCC CHECKIDENT ('TurnoMedicamentos', RESEED, 0)
        PRINT '  ✓ TurnoMedicamentos reiniciado a 0'
    END
    
    IF @EmptyTI = 0
    BEGIN
        DBCC CHECKIDENT ('TurnoInsumos', RESEED, 0)
        PRINT '  ✓ TurnoInsumos reiniciado a 0'
    END
    
    IF @EmptyFechasBloqueadas = 0
    BEGIN
        DBCC CHECKIDENT ('FechasBloqueadas', RESEED, 0)
        PRINT '  ✓ FechasBloqueadas reiniciado a 0'
    END
    
    IF @EmptyDeliveries = 0
    BEGIN
        DBCC CHECKIDENT ('Deliveries', RESEED, 0)
        PRINT '  ✓ Deliveries reiniciado a 0'
    END
    
    IF @EmptyDonations = 0
    BEGIN
        DBCC CHECKIDENT ('Donations', RESEED, 0)
        PRINT '  ✓ Donations reiniciado a 0'
    END
    
    IF @EmptyDocs = 0
    BEGIN
        DBCC CHECKIDENT ('PatientDocuments', RESEED, 0)
        PRINT '  ✓ PatientDocuments reiniciado a 0'
    END
    
    -- NavbarDecorations: NO resetear porque preservamos las predefinidas
    -- Solo informar si había decoraciones custom
    IF @CustomDecorationsCount > 0
        PRINT '  ℹ  NavbarDecorations: ' + CAST(@CustomDecorationsCount AS VARCHAR) + ' custom eliminadas (predefinidas preservadas)'
    
    PRINT ''
    PRINT '  ⚠️  NO SE REINICIAN (datos maestros):'
    PRINT '      • Medicines (preservado)'
    PRINT '      • Supplies (preservado)'
    PRINT '      • Sponsors (preservado)'
    PRINT '      • NavbarDecorations predefinidas (preservadas)'
    
END TRY
BEGIN CATCH
    PRINT '  ⚠️  No se pudieron resetear algunos contadores'
    PRINT '    ' + ERROR_MESSAGE()
END CATCH

PRINT ''
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- PARTE 4: VERIFICACIÓN FINAL Y REPORTE
-- =====================================================================================

PRINT '-- PARTE 4: Verificación final'
PRINT ''

DECLARE @FinalPatients INT, @FinalTurnos INT, @FinalTM INT, @FinalTI INT
DECLARE @FinalFechasBloqueadas INT
DECLARE @FinalDeliveries INT, @FinalDonations INT, @FinalDocs INT
DECLARE @FinalNavbarDecorationsCustom INT
DECLARE @FinalNavbarDecorationsPredefined INT
DECLARE @FinalMedicines INT, @FinalSupplies INT, @FinalSponsors INT, @FinalUsers INT

-- Contar datos después de limpieza
SELECT @FinalPatients = COUNT(*) FROM Patients
SELECT @FinalTurnos = COUNT(*) FROM Turnos
SELECT @FinalTM = COUNT(*) FROM TurnoMedicamentos
SELECT @FinalTI = COUNT(*) FROM TurnoInsumos
SELECT @FinalFechasBloqueadas = COUNT(*) FROM FechasBloqueadas
SELECT @FinalDeliveries = COUNT(*) FROM Deliveries
SELECT @FinalDonations = COUNT(*) FROM Donations
SELECT @FinalDocs = COUNT(*) FROM PatientDocuments
SELECT @FinalNavbarDecorationsCustom = COUNT(*) FROM NavbarDecorations WHERE Type = 1
SELECT @FinalNavbarDecorationsPredefined = COUNT(*) FROM NavbarDecorations WHERE Type = 0
SELECT @FinalMedicines = COUNT(*) FROM Medicines
SELECT @FinalSupplies = COUNT(*) FROM Supplies
SELECT @FinalSponsors = COUNT(*) FROM Sponsors
SELECT @FinalUsers = COUNT(*) FROM AspNetUsers

PRINT '========================================================================='
PRINT 'RESUMEN FINAL - DESPUÉS DE LIMPIEZA'
PRINT '========================================================================='
PRINT ''
PRINT '✅ TABLAS TRANSACCIONALES (VACÍAS):'
PRINT '  • Pacientes: ' + CAST(@FinalPatients AS VARCHAR) + ' (debe ser 0)'
PRINT '  • Turnos: ' + CAST(@FinalTurnos AS VARCHAR) + ' (debe ser 0)'
PRINT '  • Turno-Medicamentos: ' + CAST(@FinalTM AS VARCHAR) + ' (debe ser 0)'
PRINT '  • Turno-Insumos: ' + CAST(@FinalTI AS VARCHAR) + ' (debe ser 0)'
PRINT '  • Fechas Bloqueadas: ' + CAST(@FinalFechasBloqueadas AS VARCHAR) + ' (debe ser 0)'
PRINT '  • Entregas: ' + CAST(@FinalDeliveries AS VARCHAR) + ' (debe ser 0)'
PRINT '  • Donaciones: ' + CAST(@FinalDonations AS VARCHAR) + ' (debe ser 0)'
PRINT '  • Documentos: ' + CAST(@FinalDocs AS VARCHAR) + ' (debe ser 0)'
PRINT '  • Decoraciones Custom: ' + CAST(@FinalNavbarDecorationsCustom AS VARCHAR) + ' (debe ser 0)'
PRINT ''
PRINT '✅ DATOS MAESTROS PRESERVADOS:'
PRINT '  • Medicamentos: ' + CAST(@FinalMedicines AS VARCHAR)
PRINT '  • Insumos: ' + CAST(@FinalSupplies AS VARCHAR)
PRINT '  • Patrocinadores: ' + CAST(@FinalSponsors AS VARCHAR)
PRINT '  • Usuarios: ' + CAST(@FinalUsers AS VARCHAR)
PRINT '  • Decoraciones Predefinidas: ' + CAST(@FinalNavbarDecorationsPredefined AS VARCHAR)
PRINT ''

-- Verificación de éxito
IF @FinalPatients = 0 AND @FinalTurnos = 0 AND @FinalTM = 0 AND @FinalTI = 0 
   AND @FinalFechasBloqueadas = 0
   AND @FinalDeliveries = 0 AND @FinalDonations = 0 AND @FinalDocs = 0
   AND @FinalNavbarDecorationsCustom = 0
BEGIN
    PRINT '✅ ✅ ✅ VERIFICACIÓN EXITOSA ✅ ✅ ✅'
    PRINT ''
    PRINT '🎯 RESULTADO:'
    PRINT '  • Todas las tablas transaccionales están vacías'
    PRINT '  • Datos maestros preservados correctamente'
    PRINT '  • Stock de medicamentos e insumos ajustado'
    PRINT '  • Usuarios del sistema intactos'
    PRINT ''
    PRINT '✅ BASE DE DATOS LISTA PARA PRUEBAS DE PRE-LANZAMIENTO'
    PRINT ''
    PRINT 'Próximos pasos:'
    PRINT '  1. Crear fichas de pacientes de prueba'
    PRINT '  2. Solicitar turnos de prueba'
    PRINT '  3. Registrar entregas de prueba'
    PRINT '  4. Verificar que todo funciona correctamente'
    PRINT '  5. Ejecutar este script de nuevo antes del lanzamiento oficial'
END
ELSE
BEGIN
    PRINT '⚠️  ADVERTENCIA: Algunas tablas aún tienen datos'
    PRINT ''
    IF @FinalPatients > 0 PRINT '  - Patients: ' + CAST(@FinalPatients AS VARCHAR)
    IF @FinalTurnos > 0 PRINT '  - Turnos: ' + CAST(@FinalTurnos AS VARCHAR)
    IF @FinalTM > 0 PRINT '  - TurnoMedicamentos: ' + CAST(@FinalTM AS VARCHAR)
    IF @FinalTI > 0 PRINT '  - TurnoInsumos: ' + CAST(@FinalTI AS VARCHAR)
    IF @FinalFechasBloqueadas > 0 PRINT '  - FechasBloqueadas: ' + CAST(@FinalFechasBloqueadas AS VARCHAR)
    IF @FinalDeliveries > 0 PRINT '  - Deliveries: ' + CAST(@FinalDeliveries AS VARCHAR)
    IF @FinalDonations > 0 PRINT '  - Donations: ' + CAST(@FinalDonations AS VARCHAR)
    IF @FinalDocs > 0 PRINT '  - PatientDocuments: ' + CAST(@FinalDocs AS VARCHAR)
    IF @FinalNavbarDecorationsCustom > 0 PRINT '  - NavbarDecorations (Custom): ' + CAST(@FinalNavbarDecorationsCustom AS VARCHAR)
    PRINT ''
    PRINT 'Revisa los errores arriba y vuelve a ejecutar el script.'
END

PRINT ''
PRINT '========================================================================='
PRINT 'Finalizado: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

-- Mostrar muestra del inventario preservado
PRINT 'INVENTARIO DE MEDICAMENTOS PRESERVADO:'
SELECT TOP 10
    Name AS Medicamento,
    StockQuantity AS Stock,
    Unit AS Unidad
FROM Medicines
ORDER BY Name

PRINT ''
PRINT 'INVENTARIO DE INSUMOS PRESERVADO:'
SELECT TOP 10
    Name AS Insumo,
    StockQuantity AS Stock,
    Unit AS Unidad
FROM Supplies
ORDER BY Name

PRINT ''
PRINT '========================================================================='
GO
