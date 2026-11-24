# Base de datos — Farmacia Solidaria Cristiana

SQL Server vía EF Core. Fuente de verdad del esquema: `FarmaciaSolidariaCristiana/Migrations/` + `Data/ApplicationDbContext.cs`. Los `.sql` de `sql/` son parches históricos aplicados manualmente a producción — no usarlos como referencia del esquema actual. Además de las entidades propias hay tablas ASP.NET Identity (users, roles, claims…).

## Entidades (17, en `FarmaciaSolidariaCristiana/Models/`)

| Entidad | Qué representa | Campos clave |
|---|---|---|
| `Medicine` | Medicamento | `Name`, `StockQuantity`, `Unit` (default "comprimidos"), `NationalCode` (código nacional español) |
| `Supply` | Insumo médico (vendas, gasas, jeringas…) | `Name`, `StockQuantity`, `Unit` (default "Unidades") |
| `Donation` | Donación entrante — **suma stock** | `MedicineId`*, `SupplyId`*, `Quantity`, `DonationDate`, `DonorNote`, `Comments` |
| `Delivery` | Entrega a paciente — **resta stock** | `PatientIdentification`, `MedicineId`, `SupplyId`, `PatientId`, `TurnoId`, `Quantity`, `DeliveryDate`, `Dosage`, `TreatmentDuration`, `BatchNumber`, `ExpiryDate`, `DeliveredBy` |
| `Patient` | Paciente con historial | `IdentificationDocument`, `FullName`, `Age`, `Gender`, `Address`, `Phone`, `Municipality`, `Province`, datos clínicos (`MainDiagnosis`, `KnownAllergies`, `CurrentTreatments`, `BloodPressure`, `Weight`/`Height` decimal(5,2)), bloqueo por préstamo (`LoanBlocked…`) |
| `PatientDocument` | Documento de paciente (N:1 Patient) | `DocumentType`, `FileName`, `FilePath`, `FileSize`, `ContentType`, `Description`, `UploadDate` |
| `Turno` | Cita para retirar medicamentos — núcleo del sistema | `UserId`, `DocumentoIdentidadHash` (**SHA-256, nunca texto claro**), `FechaPreferida`, `FechaAsignada`, `Estado`, `NumeroTurno`, `NotasSolicitante`, `ComentariosFarmaceutico`, `RevisadoPorId`, `RecetaMedicaPath`, `TarjetonPath` |
| `TurnoMedicamento` | N:M Turno↔Medicine | `CantidadSolicitada`, `CantidadAprobada`, `DisponibleAlSolicitar`, `Notas` |
| `TurnoInsumo` | N:M Turno↔Supply | ídem |
| `TurnoDocumento` | Adjuntos del turno (N:1 Turno) | `DocumentType` (Receta Médica, Tarjetón Sanitario, Informe Médico, Otro), `FileName`, `FilePath`, `FileSize`, `ContentType` |
| `Sponsor` | Patrocinador (logos en reportes) | `Name`, `Description`, `LogoPath`, `IsActive`, `DisplayOrder` |
| `FechaBloqueada` | Día sin turnos (festivos, emergencias) | `Fecha` (índice único), `Motivo`, `UsuarioId`, `FechaCreacion` |
| `NavbarDecoration` | Decoración temática del navbar (Navidad, Epifanía…) | `Type` (Predefined/Custom), `PresetKey`, `DisplayText`, colores e iconos |
| `PendingNotification` | Notificación para polling de la app | `UserId`, `Title`, `Message`, `NotificationType`, `ReferenceId/Type`, `AdditionalData` (JSON), `IsRead`, `CreatedAt`, `ReadAt` |
| `UserDeviceToken` | Dispositivo OneSignal por usuario | `UserId`, `OneSignalPlayerId`, `DeviceToken`, `DeviceType`, `DeviceName`, `IsActive` |
| `BroadcastEmailQueueItem` | Cola persistente de emails masivos | `Status`, `SentAtUtc`, tope diario 350 (Gmail), processor cada 60 s |

\* Una `Donation` es de medicamento **o** de insumo (polimorfismo simple: uno de los dos FK es nulo).

## Relaciones (`Data/ApplicationDbContext.cs`)

- `Delivery` → `Medicine` (Restrict), `Delivery` → `Patient` (Restrict).
- `Donation` → `Medicine` (Restrict).
- `PatientDocument` → `Patient` (**Cascade**).
- `Turno` → `User` (Restrict), `Turno` → `RevisadoPor` (User, Restrict).
- `TurnoMedicamento` → `Turno` (**Cascade**), → `Medicine` (Restrict).
- `TurnoInsumo` → `Turno` (**Cascade**), → `Supply` (Restrict).
- `TurnoDocumento` → `Turno` (**Cascade**).
- `Patient` → `LoanUnblockedByUser` (SetNull, opcional).
- `FechaBloqueada` → `Usuario` (Restrict); `UserDeviceToken` → `User` (**Cascade**).
- Índices únicos: `FechaBloqueada.Fecha` · `UserDeviceToken(UserId, OneSignalPlayerId)` · índice también en `UserDeviceToken.OneSignalPlayerId` · `NavbarDecoration.IsActive` · `BroadcastEmailQueueItem(Status, Id)` y `SentAtUtc`.

## Reglas de negocio

### Turnos (sistema de citas)

- Horario de atención: **Martes y Jueves, 1:00–4:00 PM**, slots de 6 minutos → máximo **30 turnos/día**.
- Anti-abuso: máximo **2 turnos por mes por usuario** (10 si tiene rol ViewerPublicPlus) Y **2 turnos por mes por documento de identidad** (un usuario puede pedir turnos para distintos pacientes; cada documento tiene su propio límite).
- Estados: `Pendiente` → `Aprobado`/`Rechazado` → `Completado`/`Cancelado`. Flujo gestionado en `Services/TurnoService.cs`.
- Números de turno únicos secuenciales por día.
- Documentos de identidad: carnet cubano (11 dígitos) o pasaporte (1–3 letras + 6–7 dígitos); validación con regex del proyecto; se persisten **solo como hash SHA-256**.
- `TurnoCleanupService` (background, cada hora) cancela automáticamente turnos aprobados donde el usuario no asistió.
- Días bloqueables vía `FechaBloqueada`; los slots no se ofrecen en esas fechas.

### Stock

- **Donation suma** stock; **Delivery resta** stock. Nunca modificar `StockQuantity` fuera de la lógica de servicios correspondiente.
- `CantidadAprobada` en `TurnoMedicamento`/`TurnoInsumo` delimita cuánto se entrega realmente al completar el turno.

### Documentación completa

- Sistema de turnos a fondo: `docs/TURNOS_SYSTEM.md`.
- Contexto histórico y servicios: `docs/CONTEXTO_COMPLETO.md`.
