-- ============================================================
-- Pacientes bloqueados por préstamo de insumo
-- Ejecutar en la consola del servidor (SQL Server)
-- Fecha: 2026-05-08
-- ============================================================

SELECT
    p.Id                        AS [ID Paciente],
    p.FullName                  AS [Nombre completo],
    p.IdentificationDocument    AS [Carnet / Pasaporte],
    p.LoanBlockDate             AS [Fecha de bloqueo],
    p.LoanBlockDescription      AS [Insumo prestado],
    DATEDIFF(DAY, p.LoanBlockDate, GETDATE()) AS [Días bloqueado]
FROM
    Patients p
WHERE
    p.IsBlockedByLoan = 1
ORDER BY
    p.LoanBlockDate ASC;

-- ============================================================
-- Resumen: total de pacientes actualmente bloqueados
-- ============================================================

SELECT COUNT(*) AS [Total bloqueados por préstamo]
FROM Patients
WHERE IsBlockedByLoan = 1;
