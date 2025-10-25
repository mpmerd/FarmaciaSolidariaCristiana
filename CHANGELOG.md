# Changelog - Farmacia Solidaria Cristiana

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
