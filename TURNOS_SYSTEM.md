# Sistema de Turnos - Farmacia Solidaria Cristiana

## Descripción General

El **Sistema de Turnos** es una funcionalidad avanzada diseñada para gestionar solicitudes de medicamentos en la Farmacia Solidaria Cristiana de Cárdenas, Cuba. Este sistema permite a los usuarios solicitar turnos de manera ordenada, evitando aglomeraciones y garantizando un proceso justo y transparente.

## Contexto

Cárdenas es una ciudad de más de 250,000 habitantes con alta demanda de medicamentos. El sistema de turnos fue implementado para:

- **Organizar** el flujo de solicitudes de medicamentos
- **Prevenir abusos** con mecanismos de control (2 turnos por mes)
- **Garantizar transparencia** con números de turno únicos
- **Facilitar verificación** mediante documentos de identidad cifrados
- **Automatizar notificaciones** por email

## Arquitectura del Sistema

### Modelos de Datos

#### 1. **Turno** (`Models/Turno.cs`)

Entidad principal que representa una solicitud de turno.

```csharp
public class Turno
{
    public int Id { get; set; }
    public string UserId { get; set; }                    // FK a AspNetUsers
    public string DocumentoIdentidadHash { get; set; }    // SHA-256 hash
    public DateTime FechaPreferida { get; set; }          // Fecha/hora deseada
    public DateTime FechaSolicitud { get; set; }          // Timestamp de creación
    public string Estado { get; set; }                    // Estado del turno
    public string? RecetaMedicaPath { get; set; }         // Ruta archivo opcional
    public string? TarjetonPath { get; set; }             // Ruta archivo obligatorio
    public string? NotasSolicitante { get; set; }
    public string? ComentariosFarmaceutico { get; set; }
    public string? RevisadoPorId { get; set; }            // FK a AspNetUsers
    public DateTime? FechaRevision { get; set; }
    public DateTime? FechaEntrega { get; set; }
    public string? TurnoPdfPath { get; set; }
    public int? NumeroTurno { get; set; }                 // Número único por día
    public bool EmailEnviado { get; set; }
    
    // Navigation Properties
    public IdentityUser User { get; set; }
    public IdentityUser? RevisadoPor { get; set; }
    public ICollection<TurnoMedicamento> Medicamentos { get; set; }
}
```

**Estados válidos:**
- `Pendiente` - Solicitud recién creada, esperando revisión
- `Aprobado` - Aprobado por farmacéutico, listo para recoger
- `Rechazado` - Rechazado con motivo explicado
- `Completado` - Medicamentos entregados
- `Cancelado` - Cancelado por el usuario (futuro)

#### 2. **TurnoMedicamento** (`Models/TurnoMedicamento.cs`)

Relación many-to-many entre Turnos y Medicines.

```csharp
public class TurnoMedicamento
{
    public int Id { get; set; }
    public int TurnoId { get; set; }
    public int MedicineId { get; set; }
    public int CantidadSolicitada { get; set; }
    public bool DisponibleAlSolicitar { get; set; }       // Stock check
    public int? CantidadAprobada { get; set; }            // Puede diferir
    public string? Notas { get; set; }
    
    // Navigation Properties
    public Turno Turno { get; set; }
    public Medicine Medicine { get; set; }
}
```

### Servicios

#### **ITurnoService / TurnoService** (`Services/TurnoService.cs`)

Lógica de negocio centralizada para gestión de turnos.

**Métodos principales:**

1. **`CanUserRequestTurnoAsync(userId)`**
   - Valida límite de 2 turnos por mes
   - Retorna `(bool canRequest, string reason)`
   - Query: Busca turnos del usuario en el mes actual con estado != Rechazado

2. **`CreateTurnoAsync(userId, documentoId, fechaPref, receta, tarjeton, medicineIds, quantities, notas)`**
   - Crea turno con transacción
   - Hashea documento con SHA-256
   - Guarda archivos en `wwwroot/uploads/turnos/`
   - Verifica stock de medicamentos
   - Envía email de confirmación

3. **`ApproveTurnoAsync(turnoId, farmaceuticoId, comentarios)`**
   - Genera número de turno único (secuencial por día)
   - Actualiza estado a "Aprobado"
   - Envía email con número de turno
   - TODO: Genera PDF (pendiente)

4. **`RejectTurnoAsync(turnoId, farmaceuticoId, motivo)`**
   - Actualiza estado a "Rechazado"
   - Envía email con motivo del rechazo

5. **`CompleteTurnoAsync(turnoId)`**
   - Marca turno como "Completado"
   - Registra fecha de entrega

