# Sistema de Decoraciones del Navbar - Festividades Cristianas

## 📖 Descripción General

Sistema que permite a los administradores "adornar" el navbar de la aplicación con decoraciones temáticas según las festividades cristianas, sin necesidad de detener o reiniciar la aplicación.

## ✨ Características Principales

### 1. Decoraciones Predefinidas

El sistema incluye 5 festividades cristianas predefinidas:

#### 🎄 **Navidad**
- **Fecha**: 25 de diciembre (período: 24 dic - 6 ene)
- **Descripción**: Celebración del nacimiento de Jesús
- **Icono**: Árbol de Navidad (verde)
- **Texto predeterminado**: "¡Feliz Navidad!" (dorado)

#### ⭐ **Epifanía**
- **Fecha**: 6 de enero
- **Descripción**: Manifestación de Jesús a los Reyes Magos
- **Icono**: Estrella (dorado)
- **Texto predeterminado**: "Epifanía del Señor" (azul real)

#### ✝️ **Semana Santa**
- **Fecha**: Variable (Domingo de Ramos hasta Domingo de Resurrección)
- **Descripción**: Pasión, muerte y resurrección de Jesucristo
- **Icono**: Cruz (marrón)
- **Texto predeterminado**: "Semana Santa" (púrpura)

#### ❤️ **Aldersgate Day**
- **Fecha**: 24 de mayo
- **Descripción**: Experiencia de conversión de Juan Wesley (tradición wesleyana)
- **Icono**: Corazón con pulso (rojo carmesí)
- **Texto predeterminado**: "Aldersgate Day" (rojo carmesí)

#### 🔥 **Pentecostés**
- **Fecha**: 50 días después de la Pascua
- **Descripción**: Venida del Espíritu Santo sobre los apóstoles
- **Icono**: Llama (naranja rojizo)
- **Texto predeterminado**: "Pentecostés" (naranja rojizo)

### 2. Decoraciones Personalizadas

Los administradores pueden crear decoraciones completamente personalizadas:

- **Nombre personalizado**: Cualquier nombre para la decoración
- **Texto personalizado**: Mensaje corto a mostrar
- **Color del texto**: Selector de color (hex)
- **Icono personalizado**: Subir imagen propia (PNG, JPG, SVG, GIF)
  - Tamaño máximo: 1MB
  - Tamaño recomendado: 48x48px
- **Color del icono**: Para tematizar el diseño

**Casos de uso para decoraciones personalizadas:**
- Aniversarios de la iglesia
- Eventos especiales de la congregación
- Campañas específicas
- Celebraciones locales

### 3. Actualización Dinámica (Sin Reinicio)

- **Carga automática**: La decoración se aplica inmediatamente sin reiniciar
- **Actualización periódica**: Cada 30 segundos el sistema verifica si hay cambios
- **Sin interrupciones**: Los usuarios ven los cambios sin necesidad de recargar la página

## 🎯 Cómo Usar

### Para Administradores

1. **Acceder al Panel**
   - Ir a: **Avanzado > Decoraciones del Navbar**
   - Solo usuarios con rol "Admin" tienen acceso

2. **Activar Decoración Predefinida**
   - Seleccionar una festividad
   - Opcionalmente modificar el texto predeterminado
   - Hacer clic en "Activar"
   - ✅ ¡La decoración aparece inmediatamente en el navbar!

3. **Crear Decoración Personalizada**
   - Completar el formulario:
     - Nombre de la decoración
     - Texto a mostrar (opcional)
     - Color del texto
     - Subir icono (opcional)
     - Color del icono
   - Hacer clic en "Crear y Activar"
   - ✅ La decoración personalizada se activa de inmediato

4. **Desactivar Decoración**
   - Desde el panel, hacer clic en "Desactivar"
   - El navbar vuelve a su estado normal

5. **Vista Previa**
   - El panel muestra una vista previa de cómo se verá la decoración
   - La vista previa se actualiza cada 10 segundos

### Para Usuarios Finales

- **Automático**: No necesitan hacer nada
- **Sin interrupciones**: Los cambios aparecen gradualmente
- **Compatibilidad**: Funciona en todos los navegadores modernos

## 🔧 Aspectos Técnicos

### Base de Datos

**Tabla**: `NavbarDecorations`

Campos principales:
- `Id`: Identificador único
- `Name`: Nombre de la decoración
- `Type`: Predefined o Custom
- `PresetKey`: Clave para decoraciones predefinidas (navidad, epifania, etc.)
- `DisplayText`: Texto a mostrar
- `TextColor`: Color del texto (hex)
- `CustomIconPath`: Ruta del icono personalizado
- `IconClass`: Clase CSS para iconos FontAwesome
- `IconColor`: Color del icono (hex)
- `IsActive`: Booleano que indica si está activa
- `ActivatedAt`: Fecha/hora de activación
- `ActivatedBy`: Usuario que la activó
- `CreatedAt`: Fecha de creación

