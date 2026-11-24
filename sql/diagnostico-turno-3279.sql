-- ============================================================
-- DIAGNOSTICO: Turno 3279 del paciente 68041401061 (Aprobado sin entregar)
-- Ejecutar en Somee (solo SELECT, es seguro)
-- Sirve para ver qué se aprobó, con qué cantidades y stock actual,
-- y así entender por qué la entrega fue rechazada silenciosamente.
-- ============================================================

-- 1) Datos del turno
SELECT Id, Estado, CAST(FechaSolicitud AS varchar(30)) AS FechaSolicitud,
       CAST(FechaEntrega AS varchar(30)) AS FechaEntrega
FROM Turnos WHERE Id = 3279;

-- 2) Medicamentos aprobados en el turno (nombre, cantidades y stock actual)
SELECT tm.TurnoId, tm.MedicineId, m.Name AS Medicamento,
       tm.CantidadSolicitada, tm.CantidadAprobada,
       m.StockQuantity AS StockActual, tm.Notas
FROM TurnoMedicamentos tm
JOIN Medicines m ON m.Id = tm.MedicineId
WHERE tm.TurnoId = 3279;

-- 3) Insumos aprobados en el turno (nombre, cantidades y stock actual)
SELECT ti.TurnoId, ti.SupplyId, s.Name AS Insumo,
       ti.CantidadSolicitada, ti.CantidadAprobada,
       s.StockQuantity AS StockActual, ti.Notas
FROM TurnoInsumos ti
JOIN Supplies s ON s.Id = ti.SupplyId
WHERE ti.TurnoId = 3279;

-- 4) ¿Ya hay entregas asociadas a ese turno? (debería estar vacío)
SELECT Id, PatientId, MedicineId, SupplyId, Quantity,
       CAST(DeliveryDate AS varchar(30)) AS DeliveryDate
FROM Deliveries
WHERE TurnoId = 3279;
