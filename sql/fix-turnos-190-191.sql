-- ============================================================
-- FIX: Corrección de datos - Incidente 17/02/2026
-- Turnos 190 y 191 de la misma paciente
-- ============================================================
-- INSTRUCCIONES SOMEE:
-- 1. Ejecutar primero el bloque de VERIFICACIÓN PREVIA (solo SELECT)
-- 2. Confirmar que los datos se ven como se espera
-- 3. Ejecutar los bloques de CORRECCIÓN uno por uno
-- 4. Ejecutar la VERIFICACIÓN FINAL para confirmar el resultado
-- ============================================================


-- ============================================================
-- VERIFICACIÓN PREVIA (ejecutar primero, solo lectura)
-- ============================================================

SELECT
    t.Id            AS TurnoId,
    t.NumeroTurno,
    t.Estado,
    t.CanceladoPorNoPresentacion,
    t.FechaEntrega,
    d.Id            AS EntregaId,
    m.Name          AS Medicamento,
    d.Quantity,
    d.TurnoId       AS EntregaTurnoId
FROM Turnos t
LEFT JOIN Deliveries d ON d.TurnoId = t.Id
LEFT JOIN Medicines  m ON m.Id = d.MedicineId
WHERE t.Id IN (190, 191)
ORDER BY t.Id, d.Id;

-- Stock actual del Losartán 50mg (debe estar 28 unidades de más antes de la corrección):
SELECT Id, Name, StockQuantity FROM Medicines WHERE Name = 'Losartán 50mg';


-- ============================================================
-- CORRECCIÓN 1: Reasignar la entrega del Losartán 50mg
-- del turno 191 (incorrecto) al turno 190 (correcto)
-- ============================================================

UPDATE Deliveries
SET TurnoId = 190
WHERE TurnoId = 191
  AND MedicineId = (SELECT Id FROM Medicines WHERE Name = 'Losartán 50mg');


-- ============================================================
-- CORRECCIÓN 2: Eliminar el comentario de cancelación automática
-- del turno 190 (el texto fue agregado por el sistema en la noche)
-- ============================================================

UPDATE Turnos
SET ComentariosFarmaceutico = NULLIF(
        LTRIM(RTRIM(
            REPLACE(
                REPLACE(
                    REPLACE(COALESCE(ComentariosFarmaceutico, ''),
                        CHAR(10) + '[CANCELADO AUTOMÁTICAMENTE - ' + CONVERT(VARCHAR(10), GETDATE(), 103) + ' ' + CONVERT(VARCHAR(5), GETDATE(), 108) + ']', ''),
                    CHAR(10) + 'Motivo: Usuario no asistió a la farmacia en la fecha programada', ''),
                -- Fallback: limpiar cualquier bloque de cancelación automática del día de hoy
                CHAR(10) + '[CANCELADO AUTOMÁTICAMENTE', '')
        )),
        '')
WHERE Id = 190;


-- ============================================================
-- CORRECCIÓN 3: Descontar las 28 unidades de Losartán 50mg del stock
-- Motivo: cuando el sistema canceló el turno 190 por no-presentación,
-- devolvió automáticamente las 28 unidades que había reservado al aprobar
-- el turno, dejando el stock 28 unidades más alto de lo real
-- (el medicamento fue entregado físicamente pero el stock no lo refleja).
-- ============================================================

UPDATE Medicines
SET StockQuantity = StockQuantity - 28
WHERE Name = 'Losartán 50mg';


-- ============================================================
-- CORRECCIÓN 4: Revertir el turno 190 a Completado
-- ============================================================

UPDATE Turnos
SET Estado                      = 'Completado',
    CanceladoPorNoPresentacion  = 0,
    FechaEntrega                = (SELECT TOP 1 DeliveryDate FROM Deliveries WHERE TurnoId = 190 ORDER BY DeliveryDate DESC)
WHERE Id = 190;


-- ============================================================
-- VERIFICACIÓN FINAL (debe mostrar 190=Completado, 191=Completado,
-- cada entrega con su turno correcto)
-- ============================================================

SELECT
    t.Id            AS TurnoId,
    t.NumeroTurno,
    t.Estado,
    t.CanceladoPorNoPresentacion,
    t.FechaEntrega,
    d.Id            AS EntregaId,
    m.Name          AS Medicamento,
    d.Quantity,
    d.TurnoId       AS EntregaTurnoId,
    t.ComentariosFarmaceutico
FROM Turnos t
LEFT JOIN Deliveries d ON d.TurnoId = t.Id
LEFT JOIN Medicines  m ON m.Id = d.MedicineId
WHERE t.Id IN (190, 191)
ORDER BY t.Id, d.Id;

-- Verificar que el stock del Losartán quedó correcto (28 unidades menos que antes):
SELECT Id, Name, StockQuantity FROM Medicines WHERE Name = 'Losartán 50mg';
