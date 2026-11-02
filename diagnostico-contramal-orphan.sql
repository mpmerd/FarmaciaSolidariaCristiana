-- Diagnostico post-eliminacion de "Contramal 100 mg/ml"
-- Ejecutar en el panel de Somee (My Databases -> Query) cuando el sitio vuelva.
-- Verifica que no quedaron filas hijas apuntando al medicamento eliminado.

-- 1) El medicamento duplicado que quedo (debe existir 1 solo)
SELECT Id, Name, StockQuantity, Unit
FROM Medicines
WHERE Name LIKE 'Contramal%';

-- 2) Donaciones huerfanas (MedicineId apunta a un medicamento inexistente)
SELECT d.Id, d.MedicineId, d.Quantity, d.DonationDate
FROM Donations d
LEFT JOIN Medicines m ON m.Id = d.MedicineId
WHERE d.MedicineId IS NOT NULL AND m.Id IS NULL;

-- 3) Donaciones huerfanas por insumo
SELECT d.Id, d.SupplyId, d.Quantity, d.DonationDate
FROM Donations d
LEFT JOIN Supplies s ON s.Id = d.SupplyId
WHERE d.SupplyId IS NOT NULL AND s.Id IS NULL;

-- 4) Entregas huerfanas por medicamento
SELECT de.Id, de.MedicineId, de.Quantity, de.CreatedAt
FROM Deliveries de
LEFT JOIN Medicines m ON m.Id = de.MedicineId
WHERE de.MedicineId IS NOT NULL AND m.Id IS NULL;

-- 5) TurnoMedicamentos huerfanos
SELECT tm.Id, tm.TurnoId, tm.MedicineId, tm.CantidadSolicitada, tm.CantidadAprobada
FROM TurnoMedicamentos tm
LEFT JOIN Medicines m ON m.Id = tm.MedicineId
WHERE m.Id IS NULL;

-- 6) TurnoInsumos huerfanos
SELECT ti.Id, ti.TurnoId, ti.SupplyId, ti.CantidadSolicitada, ti.CantidadAprobada
FROM TurnoInsumos ti
LEFT JOIN Supplies s ON s.Id = ti.SupplyId
WHERE s.Id IS NULL;

-- Si las secciones 2-6 devuelven 0 filas, la BD esta consistente
-- y el 503 fue exclusivamente un problema del hosting (app pool de Somee).
