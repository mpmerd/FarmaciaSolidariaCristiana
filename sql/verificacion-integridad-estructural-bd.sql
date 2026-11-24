-- ============================================================================
-- VERIFICACION TOTAL DE INTEGRIDAD ESTRUCTURAL DE LA BD
-- Ejecutar en Somee -> My Databases -> SQL Manager (cuando el sitio vuelva,
-- o ahora mismo: la BD responde aunque la web este caida).
--
-- Verifica 3 capas:
--   A) Integridad fisica/logica de toda la BD (DBCC CHECKDB)
--   B) Que existan TODAS las foreign keys que el modelo EF espera,
--      y que esten "confiables" (is_not_trusted = 0, es decir, validadas)
--   C) Que NO exista ninguna fila huerfana en ninguna tabla hija
--      (defensa en profundidad: cubre el caso de FKs creadas WITH NOCHECK)
--
-- Todas las secciones devuelven sets de datos. Interpretacion al final.
-- ============================================================================

-- ============================================================================
-- SECCION A: Integridad fisica y logica de toda la base de datos
-- Si devuelve 0 filas y sin mensajes de error => BD estructuralmente sana.
-- (Nota: si Somee no permite ejecutar DBCC, saltara un error de permisos:
--  no es un problema de la BD, es una limitacion del panel.)
-- ============================================================================
PRINT '=== SECCION A: DBCC CHECKDB ===';
DECLARE @dbName SYSNAME;
SET @dbName = DB_NAME();
DBCC CHECKDB (@dbName) WITH NO_INFOMSGS;
PRINT 'DBCC CHECKDB: si no hubo errores arriba, la integridad fisica es total.';

-- ============================================================================
-- SECCION B: Foreign keys esperadas por el modelo de la aplicacion
-- Cada fila debe aparecer con is_not_trusted = 0.
-- is_not_trusted = 1 significa que la FK existe pero fue creada WITH NOCHECK
-- (no garantiza que los datos previos cumplan la restriccion).
-- ============================================================================

PRINT '=== SECCION B1: FKs hacia Medicines ===';
SELECT tp.name AS TablaHija,
       fk.name AS NombreFK,
       fk.is_not_trusted AS IsNotTrusted,
       fk.is_disabled AS IsDisabled
FROM sys.foreign_keys fk
JOIN sys.tables tp ON tp.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('Medicines')
ORDER BY tp.name;

PRINT '=== SECCION B2: FKs hacia Supplies ===';
SELECT tp.name AS TablaHija,
       fk.name AS NombreFK,
       fk.is_not_trusted AS IsNotTrusted,
       fk.is_disabled AS IsDisabled
FROM sys.foreign_keys fk
JOIN sys.tables tp ON tp.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('Supplies')
ORDER BY tp.name;

PRINT '=== SECCION B3: FKs hacia Turnos ===';
SELECT tp.name AS TablaHija,
       fk.name AS NombreFK,
       fk.is_not_trusted AS IsNotTrusted,
       fk.is_disabled AS IsDisabled
FROM sys.foreign_keys fk
JOIN sys.tables tp ON tp.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('Turnos')
ORDER BY tp.name;

PRINT '=== SECCION B4: FKs hacia Patients ===';
SELECT tp.name AS TablaHija,
       fk.name AS NombreFK,
       fk.is_not_trusted AS IsNotTrusted,
       fk.is_disabled AS IsDisabled
FROM sys.foreign_keys fk
JOIN sys.tables tp ON tp.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('Patients')
ORDER BY tp.name;

PRINT '=== SECCION B5: FKs hacia AspNetUsers ===';
SELECT tp.name AS TablaHija,
       fk.name AS NombreFK,
       fk.is_not_trusted AS IsNotTrusted,
       fk.is_disabled AS IsDisabled
FROM sys.foreign_keys fk
JOIN sys.tables tp ON tp.object_id = fk.parent_object_id
WHERE fk.referenced_object_id = OBJECT_ID('AspNetUsers')
ORDER BY tp.name;

-- ============================================================================
-- SECCION C: Chequeo directo de huerfanos en TODAS las tablas hijas
-- Todas deben devolver 0 filas.
-- ============================================================================

PRINT '=== SECCION C1: Donations -> Medicines huerfanas ===';
SELECT d.Id, d.MedicineId
FROM Donations d LEFT JOIN Medicines m ON m.Id = d.MedicineId
WHERE d.MedicineId IS NOT NULL AND m.Id IS NULL;

PRINT '=== SECCION C2: Donations -> Supplies huerfanos ===';
SELECT d.Id, d.SupplyId
FROM Donations d LEFT JOIN Supplies s ON s.Id = d.SupplyId
WHERE d.SupplyId IS NOT NULL AND s.Id IS NULL;

PRINT '=== SECCION C3: Deliveries -> Medicines huerfanas ===';
SELECT de.Id, de.MedicineId
FROM Deliveries de LEFT JOIN Medicines m ON m.Id = de.MedicineId
WHERE de.MedicineId IS NOT NULL AND m.Id IS NULL;

PRINT '=== SECCION C4: Deliveries -> Supplies huerfanos ===';
SELECT de.Id, de.SupplyId
FROM Deliveries de LEFT JOIN Supplies s ON s.Id = de.SupplyId
WHERE de.SupplyId IS NOT NULL AND s.Id IS NULL;

PRINT '=== SECCION C5: Deliveries -> Turnos huerfanos ===';
SELECT de.Id, de.TurnoId
FROM Deliveries de LEFT JOIN Turnos t ON t.Id = de.TurnoId
WHERE de.TurnoId IS NOT NULL AND t.Id IS NULL;

