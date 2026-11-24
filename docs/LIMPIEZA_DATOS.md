# 🧹 LIMPIEZA COMPLETA DE DATOS DE PRUEBA

## 📝 Propósito

Este script **elimina TODOS los datos de prueba** pero **preserva los usuarios reales**.

## ⚠️ IMPORTANTE: ¿Qué se ELIMINA?

- ✅ **Pacientes** (todos, incluidos los que tienen `TEMP` en identificación)
- ✅ **Medicamentos** (todo el inventario)
- ✅ **Donaciones** (todo el historial de donaciones)
- ✅ **Entregas** (todo el historial de entregas)
- ✅ **Documentos de pacientes** (todas las fotos/PDFs subidos)

## ✅ ¿Qué se PRESERVA?

- 🔐 **Usuarios** (admin, equipo, idalmis, pruebamia, adriano, Joel, perica, susej)
- 🔐 **Patrocinadores** (todos los patrocinadores registrados - son datos reales)
- 🔐 **Roles** (Admin, Farmaceutico, Viewer, ViewerPublic)
- 🔐 **Configuración de Identity**

## 📋 Cuándo Usar Este Script

Úsalo cuando:
1. Hayas terminado de probar el sistema
2. Tengas que empezar a ingresar datos reales de pacientes
3. Quieras limpiar todos los datos ficticios de una sola vez
4. Necesites resetear los contadores de IDs a 1

## 🚀 Cómo Ejecutar

### Paso 1: Acceder a la Base de Datos

1. Ve a https://somee.com
2. Inicia sesión
3. Navega a: "My Websites" → Tu sitio → "Manage my DB"
4. Haz clic en "Database Manager"

### Paso 2: Ejecutar el Script

1. Abre el archivo: `reset-data-keep-users.sql`
2. **Copia TODO el contenido**
3. Pega en el editor SQL de Somee
4. **Haz clic en "Execute"**
5. Espera a que termine

### Paso 3: Verificar el Resultado

Debes ver un mensaje como este:

```
=========================================================================
LIMPIEZA COMPLETADA EXITOSAMENTE
=========================================================================

Estado de la base de datos:
  - Pacientes: 0
  - Medicamentos: 0
  - Donaciones: 0
  - Entregas: 0
  - Documentos: 0
  - Patrocinadores: (número actual) ✅ PRESERVADOS
  - Usuarios: 8 ✅ PRESERVADOS

✅ Base de datos lista para datos de producción
```

## 🔍 Qué Hace el Script (Detallado)

### 1. Verificación Inicial
- Cuenta cuántos usuarios hay
- Muestra la lista de usuarios que se preservarán

### 2. Eliminación en Orden
Elimina los datos respetando las relaciones entre tablas:
1. **PatientDocuments** (documentos primero, dependen de pacientes)
2. **Deliveries** (entregas dependen de pacientes y medicamentos)
3. **Donations** (donaciones dependen de medicamentos)
4. **Medicines** (medicamentos)
5. **Patients** (pacientes)
6. **Sponsors** (NO SE ELIMINAN - son datos reales)

### 3. Reseteo de Contadores
- Los próximos registros empezarán con ID = 1
- Útil para mantener la base de datos limpia

### 4. Verificación Final
- Confirma que los usuarios siguen ahí
- Muestra el resumen de todos los contadores en 0

## 🛡️ Seguridad

El script **NO puede eliminar datos críticos** porque:
- Solo hace `DELETE` en tablas de datos temporales/prueba
- Nunca toca `AspNetUsers`, `AspNetRoles`, ni `AspNetUserRoles`
- Nunca toca la tabla `Sponsors` (patrocinadores reales)
- Muestra los usuarios y patrocinadores antes y después para que confirmes

## 📊 Ejemplo de Salida

```sql
=========================================================================
INICIANDO LIMPIEZA COMPLETA DE DATOS
Fecha: 2025-10-23 15:30:45
=========================================================================

-- PARTE 1: Verificando usuarios existentes...

Usuarios registrados actualmente: 8

Usuarios que se PRESERVARÁN:
xxx.   xxxxxx

=========================================================================

-- PARTE 2: Eliminando datos de prueba...

✓ Documentos de pacientes eliminados: 15
✓ Entregas eliminadas: 42
✓ Donaciones eliminadas: 23
✓ Medicamentos eliminados: 67
✓ Pacientes eliminados: 35
⚠️ Patrocinadores PRESERVADOS (datos reales): 5

-- PARTE 3: Reseteando contadores de identidad...

✓ Contador de Patients reiniciado
✓ Contador de Medicines reiniciado
✓ Contador de Donations reiniciado
✓ Contador de Deliveries reiniciado
✓ Contador de PatientDocuments reiniciado
✓ Contador de Sponsors reiniciado

-- PARTE 4: Verificando usuarios después de limpieza...

Usuarios preservados: 8

Lista de usuarios preservados:
admin, adriano, equipo, idalmis, Joel, perica, pruebamia, susej

=========================================================================
LIMPIEZA COMPLETADA EXITOSAMENTE
=========================================================================
```

## ⚠️ Antes de Ejecutar

**Pregúntate:**
1. ¿Ya guardé los datos importantes en otro lado? (si los hay)
2. ¿Estoy seguro de que quiero eliminar TODO?
3. ¿Verifiqué que los usuarios estén correctos?

**Si la respuesta es SÍ a todo**, adelante.

## 🔄 Después de Ejecutar

1. **Actualiza la aplicación web** (Ctrl+F5)
2. **Inicia sesión** con tu usuario
3. **Empieza a ingresar datos reales**:
   - Pacientes con carnets reales
   - Medicamentos del inventario real
   - Donaciones reales
   - Patrocinadores reales

## 🆘 Si Algo Sale Mal

### Error: "Cannot delete because of foreign key constraint"
**Causa:** Hay datos relacionados que no se eliminaron en orden.  
**Solución:** Vuelve a ejecutar el script. El orden está correcto.

### Error: "Invalid object name"
**Causa:** La tabla no existe en tu base de datos.  
**Solución:** Comenta esa sección del script y continúa.

### Usuarios desaparecieron
**Causa:** Imposible con este script, no toca AspNetUsers.  
**Solución:** Si pasó, usa `apply-migration-somee.sql` que restaura los usuarios.

## 📞 Contacto

Si tienes dudas antes de ejecutar, **pregunta primero**.  
Mejor prevenir que tener que restaurar backups.

---

**Archivo:** `reset-data-keep-users.sql`  
**Versión:** 1.0  
**Fecha:** 23 de octubre de 2025
