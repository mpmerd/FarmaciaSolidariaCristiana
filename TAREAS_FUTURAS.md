# Tareas Futuras - Farmacia Solidaria Cristiana

## 📋 Pendientes

### 🏆 Alta Prioridad

#### 1. Entregas y Donaciones de Insumos
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
