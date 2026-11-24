# 🚀 GUÍA DE PRUEBA - Sistema de Registro y Recuperación de Contraseña

## ✅ Lo que se ha implementado

1. ✅ Sistema de registro público con rol "Viewer"
2. ✅ Recuperación de contraseña por email
3. ✅ Servicio de email con plantillas HTML
4. ✅ Control para habilitar/deshabilitar registro
5. ✅ Documentación completa

## 📝 PASOS PARA PROBAR

### Paso 1: Configurar Gmail SMTP

**Abre el archivo:** `FarmaciaSolidariaCristiana/appsettings.Development.json`

**Reemplaza estas líneas:**
```json
"Username": "COLOCA-TU-EMAIL@gmail.com",
"Password": "COLOCA-TU-APP-PASSWORD-AQUI",
"FromEmail": "COLOCA-TU-EMAIL@gmail.com",
```

**Con tus datos reales:**
```json
"Username": "tu-email@gmail.com",
"Password": "tu-app-password-de-16-caracteres",
"FromEmail": "tu-email@gmail.com",
```

> 📖 **¿Cómo obtener el App Password?** Lee: `CONFIGURAR_GMAIL_SMTP.md`

### Paso 2: Ejecutar la aplicación

```bash
cd FarmaciaSolidariaCristiana
dotnet run
```

La aplicación estará en: **http://localhost:5003**

### Paso 3: Probar el Registro

1. Ve a: http://localhost:5003/Account/Login
2. Haz clic en el botón **"Registrarse"**
3. Llena el formulario con:
   - Nombre de usuario (mínimo 3 caracteres)
   - Tu email personal (para recibir el correo)
   - Contraseña (mínimo 6 caracteres, con mayúsculas, minúsculas, números y símbolos)
   - Confirmar contraseña
4. Haz clic en **"Registrarse"**
5. **Revisa tu email** - deberías recibir un correo de bienvenida

### Paso 4: Iniciar Sesión

1. Ve a: http://localhost:5003/Account/Login
2. Ingresa el usuario y contraseña que acabas de crear
3. Haz clic en **"Iniciar Sesión"**
4. ✅ Deberías estar dentro del sistema con permisos de solo lectura

### Paso 5: Probar Recuperación de Contraseña

1. Cierra sesión
2. En la página de login, haz clic en **"¿Olvidaste tu contraseña?"**
3. Ingresa tu email
4. **Revisa tu email** - deberías recibir un enlace de recuperación
5. Haz clic en el enlace del email
6. Ingresa una nueva contraseña
7. Haz clic en **"Restablecer Contraseña"**
8. ✅ Prueba iniciar sesión con la nueva contraseña

## 🔍 Verificar Permisos del Rol "Viewer"

Como usuario "Viewer", deberías poder:
- ✅ Ver la lista de medicamentos
- ✅ Ver detalles de medicamentos
- ✅ Ver donaciones y entregas
- ✅ Ver pacientes
- ❌ NO crear, editar o eliminar nada
- ❌ NO acceder a gestión de usuarios (solo Admin)

## 🐛 Solución de Problemas

### No recibo emails

1. **Verifica la configuración SMTP:**
   - Email correcto en `Username` y `FromEmail`
   - App Password correcto (16 caracteres sin espacios)
   - `EnableSsl` en `"true"`

2. **Revisa los logs en la consola:**
   - Busca mensajes de error relacionados con email
   - Si ves "Error sending email", verifica las credenciales

3. **Revisa spam:**
   - Los emails de prueba pueden caer en spam

### Error al compilar

```bash
# Limpia y reconstruye
dotnet clean
dotnet build
```

### Error de base de datos

```bash
# Aplica las migraciones
dotnet ef database update
```

## 📊 Verificar que todo funciona

- [ ] ✅ El botón "Registrarse" aparece en /Account/Login
- [ ] ✅ Puedo registrar un nuevo usuario
- [ ] ✅ Recibo email de bienvenida
- [ ] ✅ Puedo iniciar sesión con el usuario nuevo
- [ ] ✅ El usuario tiene rol "Viewer" (solo lectura)
- [ ] ✅ Puedo solicitar recuperación de contraseña
- [ ] ✅ Recibo email con enlace de recuperación
- [ ] ✅ Puedo restablecer la contraseña
- [ ] ✅ Puedo iniciar sesión con la nueva contraseña

## 🎯 Siguiente Paso: Deshabilitar Registro para Producción

Cuando estés listo para producción, edita `appsettings.json`:

```json
"AppSettings": {
  "EnablePublicRegistration": false,  // ← Cambiar a false
  "SiteName": "Farmacia Solidaria Cristiana",
  "SiteUrl": "https://farmaciasolidaria.somee.com"
}
```

## 📚 Documentación Adicional

- `REGISTRO_CONFIG.md` - Configuración detallada de registro
- `CONFIGURAR_GMAIL_SMTP.md` - Guía paso a paso de Gmail
- `DEPLOY_GUIDE.md` - Guía de despliegue (si existe)

---

**¿Listo para probar?** 🚀

1. Configura tu email en `appsettings.Development.json`
2. Ejecuta `dotnet run`
3. Ve a http://localhost:5003
4. ¡Prueba el sistema!

**¿Problemas?** Revisa los logs en la consola o contáctame.
