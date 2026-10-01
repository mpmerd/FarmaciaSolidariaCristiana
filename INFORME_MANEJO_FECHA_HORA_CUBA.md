# Informe: Fecha y hora de Cuba en Farmacia Solidaria Cristiana

> **Estado:** INVESTIGACIÓN — **no** se ha modificado ni una línea de código de la aplicación.
> **Rama:** `devIntrepido` (tras merge de `developerConApi` @ `12fad15`).
> **Framework:** .NET 10 (`net10.0` web / `net10.0-android` MAUI).
> **Autor:** Elintrepido · **Fecha:** 2026-10-01 · **Zona del autor:** America/Havana (UTC-4).
> **Objetivo:** definir cómo hacer que **la web** y **la app MAUI** trabajen siempre con la hora de Cuba, sin romper producción.

---

## 1. Resumen ejecutivo

Hoy **toda la aplicación depende de la hora local del servidor/host**:
- La web usa `DateTime.Now` en ~170 puntos (modelos, servicios, controladores y vistas).
- La app MAUI usa `DateTime.Now` / `DateTime.Today` para anotar entregas y para textos de fecha.
- La base de datos guarda `DateTime` sin zona (`datetime2`) y varios `DEFAULT GETDATE()` en SQL.
- El servidor de producción **no está en Cuba**: `farmaciasolidaria.somee.com` → `155.254.246.18` (Somee, datacenter en Charlotte, North Carolina, EE.UU.; `Microsoft-IIS/10.0`).

Esto **casi siempre "cuadra"** porque Cuba y la costa este de EE.UU. comparten offset (UTC-4 en verano, UTC-5 en invierno) durante la mayor parte del año. Pero es **incorrecto por diseño** y falla en los bordes:

1. En las **transiciones de horario de verano** hay ventanas de 1–2 h en las que Cuba y EE.UU. difieren (ver §3.2, medido con `zdump`).
2. Si el SO del servidor estuviera en **UTC** (evidencia de un proyecto hermano en el mismo rango de Somee), el desfase sería de **4–5 h permanentes**.
3. La hora del **teléfono** puede no ser Cuba (viaje, reloj mal puesto).

**Recomendación:** introducir un modelo de tiempo explícito — **instantes en UTC + zona de negocio `America/Havana`** — usando `TimeProvider` (.NET 10) y un servicio de zona horaria, con separación estricta entre "instantes" y "fechas/horas civiles de turno". Detalle en §5 y plan por fases en §7.

---

## 2. Cómo está construido hoy (inventario)

### 2.1 Arquitectura

| Componente | Detalle |
|---|---|
| Web | ASP.NET Core MVC + Web API, `net10.0`, EF Core 10.0.7 + SQL Server, ASP.NET Identity, SignalR |
| Hosted service | `TurnoCleanupService` (limpieza automática de turnos) |
| MAUI | `net10.0-android` (`com.fsolidaria.app`), RIDs `android-arm;android-arm64` |
| Producción | `https://farmaciasolidaria.somee.com` → `155.254.246.18` (Somee, Charlotte NC, IIS 10) |
| Base de datos | SQL Server, columnas `datetime2` **sin zona** |
| Zona de negocio real | Cuba = `America/Havana` |
| Cultura UI web | `es-ES` (fijada en `Program.cs`) |

### 2.2 Datos que la app trata como "fecha/hora" (dos naturalezas distintas)

**(A) Instantes (un momento concreto en el tiempo):**
`Turno.FechaSolicitud`, `Turno.FechaRevision`, `Turno.FechaEntrega`, `Delivery.DeliveryDate`, `Delivery.CreatedAt`, `Patient.RegistrationDate`, `Patient.LoanBlockDate/UnblockDate`, `Donation.DonationDate`, `FechaBloqueada.FechaCreacion`, `Sponsor.CreatedDate`, `PatientDocument.UploadDate`, `NavbarDecoration.*`, `PendingNotification.*`, tokens JWT.

