# Changelog - Farmacia Solidaria Cristiana

## [27 de octubre de 2025] - Módulo de Insumos y Sistema Simétrico Completo

### ✨ Nuevas Funcionalidades

#### 📦 Módulo de Insumos Completo
- **Nuevo módulo completo** para gestión de insumos médicos (materiales no medicamentosos)
- **Funcionalidades CRUD**: Crear, listar, editar, eliminar insumos
- **Campos**:
  - Nombre del insumo
  - Descripción
  - Cantidad en stock
  - Unidad fija: "Unidades" (predeterminada)
- **Autorización**: Admin y Farmacéutico pueden crear/editar/eliminar
- **Búsqueda**: Filtro de búsqueda por nombre en la lista de insumos
- **Navegación**: Nueva opción de menú con icono de caja

#### 🎯 Entregas Mejoradas - Medicamentos E Insumos
- **Entregas flexibles**: Ahora se pueden hacer entregas de medicamentos O insumos
- **Selección por tipo**: Interfaz con botones para elegir entre medicamento o insumo
- **Control de stock**: Descuento automático del stock correspondiente (medicamentos o insumos)
- **Restauración de stock**: Al eliminar una entrega, se devuelve el stock correcto
- **Validación**: No permite seleccionar ambos tipos simultáneamente
- **Reportes actualizados**: Los reportes incluyen tanto entregas de medicamentos como de insumos
- **Filtros de búsqueda**: Búsqueda unificada por nombre de medicamento o insumo

#### 🎁 Donaciones Completas - Medicamentos E Insumos
- **Donaciones flexibles**: Ahora se pueden registrar donaciones de medicamentos O insumos
- **Selección por tipo**: Interfaz con botones para elegir entre medicamento o insumo
- **Control de stock**: Incremento automático del stock correspondiente (medicamentos o insumos)
- **Validación**: No permite seleccionar ambos tipos simultáneamente
- **Reportes actualizados**: Los reportes incluyen tanto donaciones de medicamentos como de insumos
- **Filtros de búsqueda**: Búsqueda unificada por nombre de medicamento o insumo
- **Vista mejorada**: Nueva columna "Tipo" con badges visuales (Medicamento/Insumo)

#### 🏆 Gestión de Patrocinadores (Solo Admin)
- **CRUD completo** para patrocinadores (solo accesible para Admin)
- **Subida de logos**:
  - Solo formato PNG permitido
  - Tamaño máximo: 2MB
  - Compresión automática a 400x400px
  - Validación de tipo de archivo y tamaño
- **Características**:
  - Activar/desactivar patrocinadores
  - Orden de visualización personalizable
  - Vista de gestión completa (Manage)
  - Vista pública para usuarios (Index)
- **Navegación**: Nueva opción "Patrocinadores" en menú (solo visible para Admin)
- **Optimización de imágenes**: Uso de IImageCompressionService para logos

### 🔧 Cambios Técnicos

#### Base de Datos
- **Nueva tabla**: `Supplies` (Id, Name, Description, StockQuantity, Unit)
- **Migración 4**: `20251027160229_AddSuppliesTable`
- **Migración 5**: `20251027164041_AddSupplyToDeliveries`
  - `Deliveries.MedicineId` ahora es nullable (permite NULL)
  - Nueva columna `Deliveries.SupplyId` nullable
  - Foreign Key a tabla Supplies
  - Índice en SupplyId para rendimiento
- **Migración 6**: `20251027171452_AddSupplyToDonations`
  - `Donations.MedicineId` ahora es nullable (permite NULL)
  - Nueva columna `Donations.SupplyId` nullable
  - Foreign Key a tabla Supplies
  - Índice en SupplyId para rendimiento
- **Script SQL actualizado**: apply-migration-somee.sql con las 3 nuevas migraciones

#### Modelos
- **Supply.cs**: Nuevo modelo para insumos con validaciones
- **Delivery.cs**: Actualizado para soportar MedicineId O SupplyId (ambos nullable)
- **Donation.cs**: Actualizado para soportar MedicineId O SupplyId (ambos nullable)
- **Sponsor.cs**: Modelo existente actualizado con gestión mejorada

