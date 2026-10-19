-- ============================================================
-- REVISION: Carnets de pacientes duplicados (8 pares detectados)
-- Ejecutar en Somee. La primera parte es solo SELECT.
-- La segunda parte (opcional) desactiva la fila que USTED decida.
-- ============================================================

-- 1) Ver cada par de duplicados con sus entregas asociadas
--    para decidir cual fila conservar (la que tenga las entregas
--    del historial real normalmente es la que se conserva)
SELECT p.Id, p.IdentificationDocument, p.FullName, p.IsActive,
       CAST(p.RegistrationDate AS varchar(30)) AS RegistrationDate,
       (SELECT COUNT(*) FROM Deliveries d WHERE d.PatientId = p.Id) AS TotalEntregas
FROM Patients p
WHERE p.IdentificationDocument IN (
    SELECT IdentificationDocument
    FROM Patients
    GROUP BY IdentificationDocument
    HAVING COUNT(*) > 1
)
ORDER BY p.IdentificationDocument, p.Id;

-- ============================================================
-- 2) OPCIONAL: desactivar la fila redundante.
--    Cambie XXXX por el Id de la fila que quiere desactivar
--    y quite el guion de comentario antes de ejecutar.
--    Esto NO borra datos: solo la oculta de listas/busquedas
--    (las entregas historicas se conservan).
-- ============================================================
-- UPDATE Patients SET IsActive = 0 WHERE Id = XXXX;