### API REST

**Endpoint público** (sin autenticación):
```
GET /api/navbar-decoration/active
```

Respuesta cuando hay decoración activa:
```json
{
  "active": true,
  "name": "Navidad",
  "displayText": "¡Feliz Navidad!",
  "textColor": "#FFD700",
  "iconClass": "fa-solid fa-tree-christmas",
  "iconColor": "#228B22",
  "customIconPath": null,
  "type": "Predefined"
}
```

Respuesta cuando NO hay decoración:
```json
{
  "active": false
}
```

### Controladores

**NavbarDecorationsController** (solo Admin):
- `Index()`: Vista principal de gestión
- `ActivatePreset()`: Activar decoración predefinida
- `ActivateCustom()`: Activar decoración personalizada
- `DeactivateAll()`: Desactivar todas las decoraciones
- `Delete()`: Eliminar decoración personalizada
- `GetActiveDecoration()`: API pública para obtener decoración activa

### JavaScript (Layout)

Script automático que:
1. Carga la decoración activa al cargar la página
2. Actualiza cada 30 segundos sin recargar la página
3. Aplica animaciones suaves (fadeIn)
4. Maneja errores silenciosamente

### Iconos

- **Predefinidos**: Usa Font Awesome 6.5.1
- **Personalizados**: Almacenados en `/wwwroot/uploads/decorations/`
- **Formato**: PNG, JPG, SVG, GIF (máx 1MB)

## 📊 Gestión y Monitoreo

### Historial

El sistema mantiene un historial completo de:
- Todas las decoraciones creadas
- Cuándo fueron activadas
- Quién las activó
- Estado actual (activa/inactiva)

### Seguridad

- ✅ Solo usuarios con rol "Admin" pueden gestionar decoraciones
- ✅ Validación de archivos subidos (tipo y tamaño)
- ✅ Nombres únicos para archivos (GUID)
- ✅ Tokens anti-falsificación en todos los formularios
- ✅ Logging de todas las acciones

## 🎨 Diseño Visual

### Posición en el Navbar

La decoración aparece **centrada** en el navbar, entre el logo y los menús:

```
[Logo Farmacia] ← → [🎄 ¡Feliz Navidad!] ← → [Menús]
```

### Estilos

- Animación suave al aparecer
- Sombra de texto para mejor legibilidad
- Responsive: Solo visible en pantallas grandes (d-lg-flex)
- Colores personalizables según la festividad

## 💡 Mejores Prácticas

### Para Administradores

1. **Planificación**: Activar decoraciones con anticipación a las festividades
2. **Textos breves**: Máximo 3-4 palabras para mejor visualización
3. **Contraste**: Elegir colores que contrasten bien con el navbar azul
4. **Imágenes**: Usar iconos simples y reconocibles (48x48px)
5. **Desactivación**: Desactivar decoraciones después de la festividad

### Calendario Sugerido

- **Navidad**: 24 diciembre - 6 enero
- **Epifanía**: 6 enero
- **Semana Santa**: Según calendario litúrgico
- **Aldersgate**: 24 mayo (tradición metodista)
- **Pentecostés**: 50 días después de Pascua

## 🔄 Migración

Para aplicar la nueva tabla en Somee.com:

1. Ejecutar la migración generada: `20251113_AddNavbarDecorations`
2. Crear la carpeta `/wwwroot/uploads/decorations/` si no existe
3. Reiniciar el Application Pool

Script SQL manual incluido en `apply-migration-somee.sql`

## 🚀 Próximas Mejoras Potenciales

- [ ] Programación automática de decoraciones por fechas
- [ ] Múltiples decoraciones simultáneas
- [ ] Efectos de animación adicionales (parpadeo, rotación, etc.)
- [ ] Galería de iconos predefinidos adicionales
- [ ] Preview en tiempo real al crear decoración personalizada
- [ ] Exportar/importar configuraciones de decoraciones

## 📝 Notas Importantes

- Solo puede haber **una decoración activa** a la vez
- Las decoraciones predefinidas se pueden reutilizar cada año
- Las decoraciones personalizadas persisten en la base de datos
- Al eliminar una decoración personalizada, se borra también el archivo de icono
- La actualización automática no afecta el rendimiento de la aplicación

---

**Desarrollado para**: Farmacia Solidaria Cristiana - Iglesia Metodista de Cárdenas  
**Versión**: 1.0  
**Fecha**: Noviembre 2025
