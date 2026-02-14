# Plan de Optimización de API - Farmacia Solidaria Cristiana

**Fecha:** 13 de febrero de 2026  
**Estado:** En progreso

---

## Resumen Ejecutivo

Análisis de rendimiento de la API basado en logs del servidor. Se identificaron endpoints con alto uso y tiempos de respuesta elevados que requieren optimización.

---

## Endpoints Analizados

### ✅ COMPLETADO: `/api/navbar-decoration/active`

| Métrica | Antes | Después |
|---------|-------|---------|
| Llamadas | ~1000/hora | ~12/hora (estimado) |
| Consultas BD | 1 por request | 1 cada 5 min (caché) |
| Tiempo respuesta | Variable | <50ms (desde caché) |

**Optimizaciones aplicadas:**
- Caché en memoria (IMemoryCache) por 5 minutos
- Headers HTTP Cache-Control (300s)
- AsNoTracking() en consultas EF Core
- Intervalo de polling aumentado de 30s a 5 min
- Invalidación automática al cambiar decoración

---

## 🔴 PENDIENTE: Endpoints a Optimizar

### 1. `/api/medicines` - ✅ COMPLETADO

**Problema original:** Tiempo promedio de 14 segundos

**Solución implementada:**
- [x] Caché en memoria (IMemoryCache) por 2 minutos
- [x] Headers HTTP Cache-Control (60s)
- [x] AsNoTracking() en consultas EF Core
- [x] Invalidación automática en Create/Update/Delete

---

### 2. `/api/supplies` - ✅ COMPLETADO

**Problema original:** Similar a medicines, consultas lentas

**Solución implementada:**
- [x] Caché en memoria (IMemoryCache) por 2 minutos
- [x] Headers HTTP Cache-Control (60s)
- [x] AsNoTracking() en consultas EF Core
- [x] Invalidación automática en Create/Update/Delete

---

### 3. `/api/turns/my` - PRIORIDAD MEDIA (PENDIENTE)

**Problema:** Llamado frecuentemente, puede beneficiarse de caché

**Plan de acción:**
1. [ ] Implementar caché por usuario (2-3 min)
2. [ ] Invalidar caché al crear/modificar turno
3. [ ] Verificar consultas optimizadas

---

### 4. `/api/notifications/pending` - PRIORIDAD MEDIA (PENDIENTE)

**Problema:** Polling frecuente desde la app

**Plan de acción:**
1. [ ] Implementar caché corta (30-60s)
2. [ ] Evaluar ETag para respuestas condicionales
3. [ ] Considerar WebSockets para notificaciones real-time (futuro)

---

## Estrategias Generales de Optimización

### A. Caché en Servidor
```csharp
// Patrón recomendado
if (!_cache.TryGetValue(cacheKey, out var result))
{
    result = await _context.Items.AsNoTracking().ToListAsync();
    _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
}
return Ok(result);
```

### B. Headers HTTP Cache-Control
```csharp
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
```

### C. Índices de Base de Datos
- Crear índices en columnas usadas en WHERE/ORDER BY
- Verificar fragmentación periódicamente

### D. Consultas Optimizadas
- Usar `AsNoTracking()` para lecturas
- Evitar `Include()` innecesarios
- Proyectar solo campos necesarios con `Select()`

---

## Métricas Objetivo

| Endpoint | Tiempo Anterior | Objetivo | Estado |
|----------|-----------------|----------|--------|
| /api/navbar-decoration/active | ~500ms | <50ms | ✅ Completado |
| /api/medicines | 14s | <1s | ✅ Completado (caché 2min) |
| /api/supplies | ~5s | <500ms | ✅ Completado (caché 2min) |
| /api/turns/my | ? | <500ms | ⏳ Pendiente |
| /api/notifications/pending | ? | <200ms | ⏳ Pendiente |

---

## Próximos Pasos

1. ✅ **Completado:** Optimización navbar-decoration (caché 5min)
2. ✅ **Completado:** Optimización medicines (caché 2min + HTTP cache 60s)
3. ✅ **Completado:** Optimización supplies (caché 2min + HTTP cache 60s)
4. **Pendiente:** Implementar caché en endpoints de turnos por usuario
5. **Pendiente:** Optimizar notificaciones pending (caché corta por usuario)
6. **Futuro:** Considerar Redis para caché distribuida si escala

---

## Notas

- El endpoint `/api/navbarbar-decoration/active` no es usado por la app MAUI, solo por la web MVC
- La app MAUI tiene sus propios patrones de llamada a la API
- Cualquier cambio de caché requiere pruebas de invalidación

---

*Documento generado para planificación de optimizaciones de rendimiento*
