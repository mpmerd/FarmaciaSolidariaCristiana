# 🔐 Configuración de Credenciales

Este documento explica cómo configurar las credenciales necesarias para ejecutar la aplicación.

## ⚠️ IMPORTANTE

Los archivos `appsettings.json` y `appsettings.Development.json` contienen información sensible y **NO están incluidos en el repositorio** por seguridad.

## 📋 Configuración Inicial

### 1. Copiar Archivos Template

Copia los archivos template y renómbralos:

```bash
cd FarmaciaSolidariaCristiana

# Para desarrollo
cp appsettings.Development.json.template appsettings.Development.json

# Para producción
cp appsettings.json.template appsettings.json
```

### 2. Configurar Base de Datos

Edita `appsettings.json` y actualiza la cadena de conexión:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=TU_SERVIDOR;Database=FarmaciaDb;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True"
  }
}
```

**Opciones de base de datos:**
- **SQL Server local:** `Server=localhost;Database=FarmaciaDb;Integrated Security=True;`
- **SQL Server remoto:** `Server=IP_SERVIDOR,1433;Database=FarmaciaDb;User Id=usuario;Password=password;`
- **SQL Server Express:** `Server=localhost\\SQLEXPRESS;Database=FarmaciaDb;Integrated Security=True;`

### 3. Configurar SMTP (Email)

Para que funcione el registro de usuarios y recuperación de contraseña, configura el SMTP.

#### Opción A: Gmail (Recomendado para desarrollo)

1. Habilita la verificación en 2 pasos en tu cuenta de Gmail
2. Genera una contraseña de aplicación: https://myaccount.google.com/apppasswords
3. Edita `appsettings.Development.json`:

```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "tu_email@gmail.com",
    "Password": "tu_app_password_de_16_caracteres",
    "FromEmail": "tu_email@gmail.com",
    "FromName": "Farmacia Solidaria Cristiana",
    "EnableSsl": "true"
  }
}
```

#### Opción B: Otro proveedor SMTP

Consulta la documentación de tu proveedor de email y actualiza los valores correspondientes.

### 4. Configurar Opciones de la Aplicación

Edita ambos archivos de configuración:

```json
{
  "AppSettings": {
    "EnablePublicRegistration": true,  // false para producción
    "SiteName": "Farmacia Solidaria Cristiana",
    "SiteUrl": "https://tu_dominio.com"  // o http://localhost:5000 para desarrollo
  }
}
```

## 🚀 Inicializar Base de Datos

Una vez configurada la cadena de conexión, ejecuta las migraciones:

```bash
cd FarmaciaSolidariaCristiana
dotnet ef database update
```

Esto creará la base de datos y las tablas necesarias, incluyendo:
- Usuario admin por defecto
- Roles (Admin, Farmaceutico, Viewer, ViewerPublic)
- Patrocinadores de ejemplo

## 🔑 Credenciales por Defecto

Después de la inicialización, puedes acceder con:

- **Usuario:** `admin`
- **Contraseña:** `[Generada automáticamente por el sistema]`

⚠️ **IMPORTANTE:** 
- La contraseña por defecto se genera durante la inicialización de la base de datos
- Cambia esta contraseña inmediatamente después del primer acceso
- Consulta los logs de inicialización o el código de `DataSeeder.cs` para ver la contraseña generada

## 🛡️ Seguridad

### Nunca compartas:
- ❌ Archivos `appsettings.json` o `appsettings.Development.json` con credenciales reales
- ❌ Cadenas de conexión a bases de datos
- ❌ Contraseñas de aplicación de email
- ❌ Tokens o API keys

### Puedes compartir:
- ✅ Archivos `.template`
- ✅ Este archivo de documentación
- ✅ El código fuente sin credenciales

## 📝 Variables de Entorno (Alternativa)

Para producción, es más seguro usar variables de entorno en lugar de archivos de configuración:

```bash
export ConnectionStrings__DefaultConnection="Server=...;Password=..."
export SmtpSettings__Password="tu_password_smtp"
```

## ❓ Solución de Problemas

### No puedo conectarme a la base de datos
- Verifica que SQL Server esté ejecutándose
- Comprueba el nombre del servidor y puerto
- Revisa que el usuario tenga permisos suficientes

### No se envían emails
- Verifica las credenciales SMTP
- Si usas Gmail, asegúrate de usar una contraseña de aplicación (no tu contraseña normal)
- Verifica que el puerto 587 no esté bloqueado por tu firewall

### Error de migraciones
```bash
# Elimina la base de datos y vuelve a crearla
dotnet ef database drop
dotnet ef database update
```

## 📧 Contacto

Si necesitas ayuda con la configuración:
- 📧 Email: mpmerd@gmail.com
- 👤 Autor: Rev. Maikel Eduardo Peláez Martínez
