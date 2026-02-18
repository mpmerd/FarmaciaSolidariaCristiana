# Bug: Entrega asociada al turno incorrecto cuando un paciente tiene dos turnos aprobados consecutivos

**Fecha de detección:** 17 de febrero de 2026  
**Severidad:** Alta  
**Módulo afectado:** `DeliveriesController` — acción `Create`  
**Archivos corregidos:** `FarmaciaSolidariaCristiana/Controllers/DeliveriesController.cs`

---

## Descripción del incidente

Un mismo paciente tenía dos turnos aprobados simultáneamente, cada uno con un medicamento distinto:

| Turno | Medicamento           | Cantidad |
|-------|-----------------------|----------|
| 190   | Losartán 50mg         | 28 uds   |
| 191   | Ramipril 10mg         | 30 uds   |

Ambos turnos fueron **aprobados de forma consecutiva** y luego las entregas fueron **registradas de forma consecutiva** (una por una, desde el formulario de entregas).

Al registrar la primera entrega (por ejemplo, Ramipril del turno 191), el sistema asoció correctamente `TurnoId = 191`. Al registrar la segunda entrega (Losartán del turno 190), el sistema **incorrectamente** también le asignó `TurnoId = 191`, dejando el turno 190 sin ninguna entrega vinculada.

Como consecuencia, esa misma noche el `TurnoCleanupService` detectó el turno 190 en estado `Aprobado` sin entrega y lo **canceló automáticamente por no-presentación**, enviando notificaciones erróneas al paciente y a los farmacéuticos.

---

## Causa raíz — dos bugs encadenados

### Bug 1: `turnoId` global determinado solo por el primer medicamento

Antes de entrar al loop de creación de entregas, el controlador llamaba a `FindTurnoIdWithStateAsync` **una única vez** usando el primer medicamento de la lista. El `turnoId` resultante se reutilizaba para **todas** las entregas de esa sesión:

```csharp
// ❌ CÓDIGO CON BUG (antes de la corrección)
var firstMedicineId = MedicineIds.Split(',').Select(int.Parse).First();
var turnoInfo = await FindTurnoIdWithStateAsync(PatientIdentification, firstMedicineId, null);
int? turnoId = turnoInfo.turnoId; // ← se usaba para TODOS los medicamentos del loop

for (int i = 0; i < medicineIdsList.Count; i++)
{
    var delivery = new Delivery { TurnoId = turnoId, ... }; // ← siempre el mismo turno
}
```

En el escenario del incidente:
- Primera entrega procesada → medicamento perteneciente al turno 191 → `turnoId = 191`
- Segunda entrega procesada → medicamento perteneciente al turno 190 → **también se le asignó `TurnoId = 191`** (incorrecto)

### Bug 2: marcado de "Completado" solo para el primer medicamento

Tras guardar las entregas, el sistema llamaba a `CompleteTurnoIfExistsAsync` **solo con el primer medicamento**, por lo que únicamente el turno asociado a ese medicamento era marcado como `Completado`. El turno 190 nunca se marcó como completado y quedó en estado `Aprobado`.

```csharp
// ❌ CÓDIGO CON BUG (antes de la corrección)
var firstMedicineId = MedicineIds.Split(',').Select(int.Parse).First();
await CompleteTurnoIfExistsAsync(PatientIdentification, firstMedicineId, null);
// Solo se completaba el turno del primer medicamento
```

---

## Efectos secundarios del bug

1. **Turno 190 quedó en estado `Aprobado`** sin entrega vinculada.
2. **`TurnoCleanupService`** (se ejecuta cada hora) detectó el turno 190 como vencido sin asistencia y lo canceló automáticamente (`CanceladoPorNoPresentacion = true`).
3. El servicio **devolvió al stock las 28 unidades de Losartán 50mg** reservadas para ese turno (comportamiento correcto al cancelar, pero incorrecto dado que el medicamento ya había sido entregado físicamente) → el stock quedó **28 unidades inflado**.
4. Se enviaron **notificaciones falsas** de no-presentación al paciente y a los farmacéuticos.

---

## Corrección aplicada

### Código (`DeliveriesController.cs`)

El `turnoId` ahora se determina **dentro del loop, individualmente por cada medicamento/insumo**. Cada entrega obtiene el turno que realmente le corresponde:

```csharp
// ✅ CÓDIGO CORREGIDO
for (int i = 0; i < medicineIdsList.Count; i++)
{
    // Buscar el turno específico para ESTE medicamento
    var turnoInfo = await FindTurnoIdWithStateAsync(PatientIdentification, medicineIdsList[i], null);
    int? turnoId = turnoInfo.turnoId;
    bool stockYaReservado = turnoInfo.stockReservado;

    var delivery = new Delivery { TurnoId = turnoId, ... }; // ← turno correcto por item
}
```

El marcado de `Completado` también itera ahora sobre **todos** los medicamentos/insumos entregados:

```csharp
// ✅ CÓDIGO CORREGIDO
foreach (var medId in medicineIdsList)
{
    await CompleteTurnoIfExistsAsync(PatientIdentification, medId, null);
}
```

### Por qué no afecta el caso normal (un turno, varios medicamentos)

`FindTurnoIdWithStateAsync` busca el turno aprobado del paciente que **contenga ese medicamento específico y no haya sido entregado aún**. Si todos los medicamentos pertenecen al mismo turno (caso habitual), la búsqueda devolverá el mismo `turnoId` para cada uno. El comportamiento no cambia para ese escenario.

### Corrección de datos de producción (`fix-turnos-190-191.sql`)

Se aplicó el script SQL que:
1. Reasignó la entrega del Losartán 50mg de `TurnoId=191` → `TurnoId=190`
2. Limpió el comentario de cancelación automática del turno 190
3. Descontó las 28 unidades de Losartán 50mg del stock (que habían sido incorrectamente devueltas al cancelar el turno)
4. Cambió el turno 190 a `Estado=Completado`, `CanceladoPorNoPresentacion=0`

---

## Escenario que reproduce el bug (para pruebas de regresión)

1. Un paciente solicita dos turnos → cada uno con un medicamento distinto.
2. Ambos turnos son aprobados.
3. Se registran ambas entregas desde el formulario, una después de la otra.
4. **Resultado esperado (post-fix):** cada entrega queda vinculada a su turno correcto y ambos turnos pasan a `Completado`.
5. **Resultado antes del fix:** ambas entregas quedaban vinculadas al turno del primer medicamento procesado; el otro turno quedaba en `Aprobado` y era cancelado por el servicio de limpieza nocturno.
