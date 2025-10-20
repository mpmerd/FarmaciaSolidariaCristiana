# Comandos CLI Utilizados - Farmacia Solidaria Cristiana

Este documento contiene todos los comandos dotnet CLI utilizados para crear el proyecto.

## 1. Crear Proyecto MVC
```bash
dotnet new mvc -n FarmaciaSolidariaCristiana
cd FarmaciaSolidariaCristiana
```

## 2. Agregar Paquetes NuGet
```bash
# Entity Framework Core con SQL Server
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

# ASP.NET Core Identity con EF Core
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore

# EF Core Tools para migraciones
dotnet add package Microsoft.EntityFrameworkCore.Tools

# iText7 para generación de PDFs
dotnet add package itext7
```

## 3. Crear Migración Inicial
```bash
dotnet ef migrations add InitialCreate
```

## 4. Aplicar Migración a la Base de Datos
```bash
dotnet ef database update
```

## 5. Ejecutar la Aplicación
```bash
# Ejecución básica
dotnet run

# Ejecutar en red local (accesible desde otros dispositivos)
dotnet run --urls "http://0.0.0.0:5000"

# Ejecutar en puerto específico
dotnet run --urls "http://localhost:5001"
```

## Comandos Adicionales Útiles

### Gestión de Migraciones
```bash
# Listar todas las migraciones
dotnet ef migrations list

# Crear una nueva migración
dotnet ef migrations add NombreDeLaMigracion

# Revertir a una migración específica
dotnet ef database update NombreMigracionAnterior

# Eliminar la última migración (si no se ha aplicado)
dotnet ef migrations remove

# Generar script SQL de las migraciones
dotnet ef migrations script
```

### Gestión de Base de Datos
```bash
# Eliminar la base de datos
dotnet ef database drop

# Eliminar y recrear la base de datos
dotnet ef database drop -f
dotnet ef database update
```

### Compilación y Publicación
```bash
# Compilar el proyecto
dotnet build

# Compilar en modo Release
dotnet build -c Release

# Limpiar artefactos de compilación
dotnet clean

# Restaurar paquetes NuGet
dotnet restore

# Publicar para producción
dotnet publish -c Release -o ./publish

# Publicar auto-contenido (incluye runtime)
dotnet publish -c Release -r linux-x64 --self-contained -o ./publish
```

### Desarrollo
```bash
# Watch mode (recompila automáticamente al detectar cambios)
dotnet watch run

# Ejecutar con hot reload
dotnet watch

# Verificar formato de código
dotnet format

# Ejecutar tests (si existen)
dotnet test
```

### Información del Proyecto
```bash
# Ver información del SDK instalado
dotnet --info

# Ver versión de .NET
dotnet --version

# Listar todos los paquetes instalados
dotnet list package

# Ver paquetes desactualizados
dotnet list package --outdated

# Actualizar todos los paquetes
dotnet list package --outdated | grep ">" | awk '{print $2}' | xargs -I {} dotnet add package {}
```

## Estructura de Archivos Creados

### Models
- `Models/Medicine.cs`
- `Models/Delivery.cs`
- `Models/Donation.cs`
- `Models/ViewModels/LoginViewModel.cs`
- `Models/ViewModels/CreateUserViewModel.cs`

### Data
- `Data/ApplicationDbContext.cs`
- `Data/DbInitializer.cs`

### Controllers
- `Controllers/AccountController.cs`
- `Controllers/MedicinesController.cs`
- `Controllers/DeliveriesController.cs`
- `Controllers/DonationsController.cs`
- `Controllers/ReportsController.cs`

### Views
- `Views/Account/Login.cshtml`
- `Views/Account/CreateUser.cshtml`
- `Views/Account/ManageUsers.cshtml`
- `Views/Account/AccessDenied.cshtml`
- `Views/Medicines/Index.cshtml`
- `Views/Medicines/Create.cshtml`
- `Views/Medicines/Edit.cshtml`
- `Views/Medicines/Details.cshtml`
- `Views/Medicines/Delete.cshtml`
- `Views/Deliveries/Index.cshtml`
- `Views/Deliveries/Create.cshtml`
- `Views/Donations/Index.cshtml`
- `Views/Donations/Create.cshtml`
- `Views/Reports/Index.cshtml`
- `Views/Home/Index.cshtml`
- `Views/Shared/_Layout.cshtml`

### Configuración
- `Program.cs` (modificado)
- `appsettings.json` (modificado)

## Pasos de Implementación Completos

1. ✅ Crear proyecto MVC
2. ✅ Agregar paquetes NuGet
3. ✅ Crear modelos de datos
4. ✅ Configurar DbContext con Identity
5. ✅ Configurar Program.cs (Identity, EF Core, HttpClient, sin HTTPS)
6. ✅ Crear DbInitializer para seed
7. ✅ Actualizar appsettings.json con connection string
8. ✅ Crear controllers con autorización
9. ✅ Crear vistas con Bootstrap en español
10. ✅ Actualizar layout con navegación
11. ⏳ **Ejecutar migración inicial**
12. ⏳ **Aplicar migración a la base de datos**
13. ⏳ **Ejecutar y probar la aplicación**

## Próximos Pasos

Para completar la implementación:

1. **Configura tu connection string** en `appsettings.json`
2. **Ejecuta las migraciones**:
   ```bash
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
3. **Ejecuta la aplicación**:
   ```bash
   dotnet run
   ```
4. **Accede** a http://localhost:5000
5. **Inicia sesión** con admin/Admin123!
6. **Crea usuarios** para Farmaceutico y Viewer
7. **Prueba todas las funcionalidades**

## Notas Importantes

- La aplicación usa **HTTP** (no HTTPS) para red local
- El puerto por defecto es **5000**
- Usuario admin inicial: **admin** / **Admin123!**
- Compatible con **SQL Server en Linux**
- API CIMA requiere **conexión a Internet** para búsqueda de medicamentos

## Solución de Problemas Comunes

### Error: "No se puede conectar a SQL Server"
```bash
# Verifica que SQL Server está ejecutándose
# En Windows:
Get-Service MSSQLSERVER

# Verifica el connection string en appsettings.json
```

### Error: "dotnet ef no reconocido"
```bash
# Instala las herramientas de EF Core globalmente
dotnet tool install --global dotnet-ef

# O actualiza si ya está instalado
dotnet tool update --global dotnet-ef
```

### Puerto 5000 ya en uso
```bash
# Usa otro puerto
dotnet run --urls "http://localhost:5001"

# O encuentra qué proceso usa el puerto (macOS/Linux)
lsof -i :5000

# Windows
netstat -ano | findstr :5000
```

---

**Proyecto completado y listo para ejecutar** ✅
