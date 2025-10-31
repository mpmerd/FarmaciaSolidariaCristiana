-- ============================================
-- Script de Datos de Prueba - Sistema de Turnos
-- Farmacia Solidaria Cristiana
-- ============================================
-- Este script crea usuarios ViewerPublic de prueba y turnos de ejemplo
-- IMPORTANTE: Ejecutar DESPUÉS de que el sistema haya inicializado los roles

USE [FarmaciaDb];
GO

-- ============================================
-- 1. VERIFICAR ROLES EXISTENTES
-- ============================================
PRINT '=== Verificando Roles ===';
SELECT Id, Name FROM AspNetRoles;
GO

-- ============================================
-- 2. CREAR USUARIOS DE PRUEBA (ViewerPublic)
-- ============================================
PRINT '=== Creando Usuarios de Prueba ===';

-- Usuario 1: María García (Usuario activo con turno aprobado)
DECLARE @MariaId NVARCHAR(450) = NEWID();
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                         PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, 
                         TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
VALUES (
    @MariaId,
    'maria.garcia@example.com',
    'MARIA.GARCIA@EXAMPLE.COM',
    'maria.garcia@example.com',
    'MARIA.GARCIA@EXAMPLE.COM',
    1,
    'AQAAAAIAAYagAAAAEJ7VZ8qKk6K8xYQZYGC5XqR9qJ5QwJ5YqR9qJ5QwJ5YqR9qJ5QwJ5YqR9qJ5Qw==', -- Password: Test123!
    NEWID(),
    NEWID(),
    0,
    0,
    1,
    0
);

-- Asignar rol ViewerPublic a María
DECLARE @ViewerPublicRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'ViewerPublic');
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@MariaId, @ViewerPublicRoleId);

-- Usuario 2: Juan Pérez (Usuario con turno pendiente)
DECLARE @JuanId NVARCHAR(450) = NEWID();
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                         PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, 
                         TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
VALUES (
    @JuanId,
    'juan.perez@example.com',
    'JUAN.PEREZ@EXAMPLE.COM',
    'juan.perez@example.com',
    'JUAN.PEREZ@EXAMPLE.COM',
    1,
    'AQAAAAIAAYagAAAAEJ7VZ8qKk6K8xYQZYGC5XqR9qJ5QwJ5YqR9qJ5QwJ5YqR9qJ5QwJ5YqR9qJ5Qw==', -- Password: Test123!
    NEWID(),
    NEWID(),
    0,
    0,
    1,
    0
);

INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@JuanId, @ViewerPublicRoleId);

-- Usuario 3: Ana López (Usuario con turno rechazado - puede volver a solicitar)
DECLARE @AnaId NVARCHAR(450) = NEWID();
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                         PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, 
                         TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
VALUES (
    @AnaId,
    'ana.lopez@example.com',
    'ANA.LOPEZ@EXAMPLE.COM',
    'ana.lopez@example.com',
    'ANA.LOPEZ@EXAMPLE.COM',
    1,
    'AQAAAAIAAYagAAAAEJ7VZ8qKk6K8xYQZYGC5XqR9qJ5QwJ5YqR9qJ5QwJ5YqR9qJ5QwJ5YqR9qJ5Qw==', -- Password: Test123!
    NEWID(),
    NEWID(),
    0,
    0,
    1,
    0
);

INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@AnaId, @ViewerPublicRoleId);

-- Usuario 4: Carlos Rodríguez (Usuario nuevo sin turnos)
DECLARE @CarlosId NVARCHAR(450) = NEWID();
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, 
                         PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, 
                         TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
VALUES (
    @CarlosId,
    'carlos.rodriguez@example.com',
    'CARLOS.RODRIGUEZ@EXAMPLE.COM',
    'carlos.rodriguez@example.com',
    'CARLOS.RODRIGUEZ@EXAMPLE.COM',
    1,
    'AQAAAAIAAYagAAAAEJ7VZ8qKk6K8xYQZYGC5XqR9qJ5QwJ5YqR9qJ5QwJ5YqR9qJ5QwJ5YqR9qJ5Qw==', -- Password: Test123!
    NEWID(),
    NEWID(),
    0,
    0,
    1,
    0
);

INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@CarlosId, @ViewerPublicRoleId);

PRINT 'Usuarios de prueba creados exitosamente.';
PRINT '';

