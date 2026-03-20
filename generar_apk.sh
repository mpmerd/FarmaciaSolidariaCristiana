#!/bin/bash

# Script para generar APK de Farmacia Solidaria Cristiana
# Actualiza la versión y compila en modo Release

set -e

echo "🏗️  Generador de APK - Farmacia Solidaria Cristiana"
echo "=================================================="
echo ""

# Rutas
PROJECT_FILE="FarmaciaSolidariaCristiana.Maui/FarmaciaSolidariaCristiana.Maui.csproj"
OUTPUT_DIR="FarmaciaSolidariaCristiana.Maui/bin/Release/net10.0-android"

# Verificar que existe el archivo del proyecto
if [ ! -f "$PROJECT_FILE" ]; then
    echo "❌ Error: No se encuentra el archivo del proyecto"
    exit 1
fi

# Obtener versión actual
CURRENT_VERSION=$(sed -n 's/.*<ApplicationDisplayVersion>\(.*\)<\/ApplicationDisplayVersion>.*/\1/p' "$PROJECT_FILE")
CURRENT_CODE=$(sed -n 's/.*<ApplicationVersion>\(.*\)<\/ApplicationVersion>.*/\1/p' "$PROJECT_FILE")

echo "📊 Versión actual: $CURRENT_VERSION (código: $CURRENT_CODE)"
echo ""

# Preguntar si quiere cambiar la versión
read -p "¿Desea actualizar la versión? (s/N): " UPDATE_VERSION

if [[ "$UPDATE_VERSION" =~ ^[Ss]$ ]]; then
    read -p "Nueva versión (formato X.Y.Z, actual: $CURRENT_VERSION): " NEW_VERSION
    read -p "Nuevo código de versión (número entero, actual: $CURRENT_CODE): " NEW_CODE
    
    if [ ! -z "$NEW_VERSION" ] && [ ! -z "$NEW_CODE" ]; then
        echo "📝 Actualizando versión en el proyecto..."
        
        # macOS usa sed diferente que Linux
        if [[ "$OSTYPE" == "darwin"* ]]; then
            sed -i '' "s|<ApplicationDisplayVersion>.*</ApplicationDisplayVersion>|<ApplicationDisplayVersion>$NEW_VERSION</ApplicationDisplayVersion>|" "$PROJECT_FILE"
            sed -i '' "s|<ApplicationVersion>.*</ApplicationVersion>|<ApplicationVersion>$NEW_CODE</ApplicationVersion>|" "$PROJECT_FILE"
        else
            sed -i "s|<ApplicationDisplayVersion>.*</ApplicationDisplayVersion>|<ApplicationDisplayVersion>$NEW_VERSION</ApplicationDisplayVersion>|" "$PROJECT_FILE"
            sed -i "s|<ApplicationVersion>.*</ApplicationVersion>|<ApplicationVersion>$NEW_CODE</ApplicationVersion>|" "$PROJECT_FILE"
        fi
        
        CURRENT_VERSION=$NEW_VERSION
        CURRENT_CODE=$NEW_CODE
        echo "✅ Versión actualizada: $CURRENT_VERSION (código: $CURRENT_CODE)"
    fi
fi

echo ""
echo "🧹 Limpiando compilaciones anteriores..."
dotnet clean FarmaciaSolidariaCristiana.Maui -c Release -f net10.0-android

echo ""
echo "🔨 Compilando APK en modo Release..."
echo "   Versión: $CURRENT_VERSION"
echo "   Código: $CURRENT_CODE"
echo ""

dotnet publish FarmaciaSolidariaCristiana.Maui -c Release -f net10.0-android

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Compilación exitosa"
    echo "📦 Copiando APK con nombre estándar..."
    
    # Copiar el APK firmado con un nombre estándar
    SIGNED_APK=$(find "$OUTPUT_DIR" -name "*-Signed.apk" | head -n 1)
    
    if [ -f "$SIGNED_APK" ]; then
        cp "$SIGNED_APK" "$OUTPUT_DIR/farmaciasolidaria.apk"
        echo "✅ APK generado: $OUTPUT_DIR/farmaciasolidaria.apk"
        
        # Mostrar información del APK
        APK_SIZE=$(ls -lh "$OUTPUT_DIR/farmaciasolidaria.apk" | awk '{print $5}')
        echo ""
        echo "📊 Información del APK:"
        echo "   Versión: $CURRENT_VERSION"
        echo "   Código: $CURRENT_CODE"
        echo "   Tamaño: $APK_SIZE"
        echo "   Ruta: $OUTPUT_DIR/farmaciasolidaria.apk"
        echo ""
        echo "✨ ¡Listo! Ahora puedes ejecutar ./subir_apk.sh para subirlo al servidor"
    else
        echo "❌ Error: No se encontró el APK firmado"
        exit 1
    fi
else
    echo "❌ Error en la compilación"
    exit 1
fi