6. **`FindTurnoByDocumentHashAsync(documentoId)`**
   - Busca turno por documento cifrado
   - Útil para verificación en farmacia

**Seguridad:**
- Documentos hasheados con SHA-256 + Base64
- Uploads validados (tamaño, extensión)
- Transacciones para integridad de datos

#### **IEmailService / EmailService** (extensión)

Métodos agregados para turnos:

1. **`SendTurnoSolicitadoEmailAsync(email, userName)`**
   - Template azul con mensaje de confirmación
   - Explica proceso de 24-48h de revisión

2. **`SendTurnoAprobadoEmailAsync(email, userName, numeroTurno, fecha, comentarios)`**
   - Template verde con número de turno destacado
   - Incluye fecha/hora y comentarios del farmacéutico
   - Adjunta PDF (cuando esté implementado)

3. **`SendTurnoRechazadoEmailAsync(email, userName, motivo)`**
   - Template rojo con motivo del rechazo en recuadro
   - Informa que puede volver a solicitar en 30 días

### Controlador

#### **TurnosController** (`Controllers/TurnosController.cs`)

10 acciones con autorización basada en roles.

| Acción | Método | Roles | Descripción |
|--------|--------|-------|-------------|
| `Index` | GET | Authenticated | Página principal para ViewerPublic |
| `RequestForm` | GET | ViewerPublic | Formulario de solicitud |
| `RequestForm` | POST | ViewerPublic | Procesar solicitud |
| `Confirmation` | GET | Authenticated | Ver confirmación post-solicitud |
| `Dashboard` | GET | Farmaceutico, Admin | Panel de gestión |
| `Details` | GET | Authenticated | Ver detalles de turno |
| `Approve` | POST | Farmaceutico, Admin | Aprobar turno |
| `Reject` | POST | Farmaceutico, Admin | Rechazar turno |
| `Complete` | POST | Farmaceutico, Admin | Marcar como completado |
| `Verify` | GET/POST | Farmaceutico, Admin | Verificar por documento |
| `CheckStock` | GET | Authenticated | API JSON para stock |

**Validaciones implementadas:**

- **Fecha preferida:** Mínimo 24h, máximo 1 mes futuro
- **Medicamentos:** Al menos 1, cantidades válidas vs stock
- **Archivos:** Tarjetón obligatorio, receta opcional, máx 5MB, formatos JPG/PNG/PDF
- **Anti-abuso:** 2 turnos por mes por usuario
- **Estado:** Solo se puede aprobar/rechazar turnos "Pendiente"

### Vistas

#### 1. **Index.cshtml** - Página Principal

**Para usuarios ViewerPublic:**
- Botón "Solicitar Turno" (habilitado según `CanRequestTurno`)
- Tabla con historial de turnos del usuario
- Grid de medicamentos disponibles (primeros 12)
- Badges de estado con colores

**ViewData esperado:**
- `UserTurnos` (List<Turno>)
- `CanRequestTurno` (bool)
- `CannotRequestReason` (string)
- `AvailableMedicines` (List<Medicine>)

#### 2. **RequestForm.cshtml** - Formulario de Solicitud

**Características:**
- Multi-sección: Personal Info, Medicamentos, Documentos, Notas
- JavaScript interactivo para búsqueda de medicamentos
- Selección dinámica con inputs de cantidad
- Validación en tiempo real vs stock
- File upload validation
- DateTime picker con min=24h

**JavaScript funcionalidades:**
```javascript
- Búsqueda incremental (mínimo 2 caracteres)
- Click para agregar medicamento
- Inputs dinámicos con validación de cantidad
- Botón "Quitar" para remover medicamentos
- Validación de formulario (min 1 medicamento)
- File size validation (5MB)
- Prevención de doble submit
```

#### 3. **Confirmation.cshtml** - Confirmación

Muestra:
- Mensaje de éxito con icono
- "¿Qué sigue?" (5 pasos del proceso)
- Detalles de la solicitud
- Lista de medicamentos con badges de disponibilidad
- Notas del solicitante
- Avisos importantes (24-48h, revisar email)

#### 4. **Dashboard.cshtml** - Panel de Control (Farmaceutico/Admin)

**Características:**
- **Estadísticas:** Cards con contadores (Pendientes, Aprobados, Completados, Total)
- **Filtros:** Estado, rango de fechas (desde/hasta)
- **Tabla DataTables:** Ordenable, paginable, búsqueda en español
- **Modales inline:** Aprobar/Rechazar directamente desde tabla
- **Acciones rápidas:** Botón "Verificar por Documento"

