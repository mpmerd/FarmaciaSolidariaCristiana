-- Script para corregir stock inflado por cancelaciones incorrectas del 27/01/2026
-- 
-- PROBLEMA:
-- El TurnoCleanupService canceló turnos que YA tenían entregas completadas,
-- devolviendo el stock al inventario cuando NO debía hacerlo.
--
-- RESULTADO: El stock está inflado (tiene más cantidad de la real)
--
-- SOLUCIÓN:
-- 1. Identificar turnos cancelados que tienen entregas
-- 2. Ver las cantidades que fueron incorrectamente devueltas
-- 3. Re-descontarlas del stock

-- ============================================================
-- PASO 1: Identificar los turnos afectados
-- ============================================================

PRINT '========================================';
PRINT 'Identificando turnos cancelados incorrectamente...';
PRINT '========================================';
PRINT '';

-- Buscar turnos que:
-- - Están en estado "Cancelado"
-- - Tienen entregas registradas (Deliveries con TurnoId)
-- - Fueron cancelados recientemente (27-28 enero 2026)

SELECT 
    T.Id AS TurnoId,
    T.NumeroTurno,
    T.Estado,
    T.FechaPreferida AS FechaAsignada,
    T.FechaRevision AS FechaCancelacion,
    U.UserName AS Paciente,
    COUNT(DISTINCT D.Id) AS CantidadEntregas,
    STRING_AGG(M.Name, ', ') AS MedicamentosEntregados
FROM Turnos T
INNER JOIN AspNetUsers U ON T.UserId = U.Id
LEFT JOIN Deliveries D ON D.TurnoId = T.Id
LEFT JOIN Medicines M ON D.MedicineId = M.Id
WHERE T.Estado = 'Cancelado'
  AND T.FechaRevision >= '2026-01-27' -- Fecha del problema
  AND T.FechaRevision < '2026-01-29'  -- Ventana de 2 días
  AND D.Id IS NOT NULL -- Que tengan entregas
GROUP BY T.Id, T.NumeroTurno, T.Estado, T.FechaPreferida, T.FechaRevision, U.UserName
ORDER BY T.FechaRevision DESC;

PRINT '';
PRINT '========================================';
PRINT 'Cantidades que deben RE-DESCONTARSE del stock';
PRINT '========================================';
PRINT '';

-- Ver las cantidades APROBADAS que fueron incorrectamente devueltas
SELECT 
    T.Id AS TurnoId,
    T.NumeroTurno,
    M.Name AS Medicamento,
    TM.CantidadAprobada AS CantidadDevueltaIncorrectamente,
    M.StockQuantity AS StockActual,
    (M.StockQuantity - TM.CantidadAprobada) AS StockCorregido
FROM Turnos T
INNER JOIN TurnoMedicamentos TM ON TM.TurnoId = T.Id
INNER JOIN Medicines M ON M.Id = TM.MedicineId
WHERE T.Estado = 'Cancelado'
  AND T.FechaRevision >= '2026-01-27'
  AND T.FechaRevision < '2026-01-29'
  AND TM.CantidadAprobada IS NOT NULL
  AND EXISTS (SELECT 1 FROM Deliveries D WHERE D.TurnoId = T.Id)

UNION ALL

SELECT 
    T.Id AS TurnoId,
    T.NumeroTurno,
    S.Name AS Insumo,
    TI.CantidadAprobada AS CantidadDevueltaIncorrectamente,
    S.StockQuantity AS StockActual,
    (S.StockQuantity - TI.CantidadAprobada) AS StockCorregido
FROM Turnos T
INNER JOIN TurnoInsumos TI ON TI.TurnoId = T.Id
INNER JOIN Supplies S ON S.Id = TI.SupplyId
WHERE T.Estado = 'Cancelado'
  AND T.FechaRevision >= '2026-01-27'
  AND T.FechaRevision < '2026-01-29'
  AND TI.CantidadAprobada IS NOT NULL
  AND EXISTS (SELECT 1 FROM Deliveries D WHERE D.TurnoId = T.Id);

