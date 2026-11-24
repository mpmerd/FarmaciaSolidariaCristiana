-- ============================================================
-- DIAGNOSTICO: Paciente con carnet 68041401061
-- Ejecutar en la consola SQL de Somee (solo SELECT, es seguro)
-- ============================================================

-- 1) Filas de pacientes con ese carnet (activo O inactivo)
--    Si hay más de una fila => hay duplicados
SELECT Id, IdentificationDocument, FullName, Age, Gender,
       IsActive, IsBlockedByLoan,
       CAST(RegistrationDate AS varchar(30)) AS RegistrationDate,
       LEN(IdentificationDocument) AS LongitudCarnet
FROM Patients
WHERE IdentificationDocument = '68041401061';
SELECT COUNT(*) AS TotalFilasEseCarnet FROM Patients WHERE IdentificationDocument = '68041401061';

-- 2) Conteo general de pacientes (la app movil solo carga 500)
SELECT
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS PacientesActivos,
    SUM(CASE WHEN IsActive = 0 THEN 1 ELSE 0 END) AS PacientesInactivos,
    COUNT(*) AS Total
FROM Patients;

-- 3) Carnets duplicados en general (por si hay más casos)
SELECT IdentificationDocument, COUNT(*) AS Repeticiones
FROM Patients
GROUP BY IdentificationDocument
HAVING COUNT(*) > 1
ORDER BY Repeticiones DESC;

-- 4) Entregas registradas para ese paciente (por PatientId)
SELECT d.Id, d.PatientId, d.MedicineId, d.SupplyId, d.Quantity,
       d.TurnoId, CAST(d.DeliveryDate AS varchar(30)) AS DeliveryDate,
       CAST(d.CreatedAt AS varchar(30)) AS CreatedAt, d.DeliveredBy
FROM Deliveries d
WHERE d.PatientId IN (SELECT Id FROM Patients WHERE IdentificationDocument = '68041401061')
ORDER BY d.Id DESC;

-- 5) Turnos asociados a ese carnet (via hash SHA256 base64 del carnet en mayusculas)
SELECT t.Id, t.Estado, CAST(t.FechaSolicitud AS varchar(30)) AS FechaSolicitud,
       CAST(t.FechaEntrega AS varchar(30)) AS FechaEntrega,
       t.CanceladoPorNoPresentacion
FROM Turnos t
WHERE t.DocumentoIdentidadHash = '8kdH2vaf9WEkVU93bzLILBpF5Ca/5WtTzvcnuyOHZ4E=';

-- 6) Verificar que las columnas criticas existen en las tablas
--    (si falta alguna, el INSERT de entrega falla con error SQL oculto)
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Deliveries'
  AND COLUMN_NAME IN ('TurnoId','CreatedAt','Dosage','TreatmentDuration',
                      'DeliveredBy','PatientIdentification','SupplyId',
                      'LocationDetails','Observations','MedicineId','PatientId')
ORDER BY COLUMN_NAME;

SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Patients'
  AND COLUMN_NAME IN ('IsActive','IsBlockedByLoan','LoanBlockDate',
                      'LoanBlockDescription','LoanUnblockDate','LoanUnblockedByUserId')
ORDER BY COLUMN_NAME;

-- 7) Medicamentos e insumos sin stock (causa tipica de entrega rechazada silenciosamente)
SELECT 'Medicine' AS Tipo, Id, Name, StockQuantity, Unit
FROM Medicines WHERE StockQuantity <= 0
UNION ALL
SELECT 'Supply', Id, Name, StockQuantity, Unit
FROM Supplies WHERE StockQuantity <= 0;

-- 8) Ultimas 10 entregas registradas (para ver si el flujo funciona para otros pacientes)
SELECT TOP 10 d.Id, d.PatientId, d.Quantity, d.TurnoId,
       CAST(d.DeliveryDate AS varchar(30)) AS DeliveryDate, d.DeliveredBy
FROM Deliveries d
ORDER BY d.Id DESC;
