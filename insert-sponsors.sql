-- Script para insertar patrocinadores en la base de datos
-- Ejecutar en SQL Server

USE [FarmaciaDb]
GO

-- Insertar patrocinadores con sus logos
INSERT INTO [Sponsors] ([Name], [Description], [LogoPath], [IsActive], [DisplayOrder], [CreatedDate])
VALUES 
    ('ACAA', 'Asociación Cubana de Artesanos Artistas', '/images/sponsors/acaa.png', 1, 1, GETDATE()),
    ('Adriano Solidaire', 'Adriano Solidario', '/images/sponsors/adranosolidaire.png', 1, 2, GETDATE()),
    ('Apotheek', 'Apotheek Peeters Herent, Bélgica', '/images/sponsors/apotheek.png', 1, 3, GETDATE()),
    ('HSF', 'Hospital Sans Frontière', '/images/sponsors/hsf.JPG', 1, 4, GETDATE()),
    ('Farmacia Janeiro', 'Farmacia Janeiro, Portugal', '/images/sponsors/janeiro.png', 1, 5, GETDATE()),
    ('Sutures Medical', 'Aip Medical, Bélgica', '/images/sponsors/suturesmedical.png', 1, 6, GETDATE())
GO

-- Verificar los registros insertados
SELECT * FROM [Sponsors] ORDER BY [DisplayOrder]
GO
