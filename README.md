# Farmacia Solidaria Cristiana - Sistema de Gestión

## 🏥 Proyecto ASP.NET Core 8 MVC

Sistema web para la gestión de medicamentos, entregas y donaciones de la Farmacia Solidaria Cristiana de la **Iglesia Metodista de Cárdenas y Adriano Solidario**.

## ✅ Características Implementadas

### Funcionalidades
- ✅ Autenticación con ASP.NET Core Identity (3 roles: Admin, Farmaceutico, Viewer)
- ✅ CRUD Medicamentos con búsqueda CIMA API (código nacional español)
- ✅ Registro de Entregas y Donaciones con gestión automática de stock
- ✅ Generación de reportes PDF con logos institucionales (iText7)
- ✅ Datos de prueba precargados (12 medicamentos, 8 donaciones, 14 entregas)
- ✅ Interfaz en español con Bootstrap 5 y logos institucionales
- ✅ HTTP solo (sin HTTPS) se opto para pruebas en red local segura, en producción se recomienda HTTPS
- ✅ Compatible con SQL Server en Linux

### Reportes
- � Reporte de Entregas (con filtros por medicamento y fechas)
- 📊 Reporte de Donaciones (con filtros)
- 📊 Reporte Mensual (resumen de movimientos e inventario)
- 🖼️ Todos los reportes incluyen logos institucionales

## 🚀 Desarrollo Local

### 1. Configurar Base de Datos
Edita `FarmaciaSolidariaCristiana/appsettings.json`:
```json
"DefaultConnection": "Server=TU_SERVIDOR;Database=FarmaciaDb;User Id=TU_USUARIO;Password=TU_PASSWORD;TrustServerCertificate=True;"
```

> ⚠️ **IMPORTANTE:** Nunca compartas las credenciales reales. Usa variables de entorno o archivos de configuración no versionados para producción.

### 2. Aplicar Migraciones
```bash
cd FarmaciaSolidariaCristiana
dotnet ef database update
```

### 3. Ejecutar
```bash
dotnet run
```
Accede a: http://localhost:5000

### 4. Credenciales Iniciales
El sistema crea un usuario administrador por defecto. Las credenciales se configuran en el código.

> 🔒 **Seguridad:** Cambia la contraseña del administrador inmediatamente después del primer acceso.

### 5. Limpiar Datos de Prueba (Opcional)
```bash
dotnet ef database drop --force
dotnet ef database update
```

## �️ Despliegue en Ubuntu Server

### Opción 1: Script Automático (Recomendado)

```bash
# 1. Desde tu Mac: Publicar y transferir
cd FarmaciaSolidariaCristiana
dotnet publish -c Release -o ./publish
scp setup-ubuntu.sh usuario@TU_SERVIDOR_IP:~/
rsync -avz --progress ./publish/ usuario@TU_SERVIDOR_IP:~/farmacia-files/

# 2. En el servidor Ubuntu: Instalar
ssh usuario@TU_SERVIDOR_IP
bash setup-ubuntu.sh
```

### Opción 2: Instalación Manual

Ver **[DEPLOYMENT_UBUNTU.md](./DEPLOYMENT_UBUNTU.md)** para instrucciones detalladas paso a paso.

### Actualizar Aplicación

```bash
# 1. Desde tu Mac
dotnet publish -c Release -o ./publish
scp update-app.sh usuario@TU_SERVIDOR_IP:~/
rsync -avz --progress ./publish/ usuario@TU_SERVIDOR_IP:~/farmacia-new/

# 2. En Ubuntu
ssh usuario@TU_SERVIDOR_IP
bash update-app.sh
```

## 📚 Documentación

- **[SECURITY.md](./SECURITY.md)** - ⚠️ **Guía de seguridad y manejo de credenciales** (LEER PRIMERO)
- **[DEPLOYMENT_UBUNTU.md](./DEPLOYMENT_UBUNTU.md)** - Guía completa de despliegue en Ubuntu Server
- **[QUICK_COMMANDS.md](./QUICK_COMMANDS.md)** - Comandos rápidos de referencia
- **.env.example** - Plantilla de variables de entorno (copiar a .env)
- **setup-ubuntu.sh** - Script de instalación automática
- **update-app.sh** - Script de actualización

## 🔧 Tecnologías

- **Framework:** ASP.NET Core 8 MVC
- **ORM:** Entity Framework Core 8.0.11
- **Base de Datos:** SQL Server (compatible con Linux)
- **Autenticación:** ASP.NET Core Identity 8.0.11
- **PDF:** iText7 9.3.0 + BouncyCastle adapter
- **UI:** Bootstrap 5 + Bootstrap Icons
- **API Externa:** CIMA (Agencia Española de Medicamentos)

## 🌐 Acceso en Red Local

Una vez desplegado en Ubuntu Server:
- **Por IP:** http://TU_SERVIDOR_IP
- **Por nombre:** http://NOMBRE_SERVIDOR

## 📊 Estructura del Proyecto

```
FarmaciaSolidariaCristiana/
├── Controllers/         # Controladores MVC
├── Models/             # Modelos de datos
├── Views/              # Vistas Razor
├── Data/               # Contexto EF Core y migraciones
├── ViewModels/         # ViewModels para vistas
├── wwwroot/            # Archivos estáticos (CSS, JS, imágenes)
│   └── images/         # Logos institucionales
├── appsettings.json    # Configuración
└── Program.cs          # Punto de entrada
```

## 🤝 Roles y Permisos

| Rol | Permisos |
|-----|----------|
| **Admin** | Acceso completo (CRUD + Reportes + Gestión usuarios) |
| **Farmaceutico** | CRUD Medicamentos, Entregas, Donaciones, Reportes |
| **Viewer** | Solo lectura (ver medicamentos e inventario) |

## 🔒 Seguridad

- Autenticación obligatoria para todas las páginas (excepto login)
- Autorización basada en roles
- Validación de entrada en todos los formularios
- Gestión de sesiones con cookies
- Configuración de lockout para intentos fallidos


# Uso de https

En producción (ej. somee.com), se habilita HTTPS para cifrar datos en tránsito. (logins, entregas, donaciones).

## Ejemplo en somee.com
- Sube el proyecto publicado via FTP o Git deploy.
- En el panel de somee.com, activa SSL (gratuito con Let's Encrypt o similar).
- Accede via: https://tudominio.somee.com

> ⚠️ **IMPORTANTE:** Lee [SECURITY.md](./SECURITY.md) antes de configurar el proyecto. Nunca incluyas credenciales reales en el código o repositorio.

## 📝 Comandos Útiles

```bash
# Build
dotnet build

# Run
dotnet run

# Migraciones
dotnet ef migrations add MigracionNombre
dotnet ef database update
dotnet ef database drop --force

# Publicar
dotnet publish -c Release -o ./publish

# Ver logs (en Ubuntu)
sudo journalctl -u farmacia.service -f
```

## 🐛 Solución de Problemas

Ver **[DEPLOYMENT_UBUNTU.md](./DEPLOYMENT_UBUNTU.md)** sección "Solución de Problemas"

---

## 🙏 Créditos

Desarrollado para la **Iglesia Metodista de Cárdenas y Adriano Solidario**  
Sistema para servir a la comunidad con amor y dedicación.

Desarrollado por Rev. Maikel Eduardo Peláez Martínez

Contacto: mpmerd@gmail.com

## � Licencia

MIT License.
