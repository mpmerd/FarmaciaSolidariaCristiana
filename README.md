# Farmacia Solidaria Cristiana - Sistema de Gestión

## 🏥 Proyecto ASP.NET Core 10 MVC

Sistema web para la gestión de medicamentos, entregas y donaciones de la Farmacia Solidaria Cristiana de la **Iglesia Metodista de Cárdenas y Adriano Solidario**.

## ✅ Características Implementadas

### Funcionalidades
- ✅ Autenticación con ASP.NET Core Identity (4 roles: Admin, Farmaceutico, Viewer, ViewerPublic)
- ✅ **Sistema de Turnos** - Gestión completa de citas para retirar medicamentos
  - Solicitud de turnos con selección múltiple de medicamentos
  - Aprobación/rechazo por farmacéuticos con notificaciones email
  - Verificación por documento de identidad (cifrado SHA-256)
  - Anti-abuso: límite de 2 turnos por mes por usuario y 30 turnos por día
  - Horario de atención: Martes y Viernes de 1:00 PM a 4:00 PM
  - Dashboard interactivo con DataTables y filtros avanzados
  - Números de turno únicos secuenciales por día
- ✅ CRUD Medicamentos con búsqueda CIMA API (código nacional español)
- ✅ Gestión de Pacientes con documentos y fotos
- ✅ Registro de Entregas y Donaciones con gestión automática de stock
- ✅ Sistema de Patrocinadores con logos institucionales
- ✅ Generación de reportes PDF con logos institucionales (iText7)
- ✅ Datos de prueba precargados (12 medicamentos, 8 donaciones, 14 entregas)
- ✅ Interfaz en español con Bootstrap 5 y logos institucionales
- ✅ HTTP solo (sin HTTPS) solo para pruebas en red local segura, en producción se recomienda HTTPS
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

- **[SECURITY.md](./SECURITY.md)** - ⚠️ **Guía de seguridad y credenciales** (LEER PRIMERO)
- **[TURNOS_SYSTEM.md](./TURNOS_SYSTEM.md)** - Sistema de Turnos (documentación completa)
- **[API_DOCUMENTATION.md](./API_DOCUMENTATION.md)** - Documentación de la API REST
- **[DEPLOYMENT_UBUNTU.md](./DEPLOYMENT_UBUNTU.md)** - Despliegue en Ubuntu Server
- **[DEPLOYMENT_SOMEE.md](./DEPLOYMENT_SOMEE.md)** - Despliegue en somee.com
- **[ONESIGNAL_INTEGRATION.md](./ONESIGNAL_INTEGRATION.md)** - Integración de notificaciones push
- **[QUICK_COMMANDS.md](./QUICK_COMMANDS.md)** - Comandos rápidos de referencia
- **[CHANGELOG.md](./CHANGELOG.md)** - Historial de cambios
- **.env.example** - Plantilla de variables de entorno
- **setup-ubuntu.sh** - Script de instalación en Ubuntu
- **update-app.sh** - Script de actualización del servidor

## 🔧 Tecnologías

### Backend (Web + API)
- **Framework:** ASP.NET Core 10 MVC + API RESTful (net10.0)
- **ORM:** Entity Framework Core 10
- **Base de Datos:** SQL Server (compatible con Linux)
- **Autenticación Web:** ASP.NET Core Identity + cookies
- **Autenticación API:** JWT Bearer
- **PDF:** iText7 + BouncyCastle adapter
- **Imágenes:** SixLabors.ImageSharp (compresión automática)
- **UI Web:** Bootstrap 5 + FontAwesome + DataTables
- **API Externa:** CIMA (Agencia Española de Medicamentos)
- **Notificaciones Push:** OneSignal (opcional) + sistema de polling propio

### App Móvil
- **Framework:** .NET MAUI net10.0-android (workload .NET 10)
- **Patrón:** MVVM con CommunityToolkit.Mvvm
- **Navegación:** Shell
- **Almacenamiento seguro:** SecureStorage para token JWT

## 🌐 Acceso

