# 🔐 Scripts de Backup de Usuarios - NO SUBIR A GITHUB

## ⚠️ IMPORTANTE - SEGURIDAD

Estos archivos contienen o contendrán **datos reales de usuarios** incluyendo:
- Nombres de usuario
- Emails
- Hashes de contraseñas
- Roles asignados

**NUNCA los subas a GitHub o los compartas públicamente.**

## 📁 Archivos en esta Carpeta

### `backup-users-real.sql`
Script para **exportar** los usuarios actuales de la base de datos.
- Genera INSERT statements con todos los usuarios (excepto admin)
- Incluye contraseñas encriptadas y roles

### `restore-users-real.sql`
Script para **restaurar** los usuarios después de una migración.
- Debe ser editado manualmente con el output de `backup-users-real.sql`
- Elimina usuarios de prueba antes de restaurar
- Mantiene el usuario admin intacto

### `backup-helper.sh`
Script de ayuda que muestra instrucciones paso a paso.
```bash
./backup-helper.sh
```

## 🚀 Uso Rápido

### Antes de Migrar:
```sql
-- 1. Ejecuta en tu base de datos de PRODUCCIÓN
-- Archivo: backup-users-real.sql

-- 2. Copia TODO el output

-- 3. Pega en restore-users-real.sql (sección indicada)

-- 4. Guarda restore-users-real.sql
```

### Después de Migrar:
```sql
-- 5. Ejecuta en tu base de datos (ya migrada)
-- Archivo: restore-users-real.sql (con los datos pegados)

-- 6. Verifica que todos los usuarios estén presentes
```

## 📖 Documentación Completa

Lee `GUIA_MIGRACION_SEGURA.md` para instrucciones detalladas.

## ✅ Verificación de Seguridad

Estos archivos están protegidos en `.gitignore`:

```bash
# Verificar que NO aparezcan en git status
git status

# Los archivos *-real.sql NO deben aparecer en "Changes to be committed"
# Deben aparecer en "Untracked files" o estar ignorados
```

## 🗑️ Eliminar Después de Usar

Una vez que hayas aplicado las migraciones exitosamente y verificado que todo funciona:

```bash
# Opcional: Eliminar los archivos con datos sensibles
rm backup-users-real.sql
rm restore-users-real.sql

# O mantenerlos SOLO localmente para futuras migraciones
```

---

**Fecha**: 23 de octubre de 2025  
**Proyecto**: Farmacia Solidaria Cristiana
