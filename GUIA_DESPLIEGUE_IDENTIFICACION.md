# 🚀 Guía de Despliegue - Sistema de Identificación de Pacientes

## 📋 Resumen de Cambios Implementados

### ✅ Funcionalidades Completadas

1. **Campo de Identificación Obligatorio**
   - Carnet de Identidad (11 dígitos) o Pasaporte (letra + 6-7 dígitos)
   - Validación automática del formato cubano
   - Campo ahora aparece PRIMERO en los formularios

2. **Búsqueda Automática de Pacientes**
   - Al ingresar la identificación y perder el foco, busca automáticamente
   - Muestra datos del paciente si ya existe
   - Muestra historial de entregas anteriores
   - Permite editar la ficha existente

3. **Historial de Entregas en Ficha de Paciente**
   - Sección 6 actualizada con historial completo
   - Muestra medicamento, cantidad y fecha

4. **Entregas Vinculadas a Pacientes**
   - Campo de identificación obligatorio en entregas
   - Búsqueda automática del paciente
   - Muestra historial al ingresar identificación

5. **Compresión Automática de Imágenes**
   - Reduce tamaño de fotos de documentos (60-80%)
   - Optimiza almacenamiento

## 🎯 Pasos para Desplegar en Somee

### PASO 1: Preparar el Script de Migración

1. Abre el archivo donde guardaste el backup de usuarios
2. Copia TODO el contenido (los INSERT statements)
3. Abre el archivo `apply-migration-somee.sql`
4. Busca la sección que dice:
   ```sql
   -- ==================== PEGA AQUÍ LOS INSERT STATEMENTS DEL BACKUP ====================
   ```
5. Pega el contenido del backup DEBAJO de esa línea
6. Guarda el archivo

### PASO 2: Aplicar la Migración en Somee

1. Ve a https://somee.com
2. Inicia sesión en tu cuenta
3. Ve a "Manage my DB" o "Database Manager"
4. Selecciona tu base de datos
5. Abre el editor SQL
6. Copia TODO el contenido de `apply-migration-somee.sql` (ya con tus datos pegados)
7. Pega en el editor SQL de Somee
8. **Ejecuta el script**
9. Revisa el output - debe decir "MIGRACIÓN COMPLETADA EXITOSAMENTE"

### PASO 3: Desplegar la Aplicación Actualizada

Opción A - Usando el script automático:
```bash
./deploy-to-somee.sh
```

Opción B - Manual:
```bash
# 1. Publicar la aplicación
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana
dotnet publish -c Release -o ../publish

# 2. Subir los archivos a Somee vía FTP
# (Usa tu cliente FTP preferido: FileZilla, Cyberduck, etc.)
```

### PASO 4: Verificación

1. Abre tu sitio en Somee
2. Inicia sesión con admin (contraseña: Admin123!)
3. Ve a "Pacientes" → "Nuevo Paciente"
4. Verifica que el campo "Carnet de Identidad o Pasaporte" aparece PRIMERO
5. Prueba ingresar un carnet: `12345678901` (11 dígitos)
6. Debe mostrar validación de formato
7. Completa el formulario y crea un paciente de prueba
8. Ve a "Entregas" → "Nueva Entrega"
9. Ingresa el carnet del paciente que creaste
10. Debe mostrar los datos del paciente y permitir la entrega

## 🔍 Validaciones de Formato

### Carnet de Identidad (Cuba)
- **Formato**: 11 dígitos numéricos
- **Ejemplo válido**: `12345678901`
- **Ejemplo inválido**: `123456789` (menos de 11)

### Pasaporte
- **Formato**: Una letra seguida de 6 o 7 dígitos
- **Ejemplos válidos**: 
  - `A123456`
  - `B1234567`
- **Ejemplos inválidos**: 
  - `AB12345` (dos letras)
  - `A12345` (solo 5 dígitos)

## 📊 Estructura de Datos

### Tabla Patients
```sql
IdentificationDocument nvarchar(20) NOT NULL
-- Ahora es obligatorio y limitado a 20 caracteres
```

### Tabla Deliveries
```sql
PatientIdentification nvarchar(20) NOT NULL
-- Nueva columna para vincular con paciente
```

## ⚠️ Notas Importantes

### Datos Existentes
- **Pacientes sin identificación**: Se les asignará "TEMP{ID}" temporalmente
- **Entregas existentes**: Se actualizarán con la identificación del paciente vinculado
- **Usuarios**: Se mantienen todos los usuarios reales (gracias al backup)

### Seguridad
- El archivo `apply-migration-somee.sql` con tus datos NO se sube a GitHub
- Está protegido en `.gitignore`
- Elimínalo después de usarlo si contiene datos sensibles

## 🐛 Solución de Problemas

### Error: "Paciente no encontrado" en Entregas
**Causa**: El paciente no está registrado con esa identificación

**Solución**: 
1. Ve a Pacientes
2. Busca o crea el paciente con ese carnet/pasaporte
3. Intenta la entrega nuevamente

### Error: "Formato inválido"
**Causa**: El carnet/pasaporte no cumple el formato cubano

**Solución**:
- Carnet: Exactamente 11 dígitos
- Pasaporte: 1 letra + 6 o 7 dígitos

### No aparece el historial de entregas
**Causa**: El paciente es nuevo o no tiene entregas

**Solución**: Esto es normal. Haz una entrega y aparecerá en el historial

### Usuarios desaparecieron después de migración
**Causa**: No se pegaron los datos del backup en el script

**Solución**:
1. Ve a la base de datos
2. Ejecuta el script `restore-users-real.sql` (con los datos pegados)
3. Los usuarios se restaurarán

## 📞 Después del Despliegue

1. Notifica a los usuarios del cambio
2. Explica que ahora deben usar carnet/pasaporte
3. Verifica que pueden hacer entregas correctamente
4. Monitorea los logs por si hay problemas

## ✅ Checklist Final

- [ ] Backup de usuarios guardado
- [ ] Datos del backup pegados en `apply-migration-somee.sql`
- [ ] Script ejecutado en Somee sin errores
- [ ] Aplicación publicada y desplegada
- [ ] Login con admin funciona
- [ ] Crear paciente con carnet funciona
- [ ] Búsqueda automática funciona
- [ ] Historial de entregas se muestra
- [ ] Nueva entrega se registra correctamente
- [ ] Todos los usuarios reales están presentes

---

**Fecha de implementación**: 23 de octubre de 2025  
**Versión**: 2.0 - Sistema de Identificación de Pacientes
