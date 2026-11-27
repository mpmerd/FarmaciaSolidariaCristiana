-- ============================================================
-- DIAGNOSTICO: Turnos 5019 y 5020 - mismo medicamento (Terazosab 5 mg)
-- Objetivo: determinar por qué el backend permitió el duplicado.
--   Vías de escape posibles:
--   a) DocumentoIdentidadHash distinto en cada turno (documento
--      equivocado en uno de ellos => el sistema vio "otro paciente")
--   b) El turno anterior estaba ya Rechazado/Cancelado al crearse el
--      segundo (esos estados NO cuentan en la restricción)
--   c) Era un insumo (los insumos no tienen chequeo duplicado) -- en
--      este caso es medicamento, descartado de antemano
-- Ejecutar en la consola SQL de Somee (solo SELECT, es seguro)
-- ============================================================

-- 1) Cabecera de ambos turnos: usuario, estado, fechas y hash del documento
SELECT t.Id AS TurnoId, t.UserId, u.UserName, t.Estado,
       CAST(t.FechaSolicitud AS varchar(30)) AS FechaSolicitud,
       CAST(t.FechaRevision  AS varchar(30)) AS FechaRevision,
       t.DocumentoIdentidadHash,
       t.ComentariosFarmaceutico, t.NotasSolicitante
FROM Turnos t
LEFT JOIN AspNetUsers u ON u.Id = t.UserId
WHERE t.Id IN (5019, 5020)
ORDER BY t.Id;

-- 2) Veredicto directo: ¿mismo documento de identidad?
SELECT (SELECT DocumentoIdentidadHash FROM Turnos WHERE Id = 5019) AS Hash5019,
       (SELECT DocumentoIdentidadHash FROM Turnos WHERE Id = 5020) AS Hash5020,
       CASE
           WHEN (SELECT DocumentoIdentidadHash FROM Turnos WHERE Id = 5019)
              = (SELECT DocumentoIdentidadHash FROM Turnos WHERE Id = 5020)
           THEN 'MISMO documento: el backend DEBIO bloquearlo -> revisar secuencia 5) por estado/fechas'
           ELSE 'DOCUMENTOS DISTINTOS: mismo paciente con un numero equivocado (via de escape esperada, caso 4654/4656)'
       END AS Veredicto;

-- 3) Medicamentos solicitados en cada turno (confirmar el duplicado de Terazosab)
SELECT tm.TurnoId, m.Id AS MedicineId, m.Name, m.Unit,
       tm.CantidadSolicitada, tm.CantidadAprobada
FROM TurnoMedicamentos tm
JOIN Medicines m ON m.Id = tm.MedicineId
WHERE tm.TurnoId IN (5019, 5020)
ORDER BY tm.TurnoId, m.Name;

-- 4) ¿Cuál de los dos hashes corresponde a un paciente registrado?
--    El hash del turno = SHA256 en base64 de UPPER(TRIM(documento)).
--    (Válido para documentos ASCII: carnets de 11 dígitos y pasaportes sin acentos.)
--    El turno cuyo hash NO aparezca aquí es el del documento equivocado.
SELECT t.Id AS TurnoId, t.DocumentoIdentidadHash,
       p.Id AS PatientId, p.IdentificationDocument, p.FullName, p.IsActive
FROM Turnos t
LEFT JOIN (
    SELECT Id, IdentificationDocument, FullName, IsActive,
           CAST('' AS xml).value('xs:base64Binary(xs:hexBinary(sql:column("x.HashHex")))', 'varchar(44)') AS DocHash
    FROM (
        SELECT Id, IdentificationDocument, FullName, IsActive,
               CONVERT(varchar(max),
                       HASHBYTES('SHA2_256', UPPER(LTRIM(RTRIM(IdentificationDocument)))),
                       1) AS HashHex
        FROM Patients
        WHERE IdentificationDocument IS NOT NULL
    ) x
) p ON p.DocHash = t.DocumentoIdentidadHash
WHERE t.Id IN (5019, 5020)
ORDER BY t.Id;

-- 5) Secuencia: TODOS los turnos de ese usuario (cualquier documento)
--    Para ver si 5019 ya estaba Rechazado cuando se creó 5020,
--    o si son de documentos distintos pero la misma cuenta.
SELECT t.Id, t.Estado, t.DocumentoIdentidadHash,
       CAST(t.FechaSolicitud AS varchar(30)) AS FechaSolicitud,
       CAST(t.FechaRevision  AS varchar(30)) AS FechaRevision,
       t.CanceladoPorNoPresentacion, t.ComentariosFarmaceutico
FROM Turnos t
WHERE t.UserId = (SELECT UserId FROM Turnos WHERE Id = 5019)
ORDER BY t.FechaSolicitud DESC;

-- 6) Identificar el medicamento Terazosab en el catálogo
SELECT m.Id, m.Name, m.StockQuantity, m.Unit
FROM Medicines m
WHERE m.Name LIKE '%Terazos%';

-- 7) ¿Hay más casos como este? Mismo usuario, dos turnos del mismo mes con
--    un medicamento en común pero con DOCUMENTOS DISTINTOS (escape por typo)
SELECT a.Id AS TurnoA, b.Id AS TurnoB, u.UserName,
       a.DocumentoIdentidadHash AS HashA, b.DocumentoIdentidadHash AS HashB,
       m.Name AS MedicamentoComun,
       CAST(b.FechaSolicitud AS varchar(30)) AS FechaTurnoB
FROM TurnoMedicamentos tma
JOIN TurnoMedicamentos tmb
     ON tmb.MedicineId = tma.MedicineId AND tmb.TurnoId > tma.TurnoId
JOIN Turnos a ON a.Id = tma.TurnoId
JOIN Turnos b ON b.Id = tmb.TurnoId
JOIN Medicines m ON m.Id = tma.MedicineId
JOIN AspNetUsers u ON u.Id = a.UserId
WHERE a.UserId = b.UserId
  AND a.DocumentoIdentidadHash <> b.DocumentoIdentidadHash
  AND YEAR(a.FechaSolicitud) = YEAR(b.FechaSolicitud)
  AND MONTH(a.FechaSolicitud) = MONTH(b.FechaSolicitud)
  AND a.Estado IN ('Pendiente', 'Aprobado', 'Completado')
  AND b.Estado IN ('Pendiente', 'Aprobado', 'Completado')
ORDER BY b.FechaSolicitud DESC;

-- 8) Contraste: duplicados con el MISMO documento (debería devolver 0 filas;
--    si devuelve algo, habría un bug real en la validación del backend)
SELECT a.Id AS TurnoA, b.Id AS TurnoB, m.Name AS MedicamentoComun,
       CAST(b.FechaSolicitud AS varchar(30)) AS FechaTurnoB
FROM TurnoMedicamentos tma
JOIN TurnoMedicamentos tmb
     ON tmb.MedicineId = tma.MedicineId AND tmb.TurnoId > tma.TurnoId
JOIN Turnos a ON a.Id = tma.TurnoId
JOIN Turnos b ON b.Id = tmb.TurnoId
JOIN Medicines m ON m.Id = tma.MedicineId
WHERE a.DocumentoIdentidadHash = b.DocumentoIdentidadHash
  AND YEAR(a.FechaSolicitud) = YEAR(b.FechaSolicitud)
  AND MONTH(a.FechaSolicitud) = MONTH(b.FechaSolicitud)
  AND a.Estado IN ('Pendiente', 'Aprobado', 'Completado')
  AND b.Estado IN ('Pendiente', 'Aprobado', 'Completado')
ORDER BY b.FechaSolicitud DESC;
