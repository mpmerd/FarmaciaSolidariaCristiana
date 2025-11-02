# Tareas Futuras - Farmacia Solidaria Cristiana

## 📋 Pendientes

### 🏆 Alta Prioridad

#### 1. Revición de los reportes
**Descripción**: Mejorar el reporte de entregas y donaciones

En los reportes, en el caso del reporte de entregas se debe dar la posibilidad, no sólo para medicamento, sino también para insumos y lo mismo para el reporte de donaciones, que contemple tanto medicamentos como insumos.

**Estado**: 📋 Análisis técnico completado

**Análisis de Implementación:**

**Archivos a modificar:**
1. `Controllers/ReportsController.cs` - Métodos `EntregasReport` y `DonacionesReport`
2. `Views/Reports/EntregasReport.cshtml` - Agregar filtro tipo
3. `Views/Reports/DonacionesReport.cshtml` - Agregar filtro tipo
4. `Services/ReportService.cs` (si existe) o lógica en el controller

**Cambios técnicos necesarios:**

**A. Agregar parámetro de filtro de tipo:**
```csharp
public async Task<IActionResult> EntregasReport(
    DateTime? startDate, 
    DateTime? endDate,
    string? tipoFiltro) // NUEVO: "Todos", "Medicamentos", "Insumos"
{
    var query = _context.Deliveries
        .Include(d => d.Medicine)
        .Include(d => d.Supply)      // NUEVO
        .Include(d => d.Patient)
        .Where(d => d.DeliveryDate >= start && d.DeliveryDate <= end);
    
    // Aplicar filtro por tipo
    if (tipoFiltro == "Medicamentos")
        query = query.Where(d => d.MedicineId != null);
    else if (tipoFiltro == "Insumos")
        query = query.Where(d => d.SupplyId != null);
    
    // Si es "Todos" o null, no filtramos (muestra ambos)
    
    var entregas = await query.OrderBy(d => d.DeliveryDate).ToListAsync();
}
```

**B. Modificar las vistas para mostrar columna "Tipo":**
```html
<th>Tipo</th>
...
<td>
    @if (delivery.MedicineId != null)
    {
        <span class="badge bg-success">Medicamento</span>
    }
    else if (delivery.SupplyId != null)
    {
        <span class="badge bg-info">Insumo</span>
    }
</td>
<td>
    @(delivery.Medicine?.Name ?? delivery.Supply?.Name ?? "N/A")
</td>
```

**C. Agregar filtro en la vista (antes del botón generar):**
```html
<div class="col-md-3 mb-3">
    <label class="form-label">Tipo</label>
    <select name="tipoFiltro" class="form-select">
        <option value="Todos" selected>Todos</option>
        <option value="Medicamentos">Solo Medicamentos</option>
        <option value="Insumos">Solo Insumos</option>
    </select>
</div>
```

**D. Actualizar generación de PDF:**
- Agregar columna "Tipo" en el PDF
- Ajustar el ancho de las columnas existentes
- Totales separados: "Total Medicamentos: X" y "Total Insumos: Y"

**Pasos de implementación:**
1. Modificar `ReportsController.cs` - Agregar parámetro `tipoFiltro`
2. Actualizar query para filtrar según tipo
3. Modificar vista HTML para agregar dropdown de filtro
4. Agregar columna "Tipo" en tabla HTML y PDF
5. Actualizar totales para mostrar separados por tipo
6. Aplicar mismo patrón a `DonacionesReport`
7. Testing con casos: Solo medicamentos, solo insumos, todos
8. Commit y deploy

**Tiempo estimado:** 3-4 horas

---
#### 2. Agregar nuevas funcionalidades el sistema de entrega de turnos
**Descripción**: Que los usuarios con el rol de seguridad Admin puedan bloquear fechas futuras donde no se permitan dar turnos. Esto es prever desastres naturales, días festivos o acontecimientos desconocidos que se presenten e impidan a la farmacia hacer su función en un momento determinado.

También en situaciones excepcionales que los usuarios con role admin puedan reprogramar los turnos de un día determinado o de varios días y que automáticamente la aplicación busque los espacios de tiempo en los días venideros que no están bloqueados. Esta función requerirá el envío de emails automáticos a los pacientes afectados, explicando la causa de la reprogramación del turno.

**Estado**: 📋 Análisis técnico completado

**Análisis de Implementación:**

