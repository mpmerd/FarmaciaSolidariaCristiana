# Guía de Despliegue en Somee.com

Esta guía explica paso a paso cómo desplegar la aplicación **Farmacia Solidaria Cristiana** en el servicio de hosting **Somee.com**.

## 📋 Requisitos Previos

### En tu equipo local:
- .NET 8 SDK instalado
- Proyecto compilado y funcionando localmente
- Acceso a internet
- Cliente FTP instalado (lftp en macOS/Linux)
- Cuenta de Somee.com configurada

### En Somee.com:
- Cuenta activa de hosting
- Base de datos SQL Server configurada
- Acceso FTP habilitado
- Panel de control de Somee disponible

---

## 🌐 Paso 1: Configurar tu Aplicación en Somee.com

### 1.1 Crear/Verificar la Aplicación
1. Accede al panel de Somee: https://somee.com
2. Ve a **"My Websites"**
3. Verifica que tu aplicación esté creada
4. Anota el dominio: `tuapp.somee.com`

### 1.2 Verificar Base de Datos
1. En el panel de Somee, ve a **"My Databases"**
2. Verifica que tu base de datos SQL Server esté activa
3. Anota el nombre del servidor y base de datos
4. **IMPORTANTE**: Guarda estas credenciales de forma segura

### 1.3 Configurar FTP
1. En el panel de Somee, ve a **"FTP Settings"**
2. Verifica que el acceso FTP esté habilitado
3. Anota el host FTP: `tuapp.somee.com`
4. Verifica tu usuario FTP
5. **IMPORTANTE**: Ten a mano tu contraseña FTP

---

## 📦 Paso 2: Preparar la Aplicación para Despliegue

### 2.1 Configurar appsettings.json
Edita el archivo de configuración de producción:

```bash
cd FarmaciaSolidariaCristiana
```

Edita `appsettings.json` con tu editor preferido:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=NOMBRE_SERVIDOR_SOMEE;Database=NOMBRE_BD;User Id=USUARIO_BD;Password=PASSWORD_BD;TrustServerCertificate=True;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "TU_EMAIL@gmail.com",
    "SenderName": "Farmacia Solidaria Cristiana",
    "Password": "TU_APP_PASSWORD_GMAIL"
  },
  "EnablePublicRegistration": true
}
```

> ⚠️ **IMPORTANTE SEGURIDAD:**
> - Nunca incluyas credenciales reales en el código versionado
> - Usa un archivo `appsettings.Production.json` separado
> - No compartas las contraseñas en repositorios públicos
> - Para Gmail SMTP, usa "App Passwords" (contraseñas de aplicación)

### 2.2 Publicar la Aplicación
Desde el directorio raíz del proyecto:

```bash
cd /Users/tuusuario/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana

# Publicar en modo Release
dotnet publish -c Release -o ../publish
```

Este comando:
- Compila la aplicación en modo Release (optimizada)
- Genera todos los archivos necesarios
- Los coloca en la carpeta `publish/` en el directorio padre

### 2.3 Verificar Archivos Publicados
```bash
cd ../publish
ls -la
```

Deberías ver archivos como:
- `FarmaciaSolidariaCristiana.dll` (principal)
- `appsettings.json`
- `web.config`
- Carpeta `wwwroot/`
- Carpeta `Views/`
- Y otros archivos de dependencias

---

## 🗄️ Paso 3: Migrar la Base de Datos

### 3.1 Primera Vez (Nueva Instalación)

Si es tu **primera vez desplegando** en Somee:

1. Abre el archivo `apply-migration-somee.sql` del proyecto
2. **Copia TODO el contenido** del archivo
3. Ve al panel de Somee → **"Manage my DB"** → **"SQL Manager"**
4. Pega el script completo en el editor SQL
5. Haz clic en **"Execute"**
6. Espera el mensaje: **"TODAS LAS MIGRACIONES COMPLETADAS EXITOSAMENTE"**

Este script incluye:
- Creación de todas las tablas
- Relaciones y claves foráneas
- Datos iniciales (roles, usuario admin, etc.)
- Todas las migraciones históricas

### 3.2 Actualización (Ya Desplegado Anteriormente)

Si ya has desplegado antes y solo tienes **cambios nuevos**:

1. Identifica el archivo de migración específico (ej: `apply-migration-turno-documentos.sql`)
2. Copia el contenido del archivo
3. Ve a Somee → **"Manage my DB"** → **"SQL Manager"**
4. Pega y ejecuta el script
5. Verifica que se complete sin errores

Archivos de migración disponibles:
- `apply-migration-somee.sql` - **Migración completa inicial**
- `apply-migration-turno-documentos.sql` - Múltiples documentos por turno
- `apply-migration-fecha-nullable.sql` - Campos de fecha opcionales
- `apply-migration-fechas-bloqueadas-solo.sql` - Sistema de fechas bloqueadas
- `apply-migration-turnos-somee.sql` - Sistema de turnos
- `apply-migration-turnoid.sql` - ID de turno en entregas

### 3.3 Verificar Datos Iniciales

Después de la migración inicial, verifica en el SQL Manager:

```sql
-- Verificar roles
SELECT * FROM AspNetRoles;