#### Controladores
- **SuppliesController**: 
  - CRUD completo con autorización
  - Forzado de Unit="Unidades" en Create y Edit
  - Búsqueda por nombre
  - Ordenamiento alfabético
- **DeliveriesController**:
  - **Create**: Validación para seleccionar medicamento O insumo (no ambos)
  - **Create**: Descuento de stock para medicamentos O insumos
  - **Delete**: Restauración de stock correcto (medicamento o insumo)
  - **Index**: Búsqueda unificada por medicamento o insumo
  - Includes actualizados para cargar Medicine y Supply
- **DonationsController**:
  - **Create**: Validación para seleccionar medicamento O insumo (no ambos)
  - **Create**: Incremento de stock para medicamentos O insumos
  - **Index**: Búsqueda unificada por medicamento o insumo
  - Includes actualizados para cargar Medicine y Supply
- **SponsorsController**:
  - Validación PNG obligatoria
  - Compresión de imágenes con IImageCompressionService
  - Gestión de archivos (eliminar logo anterior)
  - Vista Manage para Admin, Index para público
- **ReportsController**: 
  - **DeliveriesPDF**: Incluye entregas de medicamentos e insumos con indicador de tipo
  - **DonationsPDF**: Incluye donaciones de medicamentos e insumos con indicador de tipo
  - **MonthlyPDF**: Inventario separado de Medicamentos e Insumos
  - ViewData con SupplyId para futuros filtros

#### Vistas
- **Supplies** (5 vistas):
  - Index.cshtml: Lista con búsqueda y badges de stock
  - Create.cshtml: Formulario con Unit readonly
  - Edit.cshtml: Edición con Unit fijo
  - Details.cshtml: Vista detallada
  - Delete.cshtml: Confirmación de eliminación
- **Deliveries**:
  - **Create.cshtml**: 
    - Selector de tipo (Medicamento/Insumo) con botones radio
    - JavaScript para alternar entre selects
    - Validación en cliente y servidor
  - **Index.cshtml**: 
    - Nueva columna "Tipo" con badge (Medicamento/Insumo)
    - Búsqueda unificada
    - Muestra nombre correcto según tipo
  - **Delete.cshtml**: 
    - Muestra tipo de entrega
    - Mensaje de confirmación dinámico según tipo
- **Donations**:
  - **Create.cshtml**: 
    - Selector de tipo (Medicamento/Insumo) con botones radio
    - JavaScript para alternar entre selects
    - Validación en cliente y servidor
  - **Index.cshtml**: 
    - Nueva columna "Tipo" con badge (Medicamento/Insumo)
    - Búsqueda unificada por medicamento o insumo
    - Muestra nombre correcto según tipo
- **Sponsors**:
  - Create.cshtml: Actualizado con validación PNG y tamaño
  - Edit.cshtml: Actualizado con validación PNG
  - Manage.cshtml: Vista existente para administración
- **Shared/_Layout.cshtml**: 
  - Nuevo enlace "Insumos" para todos los usuarios autenticados
  - Nuevo enlace "Patrocinadores" solo para Admin

### 📝 Documentación
- **apply-migration-somee.sql**: 
  - Actualizado con migración 4: AddSuppliesTable
  - Actualizado con migración 5: AddSupplyToDeliveries
  - Actualizado con migración 6: AddSupplyToDonations
  - Estadísticas ampliadas incluyendo Supplies, Entregas y Donaciones por tipo
  - **SEGURO**: Preserva todas las entregas y donaciones existentes con sus medicamentos
  - Verificaciones antes de ejecutar cada cambio
- **CHANGELOG.md**: Actualizado con todas las funcionalidades

### 🔒 Seguridad y Preservación de Datos
- **Datos preservados**: Medicamentos, Usuarios, Patrocinadores, Pacientes, **Entregas existentes**, **Donaciones existentes**
- **Entregas existentes**: Mantienen su MedicineId intacto (no se pierden)
- **Donaciones existentes**: Mantienen su MedicineId intacto (no se pierden)
- **Migraciones seguras**: Script SQL con verificaciones IF NOT EXISTS
- **Retrocompatibilidad**: Entregas y donaciones antiguas funcionan sin cambios
- **Validaciones**: Tipos de archivo y tamaños para uploads
- **Simetría del sistema**: Entregas y Donaciones siguen el mismo patrón para medicamentos e insumos