**PARTE 1: Sistema de Bloqueo de Fechas**

**Nueva tabla en base de datos:**
```sql
CREATE TABLE FechasBloqueadas (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Fecha DATE NOT NULL UNIQUE,
    Motivo NVARCHAR(500) NOT NULL,
    UsuarioId NVARCHAR(450) NOT NULL, -- Quien bloqueó
    FechaCreacion DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UsuarioId) REFERENCES AspNetUsers(Id)
);
```

**Modelo C#:**
```csharp
public class FechaBloqueada
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Motivo { get; set; }
    public string UsuarioId { get; set; }
    public ApplicationUser Usuario { get; set; }
    public DateTime FechaCreacion { get; set; }
}
```

**Controller nuevo: `FechasBloqueadasController.cs`**
```csharp
[Authorize(Roles = "Admin")]
public class FechasBloqueadasController : Controller
{
    // GET: Index - Listar fechas bloqueadas
    public async Task<IActionResult> Index()
    
    // POST: Create - Bloquear una fecha
    [HttpPost]
    public async Task<IActionResult> Create(DateTime fecha, string motivo)
    
    // POST: CreateRange - Bloquear rango de fechas
    [HttpPost]
    public async Task<IActionResult> CreateRange(
        DateTime fechaInicio, 
        DateTime fechaFin, 
        string motivo)
    
    // POST: Delete - Desbloquear una fecha
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
}
```

**Vista: `Views/FechasBloqueadas/Index.cshtml`**
- Tabla con fechas bloqueadas (futuras primero)
- Formulario para bloquear fecha individual
- Formulario para bloquear rango de fechas
- Botón eliminar por fecha
- Solo accesible para Admin

**Modificar TurnosController:**
```csharp
private async Task<bool> IsFechaBloqueada(DateTime fecha)
{
    return await _context.FechasBloqueadas
        .AnyAsync(f => f.Fecha.Date == fecha.Date);
}

// En método Create:
if (await IsFechaBloqueada(fechaPreferida))
{
    TempData["ErrorMessage"] = "La fecha seleccionada está bloqueada.";
    return RedirectToAction(...);
}
```

**Modificar vista de solicitud de turno:**
- Deshabilitar fechas bloqueadas en el calendario/datepicker
- Mostrar mensaje si intenta seleccionar fecha bloqueada

**PARTE 2: Reprogramación Automática de Turnos**

