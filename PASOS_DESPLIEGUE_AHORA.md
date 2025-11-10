# 🚀 PASOS PARA DESPLEGAR AHORA

## ✅ Completado
- [x] Backup de usuarios guardado
- [x] Aplicación publicada en `/publish`
- [x] Script de migración preparado

## 📋 Pasos a Seguir (EN ESTE ORDEN)

### PASO 1: Aplicar Migración en Base de Datos (Somee) ⚠️ IMPORTANTE

1. **Ve a**: https://somee.com
2. **Inicia sesión** en tu cuenta
3. **Navega a**: "My Websites" → Tu sitio → "Manage my DB"
4. **Haz clic en**: "Database Manager" o "SQL Server Management"
5. **Selecciona** tu base de datos
6. **Abre** el editor SQL
7. **Copia TODO el contenido** del archivo:
   ```
   /Users/Documents/Proyectos/FarmaciaSolidariaCristiana/apply-migration-somee.sql
   ```
8. **Pega** en el editor SQL de Somee
9. **EJECUTA** el script (botón "Execute" o "Run")
10. **Espera** a que termine y **verifica** que dice:
    ```
    MIGRACIÓN COMPLETADA EXITOSAMENTE
    ```

### PASO 2: Subir Archivos por FTP

#### Opción A: Usar el Script Automático (Recomendado)

```bash
cd /Users/Documents/Proyectos/FarmaciaSolidariaCristiana
./deploy-to-somee.sh
```

Si el script no existe o tienes problemas, usa la **Opción B**.

#### Opción B: Manual con FileZilla o Cyberduck

**Credenciales FTP:**
- **Host**: ftp://your-site.somee.com
- **Usuario**: Tu usuario de Somee
- **Contraseña**: Tu contraseña de Somee
- **Puerto**: 21

**Archivos a subir:**
- **Origen**: `/Users/Documents/Proyectos/FarmaciaSolidariaCristiana/publish/`
- **Destino**: Raíz de tu sitio web en Somee (generalmente `/wwwroot` o `/`)

**Archivos importantes a subir:**
```
✓ FarmaciaSolidariaCristiana.dll
✓ FarmaciaSolidariaCristiana.deps.json
✓ FarmaciaSolidariaCristiana.runtimeconfig.json
✓ appsettings.json
✓ web.config
✓ wwwroot/ (carpeta completa)
✓ Todas las carpetas runtimes/
```

**⚠️ NO subir:**
```
✗ appsettings.Development.json
✗ *.pdb (archivos de depuración)
```

### PASO 3: Verificar el Despliegue

1. **Abre tu sitio**: https://your-site.somee.com
2. **Espera** 1-2 minutos a que se inicie la aplicación
3. **Inicia sesión** con admin:
   - Usuario: `admin`
   - Contraseña: `Admin123!`

### PASO 4: Pruebas Funcionales

#### Prueba 1: Verificar Usuarios
1. Ve a "Gestión de Usuarios"
2. Verifica que aparecen todos tus usuarios reales:
   xxxxx

#### Prueba 2: Crear Paciente con Identificación
1. Ve a **"Pacientes"** → **"Nuevo Paciente"**
2. Verifica que el primer campo es **"Carnet de Identidad o Pasaporte"**
3. Ingresa un carnet de prueba: `12345678901`
4. Al salir del campo, debe validar el formato
5. Completa el resto del formulario
6. Guarda el paciente

#### Prueba 3: Búsqueda Automática
1. Ve a **"Pacientes"** → **"Nuevo Paciente"** de nuevo
2. Ingresa el mismo carnet: `12345678901`
3. Al salir del campo, debe mostrar:
   - Alerta azul con datos del paciente
   - Mensaje "Paciente ya registrado"
   - Botón para editar la ficha

#### Prueba 4: Historial de Entregas
1. Ve a **"Pacientes"** → Selecciona el paciente que creaste
2. Baja hasta la **Sección 6: Historial de Entregas**
3. Debe mostrar "Sin entregas" (es nuevo)

#### Prueba 5: Nueva Entrega con Identificación
1. Ve a **"Entregas"** → **"Nueva Entrega"**
2. Verifica que el primer campo es **"Carnet de Identidad o Pasaporte"**
3. Ingresa: `12345678901`
4. Al salir del campo, debe mostrar:
   - Alerta verde: "Paciente encontrado"
   - Datos del paciente
   - "Primera entrega para este paciente"
5. Selecciona un medicamento
6. Ingresa cantidad: `10`
7. Guarda la entrega

#### Prueba 6: Verificar Historial Actualizado
1. Ve a **"Pacientes"** → Selecciona el mismo paciente
2. Baja hasta **Sección 6: Historial de Entregas**
3. Ahora debe mostrar la entrega que hiciste:
   - Medicamento
   - Cantidad: 10
   - Fecha de hoy

## ❌ Si Algo Sale Mal

### Error: Usuarios desaparecieron
**Solución:**
1. Ve al panel de base de datos de Somee
2. Vuelve a ejecutar el script `apply-migration-somee.sql`
3. Verifica que los INSERT están en el script

### Error: "Paciente no encontrado" en Entregas
**Solución:**
1. Verifica que el carnet esté bien escrito
2. Asegúrate de haber creado primero el paciente
3. Formato correcto: 11 dígitos o letra+6-7 dígitos

### Error: La aplicación no carga
**Solución:**
1. Espera 2-3 minutos
2. Refresca el navegador (Ctrl+F5)
3. Verifica que todos los archivos se subieron
4. Revisa los logs en el panel de Somee

### Error: "Formato inválido" en identificación
**Solución:**
- **Carnet**: Exactamente 11 dígitos (ej: `12345678901`)
- **Pasaporte**: 1 letra + 6 o 7 dígitos (ej: `A123456` o `B1234567`)

## 📞 Contacto de Emergencia

Si tienes problemas graves:
1. NO borres la base de datos
2. Guarda los mensajes de error
3. Revisa el archivo TROUBLESHOOTING_SSH.md
4. Contacta al soporte de Somee si es necesario

## ✅ Checklist Final

Después de completar todos los pasos:

- [ ] Script SQL ejecutado sin errores
- [ ] Aplicación desplegada vía FTP
- [ ] Sitio web carga correctamente
- [ ] Login con admin funciona
- [ ] Todos los usuarios reales presentes
- [ ] Campo de identificación aparece primero
- [ ] Validación de formato funciona
- [ ] Búsqueda automática funciona
- [ ] Crear paciente funciona
- [ ] Historial se muestra en ficha
- [ ] Nueva entrega con identificación funciona
- [ ] Historial se actualiza correctamente

## 🎉 ¡Listo!

Una vez completado el checklist, tu sistema estará actualizado con:
✅ Identificación obligatoria para pacientes cubanos
✅ Búsqueda automática de pacientes
✅ Historial de entregas en ficha de paciente
✅ Vinculación de entregas con pacientes
✅ Compresión automática de imágenes

---

**Fecha**: 23 de octubre de 2025  
**Versión**: 2.0 - Sistema de Identificación de Pacientes