**(B) Fecha/hora civil de negocio (reloj de pared cubano):**
`Turno.FechaPreferida` (slots de **1:00 PM–4:00 PM**, martes y viernes), `FechaBloqueada.Fecha` (día bloqueado), ventanas de horario de atención.

> ⚠️ Estas dos naturalezas **no se pueden tratar igual**. Un instante se guarda en UTC y se convierte a Cuba al mostrar. Una "fecha de turno a la 1 PM" es **hora de pared cubana** y **no debe sufrir conversión de zona**.

### 2.3 Puntos calientes encontrados (con referencia)

| Archivo | Qué hace | Riesgo |
|---|---|---|
| `Services/TurnoService.cs` | `now = DateTime.Now` (x3); genera slots `13:00–16:00`; `FechaSolicitud = DateTime.Now`; `GetNextAvailableSlotAsync()` | Cálculo de slots y "hoy" dependen del TZ del servidor |
| `Services/TurnoCleanupService.cs` | `now = DateTime.Now`; cancela turnos del mismo día si `now.Hour >= 18` | La cancelación a las **6 PM** se dispara ~1 h antes/después si el TZ difiere de Cuba |
| `Services/EmailService.cs`, `MaintenanceService.cs` | `DateTime.Now` en cuerpos/cálculos | Textos con hora incorrecta |
| `Models/*.cs` | Inicializadores `= DateTime.Now` (Turno, Delivery, Donation, Patient, FechaBloqueada, Sponsor, PatientDocument, NavbarDecoration) | Si nadie fija el valor, EF graba la hora del **servidor** |
| `Controllers/*`, `Api/Controllers/*` | `DateTime.Now` en altas, revisiones, reportes, "días restantes" (`(FechaPreferida - DateTime.Now.Date).TotalDays`), reportes por mes (`RegistrationDate.Month == DateTime.Now.Month`) | Reglas de negocio y reportes desalineados |
| `FarmaciaSolidariaCristiana.Maui/ViewModels/NuevaEntregaViewModel.cs` | `DeliveryDate = DateTime.Now` | Graba hora del **teléfono**, no de Cuba |
| `FarmaciaSolidariaCristiana.Maui/ViewModels/EntregasViewModel.cs` | `(DateTime.Now - entrega.DeliveryDate).TotalHours` | Compara hora del teléfono contra hora del servidor |
| `FarmaciaSolidariaCristiana.Maui/Services/ApiService.cs#L1151-1154` | Normaliza fecha *date-only* a `DateTimeKind.Unspecified` | ✅ Correcto: evita que el offset mueva el día al enviar la reprogramación |
| `Api/Controllers/ApiBaseController.cs#L65` | `Timestamp = DateTime.UtcNow` | ✅ La API ya expone instantes en UTC |
| SQL (`apply-migration-somee.sql`, etc.) | `DEFAULT GETDATE()` en `FechaCreacion`/`CreatedAt` | Tercer origen de tiempo: la hora local del **servidor SQL** |

### 2.4 Verificación en vivo (solo lectura)

- `GET https://farmaciasolidaria.somee.com/api/diagnostics/ping` → `{"status":"OK","timestamp":"2026-10-01T13:41:21.22Z"}` ✅ reloj del servidor preciso en UTC.
- `HEAD /` → `server: Microsoft-IIS/10.0` (Windows/IIS; el TZ del SO **no** lo controla la app).
- `farmaciasolidaria.somee.com` → `155.254.246.18` (Somee).
- **No se pudo determinar el TZ local del SO del servidor** con medios pasivos: la API solo expone UTC. **Ver §4 (Fase 0).**

---

## 3. Por qué esto importa (riesgo real)

### 3.1 El servidor no es Cuba

`DateTime.Now` = hora **local del sistema operativo del host**. El host está en EE.UU. (Somee). Por tanto:
- Si el SO está en **Eastern (America/New_York)**: difiere de Cuba solo en las transiciones (§3.2).
- Si el SO está en **UTC**: desfase permanente de **4–5 h** (¡el bug sería grave y constante!).