**Nuevo método en TurnosController:**
```csharp
[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> ReprogramarTurnosPorFecha(
    DateTime fechaAfectada, 
    string motivo)
{
    // 1. Obtener turnos del día afectado (Aprobados y Pendientes)
    var turnosAfectados = await _context.Turnos
        .Include(t => t.Patient)
        .Where(t => t.FechaPreferida.HasValue && 
                    t.FechaPreferida.Value.Date == fechaAfectada.Date &&
                    (t.Estado == EstadoTurno.Aprobado || 
                     t.Estado == EstadoTurno.Pendiente))
        .ToListAsync();
    
    if (!turnosAfectados.Any())
    {
        TempData["InfoMessage"] = "No hay turnos en esa fecha.";
        return RedirectToAction(...);
    }
    
    // 2. Por cada turno afectado, buscar próximo slot disponible
    var turnosReprogramados = new List<TurnoReprogramacion>();
    
    foreach (var turno in turnosAfectados)
    {
        // Buscar próxima fecha disponible (Martes o Jueves)
        var nuevaFecha = await BuscarProximaFechaDisponible(
            fechaAfectada.AddDays(1));
        
        if (nuevaFecha == null)
        {
            // No hay slots disponibles en próximos 30 días
            continue;
        }
        
        // Buscar slot de hora disponible
        var nuevoSlot = await BuscarProximoSlotDisponible(nuevaFecha.Value);
        
        if (nuevoSlot == null) continue;
        
        // Guardar info para email
        turnosReprogramados.Add(new TurnoReprogramacion
        {
            Turno = turno,
            FechaOriginal = turno.FechaPreferida.Value,
            FechaNueva = nuevoSlot.Fecha,
            HoraNueva = nuevoSlot.Hora
        });
        
        // Actualizar turno
        turno.FechaPreferida = nuevoSlot.Fecha;
        turno.Comentarios += $"\n[REPROGRAMADO] Fecha original: " +
            $"{turno.FechaPreferida:dd/MM/yyyy}. Motivo: {motivo}";
    }
    
    await _context.SaveChangesAsync();
    
    // 3. Enviar emails a los pacientes afectados
    await EnviarEmailsReprogramacion(turnosReprogramados, motivo);
    
    TempData["SuccessMessage"] = 
        $"{turnosReprogramados.Count} turnos reprogramados exitosamente.";
    
    return RedirectToAction("Index");
}

private async Task<DateTime?> BuscarProximaFechaDisponible(DateTime desde)
{
    var fechaBusqueda = desde;
    var diasBuscados = 0;
    
    while (diasBuscados < 30) // Buscar hasta 30 días adelante
    {
        // Solo Martes (2) o Jueves (4)
        if (fechaBusqueda.DayOfWeek == DayOfWeek.Tuesday || 
            fechaBusqueda.DayOfWeek == DayOfWeek.Thursday)
        {
            // Verificar que no esté bloqueada
            if (!await IsFechaBloqueada(fechaBusqueda))
            {
                // Verificar que haya slots disponibles
                var turnosEnFecha = await _context.Turnos
                    .CountAsync(t => t.FechaPreferida.HasValue &&
                                     t.FechaPreferida.Value.Date == 
                                         fechaBusqueda.Date);
                
                if (turnosEnFecha < 30) // Hay espacio
                {
                    return fechaBusqueda;
                }
            }
        }
        
        fechaBusqueda = fechaBusqueda.AddDays(1);
        diasBuscados++;
    }
    
    return null; // No hay fechas disponibles
}

private async Task<SlotDisponible?> BuscarProximoSlotDisponible(
    DateTime fecha)
{
    // Horario: 1:00 PM a 4:00 PM, slots de 6 minutos
    var horaInicio = new TimeSpan(13, 0, 0);
    var horaFin = new TimeSpan(16, 0, 0);
    var duracionSlot = TimeSpan.FromMinutes(6);
    
    var horaActual = horaInicio;
    
    while (horaActual < horaFin)
    {
        var fechaHora = fecha.Date.Add(horaActual);
        
        // Verificar si este slot está ocupado
        var ocupado = await _context.Turnos
            .AnyAsync(t => t.FechaPreferida.HasValue &&
                           t.FechaPreferida.Value == fechaHora);
        
        if (!ocupado)
        {
            return new SlotDisponible
            {
                Fecha = fecha,
                Hora = horaActual
            };
        }
        
        horaActual = horaActual.Add(duracionSlot);
    }
    
    return null;
}
```

**PARTE 3: Envío de Emails de Reprogramación**

**Agregar nuevo método al EmailService existente:**
```csharp
// Agregar a IEmailService interface existente
Task EnviarEmailReprogramacion(
    string destinatario, 
    string nombrePaciente,
    DateTime fechaOriginal,
    DateTime fechaNueva,
    TimeSpan horaNueva,
    string motivo);

// Implementar en EmailService existente
public async Task EnviarEmailReprogramacion(
    string destinatario, 
    string nombrePaciente,
    DateTime fechaOriginal,
    DateTime fechaNueva,
    TimeSpan horaNueva,
    string motivo)
{
    var mensaje = new MailMessage
    {
        From = new MailAddress(_configuration["Email:FromEmail"], 
            "Farmacia Solidaria Cristiana"),
        Subject = "Reprogramación de su turno",
        Body = $@"
            <html>
            <body>
                <h2>Notificación de Reprogramación</h2>
                <p>Estimado/a {nombrePaciente},</p>
                <p>Le informamos que su turno ha sido reprogramado 
                   debido a: <strong>{motivo}</strong></p>
                <p><strong>Fecha original:</strong> 
                   {fechaOriginal:dd/MM/yyyy}</p>
                <p><strong>Nueva fecha:</strong> 
                   {fechaNueva:dd/MM/yyyy} a las 
                   {horaNueva:hh\\:mm} hrs</p>
                <p>Lamentamos las molestias.</p>
                <p>Saludos,<br/>Farmacia Solidaria Cristiana</p>
            </body>
            </html>",
        IsBodyHtml = true
    };
    
    mensaje.To.Add(destinatario);
    
    await _emailSender.SendEmailAsync(destinatario, 
        "Reprogramación de su turno", mensaje.Body);
}
```

**NOTA:** El sistema ya tiene EmailService configurado y funcionando. Solo agregar el nuevo método para reprogramaciones.

