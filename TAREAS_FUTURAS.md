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
**Descripción**: 

Dar la posibilidad a un usuario que tiene turno aprobado que lo cancele, el periodo de tiempo permitido será hasta que falte una semana para la fecha del turno.

Que los usuarios con el rol de seguridad Admin puedan bloquear fechas futuras donde no se permitan dar turnos. Esto es prever desastres naturales, días festivos o acontecimientos desconocidos que se presenten e impidan a la farmacia hacer su función en un momento determinado.

También en situaciones excepcionales que los usuarios con role admin puedan reprogramar los turnos de un día determinado o de varios días y que automáticamente la aplicación busque los espacios de tiempo en los días venideros que no están bloqueados. Esta función requerirá el envío de emails automáticos a los pacientes afectados, explicando la causa de la reprogramación del turno.

**Estado**: 📋 Análisis técnico completado

**Análisis de Implementación:**

**PARTE 0: Cancelación de Turnos por Usuario (ViewerPublic)**

**Descripción**: Permitir que usuarios con turno aprobado puedan cancelarlo hasta 7 días antes de la fecha del turno.

**Lógica de validación:**
```csharp
// En TurnoService.cs
public bool CanUserCancelTurno(Turno turno)
{
    // Solo turnos Aprobados pueden ser cancelados por usuario
    if (turno.Estado != EstadoTurno.Aprobado)
        return false;
    
    // Debe tener fecha asignada
    if (!turno.FechaPreferida.HasValue)
        return false;
    
    // Calcular días restantes
    var diasRestantes = (turno.FechaPreferida.Value.Date - DateTime.Now.Date).Days;
    
    // Permitir cancelar solo si faltan más de 7 días
    return diasRestantes > 7;
}

public string GetCancelReasonMessage(Turno turno)
{
    if (turno.Estado != EstadoTurno.Aprobado)
        return "Solo se pueden cancelar turnos aprobados.";
    
    if (!turno.FechaPreferida.HasValue)
        return "El turno no tiene fecha asignada.";
    
    var diasRestantes = (turno.FechaPreferida.Value.Date - DateTime.Now.Date).Days;
    
    if (diasRestantes <= 7)
        return $"No se puede cancelar. Faltan solo {diasRestantes} día(s). Debe cancelar con al menos 7 días de anticipación.";
    
    return string.Empty;
}
```

**Agregar a ITurnoService.cs:**
```csharp
bool CanUserCancelTurno(Turno turno);
string GetCancelReasonMessage(Turno turno);
Task<bool> CancelTurnoByUserAsync(int turnoId, string userId, string motivoCancelacion);
```

**Implementar en TurnoService.cs:**
```csharp
public async Task<bool> CancelTurnoByUserAsync(int turnoId, string userId, string motivoCancelacion)
{
    var turno = await _context.Turnos
        .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
        .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
        .FirstOrDefaultAsync(t => t.Id == turnoId && t.UserId == userId);
    
    if (turno == null)
        return false;
    
    // Validar que se puede cancelar
    if (!CanUserCancelTurno(turno))
        return false;
    
    // Cambiar estado a Cancelado (o Rechazado con nota)
    turno.Estado = EstadoTurno.Rechazado;
    turno.FechaRevision = DateTime.Now;
    turno.Comentarios += $"\n[CANCELADO POR USUARIO - {DateTime.Now:dd/MM/yyyy HH:mm}]";
    turno.Comentarios += $"\nMotivo: {motivoCancelacion}";
    
    // Liberar slot (la fecha queda disponible para otros)
    // No es necesario hacer nada, el slot se libera automáticamente
    
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Turno {TurnoId} cancelado por usuario {UserId}", turnoId, userId);
    
    return true;
}
```

**Actualizar enum EstadoTurno (si no existe):**
```csharp
public static class EstadoTurno
{
    public const string Pendiente = "Pendiente";
    public const string Aprobado = "Aprobado";
    public const string Rechazado = "Rechazado";
    public const string Completado = "Completado";
    public const string Cancelado = "Cancelado"; // NUEVO (opcional, o usar Rechazado)
}
```

