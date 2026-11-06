-- ============================================================
-- Reporte: Turnos APROBADOS con el medicamento Centrum
-- Muestra: NumeroTurno, FechaPreferida, Medicamento y Cantidad
-- Ejecutar en Somee (solo SELECT, es seguro)
-- ============================================================

SELECT
    t.NumeroTurno,
    CAST(t.FechaPreferida AS varchar(30)) AS FechaPreferida,
    m.Name AS Medicamento,
    tm.CantidadAprobada AS Cantidad
FROM Turnos t
JOIN TurnoMedicamentos tm ON tm.TurnoId = t.Id
JOIN Medicines m ON m.Id = tm.MedicineId
WHERE t.Estado = 'Aprobado'
  AND m.Name LIKE '%Centrum%'
ORDER BY t.FechaPreferida, t.NumeroTurno;
