-- Script para actualizar ContentType y FilePath de documentos de pacientes existentes
-- basado en la extensión del archivo en FilePath
-- Ejecutar una sola vez para arreglar datos históricos

-- PRIMERO: Arreglar FilePath que no comienzan con /
UPDATE PatientDocuments 
SET FilePath = '/' + FilePath
WHERE FilePath NOT LIKE '/%' AND FilePath IS NOT NULL;

-- Actualizar imágenes JPEG
UPDATE PatientDocuments 
SET ContentType = 'image/jpeg'
WHERE ContentType IS NULL 
  AND (FilePath LIKE '%.jpg' OR FilePath LIKE '%.jpeg');

-- Actualizar imágenes PNG
UPDATE PatientDocuments 
SET ContentType = 'image/png'
WHERE ContentType IS NULL 
  AND FilePath LIKE '%.png';

-- Actualizar imágenes GIF
UPDATE PatientDocuments 
SET ContentType = 'image/gif'
WHERE ContentType IS NULL 
  AND FilePath LIKE '%.gif';

-- Actualizar imágenes WebP
UPDATE PatientDocuments 
SET ContentType = 'image/webp'
WHERE ContentType IS NULL 
  AND FilePath LIKE '%.webp';

-- Actualizar PDFs
UPDATE PatientDocuments 
SET ContentType = 'application/pdf'
WHERE ContentType IS NULL 
  AND FilePath LIKE '%.pdf';

-- Verificar resultados
SELECT Id, FileName, FilePath, ContentType, FileSize 
FROM PatientDocuments 
ORDER BY UploadDate DESC;