**Columnas tabla:**
- ID, Usuario (nombre + email), Estado (badge), Número turno, Fechas, Count medicamentos, Acciones

**Modales:**
- **Aprobar:** Textarea para comentarios opcionales, info de acciones automáticas
- **Rechazar:** Textarea obligatorio para motivo

#### 5. **Details.cshtml** - Detalles del Turno

**Secciones:**
1. **Información General:** ID, Estado, Fechas (solicitud, preferida, revisión, entrega)
2. **Información del Usuario:** Username, Email, Documento Hash (parcial), Revisado por
3. **Medicamentos:** Tabla con cantidades solicitadas/aprobadas, stock, notas
4. **Documentos:** Links para descargar Receta y Tarjetón
5. **Notas:** Notas del solicitante y comentarios del farmacéutico
6. **Acciones:** Botones para Aprobar/Rechazar/Completar según estado

**Modales:**
- Aprobar con textarea para comentarios
- Rechazar con textarea obligatorio para motivo

#### 6. **Verify.cshtml** - Verificación por Documento

**Flujo:**
1. **Formulario:** Input para documento de identidad
2. **Búsqueda:** POST que hashea y busca en DB
3. **Resultado:** Muestra turno encontrado con info completa
4. **Acciones:** Botón "Marcar como Entregado" si estado=Aprobado

**Seguridad:**
- Documentos cifrados en DB
- Solo busca turnos Aprobados/Pendientes de entrega
- Alerta de verificar CI físico antes de entregar

## Mecanismos Anti-Abuso

### 1. Límite de 2 Turnos por Mes

**Implementación:** `TurnoService.CanUserRequestTurnoAsync()`

```csharp
var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
var turnosEsteMes = await _context.Turnos
    .CountAsync(t => t.UserId == userId 
        && t.FechaSolicitud >= startOfMonth 
        && t.FechaSolicitud <= endOfMonth
        && (t.Estado == "Pendiente" || t.Estado == "Aprobado" || t.Estado == "Completado"));
return turnosEsteMes < 2;
```

**Lógica:**
- Busca turnos en últimos 30 días
- Excluye turnos rechazados (pueden reintentar)
- Validación en servidor + UI

### 2. Documento de Identidad Cifrado

**Hash SHA-256:**
```csharp
public string HashDocument(string documentoIdentidad)
{
    using (var sha256 = SHA256.Create())
    {
        var bytes = Encoding.UTF8.GetBytes(documentoIdentidad);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

**Ventajas:**
- No se almacena documento en texto plano
- Verificable mediante hash
- GDPR/privacidad compliant

### 3. Número de Turno Único

**Generación:** `TurnoService.GenerateNumeroTurnoAsync()`

```csharp
// Secuencial por día: #001, #002, #003...
var today = DateTime.Today;
var lastTurno = await _context.Turnos
    .Where(t => t.FechaRevision.Value.Date == today && t.NumeroTurno.HasValue)
    .OrderByDescending(t => t.NumeroTurno)
    .FirstOrDefaultAsync();
    
return (lastTurno?.NumeroTurno ?? 0) + 1;
```

**Formato:** `#001`, `#002`, etc. (reinicia cada día)

### 4. Validación de Stock

**Al solicitar:**
```csharp
DisponibleAlSolicitar = medicine.StockQuantity >= cantidad
```

**Al aprobar:**
- Farmacéutico puede ajustar `CantidadAprobada` si stock insuficiente
- Usuario notificado por email de cambios

## Workflow Completo

### Flujo Usuario ViewerPublic

```
1. LOGIN → Index.cshtml
   ↓
2. Click "Solicitar Turno" (si CanRequest)
   ↓
3. RequestForm.cshtml
   - Ingresar documento
   - Seleccionar medicamentos (búsqueda JavaScript)
   - Ajustar cantidades
   - Subir archivos (receta opcional, tarjetón obligatorio)
   - Agregar notas
   ↓
4. POST /Turnos/RequestForm
   - Validaciones servidor
   - Hash documento
   - Guardar archivos
   - Crear Turno + TurnoMedicamentos
   - Email confirmación
   ↓
5. Confirmation.cshtml
   - Mensaje "Solicitud enviada"
   - Detalles del turno
   - Instrucciones (24-48h)
   ↓
6. EMAIL: "Tu solicitud ha sido recibida"
   ↓
7. Esperar revisión...
   ↓
8. EMAIL: "Turno Aprobado #042" o "Turno Rechazado"
   ↓
9. Si aprobado → Recoger medicamentos con CI en mano
   ↓
10. Farmacéutico marca como "Completado"
```

