-- =======================================================
-- Migración: Agregar tabla TurnoDocumentos
-- Fecha: 2026-01-14
-- Descripción: Crea la tabla TurnoDocumentos para permitir 
--              múltiples documentos por solicitud de turno
-- =======================================================

-- Crear tabla TurnoDocumentos si no existe
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TurnoDocumentos')
BEGIN
    CREATE TABLE TurnoDocumentos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        TurnoId INT NOT NULL,
        DocumentType NVARCHAR(100) NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        FileSize BIGINT NOT NULL DEFAULT 0,
        ContentType NVARCHAR(100) NULL,
        Description NVARCHAR(500) NULL,
        UploadDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT FK_TurnoDocumentos_Turnos FOREIGN KEY (TurnoId) 
            REFERENCES Turnos(Id) ON DELETE CASCADE
    );
    
    -- Crear índice para búsquedas por TurnoId
    CREATE INDEX IX_TurnoDocumentos_TurnoId ON TurnoDocumentos(TurnoId);
    
    PRINT 'Tabla TurnoDocumentos creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla TurnoDocumentos ya existe.';
END
GO

-- Verificar que la tabla se creó correctamente
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'TurnoDocumentos'
ORDER BY ORDINAL_POSITION;
GO
