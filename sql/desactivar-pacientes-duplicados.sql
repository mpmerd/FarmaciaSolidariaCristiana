-- ============================================================
-- FIX PERSONALIZADO: Desactivar pacientes duplicados sin entregas
-- Ids decididos: 264, 371, 469, 162, 404, 290, 472, 475
--
-- Seguridad incluida: solo desactiva si la fila
--   1) existe,
--   2) está activa,
--   3) pertenece a un carnet que TIENE OTRA fila activa (duplicado),
--   4) NO tiene entregas asociadas.
-- Si alguna condición falla, esa fila se salta y se reporta.
-- Esto NO borra datos: solo oculta las filas redundantes.
-- ============================================================

-- 1) Ver ANTES de tocar nada (verificar que son los duplicados correctos)
SELECT p.Id, p.IdentificationDocument, p.FullName, p.IsActive,
       (SELECT COUNT(*) FROM Deliveries d WHERE d.PatientId = p.Id) AS TotalEntregas,
       (SELECT COUNT(*) FROM Patients p2
         WHERE p2.IdentificationDocument = p.IdentificationDocument
           AND p2.IsActive = 1) AS FilasActivasEseCarnet
FROM Patients p
WHERE p.Id IN (264, 371, 469, 162, 404, 290, 472, 475)
ORDER BY p.IdentificationDocument, p.Id;

-- 2) Desactivar (con todas las condiciones de seguridad)
UPDATE p
SET p.IsActive = 0
FROM Patients p
WHERE p.Id IN (264, 371, 469, 162, 404, 290, 472, 475)
  AND p.IsActive = 1
  AND (SELECT COUNT(*) FROM Deliveries d WHERE d.PatientId = p.Id) = 0
  AND (SELECT COUNT(*) FROM Patients p2
       WHERE p2.IdentificationDocument = p.IdentificationDocument
         AND p2.IsActive = 1
         AND p2.Id <> p.Id) >= 1;

-- 3) Verificar DESPUÉS
SELECT p.Id, p.IdentificationDocument, p.FullName, p.IsActive,
       (SELECT COUNT(*) FROM Deliveries d WHERE d.PatientId = p.Id) AS TotalEntregas
FROM Patients p
WHERE p.Id IN (264, 371, 469, 162, 404, 290, 472, 475)
ORDER BY p.IdentificationDocument, p.Id;

-- 4) Confirmación general: no deben quedar carnets duplicados ACTIVOS
SELECT IdentificationDocument, COUNT(*) AS FilasActivas
FROM Patients
WHERE IsActive = 1
GROUP BY IdentificationDocument
HAVING COUNT(*) > 1;
-- Si esta ultima consulta no devuelve filas, quedo limpio.
