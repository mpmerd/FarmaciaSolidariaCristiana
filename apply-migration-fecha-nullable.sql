-- =====================================================================================
-- SCRIPT DE MIGRACIÓN: HACER FechaPreferida NULLABLE
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Fecha: 31 de octubre de 2025
-- 
-- MIGRACIÓN:
-- ✅ 20251031XXXXXX_MakeFechaPreferidaNullable
-- 
-- IMPORTANTE: Ejecutar en el panel SQL de Somee.com
-- PREREQUISITO: La migración AddTurnosSystem debe estar aplicada
-- =====================================================================================

PRINT '========================================================================='
PRINT 'MIGRACIÓN: HACER FechaPreferida NULLABLE'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

-- =====================================================================================
-- MIGRACIÓN 8: MakeFechaPreferidaNullable
-- =====================================================================================

PRINT '-- MIGRACIÓN 8: Hacer FechaPreferida nullable (asignación automática)...'
PRINT ''

-- Modificar columna FechaPreferida para permitir NULL
BEGIN TRY
    ALTER TABLE Turnos
    ALTER COLUMN FechaPreferida DATETIME2 NULL;
    
    PRINT '✓ Columna FechaPreferida modificada a NULLABLE exitosamente'
END TRY
BEGIN CATCH
    PRINT '✗ ERROR al modificar columna FechaPreferida: ' + ERROR_MESSAGE()
END CATCH

-- Registrar migración (reemplazar XXXXXX con el número real de la migración)
IF NOT EXISTS (SELECT * FROM __EFMigrationsHistory 
               WHERE MigrationId LIKE '202510%_MakeFechaPreferidaNullable')
BEGIN
    -- Obtener el último MigrationId para ver el número exacto
    DECLARE @lastMigrationId NVARCHAR(150);
    DECLARE @newMigrationId NVARCHAR(150);
    
    SELECT TOP 1 @lastMigrationId = MigrationId 
    FROM __EFMigrationsHistory 
    ORDER BY MigrationId DESC;
    
    PRINT '✓ Última migración registrada: ' + @lastMigrationId;
    PRINT '⚠ NOTA: Ejecuta este comando con el MigrationId correcto de tu sistema:';
    PRINT '   INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)';
    PRINT '   VALUES (''20251031XXXXXX_MakeFechaPreferidaNullable'', ''8.0.11'');';
    PRINT ''
    PRINT '📝 Reemplaza XXXXXX con el timestamp correcto de tu carpeta Migrations';
END

PRINT ''
PRINT '========================================================================='
PRINT 'VERIFICACIÓN DE MIGRACIÓN'
PRINT '========================================================================='
PRINT ''

-- Verificar que la columna es nullable
PRINT 'Estado de columna FechaPreferida:'
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CASE WHEN IS_NULLABLE = 'YES' THEN '✓ NULLABLE' ELSE '✗ NOT NULL' END AS Estado
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Turnos' AND COLUMN_NAME = 'FechaPreferida';

PRINT ''
PRINT '========================================================================='
PRINT 'MIGRACIÓN FechaPreferida NULLABLE COMPLETADA'
PRINT '========================================================================='
PRINT ''
PRINT '✅ CAMBIOS APLICADOS:'
PRINT '  • FechaPreferida ahora es NULLABLE'
PRINT '  • Los turnos se crean sin fecha (NULL)'
PRINT '  • Al aprobar, se asigna automáticamente (Martes/Viernes 1-4 PM)'
PRINT '  • Sistema de slots: cada 6 minutos (30 turnos/día)'
PRINT ''
PRINT '📌 PRÓXIMOS PASOS:'
PRINT '  1. Registrar migración en __EFMigrationsHistory con el ID correcto'
PRINT '  2. Desplegar aplicación actualizada'
PRINT '  3. Verificar que las nuevas solicitudes se crean sin fecha'
PRINT '  4. Probar aprobación con asignación automática'
PRINT ''
PRINT 'Finalizado: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
