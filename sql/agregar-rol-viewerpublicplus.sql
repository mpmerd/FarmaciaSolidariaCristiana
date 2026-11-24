-- Script para agregar el rol ViewerPublicPlus a la BD de producción (Somee)
-- Contexto: commit e1d4060 (rol ViewerPublicPlus). DbInitializer lo crea al arrancar,
-- pero si el seeding falla en producción el rol no existe y EditUser/CreateUser da 500
-- (UserManager.AddToRoleAsync lanza InvalidOperationException).
-- Base de datos: FarmaciaDb en Somee

-- 1. Ver roles actuales
SELECT * FROM AspNetRoles ORDER BY Name;

-- 2. Agregar el rol si no existe (idempotente)
IF NOT EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'VIEWERPUBLICPLUS')
BEGIN
    INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
    VALUES (NEWID(), 'ViewerPublicPlus', 'VIEWERPUBLICPLUS', NEWID());
    PRINT 'Rol ViewerPublicPlus creado';
END
ELSE
    PRINT 'El rol ViewerPublicPlus ya existe, no se hizo nada';

-- 3. Verificar
SELECT Id, Name, NormalizedName FROM AspNetRoles WHERE NormalizedName = 'VIEWERPUBLICPLUS';