-- ============================================
-- 3. OBTENER IDs DE MEDICAMENTOS Y ADMIN
-- ============================================
DECLARE @AdminId NVARCHAR(450) = (SELECT TOP 1 Id FROM AspNetUsers 
                                   WHERE Id IN (SELECT UserId FROM AspNetUserRoles 
                                               WHERE RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Admin')));

DECLARE @Medicine1Id INT = (SELECT TOP 1 Id FROM Medicines WHERE StockQuantity > 10 ORDER BY Id);
DECLARE @Medicine2Id INT = (SELECT TOP 1 Id FROM Medicines WHERE StockQuantity > 5 AND Id <> @Medicine1Id ORDER BY Id);
DECLARE @Medicine3Id INT = (SELECT TOP 1 Id FROM Medicines WHERE StockQuantity > 0 AND Id NOT IN (@Medicine1Id, @Medicine2Id) ORDER BY Id);

PRINT '=== IDs Obtenidos ===';
PRINT 'Admin ID: ' + ISNULL(CAST(@AdminId AS NVARCHAR(450)), 'NULL');
PRINT 'Medicine 1 ID: ' + ISNULL(CAST(@Medicine1Id AS NVARCHAR(10)), 'NULL');
PRINT 'Medicine 2 ID: ' + ISNULL(CAST(@Medicine2Id AS NVARCHAR(10)), 'NULL');
PRINT 'Medicine 3 ID: ' + ISNULL(CAST(@Medicine3Id AS NVARCHAR(10)), 'NULL');
PRINT '';

-- ============================================
-- 4. CREAR TURNOS DE PRUEBA
-- ============================================
PRINT '=== Creando Turnos de Prueba ===';

-- TURNO 1: María García - APROBADO (Número #001)
DECLARE @Turno1Id INT;
INSERT INTO Turnos (UserId, DocumentoIdentidadHash, FechaPreferida, FechaSolicitud, Estado, 
                    RecetaMedicaPath, TarjetonPath, NotasSolicitante, ComentariosFarmaceutico,
                    RevisadoPorId, FechaRevision, NumeroTurno, EmailEnviado)
VALUES (
    @MariaId,
    'vMQHhT7cR2xF8RQJ5kY5ZqYGTwZ0qW5R4pJ5QwJ5YqR=', -- Hash de "88012312345"
    DATEADD(DAY, 2, GETDATE()), -- Turno en 2 días
    DATEADD(DAY, -5, GETDATE()), -- Solicitado hace 5 días
    'Aprobado',
    NULL,
    'uploads/turnos/tarjeton_maria.jpg',
    'Necesito estos medicamentos para mi tratamiento de hipertensión.',
    'Turno aprobado. Por favor presentarse con documento de identidad.',
    @AdminId,
    DATEADD(DAY, -3, GETDATE()), -- Revisado hace 3 días
    1, -- Número de turno
    1
);
SET @Turno1Id = SCOPE_IDENTITY();

-- Medicamentos para Turno 1
INSERT INTO TurnoMedicamentos (TurnoId, MedicineId, CantidadSolicitada, DisponibleAlSolicitar, CantidadAprobada)
VALUES 
    (@Turno1Id, @Medicine1Id, 30, 1, 30),
    (@Turno1Id, @Medicine2Id, 20, 1, 20);

PRINT 'Turno 1 (María - Aprobado) creado con ID: ' + CAST(@Turno1Id AS NVARCHAR(10));

-- TURNO 2: Juan Pérez - PENDIENTE
DECLARE @Turno2Id INT;
INSERT INTO Turnos (UserId, DocumentoIdentidadHash, FechaPreferida, FechaSolicitud, Estado, 
                    RecetaMedicaPath, TarjetonPath, NotasSolicitante, EmailEnviado)
VALUES (
    @JuanId,
    'xNRIsU9dS3yG9SRK6lZ6ArZHUxA1rX6S5qK6RxK6ZrS=', -- Hash de "89021423456"
    DATEADD(DAY, 3, GETDATE()), -- Turno en 3 días
    GETDATE(), -- Solicitado hoy
    'Pendiente',
    'uploads/turnos/receta_juan.pdf',
    'uploads/turnos/tarjeton_juan.jpg',
    'Solicito estos medicamentos para tratamiento de diabetes. Adjunto receta médica.',
    0
);
SET @Turno2Id = SCOPE_IDENTITY();

-- Medicamentos para Turno 2
INSERT INTO TurnoMedicamentos (TurnoId, MedicineId, CantidadSolicitada, DisponibleAlSolicitar)
VALUES 
    (@Turno2Id, @Medicine1Id, 60, 1),
    (@Turno2Id, @Medicine3Id, 10, 1);

PRINT 'Turno 2 (Juan - Pendiente) creado con ID: ' + CAST(@Turno2Id AS NVARCHAR(10));

-- TURNO 3: Ana López - RECHAZADO (hace 45 días - puede volver a solicitar)
DECLARE @Turno3Id INT;
INSERT INTO Turnos (UserId, DocumentoIdentidadHash, FechaPreferida, FechaSolicitud, Estado, 
                    RecetaMedicaPath, TarjetonPath, NotasSolicitante, ComentariosFarmaceutico,
                    RevisadoPorId, FechaRevision, EmailEnviado)
VALUES (
    @AnaId,
    'yORJtV0eT4zH0TSL7mA7BsAIVyB2sY7T6rL7SyL7AsT=', -- Hash de "90031534567"
    DATEADD(DAY, -43, GETDATE()), -- Era para hace 43 días
    DATEADD(DAY, -45, GETDATE()), -- Solicitado hace 45 días
    'Rechazado',
    NULL,
    'uploads/turnos/tarjeton_ana.jpg',
    'Necesito medicamentos urgentes para dolor crónico.',
    'Lamentablemente no disponemos de los medicamentos solicitados en este momento. Por favor intente nuevamente en 2 semanas.',
    @AdminId,
    DATEADD(DAY, -44, GETDATE()), -- Revisado hace 44 días
    1
);
SET @Turno3Id = SCOPE_IDENTITY();

-- Medicamentos para Turno 3
INSERT INTO TurnoMedicamentos (TurnoId, MedicineId, CantidadSolicitada, DisponibleAlSolicitar, CantidadAprobada)
VALUES 
    (@Turno3Id, @Medicine2Id, 50, 0, NULL);

PRINT 'Turno 3 (Ana - Rechazado hace 45 días) creado con ID: ' + CAST(@Turno3Id AS NVARCHAR(10));

-- TURNO 4: María García - COMPLETADO (hace 60 días - puede solicitar nuevo)
DECLARE @Turno4Id INT;
INSERT INTO Turnos (UserId, DocumentoIdentidadHash, FechaPreferida, FechaSolicitud, Estado, 
                    RecetaMedicaPath, TarjetonPath, NotasSolicitante, ComentariosFarmaceutico,
                    RevisadoPorId, FechaRevision, FechaEntrega, NumeroTurno, EmailEnviado)
VALUES (
    @MariaId,
    'vMQHhT7cR2xF8RQJ5kY5ZqYGTwZ0qW5R4pJ5QwJ5YqR=', -- Hash de "88012312345"
    DATEADD(DAY, -58, GETDATE()), -- Era para hace 58 días
    DATEADD(DAY, -60, GETDATE()), -- Solicitado hace 60 días
    'Completado',
    'uploads/turnos/receta_maria_old.pdf',
    'uploads/turnos/tarjeton_maria_old.jpg',
    'Primera solicitud de medicamentos.',
    'Todos los medicamentos entregados correctamente.',
    @AdminId,
    DATEADD(DAY, -59, GETDATE()), -- Revisado hace 59 días
    DATEADD(DAY, -58, GETDATE()), -- Entregado hace 58 días
    1, -- Número de turno de ese día
    1
);
SET @Turno4Id = SCOPE_IDENTITY();

-- Medicamentos para Turno 4
INSERT INTO TurnoMedicamentos (TurnoId, MedicineId, CantidadSolicitada, DisponibleAlSolicitar, CantidadAprobada)
VALUES 
    (@Turno4Id, @Medicine1Id, 30, 1, 30),
    (@Turno4Id, @Medicine2Id, 15, 1, 15);

PRINT 'Turno 4 (María - Completado hace 60 días) creado con ID: ' + CAST(@Turno4Id AS NVARCHAR(10));

PRINT '';
PRINT '=== RESUMEN DE DATOS DE PRUEBA ===';
PRINT 'Total Usuarios ViewerPublic creados: 4';
PRINT '  - Maria García (maria.garcia@example.com): 1 turno APROBADO actual + 1 COMPLETADO antiguo';
PRINT '  - Juan Pérez (juan.perez@example.com): 1 turno PENDIENTE';
PRINT '  - Ana López (ana.lopez@example.com): 1 turno RECHAZADO (puede solicitar nuevo)';
PRINT '  - Carlos Rodríguez (carlos.rodriguez@example.com): Sin turnos (puede solicitar)';
PRINT '';
PRINT 'Total Turnos creados: 4';
PRINT '  - 1 Aprobado (María - actual)';
PRINT '  - 1 Pendiente (Juan)';
PRINT '  - 1 Rechazado (Ana - hace 45 días)';
PRINT '  - 1 Completado (María - hace 60 días)';
PRINT '';
PRINT '=== CREDENCIALES DE PRUEBA ===';
PRINT 'Todos los usuarios ViewerPublic:';
PRINT 'Password: Test123!';
PRINT '';
PRINT '=== DOCUMENTOS DE IDENTIDAD (para Verify) ===';
PRINT 'María García: 88012312345';
PRINT 'Juan Pérez: 89021423456';
PRINT 'Ana López: 90031534567';
PRINT '';
PRINT '¡Datos de prueba insertados exitosamente!';
GO
