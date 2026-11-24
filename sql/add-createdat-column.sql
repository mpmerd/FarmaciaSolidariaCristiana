-- =====================================================================================
-- AGREGAR COLUMNA CreatedAt A TABLA Deliveries
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Fecha: 25 de octubre de 2025
-- 
-- PROPÓSITO: Agregar columna CreatedAt (nullable) para controlar eliminación de entregas
-- 
-- ✅ SEGURO: Esta columna es nullable, NO afecta los datos existentes
-- 
-- IMPORTANTE: Ejecutar en el panel SQL de Somee.com
-- =====================================================================================

-- Verificar si la columna ya existe
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Deliveries' 
    AND COLUMN_NAME = 'CreatedAt'
)
BEGIN
    -- Agregar columna nullable
    ALTER TABLE Deliveries ADD CreatedAt DATETIME2 NULL
    
    PRINT '✓ Columna CreatedAt agregada exitosamente'
    PRINT 'Total de entregas existentes: se les asignará CreatedAt = NULL'
END
ELSE
BEGIN
    PRINT 'La columna CreatedAt ya existe - no se realizaron cambios'
END
GO

-- Verificar resultado (ejecutar después del GO)
SELECT 
    COUNT(*) AS TotalEntregas,
    COUNT(CreatedAt) AS ConCreatedAt,
    COUNT(*) - COUNT(CreatedAt) AS SinCreatedAt
FROM Deliveries