**Nuevo método en TurnosController:**
```csharp
// POST: Turnos/Cancel/5
[HttpPost]
[Authorize(Roles = "ViewerPublic")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Cancel(int id, string motivoCancelacion)
{
    var userId = _userManager.GetUserId(User);
    
    var turno = await _context.Turnos
        .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    
    if (turno == null)
    {
        return NotFound();
    }
    
    // Validar que se puede cancelar
    if (!_turnoService.CanUserCancelTurno(turno))
    {
        var reason = _turnoService.GetCancelReasonMessage(turno);
        TempData["ErrorMessage"] = reason;
        return RedirectToAction(nameof(Index));
    }
    
    // Validar que se proporcione motivo
    if (string.IsNullOrWhiteSpace(motivoCancelacion))
    {
        TempData["ErrorMessage"] = "Debe proporcionar un motivo para la cancelación.";
        return RedirectToAction(nameof(Index));
    }
    
    // Cancelar turno
    var success = await _turnoService.CancelTurnoByUserAsync(id, userId!, motivoCancelacion);
    
    if (success)
    {
        // Enviar email de confirmación al usuario
        var user = await _userManager.GetUserAsync(User);
        if (user?.Email != null)
        {
            try
            {
                await _emailService.SendTurnoCanceladoByUserEmailAsync(
                    user.Email, 
                    user.UserName ?? "Usuario",
                    turno.NumeroTurno ?? 0,
                    turno.FechaPreferida ?? DateTime.Now,
                    motivoCancelacion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo enviar email de cancelación");
            }
        }
        
        // Notificar a farmacéuticos
        await NotificarFarmaceuticosTurnoCancelado(turno, motivoCancelacion);
        
        TempData["SuccessMessage"] = "Tu turno ha sido cancelado exitosamente.";
    }
    else
    {
        TempData["ErrorMessage"] = "No se pudo cancelar el turno.";
    }
    
    return RedirectToAction(nameof(Index));
}

private async Task NotificarFarmaceuticosTurnoCancelado(Turno turno, string motivo)
{
    var farmaceuticos = await _userManager.GetUsersInRoleAsync("Farmaceutico");
    var admins = await _userManager.GetUsersInRoleAsync("Admin");
    var destinatarios = farmaceuticos.Union(admins).Where(u => u.Email != null);
    
    foreach (var user in destinatarios)
    {
        try
        {
            await _emailService.SendNotificacionTurnoCanceladoAsync(
                user.Email!,
                user.UserName ?? "Farmacéutico",
                turno.NumeroTurno ?? 0,
                turno.FechaPreferida ?? DateTime.Now,
                motivo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enviando notificación a {Email}", user.Email);
        }
    }
}
```

**Agregar métodos de email a IEmailService.cs:**
```csharp
Task SendTurnoCanceladoByUserEmailAsync(
    string destinatario, 
    string nombreUsuario, 
    int numeroTurno, 
    DateTime fechaTurno,
    string motivo);

Task SendNotificacionTurnoCanceladoAsync(
    string destinatario,
    string nombreFarmaceutico,
    int numeroTurno,
    DateTime fechaTurno,
    string motivo);
```

**Implementar en EmailService.cs:**
```csharp
public async Task SendTurnoCanceladoByUserEmailAsync(
    string destinatario, 
    string nombreUsuario, 
    int numeroTurno, 
    DateTime fechaTurno,
    string motivo)
{
    var subject = $"Turno #{numeroTurno:000} Cancelado";
    
    var body = $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <div style='background-color: #f8d7da; padding: 20px; border-radius: 5px;'>
                <h2 style='color: #721c24;'>Turno Cancelado</h2>
                <p>Estimado/a <strong>{nombreUsuario}</strong>,</p>
                
                <p>Tu turno ha sido cancelado según tu solicitud.</p>
                
                <div style='background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #dc3545;'>
                    <p><strong>Número de Turno:</strong> #{numeroTurno:000}</p>
                    <p><strong>Fecha del Turno:</strong> {fechaTurno:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Motivo:</strong> {motivo}</p>
                </div>
                
                <p>Si necesitas solicitar un nuevo turno, puedes hacerlo desde nuestra plataforma.</p>
                
                <p style='margin-top: 20px;'>
                    Saludos,<br/>
                    <strong>Farmacia Solidaria Cristiana</strong>
                </p>
            </div>
        </body>
        </html>";
    
    await SendEmailAsync(destinatario, subject, body);
}

public async Task SendNotificacionTurnoCanceladoAsync(
    string destinatario,
    string nombreFarmaceutico,
    int numeroTurno,
    DateTime fechaTurno,
    string motivo)
{
    var subject = $"Usuario canceló Turno #{numeroTurno:000}";
    
    var body = $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <div style='background-color: #fff3cd; padding: 20px; border-radius: 5px;'>
                <h2 style='color: #856404;'>Turno Cancelado por Usuario</h2>
                <p>Hola <strong>{nombreFarmaceutico}</strong>,</p>
                
                <p>Un usuario ha cancelado su turno aprobado:</p>
                
                <div style='background-color: white; padding: 15px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                    <p><strong>Número de Turno:</strong> #{numeroTurno:000}</p>
                    <p><strong>Fecha del Turno:</strong> {fechaTurno:dd/MM/yyyy HH:mm}</p>
                    <p><strong>Motivo de cancelación:</strong> {motivo}</p>
                </div>
                
                <p>El slot de tiempo quedó disponible para otros usuarios.</p>
                
                <p style='margin-top: 20px;'>
                    Sistema Automático<br/>
                    <strong>Farmacia Solidaria Cristiana</strong>
                </p>
            </div>
        </body>
        </html>";
    
    await SendEmailAsync(destinatario, subject, body);
}
```

