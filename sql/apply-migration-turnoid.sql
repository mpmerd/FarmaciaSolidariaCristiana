-- =====================================================================================
-- MIGRACIÓN: Agregar TurnoId a Deliveries
-- Fecha: 4 de noviembre de 2025
-- Propósito: Relacionar entregas con turnos para mejor trazabilidad
-- =====================================================================================

PRINT '========================================================================='
PRINT 'APLICANDO MIGRACIÓN: AddTurnoIdToDeliveries'
PRINT 'Fecha: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
PRINT ''

BEGIN TRANSACTION

BEGIN TRY

    -- Verificar si la columna ya existe
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Deliveries') AND name = 'TurnoId')
    BEGIN
        PRINT 'Paso 1/3: Agregando columna TurnoId a Deliveries...'
        
        ALTER TABLE Deliveries 
        ADD TurnoId INT NULL
        
        PRINT '  ✓ Columna TurnoId agregada'
    END
    ELSE
    BEGIN
        PRINT 'Paso 1/3: Columna TurnoId ya existe, saltando...'
    END
    PRINT ''

    -- Crear índice
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Deliveries') AND name = 'IX_Deliveries_TurnoId')
    BEGIN
        PRINT 'Paso 2/3: Creando índice IX_Deliveries_TurnoId...'
        
        CREATE NONCLUSTERED INDEX [IX_Deliveries_TurnoId] 
        ON [Deliveries] ([TurnoId])
        
        PRINT '  ✓ Índice creado'
    END
    ELSE
    BEGIN
        PRINT 'Paso 2/3: Índice IX_Deliveries_TurnoId ya existe, saltando...'
    END
    PRINT ''

    -- Crear foreign key
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Deliveries_Turnos_TurnoId')
    BEGIN
        PRINT 'Paso 3/3: Creando foreign key FK_Deliveries_Turnos_TurnoId...'
        
        ALTER TABLE [Deliveries]
        ADD CONSTRAINT [FK_Deliveries_Turnos_TurnoId] 
        FOREIGN KEY ([TurnoId]) REFERENCES [Turnos] ([Id])
        
        PRINT '  ✓ Foreign key creada'
    END
    ELSE
    BEGIN
        PRINT 'Paso 3/3: Foreign key FK_Deliveries_Turnos_TurnoId ya existe, saltando...'
    END
    PRINT ''

    COMMIT TRANSACTION
    PRINT '✅ MIGRACIÓN COMPLETADA EXITOSAMENTE'
    PRINT ''
    PRINT '========================================================================='
    PRINT 'NOTAS IMPORTANTES:'
    PRINT '  • Las entregas existentes tendrán TurnoId = NULL'
    PRINT '  • Las nuevas entregas se asociarán automáticamente al turno correspondiente'
    PRINT '  • En la vista de entregas ahora aparecerá la columna "Turno #"'
    PRINT '========================================================================='

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION
    
    PRINT ''
    PRINT '❌ ERROR DURANTE LA MIGRACIÓN'
    PRINT '========================================================================='
    PRINT 'Error: ' + ERROR_MESSAGE()
    PRINT 'Línea: ' + CAST(ERROR_LINE() AS VARCHAR)
    PRINT ''
    PRINT '⚠️  TRANSACCIÓN REVERTIDA - NO SE APLICARON CAMBIOS'
    PRINT ''
    
    RETURN
END CATCH

PRINT ''
PRINT 'Finalizado: ' + CONVERT(VARCHAR, GETDATE(), 120)
PRINT '========================================================================='
GO