Una vez desplegado:
- **Red local (Ubuntu):** `http://TU_SERVIDOR_IP`
- **Producción:** `https://tudominio.somee.com`
- **App Android:** disponible en `https://tudominio.somee.com/android/`

## 📊 Estructura del Proyecto

```
FarmaciaSolidariaCristiana.sln
├── FarmaciaSolidariaCristiana/          # Backend ASP.NET Core 10
│   ├── Controllers/                     # Controladores MVC (14)
│   ├── Api/
│   │   ├── Controllers/                 # Controladores API REST (16)
│   │   └── Models/                      # DTOs de la API
│   ├── Models/                          # Entidades EF Core
│   │   └── ViewModels/                  # ViewModels para vistas MVC
│   ├── Views/                           # Vistas Razor
│   ├── Services/                        # Servicios de negocio
│   ├── Middleware/                      # Middleware (ej. verificación de versión)
│   ├── Filters/                         # Filtros de acción
│   ├── Data/                            # ApplicationDbContext + migraciones
│   ├── wwwroot/                         # Estáticos (CSS, JS, imágenes, APK)
│   ├── appsettings.json                 # Configuración (plantilla)
│   └── Program.cs                       # Punto de entrada
└── FarmaciaSolidariaCristiana.Maui/     # App Android .NET MAUI
    ├── ViewModels/                      # 21 ViewModels (MVVM)
    ├── Views/                           # Páginas XAML
    ├── Services/                        # Servicios (API, auth, polling, etc.)
    ├── Models/                          # Modelos cliente
    ├── Helpers/                         # Constantes y utilidades
    └── Converters/                      # Convertidores XAML
```

## 🤝 Roles y Permisos

| Rol | Permisos |
|-----|----------|
| **Admin** | Acceso completo (CRUD + Reportes + Gestión usuarios + Gestión turnos) |
| **Farmaceutico** | CRUD Medicamentos, Entregas, Donaciones, Reportes, Gestión turnos |
| **Viewer** | Solo lectura (ver medicamentos e inventario) |
| **ViewerPublic** | Solicitar turnos, ver estado de sus turnos, ver medicamentos disponibles |

## 🔒 Seguridad

- Autenticación obligatoria para todas las rutas (excepto login y endpoints públicos de la API)
- Autorización basada en roles en controladores MVC y API
- Tokens JWT con expiración de 8 horas
- Documentos de identidad almacenados como hash SHA-256 (nunca en texto claro)
- Verificación de email al registrarse (código de uso único con expiración)
- Lockout automático tras 5 intentos fallidos de login (5 minutos)
- Validación de entrada en todos los formularios y DTOs de la API
- Verificación de versión mínima de la app móvil en cada petición a la API
- HTTPS habilitado en producción
- Imágenes y documentos servidos desde rutas no predecibles

> ⚠️ **IMPORTANTE:** Lee [SECURITY.md](./SECURITY.md) antes de configurar el proyecto. Nunca incluyas credenciales reales en el código o en el repositorio.

## 🌐 HTTPS en Producción

En producción (ej. somee.com) se habilita HTTPS para cifrar todos los datos en tránsito.

- Sube el proyecto publicado vía FTP o deploy desde terminal.
- Activa SSL desde el panel de control del hosting.
- Accede vía: `https://tudominio.somee.com`

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

## 📄 Licencia

**Copyright (c) 2024-2026 Rev. Maikel Eduardo Peláez Martínez — Todos los derechos reservados.**

El uso, copia, modificación o distribución de este software requiere una licencia comercial pagada otorgada por el autor.

Para obtener una licencia comercial, contactar al autor: mpmerd@gmail.com

> **Nota sobre versiones anteriores:** Las versiones publicadas antes del 30 de abril de 2026 (commit `d811883`) fueron distribuidas bajo MIT License. A partir de esa fecha, todas las versiones nuevas se distribuyen exclusivamente bajo esta licencia propietaria comercial. Quienes obtuvieron el código bajo MIT conservan sus derechos sobre esa versión, pero no sobre versiones posteriores.
