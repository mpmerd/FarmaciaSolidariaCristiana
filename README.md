# Farmacia Solidaria Cristiana - Sistema de Gestión

## 🏥 Proyecto ASP.NET Core 8 MVC Completado

Sistema web para la gestión de medicamentos, entregas y donaciones de la Farmacia Solidaria Cristiana de la Iglesia Metodista de Cárdenas.

## ✅ Características Implementadas

- ✅ Autenticación con ASP.NET Core Identity
- ✅ Tres roles: Admin, Farmaceutico, Viewer
- ✅ CRUD Medicamentos con búsqueda CIMA API (CN)
- ✅ Registro de Entregas y Donaciones con gestión automática de stock
- ✅ Generación de reportes PDF con iText7
- ✅ Interfaz en español con Bootstrap 5
- ✅ HTTP solo (sin HTTPS) para red local
- ✅ Compatible con SQL Server en Linux

## 🚀 Inicio Rápido

### 1. Configurar Base de Datos
Edita `FarmaciaSolidariaCristiana/appsettings.json`:
```json
"DefaultConnection": "Server=TU_SERVIDOR;Database=FarmaciaDb;User Id=usuario;Password=contraseña;TrustServerCertificate=True;"
```

### 2. Crear Base de Datos
```bash
cd FarmaciaSolidariaCristiana
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Ejecutar
```bash
dotnet run
```
Accede a: http://localhost:5000

### 4. Login
- Usuario: `admin`
- Contraseña: `Admin123!`

## 📖 Documentación Completa
Ver **IMPLEMENTATION_GUIDE.md** para guía completa de implementación y vistas adicionales.

---
**Iglesia Metodista de Cárdenas** - Sistema para servir a la comunidad 🙏
