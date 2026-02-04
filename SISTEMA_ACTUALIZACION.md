# Sistema de Control de Versiones - Farmacia Solidaria Cristiana

Este sistema permite publicar y actualizar automáticamente la aplicación Android mediante un servidor web.

## 📋 Componentes

### 1. Servidor Web
- **URL Base**: `https://farmaciasolidaria.somee.com/android/`
- **Archivos alojados**:
  - `farmaciasolidaria.apk` - El archivo APK instalable
  - `version.json` - Metadata de la versión actual
  - `index.html` - Página de descarga
  - `web.config` - Configuración del servidor IIS

### 2. Aplicación MAUI
- **UpdateService.cs**: Verifica automáticamente si hay actualizaciones disponibles
- Se ejecuta 2 segundos después de iniciar la app
- Compara la versión local con la del servidor
- Muestra un diálogo al usuario si hay una actualización disponible

### 3. Scripts de Automatización

#### `generar_apk.sh`
Compila la aplicación y genera el APK.

**Uso:**
```bash
./generar_apk.sh
```

**Funcionalidades:**
- Pregunta si deseas actualizar la versión
- Actualiza el archivo `.csproj` con la nueva versión
- Limpia compilaciones anteriores
- Compila en modo Release
- Copia el APK con nombre estándar `farmaciasolidaria.apk`
- Muestra información del APK generado

#### `subir_apk.sh`
Sube el APK y archivos relacionados al servidor FTP.

⚠️ **IMPORTANTE**: Este archivo contiene credenciales sensibles y está en `.gitignore`

**Uso:**
```bash
./subir_apk.sh
```

**Funcionalidades:**
- Lee la versión del archivo `.csproj`
- Solicita notas de la versión
- Actualiza `version.json` automáticamente
- Sube archivos al servidor vía FTP:
  - `farmaciasolidaria.apk`
  - `version.json`
  - `index.html`
  - `web.config`
- Muestra URLs de acceso al finalizar

## 🚀 Flujo de Publicación

### Paso 1: Generar APK
```bash
./generar_apk.sh
```
- Responde si quieres actualizar la versión
- Ingresa nueva versión (ej: `1.0.1`)
- Ingresa nuevo código de versión (ej: `2`)
- Espera a que compile

### Paso 2: Subir al Servidor
```bash
./subir_apk.sh
```
- Ingresa las notas de la versión (línea por línea)
- Presiona Enter en línea vacía para finalizar
- Confirma la subida con `s`
- Espera a que suban todos los archivos

### Paso 3: Verificar
- Accede a `https://farmaciasolidaria.somee.com/android/`
- Verifica que la versión sea correcta
- Descarga y prueba el APK

## 📱 Funcionamiento en la App

1. **Al iniciar la app**:
   - Espera 2 segundos
   - Descarga `version.json` del servidor
   - Compara con la versión instalada

2. **Si hay actualización**:
   - Muestra diálogo con:
     - Número de versión
     - Fecha de lanzamiento
     - Tamaño del archivo
     - Notas de la versión
   - Opciones: "Descargar" o "Más tarde"

3. **Si el usuario acepta**:
   - Abre el navegador
   - Descarga el APK
   - Android solicita permiso para instalar

## 🔧 Configuración FTP

El archivo `subir_apk.sh` contiene las credenciales FTP:

```bash
FTP_HOST="155.254.246.18"
FTP_USER="maikelpelaez"
FTP_PASS="qadsyk-9fyrfu-kucpAb"
FTP_PATH="/www.farmaciasolidaria.somee.com/android"
```

⚠️ **Este archivo NO debe subirse a GitHub** - Ya está en `.gitignore`

## 📂 Estructura de Archivos

```
FarmaciaSolidariaCristiana/
├── generar_apk.sh              # Script para generar APK (seguro para Git)
├── subir_apk.sh                # Script para subir (EN .gitignore)
├── android/                    # Archivos para el servidor
│   ├── version.json            # Metadata de versión
│   ├── index.html              # Página de descarga
│   └── web.config              # Configuración IIS
└── FarmaciaSolidariaCristiana.Maui/
    ├── Services/
    │   └── UpdateService.cs    # Servicio de actualización
    └── App.xaml.cs             # Inicializa UpdateService
```

## 🌐 URLs Importantes

- **Página de descarga**: https://farmaciasolidaria.somee.com/android/
- **APK directo**: https://farmaciasolidaria.somee.com/android/farmaciasolidaria.apk
- **Info de versión**: https://farmaciasolidaria.somee.com/android/version.json

## 🔒 Seguridad

- El script `subir_apk.sh` está en `.gitignore`
- Nunca subas credenciales a GitHub
- Si necesitas compartir el script, crea una versión template sin credenciales:

```bash
# Crear template sin credenciales
cp subir_apk.sh subir_apk.template.sh
# Editar manualmente y reemplazar credenciales por placeholders
```

## 📝 Notas

- La versión en `.csproj` debe seguir formato semántico: `X.Y.Z`
- El código de versión debe ser un número entero incremental
- Las notas de versión soportan múltiples líneas
- El tamaño del APK se calcula automáticamente

## ❓ Troubleshooting

### Error: "No se encuentra el APK"
```bash
# Ejecuta primero el generador
./generar_apk.sh
```

### Error de FTP
- Verifica las credenciales en `subir_apk.sh`
- Verifica conexión a internet
- Verifica que el servidor FTP esté activo

### La app no detecta actualizaciones
- Verifica que `version.json` esté actualizado en el servidor
- Verifica que la URL sea accesible desde el móvil
- Revisa los logs de debug en Visual Studio