-- Verificar usuario admin
SELECT * FROM AspNetUsers WHERE UserName = 'admin';

-- Verificar medicamentos de prueba
SELECT COUNT(*) FROM Medicines;
```

---

## 🚀 Paso 4: Desplegar con el Script Automatizado

### 4.1 Instalar lftp (Si no lo tienes)

**En macOS:**
```bash
brew install lftp
```

**En Linux:**
```bash
sudo apt install lftp
```

### 4.2 Ejecutar Script de Despliegue

Desde el directorio raíz del proyecto:

```bash
bash deploy-to-somee.sh
```

El script te guiará a través de:
1. Verificación de que aplicaste las migraciones SQL
2. Confirmación de archivos a subir
3. Solicitud de credenciales FTP (de forma segura)
4. Creación de directorios necesarios en el servidor
5. Subida de archivos vía FTP
6. Verificación del despliegue

### 4.3 Durante la Ejecución

El script te preguntará:
- **¿Ya aplicaste la migración SQL?** → Responde `s` si completaste el Paso 3
- **Ingresa la contraseña FTP** → Introduce tu contraseña (no se verá en pantalla)

**Ejemplo de ejecución:**
```
==========================================
Farmacia Solidaria Cristiana
Despliegue a Somee.com
==========================================

⚠️  IMPORTANTE: Migración de Base de Datos

Si esta es la primera vez que despliegas O tienes cambios en la BD:
  1. Ve al panel de Somee → Manage my DB → SQL Manager
  2. Ejecuta el script: apply-migration-somee.sql
  3. Espera a que diga: TODAS LAS MIGRACIONES COMPLETADAS EXITOSAMENTE

¿Ya aplicaste la migración SQL? (s/n): s

✓ Migración confirmada. Continuando con el despliegue...

Verificando archivos publicados...
✓ Encontrados 127 archivos para subir

Datos de conexión FTP:
  Host: farmaciasolidaria.somee.com
  Usuario: [tu usuario]
  Ruta remota: /www.farmaciasolidaria.somee.com

