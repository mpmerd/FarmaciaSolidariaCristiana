-- Script para verificar y corregir el rol Admin del usuario admin
-- Base de datos: FarmaciaDb en Somee

-- 1. Ver todos los usuarios y sus roles actuales
SELECT 
    u.UserName,
    u.Email,
    u.NormalizedUserName,
    u.NormalizedEmail,
    r.Name as RoleName
FROM AspNetUsers u
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
ORDER BY u.UserName;

-- 2. Ver todos los roles disponibles
SELECT * FROM AspNetRoles;

-- 3. Encontrar el ID del usuario 'admin'
DECLARE @AdminUserId NVARCHAR(450);
DECLARE @AdminRoleId NVARCHAR(450);

SELECT @AdminUserId = Id FROM AspNetUsers WHERE NormalizedUserName = 'ADMIN';
SELECT @AdminRoleId = Id FROM AspNetRoles WHERE NormalizedName = 'ADMIN';

-- 4. Verificar si el usuario admin tiene el rol Admin
SELECT 
    u.UserName,
    r.Name as RoleName,
    CASE 
        WHEN ur.UserId IS NOT NULL THEN 'SI'
        ELSE 'NO'
    END as TieneRol
FROM AspNetUsers u
CROSS JOIN AspNetRoles r
LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId AND r.Id = ur.RoleId
WHERE u.NormalizedUserName = 'ADMIN' AND r.NormalizedName = 'ADMIN';

-- 5. CORREGIR: Eliminar todos los roles del usuario admin
DELETE FROM AspNetUserRoles WHERE UserId = @AdminUserId;

-- 6. CORREGIR: Asignar el rol Admin al usuario admin
INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES (@AdminUserId, @AdminRoleId);

-- 7. Verificar la corrección
SELECT 
    u.UserName,
    u.Email,
    r.Name as RoleName
FROM AspNetUsers u
INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
WHERE u.NormalizedUserName = 'ADMIN';