PRINT '=== SECCION C6: Deliveries -> Patients huerfanos ===';
SELECT de.Id, de.PatientId
FROM Deliveries de LEFT JOIN Patients p ON p.Id = de.PatientId
WHERE de.PatientId IS NOT NULL AND p.Id IS NULL;

PRINT '=== SECCION C7: TurnoMedicamentos -> Medicines huerfanas ===';
SELECT tm.Id, tm.TurnoId, tm.MedicineId
FROM TurnoMedicamentos tm LEFT JOIN Medicines m ON m.Id = tm.MedicineId
WHERE m.Id IS NULL;

PRINT '=== SECCION C8: TurnoMedicamentos -> Turnos huerfanos ===';
SELECT tm.Id, tm.TurnoId
FROM TurnoMedicamentos tm LEFT JOIN Turnos t ON t.Id = tm.TurnoId
WHERE t.Id IS NULL;

PRINT '=== SECCION C9: TurnoInsumos -> Supplies huerfanos ===';
SELECT ti.Id, ti.TurnoId, ti.SupplyId
FROM TurnoInsumos ti LEFT JOIN Supplies s ON s.Id = ti.SupplyId
WHERE s.Id IS NULL;

PRINT '=== SECCION C10: TurnoInsumos -> Turnos huerfanos ===';
SELECT ti.Id, ti.TurnoId
FROM TurnoInsumos ti LEFT JOIN Turnos t ON t.Id = ti.TurnoId
WHERE t.Id IS NULL;

PRINT '=== SECCION C11: TurnoDocumentos -> Turnos huerfanos ===';
SELECT td.Id, td.TurnoId
FROM TurnoDocumentos td LEFT JOIN Turnos t ON t.Id = td.TurnoId
WHERE t.Id IS NULL;

PRINT '=== SECCION C12: PatientDocuments -> Patients huerfanos ===';
SELECT pd.Id, pd.PatientId
FROM PatientDocuments pd LEFT JOIN Patients p ON p.Id = pd.PatientId
WHERE p.Id IS NULL;

PRINT '=== SECCION C13: Patients -> AspNetUsers huerfanos (desbloqueos por usuario inexistente) ===';
SELECT p.Id, p.LoanUnblockedByUserId
FROM Patients p LEFT JOIN AspNetUsers u ON u.Id = p.LoanUnblockedByUserId
WHERE p.LoanUnblockedByUserId IS NOT NULL AND u.Id IS NULL;

PRINT '=== SECCION C13b: FechasBloqueadas -> AspNetUsers huerfanas ===';
SELECT fb.Id, fb.UsuarioId
FROM FechasBloqueadas fb LEFT JOIN AspNetUsers u ON u.Id = fb.UsuarioId
WHERE u.Id IS NULL;

PRINT '=== SECCION C14: Turnos -> AspNetUsers huerfanos ===';
SELECT t.Id, t.UserId
FROM Turnos t LEFT JOIN AspNetUsers u ON u.Id = t.UserId
WHERE u.Id IS NULL;

PRINT '=== SECCION C15: UserDeviceTokens -> AspNetUsers huerfanos ===';
SELECT udt.Id, udt.UserId
FROM UserDeviceTokens udt LEFT JOIN AspNetUsers u ON u.Id = udt.UserId
WHERE u.Id IS NULL;

PRINT '=== SECCION C16: PendingNotifications -> AspNetUsers huerfanos ===';
SELECT pn.Id, pn.UserId
FROM PendingNotifications pn LEFT JOIN AspNetUsers u ON u.Id = pn.UserId
WHERE u.Id IS NULL;

PRINT '=== SECCION C17: BroadcastEmailQueueItems -> AspNetUsers huerfanos ===';
SELECT q.Id, q.UserId
FROM BroadcastEmailQueueItems q LEFT JOIN AspNetUsers u ON u.Id = q.UserId
WHERE q.UserId IS NOT NULL AND u.Id IS NULL;

-- ============================================================================
-- INTERPRETACION DE RESULTADOS
-- ============================================================================
-- VERDE (BD totalmente estable) si se cumple TODO esto:
--   1. Seccion A: DBCC CHECKDB termina sin errores ni filas.
--   2. Seccion B: aparecen las FKs esperadas y todas con
--      NoConfiable = 0 y Deshabilitada = 0.
--      FKs esperadas hacia Medicines: Deliveries, Donations, TurnoMedicamentos.
--      FKs esperadas hacia Supplies:  Deliveries, Donations, TurnoInsumos.
--      FKs esperadas hacia Turnos:    Deliveries, TurnoMedicamentos,
--                                    TurnoInsumos, TurnoDocumentos.
--      FKs esperadas hacia Patients:  Deliveries, PatientDocuments.
--      FKs esperadas hacia AspNetUsers: Patients, Turnos, UserDeviceTokens,
--                                    PendingNotifications, BroadcastEmailQueueItems,
--                                    FechasBloqueadas.
--   3. Seccion C: TODAS las consultas (C1 a C17) devuelven 0 filas.
--
-- AMARILLO (revisar): alguna FK con NoConfiable = 1 (existe pero no validada)
--   o falta alguna FK esperada. Mientras la Seccion C devuelva 0 filas,
--   los DATOS estan sanos; solo faltaria crear/validar la FK faltante.
--
-- ROJO (inconsistencia real): alguna consulta de la Seccion C devuelve filas.
--   Guardar el resultado y NO borrar nada manualmente: compartirlo para
--   decidir la correccion (re-insertar el padre, o reasignar/borrar el huerfano).
-- ============================================================================
