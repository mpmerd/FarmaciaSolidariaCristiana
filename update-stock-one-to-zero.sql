-- =====================================================================================
-- ACTUALIZACIÓN DE STOCK: Cambiar cantidades de 1 a 0 (SOLO MEDICAMENTOS)
-- =====================================================================================
-- FECHA: 15 de diciembre de 2025
-- PROPÓSITO: Actualizar todas las cantidades de MEDICAMENTOS que estén en 1 y llevarlas a 0
-- IMPORTANTE: Este script incluye backup y es reversible
-- NOTA: Solo afecta la tabla Medicines, NO afecta Supplies (insumos)
-- =====================================================================================

-- =====================================================================================
-- PARTE 1: BACKUP DE DATOS ANTES DE MODIFICAR
-- =====================================================================================

PRINT '====================================================================================='
PRINT 'INICIANDO BACKUP DE REGISTROS QUE SERÁN MODIFICADOS'
PRINT '====================================================================================='

-- Mostrar medicamentos con stock = 1 (ANTES)
PRINT ''
PRINT '--- MEDICAMENTOS CON STOCK = 1 (ANTES) ---'
SELECT 
    Id, 
    Name AS Nombre, 
    StockQuantity AS Stock_Actual, 
    Unit AS Unidad,
    Description AS Descripcion
FROM Medicines 
WHERE StockQuantity = 1
ORDER BY Name;

-- Contar registros afectados
PRINT ''
PRINT '--- RESUMEN DE REGISTROS QUE SERÁN MODIFICADOS ---'
SELECT 
    'Medicamentos' AS Tipo,
    COUNT(*) AS Cantidad_Registros
FROM Medicines 
WHERE StockQuantity = 1;

PRINT ''
PRINT '====================================================================================='
PRINT 'BACKUP COMPLETADO - Revisa los datos anteriores antes de continuar'
PRINT '====================================================================================='
PRINT ''
PRINT 'Si deseas continuar con la actualización, ejecuta las siguientes líneas:'
PRINT ''

-- =====================================================================================
-- PARTE 2: ACTUALIZACIÓN DE DATOS (COMENTADO POR SEGURIDAD)
-- =====================================================================================
-- Descomenta las siguientes líneas SOLO después de revisar el backup anterior

/*
PRINT '====================================================================================='
PRINT 'INICIANDO ACTUALIZACIÓN DE STOCK'
PRINT '====================================================================================='

-- Iniciar transacción para poder hacer rollback si algo sale mal
BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @MedicinesAffected INT;

    -- Actualizar medicamentos
    UPDATE Medicines 
    SET StockQuantity = 0 
    WHERE StockQuantity = 1;
    
    SET @MedicinesAffected = @@ROWCOUNT;
    
    -- Mostrar resultados
    PRINT ''
    PRINT '--- ACTUALIZACIÓN COMPLETADA ---'
    PRINT 'Medicamentos actualizados: ' + CAST(@MedicinesAffected AS VARCHAR(10))
    PRINT ''
    
    -- Verificar los cambios
    PRINT '--- MEDICAMENTOS DESPUÉS DE LA ACTUALIZACIÓN (deben estar en 0) ---'
    SELECT 
        Id, 
        Name AS Nombre, 
        StockQuantity AS Stock_Nuevo, 
        Unit AS Unidad
    FROM Medicines 
    WHERE Id IN (
        SELECT Id FROM Medicines WHERE StockQuantity = 0
    )
    ORDER BY Name;
    
    -- Si todo está bien, descomentar la siguiente línea para confirmar los cambios:
    -- COMMIT TRANSACTION;
    
    -- Por seguridad, por defecto hacemos ROLLBACK (deshacer cambios)
    -- Comenta esta línea y descomenta COMMIT cuando estés seguro
    ROLLBACK TRANSACTION;
    PRINT ''
    PRINT 'ROLLBACK EJECUTADO - Los cambios NO se guardaron'
    PRINT 'Para guardar los cambios: Comenta ROLLBACK y descomenta COMMIT'
    
END TRY
BEGIN CATCH
    -- Si hay algún error, deshacer todos los cambios
    ROLLBACK TRANSACTION;
    
    PRINT ''
    PRINT '¡ERROR! La transacción fue revertida'
    PRINT 'Mensaje de error: ' + ERROR_MESSAGE()
    PRINT 'Línea: ' + CAST(ERROR_LINE() AS VARCHAR(10))
    PRINT ''
END CATCH
*/

PRINT ''
PRINT '====================================================================================='
PRINT 'INSTRUCCIONES:'
PRINT '1. Revisa el backup mostrado arriba'
PRINT '2. Si todo está correcto, descomenta la sección PARTE 2'
PRINT '3. Ejecuta nuevamente el script'
PRINT '4. Revisa los resultados de la actualización'
PRINT '5. Si todo está bien, comenta ROLLBACK y descomenta COMMIT'
PRINT '6. Ejecuta una última vez para confirmar los cambios'
PRINT '====================================================================================='
