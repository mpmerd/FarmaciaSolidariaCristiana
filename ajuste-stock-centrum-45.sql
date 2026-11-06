-- ============================================================
-- AJUSTE DE STOCK: Centrum 0 -> 45 (inventario físico 2026-11)
--
-- Contexto:
--   - Stock en BD: 0. Stock físico: 45.
--   - No hay turnos 'Aprobado' pendientes con Centrum (sección 6 vacía).
--   - Los turnos Rechazados no descontaron stock (CantidadAprobada NULL).
--   - La diferencia de 45 era el saldo de enero; el físico extra es una
--     entrada no registrada. Se registra como donación-ajuste para trazabilidad.
--
-- Ejecutar UNA SOLA VEZ en Somee.
-- Sustituir <MEDICINE_ID> por el Id real del Centrum (bloque 1 lo muestra).
-- ============================================================

-- 1) Verificar estado antes de tocar nada
SELECT Id, Name, StockQuantity FROM Medicines WHERE Name LIKE '%Centrum%';

-- 2) Guardia de seguridad: ajustar SOLO si el stock sigue en cero
UPDATE Medicines
SET StockQuantity = 45
WHERE Name LIKE '%Centrum%'
  AND StockQuantity = 0;

-- Comprobar que afectó exactamente 1 fila (si dice 0 filas, alguien ya lo cambió: no repetir)

-- 3) Dejar registro del ajuste de inventario como donación anotada
--    (mantén la coherencia: StockQuantity debe igualar din.DonationDate=ahora)
INSERT INTO Donations (MedicineId, Quantity, DonationDate, DonorNote, Comments)
SELECT Id, 45, GETDATE(),
       'AJUSTE INVENTARIO 2026-11',
       'Inventario físico 2026-11: stock físico 45 vs BD 0. Diferencia regularizada. Sin turnos Aprobado pendientes al momento del ajuste.'
FROM Medicines
WHERE Name LIKE '%Centrum%' AND StockQuantity = 45;

-- 4) Verificación final
SELECT Id, Name, StockQuantity FROM Medicines WHERE Name LIKE '%Centrum%';
