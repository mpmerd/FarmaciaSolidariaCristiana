# Sistema de Gestión de Pacientes

## Descripción

El sistema de gestión de pacientes permite crear y mantener fichas completas de los pacientes que reciben medicamentos de la Farmacia Solidaria Cristiana. Incluye la capacidad de subir fotos de documentos médicos (recetas, tarjetones, informes, etc.).

## Características Principales

### 1. Ficha del Paciente Completa

La ficha del paciente incluye los siguientes datos:

#### **Datos Personales**
- Nombre completo
- Edad
- Género (Masculino/Femenino)
- Dirección
- Teléfono
- DNI o documento de identificación
- Municipio
- Provincia

#### **Datos Clínicos**
- Diagnóstico principal
- Patologías asociadas
- Alergias conocidas
- Tratamientos actuales

#### **Datos Vitales**
- Presión arterial (sistólica/diastólica)
- Peso (kg)
- Altura (cm)

#### **Documentos**
- Posibilidad de subir múltiples fotos/documentos
- Tipos de documentos soportados:
  - Receta Médica
  - Tarjetón Sanitario
  - Tratamiento
  - Informe Médico
  - Otro
- Descripción opcional para cada documento
- Visualización de documentos con vista previa de imágenes

#### **Observaciones**
- Campo de texto libre para anotaciones generales

#### **Historial de Entregas**
- Lista de todas las entregas de medicamentos realizadas al paciente
- Incluye: fecha, medicamento, cantidad, dosis, duración del tratamiento, quién lo entregó

### 2. Gestión de Documentos

- **Subida de múltiples documentos**: Puede agregar varios documentos al crear o editar un paciente
- **Tipos de archivo aceptados**: Imágenes (JPG, PNG, etc.) y PDF
- **Vista previa**: Las imágenes se muestran con vista previa en la ficha del paciente
- **Eliminación**: Posibilidad de eliminar documentos individuales
- **Almacenamiento**: Los archivos se guardan en `wwwroot/uploads/patient-documents/`

### 3. Funcionalidades CRUD

- **Listar Pacientes** (`/Patients`): Vista de todos los pacientes activos con información básica
- **Ver Detalles** (`/Patients/Details/{id}`): Vista completa de la ficha del paciente con todos sus datos, documentos e historial
- **Crear Paciente** (`/Patients/Create`): Formulario para registrar un nuevo paciente con posibilidad de subir documentos
- **Editar Paciente** (`/Patients/Edit/{id}`): Formulario para actualizar datos del paciente y agregar nuevos documentos
- **Eliminar Paciente**: Solo para administradores, realiza un "soft delete" (marca como inactivo)

### 4. Vinculación con Entregas

El sistema de entregas se ha actualizado para poder vincularse con pacientes:
- Campo opcional de paciente en el formulario de entrega
- Campos adicionales:
  - Dosis
  - Duración del tratamiento
  - Número de lote
  - Fecha de vencimiento
  - Entregado por

## Navegación

Acceda al sistema de pacientes desde el menú principal:
- **Pacientes** (icono de persona con líneas)

## Permisos de Acceso

- **Ver pacientes**: Todos los usuarios autenticados
- **Crear/Editar pacientes**: Todos los usuarios autenticados
- **Eliminar pacientes**: Solo administradores

## Flujo de Trabajo Recomendado

1. **Registrar nuevo paciente**:
   - Click en "Nuevo Paciente"
   - Completar datos personales (obligatorios: nombre, edad, género)
   - Agregar datos clínicos y vitales
   - Subir fotos de documentos (recetas, tarjetones)
   - Agregar observaciones si es necesario
   - Guardar

2. **Registrar entrega de medicamento**:
   - Ir a Entregas → Nueva Entrega
   - Seleccionar el paciente de la lista
   - Elegir el medicamento y cantidad
   - Indicar dosis y duración del tratamiento
   - Completar datos adicionales (lote, vencimiento, quien entrega)
   - Guardar

3. **Actualizar ficha del paciente**:
   - Buscar el paciente en la lista
   - Click en "Editar"
   - Actualizar datos necesarios
   - Agregar nuevos documentos si es necesario
   - Guardar cambios

4. **Consultar historial**:
   - Buscar el paciente en la lista
   - Click en "Ver Detalles"
   - Revisar todos los datos, documentos e historial de entregas

