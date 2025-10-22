# Guía Rápida: Configurar Gmail para SMTP

## 📧 Pasos para Configurar Gmail

### 1. Preparar cuenta de Gmail

1. Ir a la cuenta de Gmail (puede ser una cuenta personal o crear una nueva específica para el proyecto)

### 2. Activar la Verificación en 2 Pasos

1. Ve a: https://myaccount.google.com/security
2. En la sección "Cómo iniciar sesión en Google", haz clic en "Verificación en dos pasos"
3. Sigue los pasos para activarla (necesitarás tu teléfono)

### 3. Generar una Contraseña de Aplicación

1. Una vez activada la verificación en 2 pasos, regresar a: https://myaccount.google.com/security
2. Busca "Contraseñas de aplicaciones" (aparece después de activar 2 pasos)
3. Haz clic en "Contraseñas de aplicaciones"
4. Selecciona:
   - **Aplicación:** Correo
   - **Dispositivo:** Otro (personalizado)
   - **Nombre:** FarmaciaSolidaria
5. Haz clic en "Generar"
6. **IMPORTANTE:** Copia la contraseña de 16 caracteres que aparece (sin espacios)

### 4. Configurar appsettings.json

Edita el archivo `appsettings.json` y reemplaza:

```json
"SmtpSettings": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "Username": "TU-EMAIL@gmail.com",          ← Tu correo de Gmail
  "Password": "xxxx xxxx xxxx xxxx",          ← La contraseña de aplicación (16 caracteres)
  "FromEmail": "TU-EMAIL@gmail.com",          ← Tu correo de Gmail
  "FromName": "Farmacia Solidaria Cristiana",
  "EnableSsl": "true"
}
```

### 5. Ejemplo Completo

```json
"SmtpSettings": {
  "Host": "smtp.gmail.com",
  "Port": "587",
  "Username": "farmacia.solidaria@gmail.com",
  "Password": "abcd efgh ijkl mnop",
  "FromEmail": "farmacia.solidaria@gmail.com",
  "FromName": "Farmacia Solidaria Cristiana",
  "EnableSsl": "true"
}
```

## 🧪 Probar la Configuración

1. Ejecuta la aplicación:
   ```bash
   dotnet run
   ```

2. Ve a: http://localhost:5003/Account/Register

3. Regístrate con tu email personal (para recibir el correo de bienvenida)

4. Prueba recuperar contraseña en: http://localhost:5003/Account/ForgotPassword

## ⚠️ Solución de Problemas

### Error: "The SMTP server requires a secure connection"
- Verifica que `EnableSsl` esté en `"true"`
- Verifica que el puerto sea `587`

### Error: "Username and Password not accepted"
- Asegúrate de usar la **contraseña de aplicación**, NO tu contraseña normal de Gmail
- Verifica que la verificación en 2 pasos esté activada
- Copia la contraseña sin espacios

### Error: "Unable to connect to the remote server"
- Verifica tu conexión a internet
- Verifica que Gmail no esté bloqueado por firewall

## 🔐 Seguridad

- ⚠️ **NUNCA** subas el archivo `appsettings.json` con credenciales reales a GitHub
- Usa `appsettings.Development.json` para desarrollo local
- Usa variables de entorno en producción
- Considera crear un email específico para el proyecto (ej: farmaciasolidaria@gmail.com)

## 📝 Nota

Si no quieres usar Gmail, puedes usar cualquier otro proveedor SMTP (Outlook, SendGrid, Mailgun, etc.) cambiando el Host, Port y credenciales correspondientes.

---

**Última actualización:** 21 de octubre de 2025
