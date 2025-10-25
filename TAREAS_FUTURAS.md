# Tareas Futuras - Farmacia Solidaria Cristiana

## 📋 Pendientes

### 🏆 Alta Prioridad

#### 1. CRUD de Patrocinadores (Solo Admin)
**Descripción**: Implementar funcionalidad completa para gestionar patrocinadores

**Requisitos**:
- Solo usuarios con rol `Admin` pueden acceder
- Funcionalidades:
  - ✅ Ver lista de patrocinadores (ya existe en Home)
  - ➕ Crear nuevo patrocinador
  - ✏️ Editar patrocinador existente
  - 🗑️ Eliminar patrocinador
  - 🔄 Activar/Desactivar patrocinador

**Campos del Modelo** (ya existe en `Models/Sponsor.cs`):
- Nombre del patrocinador (string)
- Descripción (string, opcional)
- Logo (IFormFile → guardar como PNG en `/wwwroot/images/sponsors/`)
- DisplayOrder (int) - Orden de visualización
- IsActive (bool) - Si se muestra en la página principal
- CreatedDate (DateTime)

**Tareas Técnicas**:
1. Crear `SponsorsController.cs` con acciones CRUD
   - `[Authorize(Roles = "Admin")]` en todas las acciones
2. Crear vistas:
   - `Views/Sponsors/Index.cshtml` - Lista con tabla de patrocinadores
   - `Views/Sponsors/Create.cshtml` - Formulario para nuevo patrocinador
   - `Views/Sponsors/Edit.cshtml` - Formulario de edición
   - `Views/Sponsors/Delete.cshtml` - Confirmación de eliminación
   - `Views/Sponsors/Details.cshtml` (opcional) - Vista detallada
3. Manejo de imágenes:
   - Upload de archivo PNG
   - Validación de tipo de archivo (solo PNG)
   - Validación de tamaño (máximo 2MB)
   - Redimensionar imagen si es necesario (usar `IImageCompressionService`)
   - Guardar en `/wwwroot/images/sponsors/`
   - Nombrar archivo: `{nombre-patrocinador}.png`
   - Al editar logo: eliminar logo anterior si existe
   - Al eliminar patrocinador: eliminar logo del disco
4. Validaciones:
   - Nombre requerido (máximo 100 caracteres)
   - Descripción opcional (máximo 500 caracteres)
   - Logo requerido al crear, opcional al editar
   - DisplayOrder único (no duplicados)
   - Al desactivar, ocultar en Home pero preservar en BD
5. Agregar enlace en menú de navegación (solo visible para Admin):
   ```html
   @if (User.IsInRole("Admin"))
   {
       <li class="nav-item">
           <a class="nav-link" asp-controller="Sponsors" asp-action="Index">
               <i class="bi bi-award"></i> Patrocinadores
           </a>
       </li>
   }
   ```

**Consideraciones**:
- Los patrocinadores actuales ya están sembrados en `DbInitializer.cs`
- La vista `Home/Index.cshtml` ya muestra los patrocinadores activos
- NO usar soft delete, usar campo `IsActive` para activar/desactivar
- Ordenar por `DisplayOrder` en la vista pública

**Estimación**: 4-6 horas

---

### 🔜 Media Prioridad

#### 2. Filtros Avanzados en Reportes
- Filtrar por rango de fechas más amplio
- Filtrar por tipo de medicamento
- Exportar a Excel además de PDF

#### 3. Dashboard con Estadísticas
- Medicamentos con stock bajo (alertas)
- Total de entregas por mes (gráfico)
- Medicamentos más entregados (top 10)
- Pacientes activos vs totales

#### 4. Sistema de Notificaciones
- Email cuando stock llegue a mínimo
- Email de confirmación al registrar entrega
- Recordatorios de renovación de tratamiento

#### 5. Mejorar Búsqueda de Pacientes
- Búsqueda por nombre además de identificación
- Autocompletar en campo de búsqueda
- Mostrar foto del paciente en resultados (opcional)

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

### Versión 1.1 (Próxima)
- ✅ Control de eliminación con validaciones
- ✅ Ordenamiento alfabético
- ✅ Validación de fechas de entrega
- 🔄 CRUD de Patrocinadores (En planificación)

### Versión 1.2 (Futuro)
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