### Flujo Farmacéutico/Admin

```
1. LOGIN → Dashboard.cshtml
   ↓
2. Ver estadísticas (Pendientes/Aprobados/Completados)
   ↓
3. Filtrar por Estado="Pendiente"
   ↓
4. Click "Ver Detalles" en turno
   ↓
5. Details.cshtml
   - Revisar info usuario
   - Ver medicamentos solicitados
   - Descargar documentos (receta/tarjetón)
   - Verificar stock actual
   ↓
6. DECISIÓN:
   
   A) APROBAR:
      - Click "Aprobar Turno"
      - Modal: Agregar comentarios opcionales
      - POST /Turnos/Approve
      - Sistema genera número turno único
      - Email automático al usuario
      - PDF generado (si implementado)
      
   B) RECHAZAR:
      - Click "Rechazar Turno"
      - Modal: Ingresar motivo (obligatorio)
      - POST /Turnos/Reject
      - Email automático con motivo
   ↓
7. Usuario llega a farmacia con CI
   ↓
8. Farmacéutico → Verify.cshtml
   - Ingresar número de documento
   - Sistema busca por hash
   - Muestra turno con medicamentos aprobados
   ↓
9. Verificar CI físico
   ↓
10. Click "Marcar como Entregado"
    ↓
11. POST /Turnos/Complete
    - Estado → "Completado"
    - FechaEntrega registrada
```

## Seguridad

### Autenticación y Autorización

**Roles utilizados:**
- `ViewerPublic` - Puede solicitar turnos
- `Farmaceutico` - Puede gestionar turnos
- `Admin` - Acceso completo

**Authorization attributes:**
```csharp
[Authorize] // Requiere login
[Authorize(Roles = "ViewerPublic")] // Solo ViewerPublic
[Authorize(Roles = "Farmaceutico,Admin")] // Farmaceutico O Admin
```

### Protección de Datos

1. **Documentos de Identidad:**
   - Hash SHA-256 irreversible
   - No se almacena texto plano
   - Verificable mediante hash

2. **Archivos Subidos:**
   - Validación de extensión (JPG, PNG, PDF)
   - Límite de tamaño (5MB)
   - Nombres únicos con GUID
   - Almacenados fuera de acceso público directo

3. **Anti-CSRF:**
   - `@Html.AntiForgeryToken()` en todos los formularios
   - `[ValidateAntiForgeryToken]` en POST actions

4. **Prevención de Inyección SQL:**
   - Entity Framework con queries parametrizadas
   - Linq to SQL

### Email Security

- SMTP configurado en appsettings.json
- Credenciales protegidas
- Templates HTML sanitizados
- Rate limiting (TODO: implementar)

## Base de Datos

### Tablas Creadas (Migración 20251031141709_AddTurnosSystem)

**Turnos:**
```sql
CREATE TABLE Turnos (
    Id INT PRIMARY KEY IDENTITY,
    UserId NVARCHAR(450) NOT NULL,
    DocumentoIdentidadHash NVARCHAR(MAX) NOT NULL,
    FechaPreferida DATETIME2 NOT NULL,
    FechaSolicitud DATETIME2 NOT NULL,
    Estado NVARCHAR(50) NOT NULL,
    RecetaMedicaPath NVARCHAR(MAX),
    TarjetonPath NVARCHAR(MAX),
    NotasSolicitante NVARCHAR(MAX),
    ComentariosFarmaceutico NVARCHAR(MAX),
    RevisadoPorId NVARCHAR(450),
    FechaRevision DATETIME2,
    FechaEntrega DATETIME2,
    TurnoPdfPath NVARCHAR(MAX),
    NumeroTurno INT,
    EmailEnviado BIT NOT NULL,
    CONSTRAINT FK_Turnos_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT,
    CONSTRAINT FK_Turnos_AspNetUsers_RevisadoPorId FOREIGN KEY (RevisadoPorId) REFERENCES AspNetUsers(Id) ON DELETE RESTRICT
);
```

**TurnoMedicamentos:**
```sql
CREATE TABLE TurnoMedicamentos (
    Id INT PRIMARY KEY IDENTITY,
    TurnoId INT NOT NULL,
    MedicineId INT NOT NULL,
    CantidadSolicitada INT NOT NULL,
    DisponibleAlSolicitar BIT NOT NULL,
    CantidadAprobada INT,
    Notas NVARCHAR(MAX),
    CONSTRAINT FK_TurnoMedicamentos_Turnos FOREIGN KEY (TurnoId) REFERENCES Turnos(Id) ON DELETE CASCADE,
    CONSTRAINT FK_TurnoMedicamentos_Medicines FOREIGN KEY (MedicineId) REFERENCES Medicines(Id) ON DELETE RESTRICT
);
```

