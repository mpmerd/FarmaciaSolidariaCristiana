-- =====================================================================
-- entregas-sin-turno-por-farmaceutico.sql
-- Muestra, por cada usuario con rol Farmaceutico, la cantidad de
-- entregas SIN turno (Deliveries con TurnoId NULL) que ha realizado:
--   1. La cantidad absoluta de entregas sin turno.
--   2. El porcentaje respecto al total de entregas sin turno.
--   3. El porcentaje respecto al total general de turnos de la farmacia.
-- Nota: se relaciona el usuario vía DeliveredBy, que se registra con
--       User.Identity.Name (el UserName del usuario).
-- (SQL Server / Somee)
-- =====================================================================

SELECT
    u.Email                                                    AS UsuarioEmail,
    u.UserName                                                 AS UsuarioNombre,
    COUNT(d.Id)                                                AS EntregasSinTurno,
    CAST(COUNT(d.Id) * 100.0
         / (SELECT COUNT(*) FROM Deliveries WHERE TurnoId IS NULL)
         AS DECIMAL(5,2))                                      AS PorcentajeEntregasSinTurno,
    CAST(COUNT(d.Id) * 100.0 / (SELECT COUNT(*) FROM Turnos)
         AS DECIMAL(5,2))                                      AS PorcentajeDelTotalGeneralTurnos
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON ur.UserId = u.Id
INNER JOIN AspNetRoles r      ON r.Id = ur.RoleId AND r.Name = 'Farmaceutico'
LEFT  JOIN Deliveries d
       ON d.DeliveredBy = u.UserName
      AND d.TurnoId IS NULL
GROUP BY u.Id, u.Email, u.UserName
ORDER BY EntregasSinTurno DESC;