PRINT '';
PRINT '========================================';
PRINT 'SCRIPT DE CORRECCIÓN';
PRINT '========================================';
PRINT '';
PRINT '⚠️  INSTRUCCIONES:';
PRINT '';
PRINT '1. REVISA los resultados de las consultas anteriores';
PRINT '2. VERIFICA que los turnos listados son los del problema de ayer';
PRINT '3. Si es correcto, DESCOMENTA y EJECUTA el bloque de corrección abajo';
PRINT '';
PRINT '========================================';
PRINT '';

-- ============================================================
-- PASO 2: SCRIPT DE CORRECCIÓN (DESCOMENTA PARA EJECUTAR)
-- ============================================================

/*
BEGIN TRANSACTION;

PRINT 'Corrigiendo stock de MEDICAMENTOS...';

-- Re-descontar medicamentos que fueron incorrectamente devueltos
UPDATE M
SET M.StockQuantity = M.StockQuantity - TM.CantidadAprobada
FROM Medicines M
INNER JOIN TurnoMedicamentos TM ON TM.MedicineId = M.Id
INNER JOIN Turnos T ON T.Id = TM.TurnoId
WHERE T.Estado = 'Cancelado'
  AND T.FechaRevision >= '2026-01-27'
  AND T.FechaRevision < '2026-01-29'
  AND TM.CantidadAprobada IS NOT NULL
  AND EXISTS (SELECT 1 FROM Deliveries D WHERE D.TurnoId = T.Id);

PRINT 'Stock de medicamentos corregido.';
PRINT '';

PRINT 'Corrigiendo stock de INSUMOS...';

-- Re-descontar insumos que fueron incorrectamente devueltos
UPDATE S
SET S.StockQuantity = S.StockQuantity - TI.CantidadAprobada
FROM Supplies S
INNER JOIN TurnoInsumos TI ON TI.SupplyId = S.Id
INNER JOIN Turnos T ON T.Id = TI.TurnoId
WHERE T.Estado = 'Cancelado'
  AND T.FechaRevision >= '2026-01-27'
  AND T.FechaRevision < '2026-01-29'
  AND TI.CantidadAprobada IS NOT NULL
  AND EXISTS (SELECT 1 FROM Deliveries D WHERE D.TurnoId = T.Id);

PRINT 'Stock de insumos corregido.';
PRINT '';

-- OPCIONAL: Cambiar el estado de los turnos a "Completado" ya que sí se entregaron
UPDATE T
SET T.Estado = 'Completado',
    T.ComentariosFarmaceutico = T.ComentariosFarmaceutico + CHAR(13) + CHAR(10) + 
        '[CORREGIDO - ' + CONVERT(VARCHAR, GETDATE(), 120) + '] Turno marcado como Completado porque sí tuvo entregas. Cancelación automática fue incorrecta.'
FROM Turnos T
WHERE T.Estado = 'Cancelado'
  AND T.FechaRevision >= '2026-01-27'
  AND T.FechaRevision < '2026-01-29'
  AND EXISTS (SELECT 1 FROM Deliveries D WHERE D.TurnoId = T.Id);

PRINT 'Estados de turnos corregidos.';
PRINT '';

-- Verificar resultados
PRINT '========================================';
PRINT 'Verificando correcciones...';
PRINT '========================================';

SELECT 
    M.Name AS Medicamento,
    M.StockQuantity AS StockCorregido
FROM Medicines M
WHERE M.Id IN (
    SELECT DISTINCT TM.MedicineId 
    FROM TurnoMedicamentos TM
    INNER JOIN Turnos T ON T.Id = TM.TurnoId
    WHERE T.Estado = 'Completado'
      AND T.FechaRevision >= '2026-01-27'
      AND T.FechaRevision < '2026-01-29'
);

-- SI TODO SE VE BIEN, hacer commit:
COMMIT;
PRINT '';
PRINT '✓ CORRECCIÓN COMPLETADA EXITOSAMENTE';

-- SI ALGO SALE MAL, hacer rollback:
-- ROLLBACK;
-- PRINT 'X CORRECCIÓN CANCELADA - Se hizo rollback';
*/

PRINT '';
PRINT '========================================';
PRINT 'FIN DEL SCRIPT';
PRINT '========================================';