Ingresa la contraseña FTP: 
```

### 4.4 Monitoreo del Proceso

Durante la subida verás:
- Lista de archivos siendo transferidos
- Progreso de cada archivo
- Advertencias (normales) sobre permisos chmod
- Confirmación final

**Nota:** Las advertencias sobre `chmod` son normales en Somee y pueden ignorarse.

---

## ✅ Paso 5: Verificación Post-Despliegue

### 5.1 Acceder a la Aplicación

Abre tu navegador y ve a:
```
https://tuapp.somee.com
```

### 5.2 Probar Login de Administrador

Credenciales por defecto:
- **Usuario:** `admin`
- **Contraseña:** (la que definiste en el seed o la por defecto del proyecto)

### 5.3 Verificaciones Esenciales

Prueba las siguientes funcionalidades:

1. **Login:**
   - Accede con admin
   - Verifica que cargue el dashboard

2. **Registro Público** (si está habilitado):
   - Haz clic en "Registrarse"
   - Crea un usuario de prueba
   - Verifica que llegue el email de confirmación

3. **Medicamentos:**
   - Ve a "Medicamentos"
   - Verifica que se vean los medicamentos de prueba
   - Intenta agregar uno nuevo

4. **Sistema de Turnos:**
   - Solicita un turno como usuario normal
   - Verifica que se envíe el email
   - Aprueba el turno como farmacéutico
   - Verifica el email de aprobación

5. **Subida de Archivos:**
   - Sube una foto de paciente
   - Sube un documento PDF en un turno
   - Verifica que se guarden correctamente

### 5.4 Revisar Logs

En caso de errores:

1. Ve al panel de Somee
2. Accede a **"Error Log"** o **"Website Logs"**
3. Busca mensajes de error recientes
4. Anota el stack trace completo

---

## 🔧 Solución de Problemas Comunes

### Problema: La aplicación no carga

**Posibles causas:**
1. **DLL principal no actualizado**
   - Solución: Reinicia la aplicación en el panel de Somee
   - Ve a "Control Panel" → "Website" → "Restart"

2. **Error de conexión a BD**
   - Verifica el connection string en `appsettings.json`
   - Prueba la conexión desde el SQL Manager de Somee

3. **Archivos no subidos correctamente**
   - Ejecuta de nuevo `deploy-to-somee.sh`
   - Verifica que todos los archivos estén en el FTP

### Problema: Errores 500 (Internal Server Error)

**Diagnóstico:**
1. Activa logs detallados en `appsettings.json`:
   ```json
   "Logging": {
     "LogLevel": {
       "Default": "Debug",
       "Microsoft.AspNetCore": "Debug"
     }
   }
   ```
2. Revisa los logs en el panel de Somee
3. Verifica que todas las migraciones se ejecutaron

### Problema: No llegan los emails

**Verificaciones:**
1. **Gmail SMTP:**
   - Verifica que uses "App Password" en lugar de tu contraseña normal
   - Confirma que el SMTP esté en 587
   - Asegúrate de que la autenticación de 2 factores esté activa

2. **SMTP de Somee:**
   - Somee tiene limitaciones en SMTP gratuito
   - Considera usar un servicio externo (Gmail, SendGrid, etc.)

3. **Configuración:**
   - Verifica `EmailSettings` en `appsettings.json`
   - Revisa los logs de la aplicación

### Problema: Archivos subidos no se actualizan

**Solución: Subida forzada**

Si el DLL principal no se actualiza:

```bash
# Reinicia la app en el panel de Somee primero
# Luego ejecuta el script de nuevo
bash deploy-to-somee.sh
```

Si persiste:
1. Ve al panel de Somee
2. Detén la aplicación completamente
3. Espera 30 segundos
4. Vuelve a iniciar la aplicación
5. Ejecuta `deploy-to-somee.sh` nuevamente

### Problema: Límites de Somee

Somee **Free** tiene limitaciones:
- Espacio en disco limitado
- Tiempo de CPU limitado
- La app se detiene después de inactividad
- Limitaciones en SMTP

**Soluciones:**
- Limpia archivos antiguos regularmente
- Considera actualizar a plan de pago si es necesario
- Usa servicios externos para email (Gmail SMTP)

---

## 🔄 Proceso de Actualización Rápida

Para actualizaciones futuras (después del primer despliegue):

```bash
# 1. Publicar nueva versión
cd FarmaciaSolidariaCristiana
dotnet publish -c Release -o ../publish

# 2. Aplicar migraciones SQL (si las hay)
# Ve a Somee → SQL Manager → Ejecuta el script de migración necesario

