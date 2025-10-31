-- ============================================
-- Script para ELIMINAR Datos de Prueba - Sistema de Turnos
-- Farmacia Solidaria Cristiana
-- ============================================
-- Este script elimina SOLO los usuarios de prueba y sus turnos
-- NO afecta datos de producción (usuarios reales, medicamentos, patrocinadores)

USE [FarmaciaDb];
GO

PRINT '========================================================================='
PRINT 'ELIMINACIÓN DE DATOS DE PRUEBA - SISTEMA DE TURNOS'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

-- ============================================
-- 1. IDENTIFICAR USUARIOS DE PRUEBA
-- ============================================
PRINT '=== Identificando usuarios de prueba ===';

DECLARE @UsersToDelete TABLE (
    UserId NVARCHAR(450),
    Email NVARCHAR(256)
);

-- Buscar usuarios de prueba por email
INSERT INTO @UsersToDelete (UserId, Email)
SELECT Id, Email 
FROM AspNetUsers
WHERE Email IN (
    'maria.garcia@example.com',
    'juan.perez@example.com',
    'ana.lopez@example.com',
    'carlos.rodriguez@example.com'
);

DECLARE @UserCount INT = (SELECT COUNT(*) FROM @UsersToDelete);
PRINT 'Usuarios de prueba encontrados: ' + CAST(@UserCount AS NVARCHAR(10));

IF @UserCount = 0
BEGIN
    PRINT '✓ No hay usuarios de prueba para eliminar.';
    PRINT 'El sistema está limpio.';
    PRINT '';
    RETURN;
END

-- Mostrar usuarios que serán eliminados
PRINT '';
PRINT 'Los siguientes usuarios serán eliminados:';
SELECT Email FROM @UsersToDelete;
PRINT '';

-- ============================================
-- 2. CONTAR DATOS RELACIONADOS
-- ============================================
PRINT '=== Contando datos relacionados ===';

DECLARE @TotalTurnos INT = (
    SELECT COUNT(*) 
    FROM Turnos 
    WHERE UserId IN (SELECT UserId FROM @UsersToDelete)
);

DECLARE @TotalTurnoMedicamentos INT = (
    SELECT COUNT(*) 
    FROM TurnoMedicamentos 
    WHERE TurnoId IN (
        SELECT Id FROM Turnos 
        WHERE UserId IN (SELECT UserId FROM @UsersToDelete)
    )
);

PRINT 'Turnos a eliminar: ' + CAST(@TotalTurnos AS NVARCHAR(10));
PRINT 'TurnoMedicamentos a eliminar: ' + CAST(@TotalTurnoMedicamentos AS NVARCHAR(10));
PRINT '';

-- ============================================
-- 3. ELIMINAR DATOS (CASCADE)
-- ============================================
PRINT '=== Iniciando eliminación ===';
BEGIN TRANSACTION;