Evidencia indirecta: un proyecto hermano alojado en el mismo rango de Somee (`gabimecu.somee.com`, `155.254.246.43`) **sellaba sus timestamps en UTC**. No es prueba para esta app, pero es una señal de que **hay que medirlo** antes de asumir nada.

### 3.2 Cuba vs. costa este de EE.UU. — medido con `zdump` (2026)

Ambas zonas coinciden casi todo el año, pero **no siempre**:

| Transición | Cuba (`America/Havana`) | EE.UU. Este (`America/New_York`) | Desfase |
|---|---|---|---|
| Marzo 2026 | Cambia a UTC-4 el **dom 8-mar 05:00 UTC** | Cambia a UTC-4 el **dom 8-mar 07:00 UTC** | ~**2 h** de diferencia |
| Noviembre 2026 | Vuelve a UTC-5 el **dom 1-nov 05:00 UTC** | Vuelve a UTC-5 el **dom 1-nov 06:00 UTC** | ~**1 h** de diferencia |

> Nota: Cuba y la costa este **no** cambian a la misma hora exacta. Coincidir "casi siempre" es justo lo que ha ocultado el problema.

### 3.3 Impacto concreto en reglas de negocio

- **Cancelación automática** (`TurnoCleanupService`, umbral 6 PM): puede adelantarse o atrasarse.
- **Slots de turno** (1–4 PM): son hora de pared cubana; si algún día se aplicara un offset indebido, se correrían.
- **"Días restantes" para cancelar un turno** (`Api/Controllers/TurnosApiController.cs#L787`, `Views/Turnos/Index.cshtml#L217`): se recalcula con `DateTime.Now.Date`.
- **Reportes por mes**: `RegistrationDate.Month == DateTime.Now.Month` (`PatientsApiController.cs#L529`) puede cruzar de mes mal.
- **JWT** (`ClockSkew = TimeSpan.Zero`): sensible a relojes, pero correcto mientras se use UTC (los `exp` son epoch).
- **MAUI**: un paciente con el teléfono en otra zona genera `DeliveryDate` desplazado.

---

## 4. Fase 0 — Verificar el TZ real del servidor (antes de tocar nada)

Añadir temporalmente un log al arranque (o usar el endpoint de diagnóstico en Development) que reporte:

```csharp
// SOLO diagnóstico — no forma parte de la solución final
logger.LogInformation("TZ del host: {Id} (offset ahora {Offset}, ¿DST? {Dst})",
    TimeZoneInfo.Local.Id,
    TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow),
    TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now));
```

Y en la base de datos: `SELECT SYSDATETIMEOFFSET(), GETDATE(), GETUTCDATE();`

**No asumir.** Ese dato define el plan de migración de datos históricos (§7, Fase 4).

---

## 5. Solución propuesta (diseño)

### 5.0 Principios

1. **Nunca** usar la zona del host como "hora de Cuba".
2. **Instantes → UTC** en almacenamiento y transporte; **convertir a `America/Havana`** al mostrar/regla de negocio.
3. **Fechas/horas civiles de turno → hora de pared cubana**, *sin* conversión (tratarlas como "naive"/`Unspecified` o `DateOnly`+`TimeOnly`).
4. **Un solo reloj inyectable** (`TimeProvider`) para que todo sea testeable y no queden `DateTime.Now` sueltos.
5. **Una sola fuente de verdad** para la zona: configuración `"App:TimeZone": "America/Havana"` validada al arranque.

### 5.1 La zona de negocio

```csharp
public static class CubaTime
{
    private static readonly TimeZoneInfo Tz = Resolve();
    public static TimeZoneInfo Zone => Tz;

    private static TimeZoneInfo Resolve()
    {
        // ICU (Linux/macOS/.NET 10) primero; fallback al ID de Windows.
        foreach (var id in new[] { "America/Havana", "Cuba Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        throw new InvalidOperationException("No se pudo resolver la zona America/Havana.");
    }

    public static DateTime ToCuba(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Tz);

    public static DateTime CubaNow(TimeProvider clock) => ToCuba(clock.GetUtcNow().UtcDateTime);
}
```

