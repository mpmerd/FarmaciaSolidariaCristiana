-- Migración: Agregar campo CanceladoPorNoPresentacion a Turnos
-- Fecha: 2025-01-XX
-- Propósito: Marcar turnos cancelados por no presentación para penalización en límite mensual

-- =============================================
-- SQL Server
-- =============================================

IF NOT EXISTS (
    SELECT 1 
    FROM sys.columns 
    WHERE object_id = OBJECT_ID('Turnos') 
    AND name = 'CanceladoPorNoPresentacion'
)
BEGIN
    ALTER TABLE Turnos ADD CanceladoPorNoPresentacion BIT NOT NULL DEFAULT 0;
    PRINT 'Columna CanceladoPorNoPresentacion agregada exitosamente a Turnos';
END
ELSE
BEGIN
    PRINT 'Columna CanceladoPorNoPresentacion ya existe en Turnos';
END
GO

-- =============================================
-- SQLite (para desarrollo local si aplica)
-- =============================================
-- ALTER TABLE Turnos ADD COLUMN CanceladoPorNoPresentacion INTEGER NOT NULL DEFAULT 0;
