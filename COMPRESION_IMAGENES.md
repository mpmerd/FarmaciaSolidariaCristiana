# Sistema de Compresión Automática de Imágenes

## Descripción

El sistema ahora incluye compresión automática de imágenes para los documentos de pacientes, optimizando el almacenamiento y mejorando el rendimiento de la aplicación.

## Características

### ✅ Compresión Inteligente
- **Detección Automática**: El sistema detecta automáticamente si un archivo subido es una imagen
- **Formatos Soportados**: JPEG, JPG, PNG, GIF, BMP, WebP, TIFF
- **Preservación de Archivos No-Imagen**: Los documentos PDF y otros archivos se guardan sin modificar

### 📏 Dimensiones
- **Tamaño Máximo**: 1920x1920 píxeles para documentos de pacientes
- **Proporción Mantenida**: La relación de aspecto original se preserva siempre
- **Redimensionamiento Inteligente**: Solo se redimensiona si la imagen supera las dimensiones máximas

### 🎯 Calidad de Compresión
- **JPEG**: Calidad 85% (balance óptimo entre calidad y tamaño)
- **PNG**: Compresión máxima (sin pérdida de calidad)
- **Conversión Automática**: Imágenes que no son PNG se convierten a JPEG para mejor compresión

### 📊 Beneficios

#### Ahorro de Espacio
- **Reducción Típica**: 60-80% del tamaño original para fotos de alta resolución
- **Ejemplo**: Una foto de 5MB se reduce a ~1MB o menos
- **Registro de Compresión**: El sistema registra el ratio de compresión en los logs

#### Rendimiento Mejorado
- **Carga Más Rápida**: Las imágenes comprimidas se cargan más rápido
- **Menos Ancho de Banda**: Ideal para conexiones lentas
- **Mejor Experiencia**: Navegación más fluida en dispositivos móviles

### 🔧 Implementación Técnica

#### Servicio de Compresión (`IImageCompressionService`)

```csharp
// Comprimir una imagen
var compressedStream = await _imageCompressionService.CompressImageAsync(
    inputStream,
    contentType,
    maxWidth: 1920,
    maxHeight: 1920,
    quality: 85
);

// Verificar si un archivo es imagen
bool isImage = _imageCompressionService.IsImage(contentType);
```

#### Proceso Automático

1. **Subida de Documento**: Usuario selecciona archivo(s) para un paciente
2. **Detección**: El sistema verifica si es una imagen
3. **Compresión**: Si es imagen, se comprime automáticamente
   - Redimensiona si es necesario
   - Aplica compresión JPEG o PNG según el tipo
4. **Almacenamiento**: Guarda el archivo optimizado
5. **Registro**: Actualiza la base de datos con el tamaño real del archivo

### 📝 Logs y Monitoreo

El sistema registra información detallada sobre la compresión:

```
[INFO] Compressing image: foto_paciente.jpg, Original size: 5242880 bytes
[INFO] Image resized to: 1920x1080
[INFO] Compression complete. New size: 1048576 bytes, Compression: 80.00%
```

### ⚙️ Configuración

#### Parámetros Ajustables en `PatientsController.cs`:

```csharp
await _imageCompressionService.CompressImageAsync(
    inputStream, 
    file.ContentType,
    maxWidth: 1920,    // Ajustar según necesidades
    maxHeight: 1920,   // Ajustar según necesidades
    quality: 85        // Rango: 1-100 (85 recomendado)
);
```

#### Recomendaciones:
- **Calidad 85-90**: Balance óptimo para fotos médicas
- **Calidad 70-85**: Documentos escaneados
- **Dimensiones 1920**: Suficiente para visualización en pantalla completa

### 🔒 Seguridad

- ✅ Validación de tipos MIME
- ✅ Manejo de errores robusto
- ✅ Fallback al original si la compresión falla
- ✅ Sin modificación de archivos no-imagen

### 📦 Dependencias

**Paquete NuGet**: `SixLabors.ImageSharp` v3.1.11
- Biblioteca de procesamiento de imágenes de alto rendimiento
- Cross-platform (Windows, Linux, macOS)
- Sin dependencias nativas

### 🚀 Uso en la Aplicación

#### Para Usuarios
La compresión es **completamente transparente**:
1. Sube documentos/imágenes de pacientes normalmente
2. El sistema optimiza automáticamente las imágenes
3. No se requiere ninguna acción adicional

#### Para Administradores
Monitorear los logs para ver el ahorro de espacio:
```bash
# Ver logs de compresión
dotnet run | grep "Compression complete"
```

### 🔄 Mantenimiento

#### Actualizar la Biblioteca de Imágenes
```bash
dotnet add package SixLabors.ImageSharp --version [nueva-versión]
```

#### Limpiar Imágenes Antiguas (si se necesita migración)
Si hay imágenes existentes sin comprimir, se puede crear un script de migración para comprimirlas retroactivamente.

### ❓ Preguntas Frecuentes

**Q: ¿Se pierden datos médicos importantes al comprimir?**  
A: No. Con calidad 85%, la diferencia visual es imperceptible. Para documentos críticos, se puede aumentar a 90-95%.

**Q: ¿Qué pasa con los PDFs?**  
A: Los PDFs y otros documentos no-imagen se guardan sin modificar.

**Q: ¿Puedo desactivar la compresión?**  
A: Sí, simplemente comenta las líneas de compresión en `UploadDocuments()` y guarda directamente el archivo.

**Q: ¿Funciona en producción?**  
A: Sí, SixLabors.ImageSharp es una biblioteca estable y ampliamente utilizada en producción.

### 📈 Estadísticas Esperadas

Para una clínica con 100 pacientes y 3 imágenes promedio por paciente:

- **Sin Compresión**: ~1.5GB (5MB por imagen)
- **Con Compresión**: ~300MB (1MB por imagen)
- **Ahorro**: ~1.2GB (80%)

---

**Última Actualización**: 23 de octubre de 2025  
**Versión**: 1.0
