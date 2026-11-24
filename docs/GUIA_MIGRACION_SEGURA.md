# Guía de Migración Segura - Preservación de Usuarios Reales

## 📋 Resumen

Esta guía te ayudará a aplicar migraciones de base de datos sin perder los usuarios reales que ya están creados en el sistema.

## 🔐 Archivos de Seguridad

Los siguientes archivos están protegidos en `.gitignore` y **NO se subirán a GitHub**:

- `backup-users-real.sql` - Script para exportar usuarios
- `restore-users-real.sql` - Script para restaurar usuarios
- Cualquier archivo que termine en `-production-data.sql`

## 📝 Proceso Paso a Paso

### ANTES de Aplicar Migraciones

#### Paso 1: Hacer Backup de Usuarios

1. Abre **SQL Server Management Studio (SSMS)** o el panel de **Somee.com**
2. Conecta a tu base de datos de **PRODUCCIÓN**
3. Ejecuta el script `backup-users-real.sql`
4. **Copia TODO el output** que aparece en los resultados

El output será algo como:

```sql
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, ...) VALUES ('abc123...', 'usuario1', ...);
INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, ...) VALUES ('def456...', 'usuario2', ...);
INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES ('abc123...', 'role-id-123');
...
```

#### Paso 2: Guardar los Datos

1. Abre el archivo `restore-users-real.sql`
2. Busca la sección que dice:
   ```sql
   -- ==================== PEGA AQUÍ LOS INSERT STATEMENTS ====================
   ```
3. **Pega** todo el output copiado del Paso 1
4. **Guarda** el archivo `restore-users-real.sql`

### APLICAR LAS MIGRACIONES

#### Opción A: Entorno Local (Desarrollo)

```bash
# En la terminal, desde la raíz del proyecto
cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana

# Crear la migración
dotnet ef migrations add AddPatientIdentificationRequired

# Aplicar la migración a tu base de datos local
dotnet ef database update
```

#### Opción B: Entorno de Producción (Somee)

1. Genera la migración localmente (ver Opción A)
2. Copia el contenido del archivo de migración generado en `Migrations/`
3. Conviértelo a script SQL o aplícalo directamente en Somee

### DESPUÉS de Aplicar Migraciones

#### Paso 3: Restaurar Usuarios Reales

1. Abre **SQL Server Management Studio** o el panel de **Somee.com**
2. Conecta a tu base de datos (ahora con las migraciones aplicadas)
3. Ejecuta el archivo `restore-users-real.sql` **COMPLETO**
   - El script borrará usuarios de prueba
   - Mantendrá el usuario `admin`
   - Restaurará todos los usuarios reales

#### Paso 4: Verificación

El script mostrará automáticamente:

- ✅ Lista de usuarios restaurados
- ✅ Total de usuarios en el sistema
- ✅ Distribución de usuarios por rol

Verifica que todos tus usuarios reales aparezcan en la lista.

## ⚠️ IMPORTANTE - Notas de Seguridad

### ❌ NUNCA Hagas Esto:

- **NO** subas `backup-users-real.sql` a GitHub (ya está en .gitignore)
- **NO** subas `restore-users-real.sql` a GitHub (ya está en .gitignore)
- **NO** compartas estos archivos públicamente
- **NO** los incluyas en commits

### ✅ SÍ Haz Esto:

- **SÍ** mantén estos archivos solo localmente
- **SÍ** haz copias de seguridad adicionales en un lugar seguro
- **SÍ** elimina los archivos si ya no los necesitas
- **SÍ** verifica el `.gitignore` antes de cada commit

## 🔄 Ejemplo Completo de Flujo

```bash
# 1. BACKUP (en SSMS o Somee)
# Ejecuta: backup-users-real.sql
# Copia el output

# 2. PREPARAR RESTAURACIÓN
# Pega el output en restore-users-real.sql
# Guarda el archivo

# 3. APLICAR MIGRACIÓN (local)
dotnet ef migrations add AddPatientIdentificationRequired
dotnet ef database update

# 4. RESTAURAR USUARIOS (en SSMS o Somee)
# Ejecuta: restore-users-real.sql

# 5. VERIFICAR
# Revisa que todos los usuarios estén presentes
```

## 📊 Datos que se Preservan

### ✅ Se Preservan (Usuarios Reales):

- Todos los usuarios **excepto** `admin`
- Contraseñas encriptadas
- Roles asignados
- Emails y configuraciones
- Estado de confirmación de email

### ❌ Se Pierden (Datos de Prueba):

- Medicamentos de prueba (inyectados por DataSeeder)
- Donaciones de prueba
- Entregas de prueba
- Patrocinadores de prueba

### ♻️ Se Recrean Automáticamente:

- Usuario `admin` (con contraseña: Admin123!)
- Roles base: Admin, Farmaceutico, Viewer, ViewerPublic
- Estructura de base de datos actualizada

## 🆘 Solución de Problemas

### Problema: "Cannot insert duplicate key"

**Causa**: Los usuarios ya existen en la base de datos

**Solución**: El script `restore-users-real.sql` ya elimina usuarios existentes (excepto admin). Si persiste:

```sql
-- Ejecuta esto ANTES de restore-users-real.sql
DELETE FROM AspNetUserRoles WHERE UserId NOT IN (SELECT Id FROM AspNetUsers WHERE UserName = 'admin');
DELETE FROM AspNetUsers WHERE UserName <> 'admin';
```

### Problema: "User not found after migration"

**Causa**: No se ejecutó el script de restauración

**Solución**: Ejecuta `restore-users-real.sql` con los datos pegados

### Problema: "Lost admin password"

**Causa**: Se eliminó accidentalmente el admin

**Solución**: El admin se recrea automáticamente con las migraciones:
- Usuario: `admin`
- Contraseña: `Admin123!`

## 📞 Contacto y Soporte

Si tienes problemas durante la migración:

1. **NO** borres la base de datos sin hacer backup
2. Revisa los mensajes de error cuidadosamente
3. Verifica que copiaste TODO el output del backup
4. Asegúrate de estar conectado a la base de datos correcta

---

**Última actualización**: 23 de octubre de 2025  
**Versión**: 1.0