> En .NET 6+ sobre Windows con ICU, `"America/Havana"` suele resolverse; el fallback `"Cuba Standard Time"` cubre hosts sin ICU. En Android, ICU está presente (siempre que **no** se active `InvariantGlobalization`).

### 5.2 Web (ASP.NET Core, .NET 10)

1. **Registrar el reloj y el servicio** en `Program.cs`:
   ```csharp
   builder.Services.AddSingleton(TimeProvider.System);
   builder.Services.AddSingleton<ICubaClock, CubaClock>();   // envuelve CubaTime
   ```
2. **Config**: `appsettings.json` → `"App": { "TimeZone": "America/Havana" }` + validación *fail-fast* al arranque.
3. **Sustituir `DateTime.Now`**:
   - Para **instantes**: `_clock.GetUtcNow()` (o `DateTime.UtcNow` mientras se migra) y guardar en UTC.
   - Para **reglas/visualización**: `_cubaClock.Now` (ya en Cuba) o `CubaTime.ToCuba(...)`.
4. **Model binding / JSON**: los `DateTimeOffset` serializan con offset; los instantes en `DateTime` deben ir con `Z` (UTC).
5. **EF Core**: para columnas nuevas usar `DateTimeOffset`; para las existentes, un `ValueConverter` que garantice `Kind=Utc` al leer/escribir.
6. **Date-only**: migrar a `DateOnly` (columna `date`) para `FechaBloqueada.Fecha` y días de turno.
7. **Background services**: `PeriodicTimer` (o `Task.Delay`) y calcular "hora Cuba" con `CubaTime`, en lugar de `now.Hour >= 18` sobre la hora del host.
8. **No** depender de `TZ=America/Havana` en el host: en IIS/Windows no aplica y cambiar el TZ del servidor es frágil y afecta a todo el host.
9. **SQL**: eliminar dependencias de `GETDATE()` en defaults (usar valor enviado por la app en UTC) o cambiar a `SYSUTCDATETIME()` + conversión en la app.

### 5.3 App MAUI (.NET 10 Android)

1. **Instantes que van al servidor**: enviar siempre UTC (ISO 8601 con `Z`). `NuevaEntregaViewModel` debe dejar de usar `DateTime.Now` para `DeliveryDate`.
2. **Mostrar**: convertir con el mismo helper `CubaTime` (idealmente en el proyecto **compartido `Contracts`** ya existente, para no duplicar lógica).
3. **Fechas date-only**: mantener la normalización `Unspecified` ya presente en `ApiService.cs#L1151` (evita corrimiento de día).
4. **No** usar la hora del teléfono para reglas de negocio; usar la del servidor o `CubaTime`.
5. **Globalización**: verificar que el APK **no** publique con `InvariantGlobalization=true` (de lo contrario `TimeZoneInfo`/ICU fallan). Hoy el `.csproj` no lo activa → ✅.
6. **Formato**: presentar en horario cubano (12 h con AM/PM o 24 h 13:00–16:00), respetando el estilo actual.

### 5.4 Contrato API (convención recomendada)

| Tipo | Formato | Ejemplo |
|---|---|---|
| Instante | ISO 8601 **UTC** | `2026-10-01T13:41:21Z` |
| Fecha civil | `yyyy-MM-dd` | `2026-10-06` |
| Hora de turno | fecha + `horaCuba` explícita | `2026-10-06` + `13:00` (Cuba) |

Documentar la convención en `API_DOCUMENTATION.md`. La API **ya** usa UTC en `ApiBaseController.Timestamp`; se trata de generalizarla.

---

## 6. Pruebas y verificación

