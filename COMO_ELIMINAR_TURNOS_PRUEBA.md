# Guía: Cómo Eliminar Turnos de Prueba

## 🎯 Opciones para Eliminar Turnos

### Opción 1: Eliminar TODOS los datos de prueba (Recomendado)
**Script:** `delete-turnos-test-data.sql`

**Elimina:**
- ✅ 4 usuarios de prueba (María, Juan, Ana, Carlos)
- ✅ Todos sus turnos
- ✅ Todos sus turno-medicamentos

**NO elimina:**
- ❌ Usuarios reales
- ❌ Turnos de usuarios reales
- ❌ Medicamentos, Patrocinadores, Entregas, etc.

**Uso:**
```bash
# Desde panel Somee → SQL Manager
# O desde terminal local:
sqlcmd -S TU_SERVIDOR -d FarmaciaDb -i delete-turnos-test-data.sql
```

---

### Opción 2: Eliminar turnos específicos manualmente
**Script:** `delete-specific-turnos.sql` (crear si es necesario)

Si quieres eliminar solo algunos turnos específicos por su ID:

```sql
USE [FarmaciaDb];
GO

-- Eliminar un turno específico
DECLARE @TurnoId INT = 1; -- Cambiar por el ID del turno

BEGIN TRANSACTION;

-- 1. Eliminar medicamentos del turno
DELETE FROM TurnoMedicamentos WHERE TurnoId = @TurnoId;

-- 2. Eliminar el turno
DELETE FROM Turnos WHERE Id = @TurnoId;

COMMIT TRANSACTION;

PRINT 'Turno #' + CAST(@TurnoId AS NVARCHAR(10)) + ' eliminado exitosamente';
```

---

### Opción 3: Eliminar turnos por rango de fechas
**Útil para limpiar turnos de prueba antiguos**

```sql
USE [FarmaciaDb];
GO

-- Eliminar turnos antiguos de usuarios de prueba
BEGIN TRANSACTION;

DECLARE @EmailsPrueba TABLE (Email NVARCHAR(256));
INSERT INTO @EmailsPrueba VALUES 
    ('maria.garcia@example.com'),
    ('juan.perez@example.com'),
    ('ana.lopez@example.com'),
    ('carlos.rodriguez@example.com');

-- Obtener IDs de usuarios de prueba
DECLARE @UserIdsPrueba TABLE (UserId NVARCHAR(450));
INSERT INTO @UserIdsPrueba
SELECT Id FROM AspNetUsers WHERE Email IN (SELECT Email FROM @EmailsPrueba);

-- Eliminar turnos anteriores a una fecha específica
DECLARE @FechaLimite DATETIME = '2025-11-01'; -- Cambiar fecha

DELETE FROM TurnoMedicamentos
WHERE TurnoId IN (
    SELECT Id FROM Turnos 
    WHERE UserId IN (SELECT UserId FROM @UserIdsPrueba)
    AND FechaSolicitud < @FechaLimite
);

DELETE FROM Turnos
WHERE UserId IN (SELECT UserId FROM @UserIdsPrueba)
AND FechaSolicitud < @FechaLimite;

COMMIT TRANSACTION;

PRINT 'Turnos de prueba anteriores a ' + CONVERT(VARCHAR, @FechaLimite, 120) + ' eliminados';
```

---

### Opción 4: Desde la interfaz web (Manual)
**Solo Admin/Farmacéutico**

⚠️ **IMPORTANTE:** Actualmente NO existe un botón "Eliminar" en la interfaz web para turnos.

Si quieres agregar esta funcionalidad:

1. **Agregar acción en TurnosController:**
```csharp
[HttpPost]
[Authorize(Roles = "Admin,Farmaceutico")]
public async Task<IActionResult> Delete(int id)
{
    var turno = await _context.Turnos
        .Include(t => t.Medicamentos)
        .FirstOrDefaultAsync(t => t.Id == id);
    
    if (turno == null) return NotFound();
    
    _context.Turnos.Remove(turno); // Cascade eliminará TurnoMedicamentos
    await _context.SaveChangesAsync();
    
    TempData["SuccessMessage"] = "Turno eliminado correctamente";
    return RedirectToAction(nameof(Dashboard));
}
```

2. **Agregar botón en Dashboard.cshtml:**
```html
<form asp-action="Delete" asp-route-id="@turno.Id" method="post" 
      onsubmit="return confirm('¿Eliminar este turno?');">
    <button type="submit" class="btn btn-sm btn-danger">
        <i class="bi bi-trash"></i> Eliminar
    </button>
</form>
```

---

## 🔐 Seguridad al Eliminar

### ✅ Buenas Prácticas:
1. **Siempre usar transacciones** (BEGIN TRANSACTION / COMMIT)
2. **Verificar antes de eliminar** (SELECT para confirmar qué se eliminará)
3. **Hacer backup** antes de eliminar datos importantes
4. **Solo Admin** debe poder eliminar turnos

### ⚠️ Advertencias:
- Eliminar turnos de usuarios reales puede causar confusión
- Los PDFs generados NO se eliminan automáticamente (solo la referencia en BD)
- Los archivos subidos (receta, tarjetón) NO se eliminan automáticamente

---

## 📊 Scripts SQL Disponibles

| Script | Descripción |
|--------|-------------|
| **delete-turnos-test-data.sql** | ✅ Elimina TODOS los usuarios y turnos de prueba |
| Manual (SQL directo) | Eliminar turnos específicos por ID |
| Manual (SQL por fecha) | Eliminar turnos anteriores a una fecha |

---

## 🚀 Recomendación

Para **testing rápido y limpio**:

1. **Crear datos de prueba:**
   ```bash
   sqlcmd -S TU_SERVIDOR -d FarmaciaDb -i seed-turnos-test-data.sql
   ```

2. **Probar el sistema** (solicitar, aprobar, rechazar, verificar)

3. **Limpiar todo:**
   ```bash
   sqlcmd -S TU_SERVIDOR -d FarmaciaDb -i delete-turnos-test-data.sql
   ```

4. **Repetir** cuantas veces necesites

---

## 📞 Soporte

**Desarrollador:** Rev. Maikel Eduardo Peláez Martínez  
**Email:** mpmerd@gmail.com
