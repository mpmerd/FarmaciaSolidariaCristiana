# Consultas SQL para Reportes de la Farmacia

Consultas útiles para copiar y ejecutar directamente en la consola SQL de la base de datos.

---

## 1. Cantidad de medicamentos con stock mayor que cero

```sql
SELECT COUNT(*) AS MedicamentosConStock
FROM Medicines
WHERE StockQuantity > 0;
```

---

## 2. Cantidad de insumos con stock mayor que cero

```sql
SELECT COUNT(*) AS InsumosConStock
FROM Supplies
WHERE StockQuantity > 0;
```

---

## 3. Cantidad de pacientes registrados

```sql
SELECT COUNT(*) AS TotalPacientes
FROM Patients;
```

---

## 4. Cantidad de entregas de medicamentos por paciente por mes

> Muestra cuántas entregas de medicamentos se hicieron y a cuántos pacientes distintos, agrupado por año y mes.

```sql
SELECT
    YEAR(d.DeliveryDate)  AS Anio,
    MONTH(d.DeliveryDate) AS Mes,
    COUNT(*)              AS TotalEntregasMedicamentos,
    COUNT(DISTINCT d.PatientId) AS PacientesDistintos
FROM Deliveries d
WHERE d.MedicineId IS NOT NULL
GROUP BY YEAR(d.DeliveryDate), MONTH(d.DeliveryDate)
ORDER BY Anio, Mes;
```

---

## 5. Cantidad de entregas de insumos por paciente por mes

> Muestra cuántas entregas de insumos se hicieron y a cuántos pacientes distintos, agrupado por año y mes.

```sql
SELECT
    YEAR(d.DeliveryDate)  AS Anio,
    MONTH(d.DeliveryDate) AS Mes,
    COUNT(*)              AS TotalEntregasInsumos,
    COUNT(DISTINCT d.PatientId) AS PacientesDistintos
FROM Deliveries d
WHERE d.SupplyId IS NOT NULL
GROUP BY YEAR(d.DeliveryDate), MONTH(d.DeliveryDate)
ORDER BY Anio, Mes;
```

---

## 6. Resumen combinado: entregas de medicamentos e insumos por mes

> Vista unificada con medicamentos e insumos en la misma tabla.

```sql
SELECT
    YEAR(d.DeliveryDate)  AS Anio,
    MONTH(d.DeliveryDate) AS Mes,
    SUM(CASE WHEN d.MedicineId IS NOT NULL THEN 1 ELSE 0 END) AS EntregasMedicamentos,
    COUNT(DISTINCT CASE WHEN d.MedicineId IS NOT NULL THEN d.PatientId END) AS PacientesMedicamentos,
    SUM(CASE WHEN d.SupplyId IS NOT NULL THEN 1 ELSE 0 END) AS EntregasInsumos,
    COUNT(DISTINCT CASE WHEN d.SupplyId IS NOT NULL THEN d.PatientId END) AS PacientesInsumos
FROM Deliveries d
GROUP BY YEAR(d.DeliveryDate), MONTH(d.DeliveryDate)
ORDER BY Anio, Mes;
```

---

## 7. Cantidad de turnos de medicamentos completados por mes

> Turnos que tienen al menos un medicamento solicitado, con estado "Completado".

```sql
SELECT
    YEAR(t.FechaEntrega)  AS Anio,
    MONTH(t.FechaEntrega) AS Mes,
    COUNT(*)              AS TurnosCompletados
FROM Turnos t
WHERE t.Estado = 'Completado'
  AND EXISTS (SELECT 1 FROM TurnoMedicamentos tm WHERE tm.TurnoId = t.Id)
GROUP BY YEAR(t.FechaEntrega), MONTH(t.FechaEntrega)
ORDER BY Anio, Mes;
```

---

## 8. Cantidad de turnos de insumos completados por mes

> Turnos que tienen al menos un insumo solicitado, con estado "Completado".

