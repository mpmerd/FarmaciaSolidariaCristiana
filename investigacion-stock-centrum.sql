-- ============================================================
-- INVESTIGACIÓN: Medicamento Centrum
-- Objetivo: detectar inconsistencias de stock.
--   1) Datos actuales del medicamento (stock en cero)
--   2) Entregas con turno vs sin turno desde 2026-01-01
--   3) Donaciones (entradas de stock) desde 2026-01-01
--   4) Reconstrucción del balance de stock: entradas - salidas vs stock actual
--   5) Entregas duplicadas o con cantidad sospechosa
--   6) Turnos aprobados que reservaron/descontaron stock pero nunca se entregaron
-- Ejecutar en Somee (solo SELECT, es seguro)
-- ============================================================

-- 1) Datos actuales del medicamento
SELECT Id, Name, StockQuantity, Unit, NationalCode
FROM Medicines
WHERE Name LIKE '%Centrum%';

-- 2) Entregas del Centrum desde enero de este año
--    (con turno, sin turno, y resumen por mes)
SELECT d.Id,
       d.PatientIdentification,
       d.PatientId,
       d.TurnoId,                          -- NULL = entrega SIN turno
       d.Quantity,
       CAST(d.DeliveryDate AS varchar(30)) AS DeliveryDate,
       d.DeliveredBy,
       d.Comments
FROM Deliveries d
JOIN Medicines m ON m.Id = d.MedicineId
WHERE m.Name LIKE '%Centrum%'
  AND d.DeliveryDate >= '2026-01-01'
ORDER BY d.DeliveryDate;

-- Resumen: con/sin turno por mes
SELECT YEAR(d.DeliveryDate) AS Anio,
       MONTH(d.DeliveryDate) AS Mes,
       CASE WHEN d.TurnoId IS NULL THEN 'SIN TURNO' ELSE 'CON TURNO' END AS Tipo,
       COUNT(*) AS Entregas,
       SUM(d.Quantity) AS TotalUnidades
FROM Deliveries d
JOIN Medicines m ON m.Id = d.MedicineId
WHERE m.Name LIKE '%Centrum%'
  AND d.DeliveryDate >= '2026-01-01'
GROUP BY YEAR(d.DeliveryDate), MONTH(d.DeliveryDate),
         CASE WHEN d.TurnoId IS NULL THEN 'SIN TURNO' ELSE 'CON TURNO' END
ORDER BY Anio, Mes, Tipo;

-- Entregas sin turno (detalle, para revisar quién entregó y por qué)
SELECT d.Id,
       d.PatientIdentification,
       d.Quantity,
       CAST(d.DeliveryDate AS varchar(30)) AS DeliveryDate,
       d.DeliveredBy,
       d.Comments
FROM Deliveries d
JOIN Medicines m ON m.Id = d.MedicineId
WHERE m.Name LIKE '%Centrum%'
  AND d.TurnoId IS NULL
  AND d.DeliveryDate >= '2026-01-01'
ORDER BY d.DeliveryDate;

-- 3) Donaciones (entradas de stock) del Centrum desde enero
SELECT dn.Id,
       dn.Quantity,
       CAST(dn.DonationDate AS varchar(30)) AS DonationDate,
       dn.DonorNote,
       dn.Comments
FROM Donations dn
JOIN Medicines m ON m.Id = dn.MedicineId
WHERE m.Name LIKE '%Centrum%'
  AND dn.DonationDate >= '2026-01-01'
ORDER BY dn.DonationDate;

-- 4) Balance de stock desde el 1 de enero:
--    entradas (donaciones) - salidas (entregas) vs stock actual
--    Si StockActual no coincide con el balance, hay inconsistencia.
;WITH C AS (SELECT Id, StockQuantity FROM Medicines WHERE Name LIKE '%Centrum%'),
E AS (SELECT COALESCE(SUM(dn.Quantity),0) AS Entradas
      FROM Donations dn JOIN C ON dn.MedicineId = C.Id
      WHERE dn.DonationDate >= '2026-01-01'),
S AS (SELECT COALESCE(SUM(d.Quantity),0) AS Salidas
      FROM Deliveries d JOIN C ON d.MedicineId = C.Id
      WHERE d.DeliveryDate >= '2026-01-01')
SELECT C.Id, C.StockQuantity AS StockActual,
       E.Entradas AS DonacionesDesdeEnero,
       S.Salidas AS EntregasDesdeEnero,
       E.Entradas - S.Salidas AS BalanceCalculado,
       C.StockQuantity - (E.Entradas - S.Salidas) AS DiferenciaSinExplicar
FROM C, E, S;

-- 5) Posibles duplicados: entregas del mismo paciente el mismo día con cantidad anormal
SELECT d.PatientIdentification,
       CAST(d.DeliveryDate AS date) AS Dia,
       COUNT(*) AS NumEntregas,
       SUM(d.Quantity) AS UnidadesTotales
FROM Deliveries d
JOIN Medicines m ON m.Id = d.MedicineId
WHERE m.Name LIKE '%Centrum%'
  AND d.DeliveryDate >= '2026-01-01'
GROUP BY d.PatientIdentification, CAST(d.DeliveryDate AS date)
HAVING COUNT(*) > 1 OR SUM(d.Quantity) > 5
ORDER BY Dia;

-- 6) Turnos aprobados con Centrum que nunca fueron retirados/entregados
--    (el stock pudo quedar descontado o reservado sin salida real)
SELECT t.Id AS TurnoId,
       t.NumeroTurno,
       t.Estado,
       CAST(t.FechaPreferida AS varchar(30)) AS FechaPreferida,
       tm.CantidadAprobada,
       DATEDIFF(day, t.FechaPreferida, GETDATE()) AS DiasDesdeFechaTurno
FROM Turnos t
JOIN TurnoMedicamentos tm ON tm.TurnoId = t.Id
JOIN Medicines m ON m.Id = tm.MedicineId
LEFT JOIN Deliveries d ON d.TurnoId = t.Id AND d.MedicineId = tm.MedicineId
WHERE m.Name LIKE '%Centrum%'
  AND t.Estado = 'Aprobado'
  AND d.Id IS NULL
  AND t.FechaPreferida IS NOT NULL
ORDER BY t.FechaPreferida;