BEGIN TRY
    -- 3.1 Eliminar TurnoMedicamentos (se eliminan automáticamente por CASCADE, pero por claridad lo hacemos explícito)
    PRINT 'Paso 1/4: Eliminando TurnoMedicamentos...';
    DELETE FROM TurnoMedicamentos
    WHERE TurnoId IN (
        SELECT Id FROM Turnos 
        WHERE UserId IN (SELECT UserId FROM @UsersToDelete)
    );
    PRINT '✓ TurnoMedicamentos eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
    
    -- 3.2 Eliminar Turnos
    PRINT 'Paso 2/4: Eliminando Turnos...';
    DELETE FROM Turnos
    WHERE UserId IN (SELECT UserId FROM @UsersToDelete);
    PRINT '✓ Turnos eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
    
    -- 3.3 Eliminar relaciones de roles (AspNetUserRoles)
    PRINT 'Paso 3/4: Eliminando roles de usuarios...';
    DELETE FROM AspNetUserRoles
    WHERE UserId IN (SELECT UserId FROM @UsersToDelete);
    PRINT '✓ Relaciones de roles eliminadas: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
    
    -- 3.4 Eliminar usuarios
    PRINT 'Paso 4/4: Eliminando usuarios...';
    DELETE FROM AspNetUsers
    WHERE Id IN (SELECT UserId FROM @UsersToDelete);
    PRINT '✓ Usuarios eliminados: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
    
    COMMIT TRANSACTION;
    
    PRINT '';
    PRINT '========================================================================='
    PRINT '✅ ELIMINACIÓN COMPLETADA EXITOSAMENTE'
    PRINT '========================================================================='
    PRINT '';
    PRINT 'Resumen:';
    PRINT '  • ' + CAST(@UserCount AS NVARCHAR(10)) + ' usuarios de prueba eliminados';
    PRINT '  • ' + CAST(@TotalTurnos AS NVARCHAR(10)) + ' turnos eliminados';
    PRINT '  • ' + CAST(@TotalTurnoMedicamentos AS NVARCHAR(10)) + ' turno-medicamentos eliminados';
    PRINT '';
    PRINT '📌 DATOS PRESERVADOS:';
    PRINT '  ✅ Usuarios reales (Admin, Farmacéuticos, ViewerPublic reales)';
    PRINT '  ✅ Medicamentos';
    PRINT '  ✅ Patrocinadores';
    PRINT '  ✅ Pacientes';
    PRINT '  ✅ Entregas';
    PRINT '  ✅ Donaciones';
    PRINT '  ✅ Insumos';
    PRINT '';
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    
    PRINT '';
    PRINT '========================================================================='
    PRINT '❌ ERROR EN LA ELIMINACIÓN'
    PRINT '========================================================================='
    PRINT 'Error: ' + ERROR_MESSAGE();
    PRINT 'Línea: ' + CAST(ERROR_LINE() AS NVARCHAR(10));
    PRINT '';
    PRINT '⚠️  TRANSACCIÓN REVERTIDA - NO SE ELIMINÓ NADA';
    PRINT '';
END CATCH

-- ============================================
-- 4. VERIFICACIÓN FINAL
-- ============================================
PRINT '=== Verificación final ===';
PRINT '';

-- Verificar que los usuarios ya no existen
DECLARE @RemainingUsers INT = (
    SELECT COUNT(*) 
    FROM AspNetUsers 
    WHERE Email IN (
        'maria.garcia@example.com',
        'juan.perez@example.com',
        'ana.lopez@example.com',
        'carlos.rodriguez@example.com'
    )
);

IF @RemainingUsers = 0
BEGIN
    PRINT '✅ Todos los usuarios de prueba fueron eliminados correctamente.';
END
ELSE
BEGIN
    PRINT '⚠️  Aún quedan ' + CAST(@RemainingUsers AS NVARCHAR(10)) + ' usuarios de prueba en la base de datos.';
END

-- Estadísticas actuales
PRINT '';
PRINT 'Estadísticas actuales del sistema:';
SELECT 
    (SELECT COUNT(*) FROM AspNetUsers) AS TotalUsuarios,
    (SELECT COUNT(*) FROM AspNetUsers WHERE Id IN (
        SELECT UserId FROM AspNetUserRoles WHERE RoleId IN (
            SELECT Id FROM AspNetRoles WHERE Name = 'ViewerPublic'
        )
    )) AS UsuariosViewerPublic,
    (SELECT COUNT(*) FROM Turnos) AS TotalTurnos,
    (SELECT COUNT(*) FROM TurnoMedicamentos) AS TotalTurnoMedicamentos,
    (SELECT COUNT(*) FROM Medicines) AS TotalMedicamentos,
    (SELECT COUNT(*) FROM Patients) AS TotalPacientes,
    (SELECT COUNT(*) FROM Sponsors) AS TotalPatrocinadores;

PRINT '';
PRINT 'Finalizado: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
GO
