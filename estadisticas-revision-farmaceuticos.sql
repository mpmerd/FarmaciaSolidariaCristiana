-- =====================================================================
-- Estadísticas de revisión de turnos por usuarios con rol Farmaceutico
-- Muestra, por cada farmacéutico, qué porcentaje del total de turnos
-- revisados ha aprobado y cuál ha rechazado.
-- (SQL Server / Somee)
-- =====================================================================

SELECT
    u.Id                                                       AS UsuarioId,
    u.Email                                                    AS UsuarioEmail,
    u.UserName                                                 AS UsuarioNombre,
    COUNT(t.Id)                                                AS TotalTurnosRevisados,
    SUM(CASE WHEN t.Estado IN ('Aprobado', 'Completado')
             THEN 1 ELSE 0 END)                                AS TurnosAprobados,
    SUM(CASE WHEN t.Estado = 'Rechazado'
             THEN 1 ELSE 0 END)                                AS TurnosRechazados,
    CAST(SUM(CASE WHEN t.Estado IN ('Aprobado', 'Completado')
                  THEN 1 ELSE 0 END) * 100.0 / COUNT(t.Id)
         AS DECIMAL(5,2))                                      AS PorcentajeAprobados,
    CAST(SUM(CASE WHEN t.Estado = 'Rechazado'
                  THEN 1 ELSE 0 END) * 100.0 / COUNT(t.Id)
         AS DECIMAL(5,2))                                      AS PorcentajeRechazados,
    CAST(COUNT(t.Id) * 100.0 / (SELECT COUNT(*) FROM Turnos)
         AS DECIMAL(5,2))                                      AS PorcentajeDelTotalGeneral
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON ur.UserId = u.Id
INNER JOIN AspNetRoles r      ON r.Id = ur.RoleId AND r.Name = 'Farmaceutico'
INNER JOIN Turnos t           ON t.RevisadoPorId = u.Id
GROUP BY u.Id, u.Email, u.UserName
ORDER BY TotalTurnosRevisados DESC;

-- ---------------------------------------------------------------------
-- Versión incluyendo farmacéuticos sin turnos revisados aún
-- ---------------------------------------------------------------------

SELECT
    u.Id                                                       AS UsuarioId,
    u.Email                                                    AS UsuarioEmail,
    u.UserName                                                 AS UsuarioNombre,
    COUNT(t.Id)                                                AS TotalTurnosRevisados,
    ISNULL(SUM(CASE WHEN t.Estado IN ('Aprobado', 'Completado')
                    THEN 1 ELSE 0 END), 0)                     AS TurnosAprobados,
    ISNULL(SUM(CASE WHEN t.Estado = 'Rechazado'
                    THEN 1 ELSE 0 END), 0)                     AS TurnosRechazados,
    ISNULL(CAST(SUM(CASE WHEN t.Estado IN ('Aprobado', 'Completado')
                         THEN 1 ELSE 0 END) * 100.0 / COUNT(t.Id)
              AS DECIMAL(5,2)), 0)                             AS PorcentajeAprobados,
    ISNULL(CAST(SUM(CASE WHEN t.Estado = 'Rechazado'
                         THEN 1 ELSE 0 END) * 100.0 / COUNT(t.Id)
              AS DECIMAL(5,2)), 0)                             AS PorcentajeRechazados,
    ISNULL(CAST(COUNT(t.Id) * 100.0 / (SELECT COUNT(*) FROM Turnos)
              AS DECIMAL(5,2)), 0)                             AS PorcentajeDelTotalGeneral
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON ur.UserId = u.Id
INNER JOIN AspNetRoles r      ON r.Id = ur.RoleId AND r.Name = 'Farmaceutico'
LEFT  JOIN Turnos t           ON t.RevisadoPorId = u.Id
GROUP BY u.Id, u.Email, u.UserName
ORDER BY TotalTurnosRevisados DESC;
