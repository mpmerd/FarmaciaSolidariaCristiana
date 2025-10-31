# Tareas Futuras - Farmacia Solidaria Cristiana

## 📋 Pendientes

### 🏆 Alta Prioridad

#### 1. Solicitud de Turnos para Medicamentos e Insumos
**Descripción**: Permitir solicitar turnos no solo para medicamentos sino también para insumos médicos

**Contexto**:
- Actualmente el sistema de turnos solo permite solicitar medicamentos
- Los pacientes pueden necesitar recoger tanto medicamentos como insumos médicos
- La implementación debe ser similar a las entregas, que ya soportan ambos tipos
- Mantener el sistema de autocompletado (CIMA API para medicamentos, búsqueda local para insumos)

**Requisitos Funcionales**:
- El formulario debe tener dos pestañas/opciones: "Medicamento" e "Insumo"
- Al seleccionar Medicamento: usar autocompletado con CIMA API (ya implementado)
- Al seleccionar Insumo: usar autocompletado local desde la base de datos de insumos
- El PDF del turno debe mostrar claramente si es para medicamento o insumo
- Los farmacéuticos deben ver en las notificaciones qué tipo de turno se solicitó

**Tareas Técnicas**:
1. Actualizar modelo `Turno`:
   - Hacer `MedicamentoCIMA` nullable (actualmente es string)
   - Agregar `SupplyId` nullable (int?)
   - Agregar propiedad de navegación `Supply`
   - Validar: debe tener medicamento O insumo (no ambos, no ninguno)

2. Crear migración `AddSupplyToTurnos`:
   ```sql
   ALTER TABLE Turnos ADD SupplyId INT NULL;
   ALTER TABLE Turnos 
       ADD CONSTRAINT FK_Turnos_Supplies_SupplyId 
       FOREIGN KEY (SupplyId) REFERENCES Supplies (Id);
   CREATE INDEX IX_Turnos_SupplyId ON Turnos(SupplyId);
   ```

3. Actualizar `TurnosController`:
   - Método `RequestForm` GET: pasar `ViewBag.SupplyId` con lista de insumos disponibles
   - Método `RequestForm` POST: 
     * Validar medicamento XOR insumo
     * Si es insumo, validar que existe y tiene stock
   - Actualizar lógica de aprobación para manejar ambos tipos

4. Actualizar `TurnoService.cs`:
   - Método `GenerateTurnoPdfAsync()`: 
     * Agregar sección que muestre "Medicamento" o "Insumo"
     * Mostrar nombre del medicamento (de CIMA) o nombre del insumo (de BD)
   - Método `GetTurnoDetailsAsync()`: incluir información del insumo si aplica

5. Actualizar vista `Views/Turnos/RequestForm.cshtml`:
   - Agregar selector de tipo (radio buttons o pestañas) similar a `Deliveries/Create.cshtml`:
     ```html
     <div class="btn-group w-100" role="group">
         <input type="radio" class="btn-check" name="turnoType" id="medicineType" value="medicine" checked>
         <label class="btn btn-outline-primary" for="medicineType">
             <i class="bi bi-capsule"></i> Medicamento
         </label>
         <input type="radio" class="btn-check" name="turnoType" id="supplyType" value="supply">
         <label class="btn btn-outline-info" for="supplyType">
             <i class="bi bi-box-seam"></i> Insumo
         </label>
     </div>
     ```
   - Mantener el campo de autocompletado de medicamentos (CIMA API)
   - Agregar nuevo campo de autocompletado para insumos:
     ```html
     <div id="supplySelect" style="display: none;">
         <label class="form-label">Insumo</label>
         <select class="form-select" id="SupplyId" name="SupplyId">
             <option value="">-- Seleccione un Insumo --</option>
         </select>
     </div>
     ```
   - JavaScript para alternar entre medicamento e insumo:
     ```javascript
     document.querySelectorAll('input[name="turnoType"]').forEach(radio => {
         radio.addEventListener('change', (e) => {
             if (e.target.value === 'medicine') {
                 document.getElementById('medicineAutocomplete').style.display = 'block';
                 document.getElementById('supplySelect').style.display = 'none';
             } else {
                 document.getElementById('medicineAutocomplete').style.display = 'none';
                 document.getElementById('supplySelect').style.display = 'block';
             }
         });
     });
     ```

6. Actualizar vistas de listado y detalles:
   - `Views/Turnos/Index.cshtml`: mostrar columna "Tipo" (Medicamento/Insumo)
   - `Views/Turnos/Details.cshtml`: mostrar nombre de medicamento o insumo según corresponda
   - `Views/Turnos/ManageTurnos.cshtml`: agregar filtro por tipo

7. Crear API endpoint para autocompletado de insumos:
   - Agregar método en `TurnosController` o crear `SuppliesApiController`:
     ```csharp
     [HttpGet("api/supplies/search")]
     public async Task<IActionResult> SearchSupplies(string term)
     {
         var supplies = await _context.Supplies
             .Where(s => s.Name.Contains(term) && s.Quantity > 0)
             .OrderBy(s => s.Name)
             .Select(s => new { 
                 id = s.Id, 
                 name = s.Name, 
                 quantity = s.Quantity 
             })
             .Take(10)
             .ToListAsync();
         return Json(supplies);
     }
     ```

8. Actualizar `EmailService.cs`:
   - Método `SendTurnoApprovalEmailAsync()`: incluir en el cuerpo si es medicamento o insumo
   - Método `SendTurnoNotificationToFarmaceuticosAsync()`: especificar tipo en notificación

9. Actualizar `apply-migration-somee.sql`:
   - Agregar la migración `AddSupplyToTurnos` al final del script
   - Actualizar comentarios con fecha de la migración

10. Actualizar documentación:
    - `TURNOS_SYSTEM.md`: documentar nueva funcionalidad de insumos
    - `CHANGELOG.md`: agregar entrada para esta mejora

**Referencia de implementación**: 
- Ver `Views/Deliveries/Create.cshtml` (líneas 37-66) para el patrón de pestañas Medicamento/Insumo
- Ver `DeliveriesController.cs` para la validación de medicamento XOR insumo
- El autocompletado de medicamentos ya existe en `RequestForm.cshtml` usando CIMA API

**Estimación**: 4-5 horas

**Prioridad**: Alta - mejora significativa en la usabilidad del sistema de turnos

---

#### 2. Entregas y Donaciones de Insumos
**Descripción**: Extender el sistema de donaciones para incluir insumos, similar al sistema de entregas

**Requisitos**:
- Las donaciones actualmente solo soportan medicamentos
- Necesitan soportar también insumos médicos
- Similar a como se implementó para entregas

**Tareas Técnicas**:
1. Actualizar modelo `Donation`:
   - Hacer `MedicineId` nullable
   - Agregar `SupplyId` nullable
   - Agregar relación con `Supply`
2. Actualizar `DonationsController`:
   - Validar medicamento O insumo (no ambos)
   - Actualizar stock correcto según tipo
3. Actualizar vistas de donaciones:
   - Selector de tipo (Medicamento/Insumo)
   - Mostrar tipo en lista
4. Crear migración `AddSupplyToDonations`
5. Actualizar `apply-migration-somee.sql`

**Estimación**: 2-3 horas

---

### 🔜 Media Prioridad

#### 2. Filtros Avanzados en Reportes
- Filtrar entregas y donaciones por tipo (Medicamento/Insumo)
- Filtrar por rango de fechas más amplio
- Filtrar por medicamento o insumo específico
- Exportar a Excel además de PDF

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