**Índices:**
```sql
CREATE INDEX IX_Turnos_UserId ON Turnos(UserId);
CREATE INDEX IX_Turnos_RevisadoPorId ON Turnos(RevisadoPorId);
CREATE INDEX IX_TurnoMedicamentos_TurnoId ON TurnoMedicamentos(TurnoId);
CREATE INDEX IX_TurnoMedicamentos_MedicineId ON TurnoMedicamentos(MedicineId);
```

## API Endpoints

### CheckStock (AJAX)

**Endpoint:** `GET /Turnos/CheckStock?medicineIds=1,2,3`

**Respuesta JSON:**
```json
[
  {
    "medicineId": 1,
    "name": "Paracetamol",
    "stock": 150,
    "available": true
  },
  {
    "medicineId": 2,
    "name": "Ibuprofeno",
    "stock": 3,
    "available": false
  }
]
```

**Uso:** JavaScript en RequestForm.cshtml para validación en tiempo real.

## Configuración

### appsettings.json

```json
{
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "SenderEmail": "farmacia@example.com",
    "SenderName": "Farmacia Solidaria Cristiana",
    "Username": "farmacia@example.com",
    "Password": "app-specific-password"
  },
  "TurnoSettings": {
    "MaxFileSize": 5242880,  // 5MB
    "AllowedExtensions": [".jpg", ".jpeg", ".png", ".pdf"],
    "TurnoLimitDays": 30
  }
}
```

### Program.cs Registration

```csharp
builder.Services.AddScoped<ITurnoService, TurnoService>();
```

## Frontend Technologies

- **Bootstrap 5** - UI framework
- **Bootstrap Icons** - Iconografía
- **jQuery** - Manipulación DOM, AJAX
- **DataTables** - Tablas interactivas con paginación
- **JavaScript Vanilla** - Validaciones y lógica

## Mejoras Futuras (TODO)

1. **Generación de PDFs con iText7**
   - PDF con logos de la farmacia
   - Número de turno destacado
   - Lista de medicamentos aprobados
   - Código QR para verificación rápida

2. **Notificaciones Push/SMS**
   - SMS cuando turno es aprobado
   - Recordatorio 1h antes del turno

3. **Sistema de Cancelación**
   - Usuarios pueden cancelar turnos
   - Liberar slot para otros usuarios

4. **Reportes Avanzados**
   - Dashboard con gráficos (Chart.js)
   - Estadísticas por mes
   - Medicamentos más solicitados

5. **API REST para Integración**
   - Endpoints JSON para apps móviles
   - Autenticación JWT

6. **Sistema de Prioridades**
   - Adultos mayores con prioridad
   - Casos urgentes con etiqueta especial

7. **Recordatorios Automáticos**
   - Email 24h antes del turno
   - Email si turno no recogido en 7 días

## Testing

### Casos de Prueba Recomendados

1. **Solicitud de Turno:**
   - ✅ Usuario puede solicitar si no tiene turno en 30 días
   - ✅ Usuario NO puede solicitar si ya tiene turno activo
   - ✅ Validación de archivos (tamaño, extensión)
   - ✅ Validación de fecha (min 24h, max 30 días)
   - ✅ Al menos 1 medicamento requerido

2. **Aprobación/Rechazo:**
   - ✅ Solo Farmaceutico/Admin pueden aprobar
   - ✅ Genera número de turno único
   - ✅ Email enviado automáticamente
   - ✅ Motivo obligatorio al rechazar

3. **Verificación:**
   - ✅ Busca por documento hasheado
   - ✅ Solo muestra turnos aprobados
   - ✅ Marca como completado correctamente

4. **Anti-Abuso:**
   - ✅ Límite de 2 turnos por mes funciona
   - ✅ Rechazados pueden volver a solicitar
   - ✅ Documentos cifrados correctamente

## Soporte

Para dudas o problemas:

- **Desarrollador:** Rev. Maikel Eduardo Peláez Martínez
- **Email:** mpmerd@gmail.com
- **Iglesia:** Metodista de Cárdenas
- **Ubicación:** Cárdenas, Cuba

---

**Última actualización:** 31 de Octubre de 2025  
**Versión del Sistema:** 1.0  
**Estado:** Producción (Fase 2 completada - Frontend)
