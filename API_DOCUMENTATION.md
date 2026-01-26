# API RESTful - Farmacia Solidaria Cristiana

Esta documentación describe la API RESTful implementada en el proyecto, que permite acceder a las funcionalidades de la aplicación desde clientes externos (apps móviles .NET MAUI, integraciones, etc.).

## Índice

1. [Estructura de Carpetas](#estructura-de-carpetas)
2. [Configuración](#configuración)
3. [Autenticación JWT](#autenticación-jwt)
4. [Endpoints Disponibles](#endpoints-disponibles)
5. [Modelos de Respuesta](#modelos-de-respuesta)
6. [Ejemplos de Uso](#ejemplos-de-uso)

---

## Estructura de Carpetas

La API se encuentra organizada dentro de la carpeta `/Api` del proyecto:

```
FarmaciaSolidariaCristiana/
├── Api/
│   ├── Controllers/
│   │   ├── ApiBaseController.cs        # Controlador base abstracto
│   │   ├── AuthController.cs           # Autenticación JWT y gestión de usuarios
│   │   ├── MedicinesApiController.cs   # CRUD de medicamentos + integración CIMA
│   │   ├── SuppliesApiController.cs    # CRUD de insumos médicos
│   │   ├── TurnosApiController.cs      # Gestión de turnos/citas
│   │   ├── DonationsApiController.cs   # CRUD de donaciones
│   │   ├── DeliveriesApiController.cs  # CRUD de entregas
│   │   ├── PatientsApiController.cs    # CRUD de pacientes
│   │   ├── SponsorsApiController.cs    # CRUD de patrocinadores
│   │   ├── ReportsApiController.cs     # Generación de reportes PDF
│   │   ├── NotificationsApiController.cs  # Notificaciones push OneSignal
│   │   └── DiagnosticsController.cs      # Diagnóstico y health checks
│   └── Models/
│       ├── AuthDtos.cs                 # DTOs de autenticación
│       ├── MedicineDtos.cs             # DTOs de medicamentos + CIMA
│       ├── SupplyDtos.cs               # DTOs de insumos
│       ├── TurnoDtos.cs                # DTOs de turnos
│       ├── DonationDtos.cs             # DTOs de donaciones
│       ├── DeliveryDtos.cs             # DTOs de entregas
│       ├── PatientDtos.cs              # DTOs de pacientes
│       ├── SponsorDtos.cs              # DTOs de patrocinadores
│       ├── ReportDtos.cs               # DTOs de reportes y dashboard
│       └── NotificationDtos.cs         # DTOs de notificaciones push
├── Controllers/                         # Controladores MVC (web)
├── Models/                              # Entidades EF Core
├── Services/                            # Servicios de negocio
│   ├── IOneSignalNotificationService.cs # Interfaz servicio push
│   └── OneSignalNotificationService.cs  # Implementación OneSignal
└── ...
```

### Descripción de Archivos

| Archivo | Descripción |
|---------|-------------|
| `ApiBaseController.cs` | Clase base abstracta con métodos helper (`ApiOk`, `ApiError`, `ApiValidationError`) y configuración JWT. |
| `AuthController.cs` | Login JWT, validación de tokens, info de usuario y cambio de contraseña. |
| `MedicinesApiController.cs` | CRUD de medicamentos con paginación, búsqueda e integración con API CIMA. |
| `SuppliesApiController.cs` | CRUD de insumos médicos con paginación y búsqueda. |
| `TurnosApiController.cs` | Gestión de turnos: listar, aprobar, rechazar, completar y estadísticas. |
| `DonationsApiController.cs` | CRUD de donaciones (aumentan stock automáticamente). |
| `DeliveriesApiController.cs` | CRUD de entregas (reducen stock automáticamente). |
| `PatientsApiController.cs` | CRUD de pacientes con historial y documentos. |
| `SponsorsApiController.cs` | CRUD de patrocinadores con logos. |
| `ReportsApiController.cs` | Generación de PDFs: entregas, donaciones, mensual, inventario y dashboard. |
| `NotificationsApiController.cs` | Gestión de notificaciones push con OneSignal (registro de dispositivos, envío de notificaciones). |

---

## Configuración

### appsettings.json

La configuración JWT se encuentra en `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "TU_CLAVE_SECRETA_MINIMO_32_CARACTERES",
    "Issuer": "FarmaciaSolidariaCristiana",
    "Audience": "FarmaciaSolidariaCristianaApi",
    "ExpirationMinutes": 480
  }
}
```

| Parámetro | Descripción |
|-----------|-------------|
| `SecretKey` | Clave secreta para firmar tokens JWT (mínimo 32 caracteres). **¡Mantener segura!** |
| `Issuer` | Identificador del emisor del token. |
| `Audience` | Audiencia válida para el token. |
| `ExpirationMinutes` | Tiempo de vida del token en minutos (480 = 8 horas). |

### Program.cs

La API utiliza autenticación dual:
- **Cookies** para la aplicación web MVC
- **JWT Bearer** para la API RESTful

```csharp
builder.Services.AddAuthentication(...)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => { ... });
```

---

## Autenticación JWT

### Flujo de Autenticación

1. El cliente envía credenciales a `POST /api/auth/login`
2. Si son válidas, recibe un token JWT
3. El cliente incluye el token en todas las peticiones subsiguientes
4. El token expira después del tiempo configurado

### Uso del Token

Incluir en el header `Authorization` de cada petición:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Roles Disponibles

| Rol | Descripción |
|-----|-------------|
| `Admin` | Acceso total a todas las funcionalidades |
| `Farmaceutico` | Gestión operativa (medicamentos, turnos, entregas) |
| `Viewer` | Solo lectura de datos |
| `ViewerPublic` | Solicitud de turnos y consulta de disponibilidad |

---

## Endpoints Disponibles

### Autenticación (`/api/auth`)

| Método | Ruta | Autenticación | Descripción |
|--------|------|---------------|-------------|
| `POST` | `/api/auth/login` | No requerida | Iniciar sesión y obtener token JWT |
| `GET` | `/api/auth/me` | Requerida | Obtener información del usuario actual |
| `GET` | `/api/auth/validate` | Requerida | Validar si el token actual es válido |
| `POST` | `/api/auth/change-password` | Requerida | Cambiar contraseña del usuario actual |

### Medicamentos (`/api/medicines`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| `GET` | `/api/medicines` | Todos | Listar medicamentos (paginado) |
| `GET` | `/api/medicines/{id}` | Todos | Obtener un medicamento por ID |
| `GET` | `/api/medicines/available` | Todos | Listar medicamentos con stock > 0 |
| `GET` | `/api/medicines/cima/{cn}` | Admin, Farmaceutico | Buscar medicamento en API CIMA por Código Nacional |
| `POST` | `/api/medicines` | Admin, Farmaceutico | Crear nuevo medicamento |
| `PUT` | `/api/medicines/{id}` | Admin, Farmaceutico | Actualizar medicamento |
| `DELETE` | `/api/medicines/{id}` | Admin | Eliminar medicamento |

**Parámetros de query para GET /api/medicines:**
- `page` (int): Número de página (default: 1)
- `pageSize` (int): Elementos por página (default: 20)
- `search` (string): Búsqueda por nombre o código nacional

### Insumos (`/api/supplies`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| `GET` | `/api/supplies` | Todos | Listar insumos (paginado) |
| `GET` | `/api/supplies/{id}` | Todos | Obtener un insumo por ID |
| `GET` | `/api/supplies/available` | Todos | Listar insumos con stock > 0 |
| `POST` | `/api/supplies` | Admin, Farmaceutico | Crear nuevo insumo |
| `PUT` | `/api/supplies/{id}` | Admin, Farmaceutico | Actualizar insumo |
| `DELETE` | `/api/supplies/{id}` | Admin | Eliminar insumo |

### Turnos (`/api/turnos`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| `GET` | `/api/turnos` | Admin, Farmaceutico | Listar todos los turnos (paginado) |
| `GET` | `/api/turnos/my` | Todos | Obtener turnos del usuario actual |
| `GET` | `/api/turnos/{id}` | Todos* | Obtener detalle de un turno |
| `GET` | `/api/turnos/can-request` | Todos | Verificar si puede solicitar turno |
| `GET` | `/api/turnos/next-slot` | Todos | Obtener próximo slot disponible |
| `POST` | `/api/turnos/{id}/approve` | Admin, Farmaceutico | Aprobar un turno |
| `POST` | `/api/turnos/{id}/reject` | Admin, Farmaceutico | Rechazar un turno |
| `POST` | `/api/turnos/{id}/complete` | Admin, Farmaceutico | Marcar turno como completado |
| `GET` | `/api/turnos/stats` | Admin, Farmaceutico | Obtener estadísticas de turnos |

*Usuarios normales solo pueden ver sus propios turnos.

**Parámetros de query para GET /api/turnos:**
- `page` (int): Número de página
- `pageSize` (int): Elementos por página
- `estado` (string): Filtrar por estado (Pendiente, Aprobado, Rechazado, Completado, Cancelado)
- `fechaDesde` (DateTime): Filtrar desde fecha
- `fechaHasta` (DateTime): Filtrar hasta fecha

### Donaciones (`/api/donations`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| `GET` | `/api/donations` | Admin, Farmaceutico, Viewer | Listar donaciones (paginado) |
| `GET` | `/api/donations/{id}` | Admin, Farmaceutico, Viewer | Obtener una donación por ID |
| `GET` | `/api/donations/recent` | Admin, Farmaceutico, Viewer | Listar donaciones recientes (últimos 30 días) |
| `POST` | `/api/donations` | Admin, Farmaceutico | Registrar nueva donación |
| `PUT` | `/api/donations/{id}` | Admin, Farmaceutico | Actualizar donación |
| `DELETE` | `/api/donations/{id}` | Admin | Eliminar donación |

**Parámetros de query para GET /api/donations:**
- `page`, `pageSize`: Paginación
- `medicineId` (int): Filtrar por medicamento
- `supplyId` (int): Filtrar por insumo
- `startDate`, `endDate` (DateTime): Rango de fechas

### Entregas (`/api/deliveries`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| `GET` | `/api/deliveries` | Admin, Farmaceutico, Viewer | Listar entregas (paginado) |
| `GET` | `/api/deliveries/{id}` | Admin, Farmaceutico, Viewer | Obtener una entrega por ID |
| `GET` | `/api/deliveries/recent` | Admin, Farmaceutico, Viewer | Listar entregas recientes (últimos 30 días) |
| `GET` | `/api/deliveries/by-patient/{identification}` | Admin, Farmaceutico | Entregas por identificación de paciente |
| `POST` | `/api/deliveries` | Admin, Farmaceutico | Registrar nueva entrega |
| `PUT` | `/api/deliveries/{id}` | Admin, Farmaceutico | Actualizar entrega |
| `DELETE` | `/api/deliveries/{id}` | Admin | Eliminar entrega |

**Parámetros de query para GET /api/deliveries:**
- `page`, `pageSize`: Paginación
- `medicineId`, `supplyId`: Filtrar por producto
- `patientId`: Filtrar por paciente
- `turnoId`: Filtrar por turno
- `startDate`, `endDate`: Rango de fechas

### Pacientes (`/api/patients`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| `GET` | `/api/patients` | Admin, Farmaceutico, Viewer | Listar pacientes (paginado) |
| `GET` | `/api/patients/{id}` | Admin, Farmaceutico, Viewer | Obtener un paciente por ID |
| `GET` | `/api/patients/by-identification/{identification}` | Admin, Farmaceutico | Buscar paciente por carnet/pasaporte |
| `GET` | `/api/patients/{id}/deliveries` | Admin, Farmaceutico | Historial de entregas del paciente |
| `POST` | `/api/patients` | Admin, Farmaceutico | Crear nuevo paciente |
| `PUT` | `/api/patients/{id}` | Admin, Farmaceutico | Actualizar paciente |
| `DELETE` | `/api/patients/{id}` | Admin | Eliminar paciente |

**Parámetros de query para GET /api/patients:**
- `page`, `pageSize`: Paginación
- `search` (string): Búsqueda por nombre o identificación
- `activeOnly` (bool): Solo pacientes activos (default: true)

### Patrocinadores (`/api/sponsors`)

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| `GET` | `/api/sponsors` | Todos | Listar patrocinadores |
| `GET` | `/api/sponsors/{id}` | Todos | Obtener un patrocinador por ID |
| `GET` | `/api/sponsors/active` | Todos | Listar solo patrocinadores activos |
| `POST` | `/api/sponsors` | Admin | Crear nuevo patrocinador |
| `PUT` | `/api/sponsors/{id}` | Admin | Actualizar patrocinador |
| `DELETE` | `/api/sponsors/{id}` | Admin | Eliminar patrocinador |

### Reportes (`/api/reports`)

Los reportes generan archivos PDF codificados en Base64.

| Método | Ruta | Roles | Descripción |
|--------|------|-------|-------------|
| `POST` | `/api/reports/deliveries` | Admin, Farmaceutico, Viewer | Generar PDF de entregas |
| `POST` | `/api/reports/donations` | Admin, Farmaceutico, Viewer | Generar PDF de donaciones |
| `POST` | `/api/reports/monthly` | Admin, Farmaceutico, Viewer | Generar reporte mensual |
| `GET` | `/api/reports/inventory` | Admin, Farmaceutico, Viewer | Generar PDF de inventario actual |
| `GET` | `/api/reports/dashboard` | Admin, Farmaceutico, Viewer | Obtener estadísticas del dashboard (JSON) |

**Body para POST /api/reports/deliveries:**
```json
{
  "medicineId": 1,
  "supplyId": null,
  "startDate": "2026-01-01",
  "endDate": "2026-01-31",
  "tipoFiltro": "Medicamentos"
}
```

**Respuesta de reportes PDF:**
```json
{
  "success": true,
  "message": "Reporte generado exitosamente",
  "data": {
    "fileName": "Entregas_20260124.pdf",
    "contentType": "application/pdf",
    "pdfBase64": "JVBERi0xLjQKJeLjz9MKN...",
    "generatedAt": "2026-01-24T10:45:00Z"
  }
}
```

**Respuesta de GET /api/reports/dashboard:**
```json
{
  "success": true,
  "data": {
    "totalMedicines": 150,
    "totalSupplies": 45,
    "totalMedicinesStock": 12500,
    "totalSuppliesStock": 3200,
    "totalPatients": 320,
    "deliveriesToday": 15,
    "deliveriesThisMonth": 245,
    "deliveriesThisYear": 2150,
    "donationsToday": 3,
    "donationsThisMonth": 28,
    "donationsThisYear": 312,
    "medicinesOutOfStock": 5,
    "suppliesOutOfStock": 2
  }
}
```

## Modelos de Respuesta

### Respuesta Estándar (ApiResponse<T>)

Todas las respuestas de la API siguen este formato:

```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": { ... },
  "timestamp": "2026-01-24T10:30:00Z"
}
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `success` | boolean | Indica si la operación fue exitosa |
| `message` | string | Mensaje descriptivo |
| `data` | T | Datos de respuesta (varía según endpoint) |
| `timestamp` | DateTime | Fecha/hora UTC de la respuesta |

### Respuesta Paginada (PagedResult<T>)

Para endpoints que retornan listas paginadas:

```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": {
    "items": [ ... ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 150,
    "totalPages": 8,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "timestamp": "2026-01-24T10:30:00Z"
}
```

### DTOs Principales

#### LoginResponseDto
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "base64string...",
  "expiration": "2026-01-24T18:30:00Z",
  "user": {
    "id": "guid-string",
    "email": "usuario@ejemplo.com",
    "userName": "usuario",
    "roles": ["Admin", "Farmaceutico"]
  }
}
```

#### MedicineDto
```json
{
  "id": 1,
  "name": "Ibuprofeno 400mg",
  "description": "Antiinflamatorio",
  "stockQuantity": 100,
  "unit": "comprimidos",
  "nationalCode": "CN123456"
}
```

#### TurnoDto
```json
{
  "id": 1,
  "userId": "guid-string",
  "userEmail": "paciente@ejemplo.com",
  "fechaPreferida": "2026-01-28T13:00:00",
  "fechaSolicitud": "2026-01-24T10:00:00",
  "estado": "Pendiente",
  "notasSolicitante": "Necesito urgente",
  "comentariosFarmaceutico": null,
  "fechaRevision": null,
  "medicamentos": [
    {
      "medicineId": 1,
      "medicineName": "Ibuprofeno 400mg",
      "cantidadSolicitada": 20,
      "cantidadAprobada": null,
      "disponibleAlSolicitar": true
    }
  ],
  "insumos": [],
  "documentosCount": 2
}
```

---

### Notificaciones Push (`/api/notifications`)

> **Nota**: Se utiliza OneSignal como proveedor de notificaciones push porque Firebase Cloud Messaging (FCM) no está disponible en Cuba.
> Para documentación completa, ver [ONESIGNAL_INTEGRATION.md](ONESIGNAL_INTEGRATION.md)

| Método | Endpoint | Descripción | Rol Requerido |
|--------|----------|-------------|---------------|
| `POST` | `/device` | Registrar dispositivo OneSignal | Usuario |
| `POST` | `/device/unregister` | Eliminar registro de dispositivo | Usuario |
| `GET` | `/devices` | Listar dispositivos del usuario | Usuario |
| `GET` | `/push-status` | Estado de notificaciones push | Usuario |
| `POST` | `/test` | Enviar notificación de prueba | Usuario |
| `POST` | `/send` | Enviar notificación a usuario | Admin/Farmacéutico |
| `POST` | `/send/broadcast` | Enviar a todos los usuarios | Admin |

#### Registrar Dispositivo

```json
POST /api/notifications/device
{
  "oneSignalPlayerId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "deviceToken": "token_opcional",
  "deviceType": "Android",
  "deviceName": "Samsung Galaxy S21"
}
```

#### Respuesta
```json
{
  "success": true,
  "message": "Dispositivo registrado exitosamente",
  "data": {
    "id": 1,
    "oneSignalPlayerId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "deviceType": "Android",
    "deviceName": "Samsung Galaxy S21",
    "isActive": true,
    "createdAt": "2026-01-25T10:30:00Z",
    "updatedAt": "2026-01-25T10:30:00Z"
  }
}
```

#### Tipos de Notificación

| Tipo | Descripción |
|------|-------------|
| `TurnoSolicitado` | Turno solicitado por el usuario |
| `TurnoAprobado` | Turno aprobado por farmacéutico |
| `TurnoRechazado` | Turno rechazado por farmacéutico |
| `TurnoPdfDisponible` | PDF del turno disponible |
| `TurnoRecordatorio` | Recordatorio de fecha/hora |
| `TurnoCancelado` | Turno cancelado |
| `TurnoReprogramado` | Turno reprogramado |
| `General` | Notificación general |

---

## Ejemplos de Uso

### Login

**Request:**
```bash
curl -X POST https://tu-dominio.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@farmacia.com",
    "password": "MiPassword123!"
  }'
```

**Response:**
```json
{
  "success": true,
  "message": "Login exitoso",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "abc123...",
    "expiration": "2026-01-24T18:30:00Z",
    "user": {
      "id": "12345-guid",
      "email": "admin@farmacia.com",
      "userName": "admin",
      "roles": ["Admin"]
    }
  },
  "timestamp": "2026-01-24T10:30:00Z"
}
```

### Listar Medicamentos

**Request:**
```bash
curl -X GET "https://tu-dominio.com/api/medicines?page=1&pageSize=10&search=ibuprofeno" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

**Response:**
```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": {
    "items": [
      {
        "id": 1,
        "name": "Ibuprofeno 400mg",
        "description": "Antiinflamatorio no esteroideo",
        "stockQuantity": 150,
        "unit": "comprimidos",
        "nationalCode": "CN654321"
      }
    ],
    "page": 1,
    "pageSize": 10,
    "totalItems": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "timestamp": "2026-01-24T10:35:00Z"
}
```

### Crear Medicamento

**Request:**
```bash
curl -X POST https://tu-dominio.com/api/medicines \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Paracetamol 500mg",
    "description": "Analgésico y antipirético",
    "stockQuantity": 200,
    "unit": "comprimidos",
    "nationalCode": "CN789012"
  }'
```

### Aprobar Turno

**Request:**
```bash
curl -X POST https://tu-dominio.com/api/turnos/5/approve \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  -H "Content-Type: application/json" \
  -d '{
    "comentarios": "Aprobado. Presentar documentación al retirar."
  }'
```

### Obtener Estadísticas de Turnos

**Request:**
```bash
curl -X GET https://tu-dominio.com/api/turnos/stats \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

**Response:**
```json
{
  "success": true,
  "message": "Operación exitosa",
  "data": {
    "totalPendientes": 15,
    "totalAprobados": 8,
    "totalCompletados": 120,
    "totalRechazados": 5,
    "turnosHoy": 12,
    "turnosEsteMes": 45
  },
  "timestamp": "2026-01-24T10:40:00Z"
}
```

---

## Códigos de Estado HTTP

| Código | Significado |
|--------|-------------|
| 200 | OK - Operación exitosa |
| 201 | Created - Recurso creado exitosamente |
| 400 | Bad Request - Datos inválidos o error de validación |
| 401 | Unauthorized - Token inválido o expirado |
| 403 | Forbidden - Sin permisos para esta operación |
| 404 | Not Found - Recurso no encontrado |
| 500 | Internal Server Error - Error del servidor |

---

## Notas Importantes

1. **Seguridad**: La `SecretKey` de JWT debe mantenerse segura y nunca exponerse en el código fuente o repositorios públicos.

2. **HTTPS**: En producción, siempre usar HTTPS para proteger los tokens JWT en tránsito.

3. **Token Expirado**: Cuando un token expira, la respuesta incluye el header `Token-Expired: true`. El cliente debe solicitar un nuevo token.

4. **Límites de Turnos**: El sistema mantiene los mismos límites que la web:
   - 2 turnos por usuario por mes
   - 30 turnos por día máximo

5. **Roles**: Los roles se incluyen en el token JWT y se validan en cada petición. No es posible elevar privilegios sin un nuevo login.

6. **Reportes PDF**: Los PDFs se retornan codificados en Base64. El cliente debe decodificar y guardar el archivo.

7. **Stock Automático**: Las donaciones incrementan stock y las entregas lo decrementan automáticamente.

---

## Integración con API CIMA

La API integra con el servicio CIMA de la Agencia Española de Medicamentos para obtener información oficial de medicamentos por Código Nacional.

**Endpoint:** `GET /api/medicines/cima/{cn}`

**Ejemplo:**
```bash
curl -X GET https://tu-dominio.com/api/medicines/cima/654321 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..."
```

**Respuesta:**
```json
{
  "success": true,
  "data": {
    "codigoNacional": "654321",
    "nombre": "IBUPROFENO CINFA 400 MG COMPRIMIDOS",
    "principioActivo": "IBUPROFENO",
    "formaFarmaceutica": "COMPRIMIDO",
    "dosis": "400 MG",
    "laboratorio": "CINFA",
    "receta": true,
    "comercializado": true
  }
}
```

---

## Uso desde .NET MAUI

Esta API está diseñada para ser consumida por aplicaciones .NET MAUI. Ejemplo de cliente:

```csharp
public class ApiService
{
    private readonly HttpClient _httpClient;
    private string _token = string.Empty;

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://tu-dominio.com/api/")
        };
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("auth/login", 
            new { email, password });
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content
                .ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
            _token = result?.Data?.Token ?? "";
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _token);
            return true;
        }
        return false;
    }

    public async Task<List<MedicineDto>> GetMedicinesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<ApiResponse<PagedResult<MedicineDto>>>(
            "medicines");
        return response?.Data?.Items ?? new List<MedicineDto>();
    }
}
```
