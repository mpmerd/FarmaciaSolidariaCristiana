# Guía: Actualización de Stock de 1 a 0 (Solo Medicamentos)

**Fecha:** 15 de diciembre de 2025  
**Objetivo:** Actualizar todas las cantidades de medicamentos que estén en 1 y llevarlas a 0.

## ⚠️ PRECAUCIONES IMPORTANTES

Este proceso modificará datos en la base de datos de **PRODUCCIÓN**. Sigue estos pasos cuidadosamente:

1. ✅ **Hacer backup completo** de la base de datos antes de empezar
2. ✅ **Ejecutar en horario de bajo tráfico** (si es posible)
3. ✅ **Revisar todos los datos** antes de confirmar cambios
4. ✅ **El script tiene mecanismos de seguridad** incluidos

## 📋 Pasos para Ejecutar

### Paso 1: Backup de la Base de Datos

Antes de hacer cualquier cambio, crea un backup completo:

```bash
# Si usas SQL Server en Linux/Docker
# Ajusta los valores según tu configuración
docker exec -it nombre_contenedor /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U SA -P 'TuPassword' \
  -Q "BACKUP DATABASE FarmaciaDb TO DISK = '/var/opt/mssql/backup/FarmaciaDb_$(date +%Y%m%d_%H%M%S).bak'"
```

O desde SQL Server Management Studio:
- Click derecho en la base de datos → Tasks → Back Up...
- Guarda el backup en un lugar seguro

### Paso 2: Ejecutar el Script de Consulta

Abre el archivo `update-stock-one-to-zero.sql` en tu herramienta SQL preferida (SQL Server Management Studio, Azure Data Studio, etc.)

**Primera ejecución - Solo consulta:**

1. Ejecuta el script **tal como está** (la sección de actualización está comentada)
2. Esto te mostrará:
   - Todos los medicamentos con stock = 1
   - Total de registros que serán afectados
3. **REVISA CUIDADOSAMENTE** los datos mostrados

### Paso 3: Ejecutar la Actualización (Modo Prueba)

Si los datos del backup se ven correctos:

1. En el archivo `update-stock-one-to-zero.sql`, **descomenta** la sección `PARTE 2` (líneas 68-148)
   - Elimina `/*` de la línea 68
   - Elimina `*/` de la línea 148

2. Ejecuta el script nuevamente

3. El script ejecutará la actualización en modo **ROLLBACK** (sin guardar cambios)
   - Esto te permite ver cómo quedarían los datos
   - Los cambios NO se guardan aún

4. **REVISA** los resultados mostrados:
   - Cantidad de medicamentos actualizados
   - Estado final de los registros (deben mostrar 0)

### Paso 4: Confirmar los Cambios (Solo si todo está correcto)

Si después de revisar el paso 3 todo se ve bien:

1. En la línea 111 del script, **comenta** la línea:
   ```sql
   -- ROLLBACK TRANSACTION;
   ```

2. En la línea 108, **descomenta** la línea:
   ```sql
   COMMIT TRANSACTION;
   ```

3. Ejecuta el script **por última vez**

4. Esta vez los cambios **SÍ se guardarán** en la base de datos

### Paso 5: Verificación Final

Después de confirmar los cambios, verifica manualmente:

```sql
-- Verificar que no queden registros con stock = 1
SELECT 'Medicines' AS Tabla, COUNT(*) AS Stock_En_1
FROM Medicines WHERE StockQuantity = 1;

-- Debe devolver:
-- Tabla       Stock_En_1
-- Medicines   0
```

## 🔄 Cómo Revertir los Cambios (Si es necesario)

Si necesitas revertir los cambios después de confirmarlos:

### Opción 1: Restaurar el Backup

La forma más segura es restaurar el backup completo que hiciste en el Paso 1.

### Opción 2: Script de Reversión Manual

Si guardaste los resultados de la **PARTE 1** del script (el backup), puedes crear manualmente los UPDATE statements. Por ejemplo:

```sql
-- Ejemplo (ajusta según tus datos reales)
UPDATE Medicines SET StockQuantity = 1 WHERE Id = 5;
UPDATE Medicines SET StockQuantity = 1 WHERE Id = 12;
```

## 📊 Qué Tablas se Modifican

El script actualiza:

1. **Tabla `Medicines`**: Medicamentos con `StockQuantity = 1` → `StockQuantity = 0`

**No se modifican:**
- Insumos (Supplies)
- Usuarios
- Turnos
- Entregas (Deliveries)
- Donaciones
- Pacientes
- Documentos
- Ninguna otra tabla

## ⚙️ Características de Seguridad del Script

✅ **Transacciones**: Usa BEGIN TRANSACTION para poder revertir si hay errores  
✅ **Try-Catch**: Manejo de errores automático  
✅ **ROLLBACK por defecto**: Los cambios no se guardan hasta que tú lo confirmes  
✅ **Backup integrado**: Muestra todos los datos antes de modificar  
✅ **Verificación**: Muestra el estado antes y después  

## 🆘 Soporte

Si tienes dudas durante el proceso:

1. **NO continúes** si algo no se ve bien
2. Consulta los logs mostrados por el script
3. Verifica que tienes el backup completo
4. En caso de duda, contacta soporte técnico

## 📝 Registro de Ejecución

Después de ejecutar, documenta:

- ✅ Fecha y hora de ejecución
- ✅ Cantidad de medicamentos actualizados
- ✅ Ubicación del backup
- ✅ Cualquier problema encontrado

---

**Última actualización:** 15 de diciembre de 2025