- **`FakeTimeProvider`** (`Microsoft.Extensions.TimeProvider.Testing`) para fijar "ahora" y probar reglas sin depender del reloj real.
- Casos límite medidos con `zdump`:
  - **8 mar 2026, 00:00–02:00 (Cuba)** — ventana en que Cuba ya cambió y EE.UU. no.
  - **1 nov 2026, 00:00–01:00 (Cuba)** — ventana de 1 h en que Cuba volvió a UTC-5 y EE.UU. no.
- Casos de negocio: slot 13:00/16:00; cancelación a las 18:00; "7 días antes"; corte de mes en reportes.
- Doble comprobación en producción (solo lectura): comparar `ping/timestamp` (UTC) contra hora Cuba conocida.

---

## 7. Plan de adopción por fases (sin romper producción)

| Fase | Acción | Riesgo |
|---|---|---|
| **0** | Medir y documentar el TZ real del host y del SQL Server (§4) | Nulo |
| **1** | Introducir `TimeProvider` + `CubaClock` + config, **sin** cambiar comportamiento (solo DI/código nuevo) | Bajo |
| **2** | Nuevos instantes en UTC; `DateOnly` para días; converter EF | Bajo |
| **3** | Cambiar **presentación** y **reglas** (limpieza 18:00, slots, días restantes) a hora Cuba | Medio |
| **4** | Migrar **datos históricos** según el TZ documentado (ventana de mantenimiento) | Medio/Alto |
| **5** | Retirar los `DateTime.Now` restantes; añadir guardas (analizador/regla) para no reintroducirlos | Bajo |

> Regla de oro para producción: **primero observar, luego escribir en paralelo, al final cambiar la lectura**. Nunca un "big bang".

---

## 8. Inventario de `DateTime.Now` (para la fase 5)

Archivos con más usos (conteo): `Data/DataSeeder.cs` (22), `Services/TurnoService.cs` (16), `Api/Controllers/ReportsApiController.cs` (8), `Api/Controllers/PatientsApiController.cs` (8), `Data/DbInitializer.cs` (6), `Controllers/FechasBloqueadasController.cs` (6), `Controllers/PatientsController.cs` (5), `Controllers/NavbarDecorationsController.cs` (5), `Services/EmailService.cs` (4), `Controllers/DeliveriesController.cs` (4), `Api/Controllers/TurnosApiController.cs` (4), `Api/Controllers/DeliveriesApiController.cs` (4), `Services/TurnoCleanupService.cs` (3), `Controllers/TurnosController.cs` (3), `Controllers/ReportsController.cs` (3), `Models/*` (inicializadores), y el resto en MAUI y controladores varios. Total ≈ **170** referencias a `DateTime.Now`/`UtcNow` en el repo.

---

## 9. Riesgos y precauciones

- **Estamos en producción** (600+ usuarios app, 1100+ web). Cualquier cambio de fechas debe ser incremental y con pruebas.
- El punto **más delicado** es `Turno.FechaPreferida`: es **hora de pared cubana**; convertirla como si fuera un instante correría los turnos. Debe tratarse como "naive"/civil.
- Los **datos históricos** ya están grabados con el TZ del servidor; la migración debe hacerse **después** de conocer ese TZ (§4) y con respaldo previo.
- **No** cambiar el TZ del SO del host ni de la base de datos como "solución": es global, frágil y no documenta la intención.
- Mantener `ClockSkew` y el manejo de expiración JWT como están (usan UTC correctamente).

---

## 10. Conclusión

La app funciona hoy por **coincidencia de offsets**, no por diseño. La solución correcta y de bajo riesgo es: **instantes en UTC + `America/Havana` como zona de negocio + `TimeProvider` inyectable + separación entre instantes y fechas civiles de turno**, adoptada por fases. Este informe deja el diagnóstico y el plan listos; **no se ha modificado código**.

**Siguiente paso propuesto:** ejecutar la Fase 0 (medir el TZ real del host y de SQL Server) y, con ese dato, preparar un PR incremental sobre `devIntrepido`.
