# Farmacia Solidaria Cristiana - App MAUI

Aplicación móvil para Android e iOS que consume la API REST de Farmacia Solidaria Cristiana.

## 📱 Características

- **Shell con Flyout Navigation** - Navegación lateral moderna
- **MVVM Pattern** - Usando CommunityToolkit.Mvvm
- **Roles de Usuario** - Menús dinámicos según rol (Admin, Farmaceutico, Viewer, ViewerPublic)
- **Push Notifications** - Integración con OneSignal
- **Autenticación JWT** - Almacenamiento seguro con SecureStorage

## 🛠️ Requisitos

- .NET 9 SDK
- Visual Studio 2022+ con workload MAUI o VS Code con extensión .NET MAUI
- Para Android: Android SDK, JDK 17+
- Para iOS: macOS con Xcode 15+

### Instalar Workloads

```bash
dotnet workload install maui
```

## ⚙️ Configuración

### 1. URL del API

Editar `Helpers/Constants.cs`:

```csharp
// Para desarrollo local
public const string ApiBaseUrl = "http://TU_IP_LOCAL:5003";

// Para producción
public const string ApiBaseUrl = "https://farmaciasolidaria.somee.com";
```

### 2. OneSignal

Editar `Helpers/Constants.cs` con tu App ID de OneSignal:

```csharp
public const string OneSignalAppId = "TU_ONESIGNAL_APP_ID";
```

### 3. Iconos Necesarios

Colocar los siguientes iconos en `Resources/Images/`:

| Archivo | Descripción |
|---------|-------------|
| `logo.png` | Logo de la app (100x100px) |
| `home.png` | Icono de inicio |
| `calendar.png` | Icono de calendario/turnos |
| `pills.png` | Icono de medicamentos |
| `supplies.png` | Icono de insumos |
| `heart.png` | Icono de donaciones |
| `package.png` | Icono de entregas |
| `users.png` | Icono de pacientes |
| `star.png` | Icono de patrocinadores |
| `chart.png` | Icono de reportes |
| `settings.png` | Icono de configuración |
| `user.png` | Icono de perfil |
| `logout.png` | Icono de cerrar sesión |
| `eye.png` | Icono de ojo abierto |
| `eye_off.png` | Icono de ojo cerrado |

Formato recomendado: PNG 24x24px o SVG.

## 🚀 Compilar y Ejecutar

### Android

```bash
# Debug
dotnet build -f net9.0-android

# Release APK
dotnet publish -f net9.0-android -c Release
```

### iOS (solo macOS)

```bash
# Debug
dotnet build -f net9.0-ios

# Release
dotnet publish -f net9.0-ios -c Release
```

## 📂 Estructura del Proyecto

```
FarmaciaSolidariaCristiana.Maui/
├── Converters/         # Value Converters para XAML
├── Helpers/            # Constants, utilidades
├── Models/             # DTOs y modelos de datos
├── Services/           # Servicios (API, Auth, Notifications)
├── ViewModels/         # ViewModels MVVM
├── Views/              # Páginas XAML
├── Resources/
│   ├── AppIcon/        # Icono de la app
│   ├── Fonts/          # Fuentes personalizadas
│   ├── Images/         # Iconos y recursos visuales
│   ├── Splash/         # Pantalla de splash
│   └── Styles/         # Colores y estilos
├── App.xaml            # Recursos globales
├── AppShell.xaml       # Shell y navegación
└── MauiProgram.cs      # Configuración DI
```

## 🔐 Roles y Permisos

| Rol | Dashboard | Turnos | Medicamentos | Insumos | Donaciones | Entregas | Pacientes | Reportes | Avanzado |
|-----|-----------|--------|--------------|---------|------------|----------|-----------|----------|----------|
| **Admin** | ✅ | Gestión | CRUD | CRUD | CRUD | CRUD | CRUD | ✅ | ✅ |
| **Farmaceutico** | ✅ | Gestión | CRUD | CRUD | CRUD | CRUD | CRUD | ✅ | ❌ |
| **Viewer** | ✅ | Ver | Ver | Ver | Ver | Ver | Ver | ✅ | ❌ |
| **ViewerPublic** | ✅ | Mis Turnos | Ver | Ver | Ver | Ver | ❌ | ❌ | ❌ |

## 🔔 Notificaciones Push

La app se integra con OneSignal para enviar notificaciones push. Al iniciar sesión, el dispositivo se registra automáticamente con:

- **User ID**: ID del usuario autenticado
- **Tags**: Rol del usuario para segmentación

## 📝 Notas de Desarrollo

- El token JWT se almacena en SecureStorage
- La sesión persiste entre reinicios de la app
- Los menús del Flyout se ajustan dinámicamente al rol del usuario
- Pull-to-refresh disponible en todas las listas

## 🐛 Solución de Problemas

### Error de workload no instalado
```bash
dotnet workload restore
```

### Error de certificado SSL en desarrollo
Agregar en Android `network_security_config.xml`:
```xml
<domain-config cleartextTrafficPermitted="true">
    <domain includeSubdomains="true">TU_IP_LOCAL</domain>
</domain-config>
```

### La API no responde
Verificar que la API esté corriendo y accesible desde el dispositivo/emulador.