## Base de Datos

### Tabla `Patients`
```sql
- Id (int, PK)
- FullName (nvarchar(200), NOT NULL)
- Age (int, NOT NULL)
- Gender (nvarchar(1), NOT NULL) -- M/F/O
- Address (nvarchar(500))
- Phone (nvarchar(50))
- IdentificationDocument (nvarchar(50))
- Municipality (nvarchar(100))
- Province (nvarchar(100))
- MainDiagnosis (nvarchar(500))
- AssociatedPathologies (nvarchar(1000))
- KnownAllergies (nvarchar(500))
- CurrentTreatments (nvarchar(1000))
- BloodPressureSystolic (int)
- BloodPressureDiastolic (int)
- Weight (decimal(5,2))
- Height (decimal(5,2))
- Observations (nvarchar(2000))
- RegistrationDate (datetime2, NOT NULL)
- IsActive (bit, NOT NULL)
```

### Tabla `PatientDocuments`
```sql
- Id (int, PK)
- PatientId (int, FK → Patients.Id, ON DELETE CASCADE)
- DocumentType (nvarchar(100), NOT NULL)
- FileName (nvarchar(255), NOT NULL)
- FilePath (nvarchar(500), NOT NULL)
- FileSize (bigint, NOT NULL)
- ContentType (nvarchar(100))
- Description (nvarchar(500))
- UploadDate (datetime2, NOT NULL)
```

### Tabla `Deliveries` (modificada)
Campos agregados:
```sql
- PatientId (int, FK → Patients.Id, ON DELETE NO ACTION)
- Dosage (nvarchar(100))
- TreatmentDuration (nvarchar(100))
- BatchNumber (nvarchar(50))
- ExpiryDate (datetime2)
- DeliveredBy (nvarchar(200))
```

## Migración de Base de Datos

La migración `20251021154557_AddPatientAndDocuments` crea:
- Tabla `Patients`
- Tabla `PatientDocuments`
- Campos adicionales en `Deliveries`
- Índices y relaciones (Foreign Keys)

La migración ya ha sido aplicada a la base de datos de producción.

## Archivos del Sistema

### Modelos
- `Models/Patient.cs`: Modelo de datos del paciente
- `Models/PatientDocument.cs`: Modelo de documentos del paciente
- `Models/Delivery.cs`: Modificado para incluir vinculación con pacientes

### Controlador
- `Controllers/PatientsController.cs`: Controlador con toda la lógica CRUD y gestión de archivos

### Vistas
- `Views/Patients/Index.cshtml`: Lista de pacientes
- `Views/Patients/Create.cshtml`: Formulario de creación
- `Views/Patients/Edit.cshtml`: Formulario de edición
- `Views/Patients/Details.cshtml`: Vista detallada del paciente

### Configuración
- `Data/ApplicationDbContext.cs`: Configuración de DbContext con las nuevas entidades
- `Views/Shared/_Layout.cshtml`: Menú de navegación actualizado

## Notas de Seguridad

1. **Validación de archivos**: Solo se aceptan imágenes y PDFs
2. **Nombres únicos**: Los archivos se guardan con GUID para evitar colisiones
3. **Eliminación cascada**: Al eliminar un paciente, se eliminan todos sus documentos
4. **Soft delete**: Los pacientes se marcan como inactivos en lugar de eliminarse
5. **Control de acceso**: La eliminación de pacientes requiere rol de administrador

## Mejoras Futuras Sugeridas

- [ ] Búsqueda y filtrado de pacientes por nombre, municipio, etc.
- [ ] Exportación de fichas a PDF
- [ ] Estadísticas de pacientes por diagnóstico, edad, municipio
- [ ] Recordatorios de citas o renovación de tratamientos
- [ ] Integración con calendario para citas
- [ ] Historial de cambios en la ficha del paciente
- [ ] Firma digital en entregas
- [ ] Compresión automática de imágenes para optimizar almacenamiento

## Soporte

Para cualquier duda o problema con el sistema de pacientes, contacte al administrador del sistema.

---

**Fecha de implementación**: 21 de octubre de 2025  
**Versión**: 1.0  
**Desarrollador**: Sistema de Gestión Farmacia Solidaria Cristiana
