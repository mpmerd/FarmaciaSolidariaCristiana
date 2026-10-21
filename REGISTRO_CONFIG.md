# Configuración del Registro de Usuarios

## 📋 Descripción General

El sistema cuenta con un sistema de registro público que permite a usuarios externos crear cuentas con permisos de solo lectura (rol "Viewer"). Esta funcionalidad puede ser habilitada o deshabilitada según las necesidades del entorno (desarrollo, producción).

## 🔐 Roles de Usuario

El sistema maneja cuatro roles principales:

1. **Admin**: Acceso completo al sistema, gestión de usuarios
2. **Farmaceutico**: Gestión de medicamentos, donaciones, entregas y pacientes
3. **Viewer**: Solo lectura 
4. **ViwerPublic** Solo lectura mas restrictivo (usuarios auto-registrados)

## ⚙️ Configuración

### Habilitar/Deshabilitar el Registro Público

El registro público se controla mediante el archivo `appsettings.json`:

```json
{
  "AppSettings": {
    "EnablePublicRegistration": true,  // true = habilitado, false = deshabilitado
    "SiteName": "Farmacia Solidaria Cristiana",
    "SiteUrl": "https://farmaciasolidaria.somee.com"
  }
}
```

### 🔴 IMPORTANTE - Configuración para Producción

**Cuando despliegues la aplicación en el servidor HTTPS en producción:**

1. **DESHABILITAR el registro público** editando `appsettings.json`:

```json
{
  "AppSettings": {
    "EnablePublicRegistration": false,  // ⚠️ DESHABILITADO para producción
    "SiteName": "Farmacia Solidaria Cristiana",
    "SiteUrl": "https://farmaciasolidaria.somee.com"
  }
}
```

2. **Actualizar la URL del sitio** al dominio real con HTTPS

### 📧 Configuración de Email (SMTP)

Para que funcionen las notificaciones y recuperación de contraseña, configura el SMTP en `appsettings.json`:

#### Opción 1: Servidor SMTP de Somee (✅ Configuración actual de producción)

```json
{
  "SmtpSettings": {
    "Host": "smtp.farmaciasolidaria.somee.com",
    "Port": "26",
    "Username": "noreply@farmaciasolidaria.somee.com",
    "Password": "tu-contraseña-email",
    "FromEmail": "noreply@farmaciasolidaria.somee.com",
    "FromName": "Farmacia Solidaria Cristiana",
    "EnableSsl": "false"
  }
}
```

> Cambiado el 21 oct 2026: la cuenta Gmail compartida agotaba su cuota diaria
> (500/día, error `550 5.4.5`) y bloqueaba los códigos de registro. El SMTP propio
> de Somee no tiene ese tope.

#### Opción 2: Gmail (alternativa, solo desarrollo)

```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "tu-email@gmail.com",
    "Password": "tu-app-password",  // ⚠️ Usar "App Password" de Gmail
    "FromEmail": "tu-email@gmail.com",
    "FromName": "Farmacia Solidaria Cristiana",
    "EnableSsl": "true"
  }
}
```

**Cómo obtener App Password de Gmail:**
1. Ve a tu cuenta de Google → Seguridad
2. Activa "Verificación en 2 pasos"
3. Ve a "Contraseñas de aplicaciones"
4. Genera una contraseña para "Correo"
5. Usa esa contraseña en el campo `Password`

## 🚀 Proceso de Despliegue a Producción

### Checklist antes de publicar:

- [ ] 1. Cambiar `EnablePublicRegistration` a `false` en `appsettings.json`
- [ ] 2. Actualizar `SiteUrl` a la URL real de producción con HTTPS
- [ ] 3. Configurar credenciales SMTP válidas
- [ ] 4. Verificar la cadena de conexión a la base de datos de producción
- [ ] 5. Compilar en modo Release: `dotnet publish -c Release`
- [ ] 6. Subir archivos vía FTP al servidor
- [ ] 7. Verificar que el sitio funcione correctamente

### Cómo habilitar el registro posteriormente (si es necesario)

Si en algún momento deseas permitir registro público en producción:

1. Edita el archivo `appsettings.json` en el servidor
2. Cambia `"EnablePublicRegistration": false` a `"EnablePublicRegistration": true`
3. Reinicia la aplicación

## 🔍 Verificación del Estado

Para verificar si el registro está habilitado o deshabilitado:

1. Accede a `/Account/Login`
2. Si el registro está habilitado, verás el botón "Registrarse"
3. Si está deshabilitado, el botón no aparecerá

## 📝 Notas de Seguridad

### Producción (HTTPS):
- ✅ Registro público **DESHABILITADO** por defecto
- ✅ Solo administradores pueden crear usuarios
- ✅ Certificado SSL/TLS activo

### Desarrollo (HTTP):
- ✅ Registro público **HABILITADO** para pruebas
- ✅ Todos los usuarios nuevos tienen rol "Viewer"
- ⚠️ Solo para ambiente local/desarrollo

## 🛠️ Comandos Útiles

```bash
# Compilar para producción
dotnet publish -c Release -o ./publish

# Verificar configuración actual
cat appsettings.json | grep EnablePublicRegistration

# Aplicar migraciones en producción
dotnet ef database update --connection "tu-connection-string"
```

## 📞 Soporte

Si tienes dudas sobre la configuración, contacta al administrador del sistema o revisa la documentación técnica en el repositorio.

---

**Última actualización:** 21 de octubre de 2025