**Modificar Vista Index.cshtml (Mis Turnos):**
```html
@* Agregar columna "Acciones" (si se eliminó antes, volver a agregar solo para Aprobados) *@
<th>Acciones</th>

@* En el cuerpo de la tabla, dentro del foreach *@
<td>
    @if (turno.Estado == "Aprobado")
    {
        var diasRestantes = turno.FechaPreferida.HasValue ? 
            (turno.FechaPreferida.Value.Date - DateTime.Now.Date).Days : 0;
        
        if (diasRestantes > 7)
        {
            <button type="button" class="btn btn-sm btn-warning" 
                    data-bs-toggle="modal" 
                    data-bs-target="#cancelModal"
                    data-turno-id="@turno.Id"
                    data-turno-numero="@turno.NumeroTurno"
                    data-turno-fecha="@turno.FechaPreferida?.ToString("dd/MM/yyyy HH:mm")">
                <i class="bi bi-x-circle"></i> Cancelar
            </button>
            <small class="text-muted d-block">Faltan @diasRestantes días</small>
        }
        else
        {
            <small class="text-muted">
                <i class="bi bi-info-circle"></i> 
                No cancelable<br/>
                (menos de 7 días)
            </small>
        }
    }
    else
    {
        <span class="text-muted">-</span>
    }
</td>

@* Modal de confirmación al final de la vista *@
<div class="modal fade" id="cancelModal" tabindex="-1">
    <div class="modal-dialog">
        <div class="modal-content">
            <form asp-action="Cancel" method="post">
                <div class="modal-header bg-warning">
                    <h5 class="modal-title">Cancelar Turno</h5>
                    <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                </div>
                <div class="modal-body">
                    <input type="hidden" name="id" id="turnoId" />
                    
                    <div class="alert alert-warning">
                        <i class="bi bi-exclamation-triangle"></i>
                        <strong>¿Estás seguro?</strong><br/>
                        Vas a cancelar el turno <strong>#<span id="turnoNumero"></span></strong><br/>
                        Fecha: <strong><span id="turnoFecha"></span></strong>
                    </div>
                    
                    <div class="mb-3">
                        <label class="form-label">
                            <strong>Motivo de la cancelación:</strong>
                            <span class="text-danger">*</span>
                        </label>
                        <textarea name="motivoCancelacion" 
                                  class="form-control" 
                                  rows="3" 
                                  required
                                  placeholder="Por favor, explica por qué necesitas cancelar el turno..."></textarea>
                        <small class="text-muted">Este campo es obligatorio</small>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">
                        No, mantener turno
                    </button>
                    <button type="submit" class="btn btn-warning">
                        <i class="bi bi-x-circle"></i> Sí, cancelar turno
                    </button>
                </div>
            </form>
        </div>
    </div>
</div>

<script>
// Script para pasar datos al modal
document.addEventListener('DOMContentLoaded', function() {
    var cancelModal = document.getElementById('cancelModal');
    cancelModal.addEventListener('show.bs.modal', function (event) {
        var button = event.relatedTarget;
        var turnoId = button.getAttribute('data-turno-id');
        var turnoNumero = button.getAttribute('data-turno-numero');
        var turnoFecha = button.getAttribute('data-turno-fecha');
        
        document.getElementById('turnoId').value = turnoId;
        document.getElementById('turnoNumero').textContent = turnoNumero;
        document.getElementById('turnoFecha').textContent = turnoFecha;
    });
});
</script>
```

**Pasos de implementación:**
1. Agregar métodos de validación a `TurnoService.cs`
2. Implementar método `CancelTurnoByUserAsync` en servicio
3. Agregar action `Cancel` en `TurnosController`
4. Implementar métodos de email en `EmailService`
5. Modificar vista `Index.cshtml` con botón cancelar y modal
6. Testing:
   - Cancelar turno con más de 7 días → Exitoso
   - Intentar cancelar con menos de 7 días → Rechazado
   - Verificar emails enviados (usuario y farmacéuticos)
   - Verificar que slot queda disponible
7. Documentar en CHANGELOG
8. Commit y deploy

**Tiempo estimado:** 3-4 horas

---

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

**Tiempo estimado total (Tarea 2 completa):** 
- PARTE 0 (Cancelación por usuario): 3-4 horas
- PARTE 1 (Bloqueo de fechas): 3-4 horas
- PARTE 2 (Reprogramación): 4-6 horas
- PARTE 3 (Emails): 2-3 horas
- **Total: 12-17 horas**

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