# 3. Desplegar
cd ..
bash deploy-to-somee.sh
```

---

## 📊 Comandos Útiles

### Ver archivos en el servidor (vía FTP)
```bash
lftp -u tuusuario,tupassword ftp://tuapp.somee.com -e "cd /www.tuapp.somee.com; ls; exit"
```

### Descargar un archivo específico del servidor
```bash
lftp -u tuusuario,tupassword ftp://tuapp.somee.com -e "cd /www.tuapp.somee.com; get web.config; exit"
```

### Verificar tamaño de archivos subidos
```bash
cd publish
du -sh *
```

---

## 🔐 Mejores Prácticas de Seguridad

### 1. Gestión de Credenciales
- ❌ **NO** incluyas credenciales en `appsettings.json` del repositorio
- ✅ Usa `appsettings.Production.json` (agregado a `.gitignore`)
- ✅ Usa variables de entorno cuando sea posible
- ✅ Rota contraseñas regularmente

### 2. Configuración de Producción
- ✅ Deshabilita registro público si no es necesario: `"EnablePublicRegistration": false`
- ✅ Usa HTTPS (Somee proporciona certificado gratuito)
- ✅ Configura CORS apropiadamente
- ✅ Mantén logs en nivel `Information` o `Warning` (no `Debug`)

### 3. Base de Datos
- ✅ Usa contraseñas fuertes para SQL Server
- ✅ Limita permisos del usuario de BD (no uses `sa`)
- ✅ Haz backups regulares (Somee tiene herramientas de backup)

### 4. Archivos Subidos
- ✅ Valida tipos de archivo
- ✅ Limita tamaño de archivos
- ✅ Escanea archivos subidos si es posible
- ✅ Usa carpetas protegidas para documentos sensibles

---

## 📝 Checklist de Despliegue

Usa este checklist para cada despliegue:

- [ ] Código compilado y probado localmente
- [ ] `appsettings.json` configurado correctamente
- [ ] Credenciales de producción verificadas
- [ ] `dotnet publish` ejecutado exitosamente
- [ ] Migraciones SQL preparadas (si aplica)
- [ ] Migraciones ejecutadas en Somee SQL Manager
- [ ] Verificación de migración exitosa
- [ ] `deploy-to-somee.sh` ejecutado sin errores
- [ ] Aplicación accesible en el navegador
- [ ] Login de admin funciona
- [ ] Registro público funciona (si está habilitado)
- [ ] Emails se envían correctamente
- [ ] Subida de archivos funciona
- [ ] No hay errores en logs de Somee
- [ ] Funcionalidades críticas probadas

---

## 📚 Recursos Adicionales

### Documentación del Proyecto
- [README.md](README.md) - Información general del proyecto
- [DEPLOYMENT_UBUNTU.md](DEPLOYMENT_UBUNTU.md) - Despliegue en servidor Ubuntu
- [DEPLOY_GUIDE.md](DEPLOY_GUIDE.md) - Scripts de despliegue general
- [CONFIGURACION.md](CONFIGURACION.md) - Configuración del sistema
- [SECURITY.md](SECURITY.md) - Políticas de seguridad

### Scripts Relacionados
- `deploy-to-somee.sh` - Script principal de despliegue FTP
- `apply-migration-somee.sql` - Migración SQL completa
- `apply-migration-turno-documentos.sql` - Migración de documentos de turnos

### Enlaces Externos
- [Panel de Somee.com](https://somee.com)
- [Documentación de .NET 8](https://docs.microsoft.com/dotnet/)
- [Documentación de ASP.NET Core](https://docs.microsoft.com/aspnet/core/)

---

## 💡 Notas Finales

### Diferencias con Despliegue Ubuntu
- **Somee:** Usa FTP para subir archivos
- **Ubuntu:** Usa SSH y systemd para gestión de servicio
- **Somee:** Base de datos gestionada por Somee
- **Ubuntu:** SQL Server instalado localmente
- **Somee:** Sin acceso a línea de comandos en el servidor
- **Ubuntu:** Control completo del servidor

### Recomendaciones
- Para desarrollo/pruebas: Somee es excelente
- Para producción crítica: Considera un VPS con Ubuntu (mayor control)
- Monitorea los límites de recursos de Somee
- Mantén backups regulares de la base de datos

---

**¡Despliegue Completado! 🎉**

Tu aplicación Farmacia Solidaria Cristiana ahora está en vivo en Somee.com.

Para soporte o preguntas, consulta la documentación del proyecto o los logs de errores en el panel de Somee.
