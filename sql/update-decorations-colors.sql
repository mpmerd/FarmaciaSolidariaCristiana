-- =====================================================================================
-- ACTUALIZACIÓN DE COLORES DE DECORACIONES PREDEFINIDAS
-- Farmacia Solidaria Cristiana
-- =====================================================================================
-- Fecha: 13 de noviembre de 2025
-- Propósito: Actualizar los colores de las festividades ya existentes en la BD
-- =====================================================================================

PRINT '========================================================================='
PRINT 'ACTUALIZANDO COLORES DE DECORACIONES'
PRINT '========================================================================='
PRINT ''

-- Actualizar Navidad: árbol verde, texto verde
UPDATE NavbarDecorations
SET IconClass = 'fa-solid fa-tree',
    IconColor = '#228B22',
    TextColor = '#228B22',
    DisplayText = '¡Feliz Navidad!'
WHERE PresetKey = 'navidad';

IF @@ROWCOUNT > 0
    PRINT '✓ Navidad actualizada: árbol verde (fa-tree), texto verde'
ELSE
    PRINT '⚠ No se encontró decoración de Navidad'

PRINT ''

-- Actualizar Epifanía: estrella amarilla, texto amarillo
UPDATE NavbarDecorations
SET IconColor = '#FFD700',
    TextColor = '#FFD700',
    DisplayText = 'Epifanía del Señor'
WHERE PresetKey = 'epifania';

IF @@ROWCOUNT > 0
    PRINT '✓ Epifanía actualizada: estrella amarilla, texto amarillo'
ELSE
    PRINT '⚠ No se encontró decoración de Epifanía'

PRINT ''

-- Actualizar Pentecostés: llama naranja, texto dorado
UPDATE NavbarDecorations
SET IconColor = '#FF8C00',
    TextColor = '#FFD700',
    DisplayText = 'Pentecostés'
WHERE PresetKey = 'pentecostes';

IF @@ROWCOUNT > 0
    PRINT '✓ Pentecostés actualizada: llama naranja, texto dorado'
ELSE
    PRINT '⚠ No se encontró decoración de Pentecostés'

PRINT ''
PRINT '========================================================================='
PRINT 'ACTUALIZACIÓN COMPLETADA'
PRINT '========================================================================='
PRINT ''

-- Mostrar decoraciones actualizadas
SELECT 
    Id,
    Name,
    PresetKey,
    DisplayText,
    IconClass,
    IconColor,
    TextColor,
    IsActive
FROM NavbarDecorations
WHERE Type = 0 -- Solo predefinidas
ORDER BY Id;