---

## [25 de octubre de 2025] - Control de Eliminación y Mejoras de UX

### ✨ Nuevas Funcionalidades

#### 🔒 Validaciones de Eliminación
- **Medicamentos**: No se pueden eliminar si tienen entregas o donaciones asociadas
- **Pacientes**: No se pueden eliminar si tienen entregas asignadas  
- **Entregas**: Solo se pueden eliminar dentro de 2 horas desde su creación
  - Se restaura automáticamente el stock al eliminar
  - Los registros antiguos (sin CreatedAt) usan DeliveryDate como referencia

#### 📋 Ordenamiento Alfabético
- **Entregas**: Lista ordenada alfabéticamente por nombre de medicamento
- **Donaciones**: Lista ordenada alfabéticamente por nombre de medicamento
- **Medicamentos**: Lista mantenida en orden alfabético
- **Dropdowns**: Todas las listas de selección ordenadas alfabéticamente

#### 📅 Validación de Fechas de Entrega
- Fecha de entrega no puede ser futura
- Fecha de entrega no puede ser mayor a 5 días en el pasado
  - Permite registrar entregas durante cortes de electricidad/internet

### 🔧 Cambios Técnicos

#### Base de Datos
- **Nueva columna**: `Deliveries.CreatedAt` (DATETIME2, nullable)
  - Preserva datos existentes (nullable)
  - Registros nuevos capturan fecha/hora exacta de creación
- **Migración**: `20251025212114_AddCreatedAtToDeliveries`

#### Scripts SQL
- **add-createdat-column.sql**: Script seguro para agregar columna CreatedAt en producción
- **clean-test-data-keep-medicines.sql**: Limpia datos de prueba preservando medicamentos reales
- **apply-migration-somee.sql**: Actualizado con todas las migraciones (3 en total)

#### Controladores
- **DeliveriesController**: 
  - Agregados métodos Delete GET/POST
  - Validación de ventana de 2 horas
  - Restauración automática de stock
  - Validación de fechas
  - Asignación automática de CreatedAt
- **MedicinesController**: Validación de entregas/donaciones antes de eliminar
- **PatientsController**: Validación de entregas antes de eliminar

#### Vistas
- **Deliveries/Index.cshtml**: 
  - Botones de eliminar con indicador de tiempo disponible
  - Icono de candado para entregas no eliminables
- **Deliveries/Delete.cshtml**: Nueva vista de confirmación con validación visual
- **Medicines/Index.cshtml**: Display de mensajes de error
- **Patients/Index.cshtml**: Display de mensajes de error

### 📝 Documentación
- **deploy-to-somee.sh**: Actualizado con información de las 3 migraciones
- **CHANGELOG.md**: Creado para seguimiento de cambios

### 🔄 Proceso de Despliegue Actualizado

```bash
# Para nuevos despliegues o con cambios en BD:
1. Panel Somee → SQL Manager → Ejecutar apply-migration-somee.sql
2. Terminal → ./deploy-to-somee.sh

# Para limpiar datos de prueba (preserva medicamentos):
1. Panel Somee → SQL Manager → Ejecutar clean-test-data-keep-medicines.sql
```

### 🐛 Correcciones
- SQL Server compilation errors en scripts con variables declaradas incorrectamente
- Uso de `GO` para separar batches y evitar errores de compilación

---

## Versiones Anteriores

### [23 de octubre de 2025] - Sistema de Identificación de Pacientes
- Campo de identificación obligatorio para pacientes
- Campos mejorados en entregas (LocationDetails, Observations)
- Migración `20251023213325_AddPatientIdentificationRequired`
- Migración `20251023225202_AddDeliveryFieldsEnhancement`

### [20-22 de octubre de 2025] - Versión Inicial
- Sistema CRUD completo para medicamentos, pacientes, entregas y donaciones
- Integración con CIMA API para medicamentos españoles
- Sistema de usuarios con roles (Admin, Farmaceutico, Viewer)
- Generación de reportes PDF
- Sistema de patrocinadores
