# Plan final: Endurecimiento MAUI de la restricción de medicamento duplicado

## Contexto y diagnóstico (cerrado)

- **Caso 4654/4656**: no hubo bug de código. La misma cuenta usó dos números de documento distintos (mismo paciente, documento erróneo en 4656). La restricción de medicamento duplicado es por documento (paciente), por decisión de negocio explícita. Detección de "mismo paciente con documento diferente": NO es automatizable; queda cubierta por los controles operativos existentes (revisión visual del farmacéutico y verificación de ficha/documento oficial al entregar).
- **Límite 2 turnos/mes**: ya está implementado tanto por usuario como por paciente (`CanUserRequestTurnoAsync` TurnoService.cs:55-78 + `CanPatientRequestTurnoAsync` l.84-122, ambos invocados en API l.367+374 y MVC l.86+199). Sin cambios.
- **Alcance aprobado**: solo endurecimiento MAUI.

## Cambios (solo `FarmaciaSolidariaCristiana.Maui/ViewModels/SolicitarTurnoViewModel.cs`)

### 1. Re-validación en `SubmitAsync` (l.505+)
Antes de construir el `CrearTurnoMobileRequest` (tras la confirmación del usuario), si `IsMedicamento`:
- **Garantizar que `_restrictedMedicineIds` corresponde al documento que se va a enviar**: si `_lastCheckedDocument != DocumentoIdentidad.Trim().ToUpper()` o la carga previa falló, recargar `LoadRestrictedMedicinesAsync()` para el documento actual y esperar el resultado. (Nota: esto NO aplica la restricción de otro paciente — al contrario, evita validar contra una lista obsoleta de un documento previo editado, o contra una lista vacía por la race del fire-and-forget de l.92.)
- Con la lista correcta cargada, validar `ItemsSeleccionados`: si hay duplicados, mostrar error con los nombres (mismo mensaje que `SelectItem` l.307-313) y **abortar el envío**.

### 2. Reintento ante fallo de carga (`LoadRestrictedMedicinesAsync`, l.151-189)
- `_lastCheckedDocument = doc;` solo se fija cuando la carga fue exitosa. Si `GetRestrictedMedicinesAsync` falla (catch) o `result.Success == false`, NO fijar `_lastCheckedDocument` → cambios futuros del documento o el reintento del submit podrán reintentar la carga.
- Notificar al usuario que no se pudo verificar la lista de medicamentos ya solicitados este mes (el backend sigue protegiendo al guardar).

### Detalles de implementación
- Mantener normalización `Trim().ToUpper()` consistente con el backend.
- No cambiar contratos de API, ni Web MVC, ni backend.
- Mensajes en español coherentes con los existentes.

## Verificación
- `dotnet build` del proyecto MAUI.
- Pruebas manuales: (a) flujo normal de primer turno; (b) pegar documento y enviar rápidamente (race) → la re-validación del submit recarga la lista del documento actual y bloquea duplicados; (c) editar el documento tras cargar restricciones y enviar → se valida contra la lista del documento final, no la del anterior; (d) simular fallo de red en `restricted-medicines` → se permite reintento y se informa al usuario; (e) segundo turno con medicamento ya aprobado (mismo documento) → error claro sin envío.
