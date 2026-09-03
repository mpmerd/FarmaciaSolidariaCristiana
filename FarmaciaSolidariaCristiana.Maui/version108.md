# Versión 1.0.8 - Mostrar quién aprobó/rechazó el turno

## Cambio

En la vista de gestión de turnos, para roles Admin y Farmacéutico, al hacer clic en "Ver detalles" sobre un turno en estado **Aprobado**, **Rechazado** o **Completado**, ahora se muestra al final del mensaje:

```
👤 Revisado por: {UserName del revisor}
```

Solo se muestra si hay un revisor registrado en la base de datos.

## Archivos modificados

### Backend (API)

- `FarmaciaSolidariaCristiana/Api/Models/TurnoDtos.cs`
  - Agregada propiedad `RevisadoPorNombre` al `TurnoDto`.

- `FarmaciaSolidariaCristiana/Api/Controllers/TurnosApiController.cs`
  - Agregado `.Include(t => t.RevisadoPor)` en todas las consultas que devuelven `TurnoDto`.
  - Mapeo de `t.RevisadoPor?.UserName` → `RevisadoPorNombre` en `MapToDto`.

### App MAUI

- `FarmaciaSolidariaCristiana.Maui/Models/Turno.cs`
  - Agregada propiedad `RevisadoPorNombre`.

- `FarmaciaSolidariaCristiana.Maui/ViewModels/TurnosViewModel.cs`
  - En `VerDetallesTurnoAsync`, se agrega la línea `Revisado por:` al final del mensaje cuando corresponde.
