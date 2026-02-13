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

### 1. `/api/medicines` - PRIORIDAD ALTA

**Problema:** Tiempo promedio de 14 segundos (inaceptable)

**Posibles causas:**
- Falta de índices en la tabla Medicines
- Carga de datos relacionados sin necesidad
- Paginación ineficiente
- Consultas N+1

**Plan de acción:**
1. [ ] Revisar índices en BD (campos: Name, IsActive, CategoryId)
2. [ ] Implementar caché de listados (5-10 min)
3. [ ] Verificar uso de AsNoTracking()
4. [ ] Revisar includes innecesarios
5. [ ] Implementar paginación correcta si no existe
6. [ ] Considerar endpoint separado para búsqueda vs listado completo

**Script SQL para verificar índices:**
```sql
-- Verificar índices existentes
SELECT 
    i.name AS IndexName,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE OBJECT_NAME(i.object_id) = 'Medicines'

-- Crear índices recomendados si no existen
CREATE INDEX IX_Medicines_IsActive ON Medicines(IsActive);
CREATE INDEX IX_Medicines_Name ON Medicines(Name);
CREATE INDEX IX_Medicines_CategoryId ON Medicines(CategoryId);
```

---

### 2. `/api/turns/my` - PRIORIDAD MEDIA

**Problema:** Llamado frecuentemente, puede beneficiarse de caché

**Plan de acción:**
1. [ ] Implementar caché por usuario (2-3 min)
2. [ ] Invalidar caché al crear/modificar turno
3. [ ] Verificar consultas optimizadas

---

### 3. `/api/notifications/pending` - PRIORIDAD MEDIA

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

| Endpoint | Tiempo Actual | Objetivo |
|----------|---------------|----------|
| /api/navbar-decoration/active | ~500ms | <50ms ✅ |
| /api/medicines | 14s | <1s |
| /api/turns/my | ? | <500ms |
| /api/notifications/pending | ? | <200ms |

---

## Próximos Pasos

1. **Inmediato:** Monitorear mejora de navbar-decoration después del despliegue
2. **Esta semana:** Analizar y optimizar /api/medicines
3. **Siguiente:** Implementar caché en endpoints de turnos y notificaciones
4. **Futuro:** Considerar Redis para caché distribuida si escala

---

## Notas

- El endpoint `/api/navbarbar-decoration/active` no es usado por la app MAUI, solo por la web MVC
- La app MAUI tiene sus propios patrones de llamada a la API
- Cualquier cambio de caché requiere pruebas de invalidación

---

*Documento generado para planificación de optimizaciones de rendimiento*
