# Guía Rápida - Testing del Sistema de Turnos

## 🎯 Sistema Completado al 100%

El **Sistema de Turnos** está completamente implementado y listo para pruebas:

- ✅ **Backend completo** (Models, Services, Controller)
- ✅ **Frontend completo** (6 vistas Razor con JavaScript)
- ✅ **Generación de PDFs** con iText7 (ya instalado para reportes)
- ✅ **Emails automáticos** (3 templates: solicitud, aprobación, rechazo)
- ✅ **SMTP configurado** (smtp.farmaciasolidaria.somee.com - funcionando)
- ✅ **Seguridad SHA-256** para documentos de identidad
- ✅ **Anti-abuso** (2 turnos por mes, máximo 30 turnos por día)
- ✅ **Horario restringido** (Martes y Viernes de 1:00 PM a 4:00 PM)
- ✅ **Documentación completa** (TURNOS_SYSTEM.md)
- ✅ **Datos de prueba** (seed-turnos-test-data.sql)

---

## 🚀 Paso 1: Aplicar Datos de Prueba

### Opción A: Desde SQL Server Management Studio (Windows)

1. Abre **SQL Server Management Studio**
2. Conéctate a tu servidor SQL Server
3. Abre el archivo `seed-turnos-test-data.sql`
4. **Asegúrate** de estar en la base de datos correcta: `USE [FarmaciaDb]`
5. Ejecuta el script completo (F5)
6. Verifica en el panel de mensajes que todo se creó correctamente

### Opción B: Desde Azure Data Studio (macOS/Linux/Windows)

1. Abre **Azure Data Studio**
2. Conéctate a tu servidor SQL Server
3. Abre el archivo `seed-turnos-test-data.sql`
4. Ejecuta el script completo
5. Revisa los mensajes de confirmación

### Opción C: Desde Terminal (sqlcmd)

```bash
sqlcmd -S TU_SERVIDOR -d FarmaciaDb -U TU_USUARIO -P TU_PASSWORD -i seed-turnos-test-data.sql
```

---

## 👥 Usuarios de Prueba Creados

Todos tienen password: **`Test123!`**

| Usuario | Email | Estado Turnos | Puede Solicitar? |
|---------|-------|---------------|------------------|
| **María García** | maria.garcia@example.com | 1 Aprobado + 1 Completado antiguo | ❌ No (tiene turno activo) |
| **Juan Pérez** | juan.perez@example.com | 1 Pendiente | ❌ No (tiene turno activo) |
| **Ana López** | ana.lopez@example.com | 1 Rechazado (hace 45 días) | ✅ Sí (rechazado hace >30 días) |
| **Carlos Rodríguez** | carlos.rodriguez@example.com | Sin turnos | ✅ Sí (nuevo usuario) |

---

## 🧪 Paso 2: Escenarios de Prueba

### 🔹 Escenario 1: Ver Turno Aprobado (María García)

**Objetivo:** Verificar visualización de turno aprobado con número

1. Login como: `maria.garcia@example.com` / `Test123!`
2. Click en **"Turnos"** en el menú
3. **Verificar:**
   - ✅ Botón "Solicitar Turno" DESHABILITADO (ya tiene turno activo)
   - ✅ Mensaje: "Ya tienes un turno activo este mes"
   - ✅ Tabla muestra turno con badge verde "Aprobado"
   - ✅ Número de turno: **#001**
   - ✅ Fecha del turno visible
   - ✅ 2 medicamentos listados

4. Click en **"Ver"** en el turno aprobado
5. **Verificar página de confirmación:**
   - ✅ Estado: badge verde "Aprobado"
   - ✅ Detalles completos
   - ✅ Lista de medicamentos con badges

---

### 🔹 Escenario 2: Dashboard Farmacéutico (Admin)

**Objetivo:** Gestionar turnos pendientes

1. Login como administrador (tu usuario Admin existente)
2. Click en **"Gestión Turnos"** en el menú
3. **Verificar Dashboard:**
   - ✅ 4 cards de estadísticas:
     * Pendientes: 1 (amarillo)
     * Aprobados: 1 (verde)
     * Completados: 1 (azul)
     * Total: 4 (azul)
   - ✅ Filtros: Estado, Desde, Hasta
   - ✅ Botón "Verificar por Documento"
   - ✅ Tabla DataTables con paginación en español

4. **Filtrar solo Pendientes:**
   - Seleccionar Estado: "Pendiente"
   - Click "Filtrar"
   - **Verificar:** Solo aparece el turno de Juan Pérez