```sql
SELECT
    YEAR(t.FechaEntrega)  AS Anio,
    MONTH(t.FechaEntrega) AS Mes,
    COUNT(*)              AS TurnosCompletados
FROM Turnos t
WHERE t.Estado = 'Completado'
  AND EXISTS (SELECT 1 FROM TurnoInsumos ti WHERE ti.TurnoId = t.Id)
GROUP BY YEAR(t.FechaEntrega), MONTH(t.FechaEntrega)
ORDER BY Anio, Mes;
```

---

## 9. Resumen combinado: turnos completados de medicamentos e insumos por mes

```sql
SELECT
    YEAR(t.FechaEntrega)  AS Anio,
    MONTH(t.FechaEntrega) AS Mes,
    SUM(CASE WHEN EXISTS (SELECT 1 FROM TurnoMedicamentos tm WHERE tm.TurnoId = t.Id) THEN 1 ELSE 0 END) AS TurnosMedicamentos,
    SUM(CASE WHEN EXISTS (SELECT 1 FROM TurnoInsumos ti WHERE ti.TurnoId = t.Id) THEN 1 ELSE 0 END) AS TurnosInsumos
FROM Turnos t
WHERE t.Estado = 'Completado'
GROUP BY YEAR(t.FechaEntrega), MONTH(t.FechaEntrega)
ORDER BY Anio, Mes;
```

---

## 10. Entregas de medicamentos SIN turno por mes

> Entregas de medicamentos que se realizaron sin un turno asociado (entregas directas).

```sql
SELECT
    YEAR(d.DeliveryDate)  AS Anio,
    MONTH(d.DeliveryDate) AS Mes,
    COUNT(*)              AS EntregasSinTurno,
    COUNT(DISTINCT d.PatientId) AS PacientesDistintos
FROM Deliveries d
WHERE d.MedicineId IS NOT NULL
  AND d.TurnoId IS NULL
GROUP BY YEAR(d.DeliveryDate), MONTH(d.DeliveryDate)
ORDER BY Anio, Mes;
```

---

## 11. Entregas de insumos SIN turno por mes

> Entregas de insumos que se realizaron sin un turno asociado (entregas directas).

```sql
SELECT
    YEAR(d.DeliveryDate)  AS Anio,
    MONTH(d.DeliveryDate) AS Mes,
    COUNT(*)              AS EntregasSinTurno,
    COUNT(DISTINCT d.PatientId) AS PacientesDistintos
FROM Deliveries d
WHERE d.SupplyId IS NOT NULL
  AND d.TurnoId IS NULL
GROUP BY YEAR(d.DeliveryDate), MONTH(d.DeliveryDate)
ORDER BY Anio, Mes;
```

---

## 12. Resumen combinado: entregas sin turno de medicamentos e insumos por mes

```sql
SELECT
    YEAR(d.DeliveryDate)  AS Anio,
    MONTH(d.DeliveryDate) AS Mes,
    SUM(CASE WHEN d.MedicineId IS NOT NULL AND d.TurnoId IS NULL THEN 1 ELSE 0 END) AS MedicamentosSinTurno,
    COUNT(DISTINCT CASE WHEN d.MedicineId IS NOT NULL AND d.TurnoId IS NULL THEN d.PatientId END) AS PacientesMedicamentos,
    SUM(CASE WHEN d.SupplyId IS NOT NULL AND d.TurnoId IS NULL THEN 1 ELSE 0 END) AS InsumosSinTurno,
    COUNT(DISTINCT CASE WHEN d.SupplyId IS NOT NULL AND d.TurnoId IS NULL THEN d.PatientId END) AS PacientesInsumos
FROM Deliveries d
WHERE d.TurnoId IS NULL
GROUP BY YEAR(d.DeliveryDate), MONTH(d.DeliveryDate)
ORDER BY Anio, Mes;
```