**Nueva vista: `Views/Turnos/ReprogramarFecha.cshtml`**
```html
@{
    ViewData["Title"] = "Reprogramar Turnos por Fecha";
}

<h2>Reprogramar Turnos de una Fecha</h2>

<div class="alert alert-warning">
    <strong>Atención:</strong> Esta acción reprogramará automáticamente 
    todos los turnos del día seleccionado y enviará emails a los pacientes.
</div>

<form asp-action="ReprogramarTurnosPorFecha" method="post">
    <div class="mb-3">
        <label class="form-label">Fecha a reprogramar</label>
        <input type="date" name="fechaAfectada" 
               class="form-control" required />
    </div>
    
    <div class="mb-3">
        <label class="form-label">Motivo</label>
        <textarea name="motivo" class="form-control" 
                  rows="3" required
                  placeholder="Ej: Día festivo, emergencia...">
        </textarea>
    </div>
    
    <button type="submit" class="btn btn-warning">
        Reprogramar Turnos
    </button>
    <a asp-action="Index" class="btn btn-secondary">Cancelar</a>
</form>
```

**Pasos de implementación:**
1. Crear migración y tabla `FechasBloqueadas`
2. Crear modelo `FechaBloqueada` y actualizar DbContext
3. Crear `FechasBloqueadasController` con CRUD
4. Crear vistas para gestión de fechas bloqueadas
5. Modificar `TurnosController` para validar fechas bloqueadas
6. Agregar método `EnviarEmailReprogramacion` en `EmailService` existente
7. Implementar método `ReprogramarTurnosPorFecha` en TurnosController
8. Crear vista de reprogramación
9. Testing exhaustivo:
    - Bloquear fecha → Impide crear turno
    - Reprogramar día con turnos → Envía emails
    - Verificar slots disponibles correctamente
10. Documentar en CHANGELOG
11. Commit y deploy

**Tiempo estimado:** 10-14 horas (función compleja)

**Consideraciones adicionales:**
- Manejo de errores si email falla (guardar en log)
- Opción de "preview" antes de reprogramar mostrando turnos afectados
- Límite de días hacia adelante para reprogramar (máximo 30 días)
- Agregar en menú de Admin: "Gestión de Fechas"

---

### 🔜 Media Prioridad

#### 3. Dashboard con Estadísticas
- Medicamentos con stock bajo (alertas)
- Insumos con stock bajo (alertas)
- Total de entregas por mes (gráfico)
- Medicamentos/Insumos más entregados (top 10)
- Pacientes activos vs totales
- Comparativa medicamentos vs insumos

#### 4. Sistema de Notificaciones
- Email a admins, farmacéuticos y viewers cuando stock llegue a mínimo
- Alertas para medicamentos e insumos
- Notificación de entregas realizadas

#### 5. Mejorar Búsqueda de Pacientes
- Búsqueda por nombre además de identificación
- Autocompletar en campo de búsqueda
- Historial completo de entregas (medicamentos e insumos)

---

### 📌 Baja Prioridad

#### 6. Sistema de Auditoría
- Registrar quién modificó qué y cuándo
- Tabla de `AuditLog` con cambios importantes

#### 7. Impresión de Recibos
- Recibo imprimible de entrega
- Incluir código QR para verificación

#### 8. Multi-idioma
- Soporte para inglés además de español
- Usar recursos `.resx`

---

## 🎯 Roadmap

### Versión 1.1 (Completada - 27/10/2025)
- ✅ Control de eliminación con validaciones
- ✅ Ordenamiento alfabético
- ✅ Validación de fechas de entrega
- ✅ CRUD de Patrocinadores (Admin only, PNG, compresión)
- ✅ Módulo completo de Insumos
- ✅ Entregas de medicamentos E insumos
- ✅ Reportes con inventarios separados

### Versión 1.2 (En planificación)
- Donaciones de insumos
- Filtros avanzados en reportes
- Dashboard con estadísticas

### Versión 2.0 (Largo plazo)
- Sistema de notificaciones por email
- Sistema de auditoría completo
- Multi-idioma

---

## 📝 Notas

- Priorizar funcionalidades solicitadas por usuarios reales de la farmacia
- Mantener simplicidad en la UX
- Todos los cambios deben incluir:
  - Tests (si es posible)
  - Actualización de documentación
  - Scripts SQL de migración para Somee
  - Entrada en CHANGELOG.md