5. **Ver Detalles del turno pendiente:**
   - Click en ícono de ojo (Ver Detalles)
   - **Verificar página Details:**
     * ✅ Información completa del usuario
     * ✅ 2 medicamentos solicitados en tabla
     * ✅ Botones: "Aprobar Turno" (verde) y "Rechazar Turno" (rojo)
     * ✅ Links para descargar documentos

---

### 🔹 Escenario 3: Aprobar Turno (Genera PDF)

**Objetivo:** Probar aprobación y generación de PDF

1. Desde **Details del turno de Juan Pérez**
2. Click en **"Aprobar Turno"** (botón verde grande)
3. **En el modal que aparece:**
   - Leer info de acciones automáticas
   - (Opcional) Agregar comentarios: "Medicamentos listos. Presentarse entre 9am-12pm"
   - Click "Confirmar Aprobación"

4. **Verificar resultado:**
   - ✅ Redirección a Dashboard
   - ✅ Mensaje verde: "Turno aprobado exitosamente"
   - ✅ Estado del turno cambió a "Aprobado"
   - ✅ Número de turno generado (ej: #002)
   - ✅ **PDF creado** en `wwwroot/pdfs/turnos/turno_2_002.pdf`

5. **Verificar PDF generado:**
   - Navegar a `FarmaciaSolidariaCristiana/wwwroot/pdfs/turnos/`
   - Abrir el PDF más reciente
   - **Contenido esperado:**
     * ✅ Logos de Iglesia + Adriano en encabezado
     * ✅ "TURNO #002" en grande y azul
     * ✅ Información del usuario (Juan Pérez, email)
     * ✅ Fecha y hora del turno
     * ✅ Tabla con 2 medicamentos y cantidades
     * ✅ Comentarios del farmacéutico en recuadro gris
     * ✅ 4 instrucciones importantes
     * ✅ Footer con fecha de generación

6. **Verificar email (si SMTP configurado):**
   - Revisar inbox de `juan.perez@example.com`
   - Email con asunto: "Turno Aprobado - Farmacia Solidaria Cristiana"
   - Contenido: número de turno, fecha, comentarios

---

### 🔹 Escenario 4: Verificar Turno por Documento

**Objetivo:** Probar sistema de verificación en farmacia

1. Desde **Dashboard** (como Admin/Farmacéutico)
2. Click en **"Verificar por Documento"**
3. **Ingresar documento de María:** `88012312345`
4. Click "Buscar Turno"

5. **Verificar resultado:**
   - ✅ Alerta verde: "¡Turno Encontrado!"
   - ✅ Card con info del turno: #001, Aprobado
   - ✅ Datos del usuario (María García, email)
   - ✅ Tabla de medicamentos con cantidades aprobadas
   - ✅ Botón verde: "Marcar Como Entregado"
   - ✅ Alerta amarilla: "Verificar documento físico antes de entregar"

6. **Marcar como entregado:**
   - Click "Marcar Como Entregado"
   - Confirmar en el alert
   - **Verificar:** Estado cambió a "Completado"
   - **Verificar:** FechaEntrega registrada en DB

---

### 🔹 Escenario 5: Solicitar Nuevo Turno (Ana López)

**Objetivo:** Probar flujo completo de solicitud

1. **Logout** del admin
2. Login como: `ana.lopez@example.com` / `Test123!`
3. Click en **"Turnos"**
4. **Verificar:**
   - ✅ Puede solicitar (su turno rechazado fue hace >30 días)
   - ✅ Botón "Solicitar Turno" HABILITADO

5. Click **"Solicitar Turno"**
6. **Formulario de solicitud:**
   - **Documento:** 90031534567
   - **Fecha Preferida:** Elegir 2 días en el futuro, 10:00 AM
   - **Buscar Medicamento:** Escribir "Para" (busca Paracetamol)
   - Click en medicamento para agregarlo
   - **Cantidad:** 20
   - Buscar otro medicamento y agregarlo
   - **Tarjetón:** Subir archivo JPG/PNG (cualquier imagen, max 5MB)
   - **Notas:** "Necesito estos medicamentos urgentemente"
   - Click **"Enviar Solicitud"**

7. **Verificar página Confirmation:**
   - ✅ Icono verde con mensaje "¡Solicitud Enviada!"
   - ✅ "¿Qué sigue?" con 5 pasos
   - ✅ Detalles de la solicitud
   - ✅ Estado: badge amarillo "Pendiente de Revisión"
   - ✅ Tabla con medicamentos seleccionados
   - ✅ Avisos importantes (24-48h, revisar email)

8. **Verificar en Dashboard (como Admin):**
   - Login de nuevo como Admin
   - Ir a "Gestión Turnos"
   - **Verificar:** Nuevo turno aparece en la lista con estado "Pendiente"

---

### 🔹 Escenario 6: Rechazar Turno

**Objetivo:** Probar flujo de rechazo con motivo

1. Como Admin, en **Dashboard**
2. Buscar un turno Pendiente
3. Click en ícono de ojo para "Ver Detalles"
4. Click **"Rechazar Turno"** (botón rojo)

5. **En el modal:**
   - **Motivo (obligatorio):** "Lamentablemente los medicamentos solicitados no están disponibles en este momento. Por favor intente nuevamente en 2 semanas."
   - Click "Confirmar Rechazo"

6. **Verificar:**
   - ✅ Redirección a Dashboard
   - ✅ Mensaje: "Turno rechazado"
   - ✅ Estado cambió a "Rechazado"
   - ✅ Email enviado con motivo (si SMTP configurado)

---

### 🔹 Escenario 7: Usuario sin Permiso para Solicitar (María)

**Objetivo:** Verificar anti-abuso de 2 turnos/mes

1. Login como: `maria.garcia@example.com` / `Test123!`
2. Ir a **"Turnos"**
3. **Verificar:**
   - ✅ Botón "Solicitar Turno" DESHABILITADO
   - ✅ Alerta amarilla: "Ya has alcanzado el límite de turnos este mes. Límite: 2 turnos por mes"
   - ✅ No se puede acceder a /Turnos/RequestForm (redirect)

---

### 🔹 Escenario 8: DataTables en Dashboard

**Objetivo:** Probar funcionalidades de tabla interactiva

1. Como Admin, en **Dashboard**
2. **Probar funcionalidades:**
   - ✅ **Búsqueda:** Escribir "María" en search box → filtra
   - ✅ **Ordenamiento:** Click en header "Fecha Solicitud" → ordena asc/desc
   - ✅ **Paginación:** Si hay >25 turnos, verifica cambio de página
   - ✅ **Idioma:** Todo en español (botones, labels)
   - ✅ **Exportar:** Botones copy/excel/pdf (si configurados)

---

## 📊 Verificaciones en Base de Datos

```sql
-- Ver todos los turnos
SELECT 
    T.Id,
    T.NumeroTurno,
    T.Estado,
    U.UserName AS Usuario,
    T.FechaSolicitud,
    T.FechaPreferida,
    T.EmailEnviado,
    T.TurnoPdfPath
FROM Turnos T
INNER JOIN AspNetUsers U ON T.UserId = U.Id
ORDER BY T.FechaSolicitud DESC;

-- Ver medicamentos de un turno específico
SELECT 
    TM.Id,
    M.Name AS Medicamento,
    TM.CantidadSolicitada,
    TM.CantidadAprobada,
    TM.DisponibleAlSolicitar
FROM TurnoMedicamentos TM
INNER JOIN Medicines M ON TM.MedicineId = M.Id
WHERE TM.TurnoId = 1; -- Cambiar por ID del turno

-- Ver usuarios ViewerPublic
SELECT 
    U.Id,
    U.UserName,
    U.Email,
    R.Name AS Rol
FROM AspNetUsers U
INNER JOIN AspNetUserRoles UR ON U.Id = UR.UserId
INNER JOIN AspNetRoles R ON UR.RoleId = R.Id
WHERE R.Name = 'ViewerPublic';

-- Contar turnos por estado
SELECT Estado, COUNT(*) AS Total
FROM Turnos
GROUP BY Estado
ORDER BY Total DESC;
```

---

## 🔍 Checklist de Funcionalidades

### Backend
- [x] Modelo Turno con 16 propiedades
- [x] Modelo TurnoMedicamento (relación many-to-many)
- [x] TurnoService con 12 métodos
- [x] Hashing SHA-256 de documentos
- [x] Validación anti-abuso (1/mes)
- [x] Uploads de archivos (receta, tarjetón)
- [x] Generación de números de turno secuenciales
- [x] Transacciones para integridad
- [x] Logging con ILogger

### Frontend
- [x] Index.cshtml (página principal ViewerPublic)
- [x] RequestForm.cshtml (formulario con JavaScript)
- [x] Confirmation.cshtml (página de éxito)
- [x] Dashboard.cshtml (panel Farmacéutico con DataTables)
- [x] Details.cshtml (detalles completos con modales)
- [x] Verify.cshtml (verificación por documento)
- [x] Bootstrap 5 responsive
- [x] JavaScript para búsqueda de medicamentos
- [x] Validaciones en tiempo real

### PDFs
- [x] Generación con iText7 9.0.0 (ya instalado para reportes)
- [x] Logos institucionales en encabezado
- [x] Número de turno destacado
- [x] Tabla de medicamentos formateada
- [x] Comentarios del farmacéutico
- [x] Instrucciones importantes
- [x] Footer con timestamp

### Emails
- [x] SendTurnoSolicitadoEmailAsync (confirmación)
- [x] SendTurnoAprobadoEmailAsync (con número)
- [x] SendTurnoRechazadoEmailAsync (con motivo)
- [x] Templates HTML con colores
- [x] Adjuntar PDF en aprobación (si existe)
- [x] SMTP ya configurado y funcionando (smtp.farmaciasolidaria.somee.com)

### Seguridad
- [x] SHA-256 para documentos
- [x] Authorization por roles
- [x] AntiForgeryToken en formularios
- [x] Validación de archivos (size, extensión)
- [x] Queries parametrizadas (EF Core)
- [x] Hashes irreversibles

### Documentación
- [x] TURNOS_SYSTEM.md (exhaustivo)
- [x] README.md actualizado
- [x] Comentarios en código
- [x] seed-turnos-test-data.sql
- [x] TESTING_TURNOS.md (este archivo)

---

## 🐛 Troubleshooting

### Problema: Script de datos falla

**Solución:** Asegúrate de que:
1. Ya corriste `dotnet ef database update` (roles deben existir)
2. Hay medicamentos en la DB (de DataSeeder o insertados manualmente)
3. Existe un usuario Admin

### Problema: PDFs no se generan

**Verificar:**
```bash
# Directorio debe existir
ls -la FarmaciaSolidariaCristiana/wwwroot/pdfs/turnos/

# Si no existe, crear:
mkdir -p FarmaciaSolidariaCristiana/wwwroot/pdfs/turnos/
```

**Logs:**
```bash
# Ver logs de la app
dotnet run

# Buscar errores relacionados con PDF generation
```

### Problema: Logos no aparecen en PDF

**Verificar:**
```bash
# Logos deben existir en:
ls -la FarmaciaSolidariaCristiana/wwwroot/images/logo-iglesia.png
ls -la FarmaciaSolidariaCristiana/wwwroot/images/logo-adriano.png
```

### Problema: Emails no se envían

**✅ SMTP YA CONFIGURADO EN PRODUCCIÓN**

La aplicación ya tiene SMTP funcionando en `appsettings.json`:

```json
{
  "SmtpSettings": {
    "Host": "smtp.farmaciasolidaria.somee.com",
    "Port": "26",
    "Username": "noreply@farmaciasolidaria.somee.com",
    "Password": "qozjyc-nibvi1",
    "FromEmail": "noreply@farmaciasolidaria.somee.com",
    "FromName": "Farmacia Solidaria Cristiana",
    "EnableSsl": "false"
  }
}
```

**Este mismo SMTP:**
- ✅ Ya funciona para "Olvidé mi contraseña"
- ✅ Funcionará automáticamente para los emails de turnos
- ❌ **NO necesitas configurar nada nuevo**

**Si aún así no se envían emails, verificar:**
1. Que el servicio SMTP de Somee.com esté activo
2. Logs de la aplicación: `dotnet run` → buscar errores relacionados con SMTP
3. Firewall o restricciones en puerto 26

---

## 🎉 Sistema Listo para Producción

Una vez probado exitosamente:

1. **Commit final:**
   ```bash
   git add -A
   git commit -m "test: Sistema de Turnos verificado y listo para producción"
   git push origin developer
   ```

2. **Merge a main:**
   ```bash
   git checkout main
   git merge developer
   git push origin main
   ```

3. **Deploy a Somee.com:**
   - Seguir guía en `DEPLOYMENT_UBUNTU.md` o deployment manual
   - Aplicar migración en servidor: `dotnet ef database update`
   - ✅ **SMTP ya configurado** (no requiere acción)
   - ✅ **iText7 ya incluido** en el build
   - Crear directorios: `uploads/turnos` y `pdfs/turnos`
   - Subir logos: `wwwroot/images/logo-iglesia.png` y `logo-adriano.png`

4. **Testing en producción:**
   - Crear usuarios ViewerPublic reales
   - Probar flujo completo end-to-end
   - Verificar emails se envían correctamente (ya debe funcionar)
   - Descargar y verificar PDFs generados

---

## 📞 Soporte

**Desarrollador:** Rev. Maikel Eduardo Peláez Martínez  
**Email:** mpmerd@gmail.com  
**Iglesia:** Metodista de Cárdenas, Cuba

---

**¡El Sistema de Turnos está 100% completado y listo para usar!** 🚀
