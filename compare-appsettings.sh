#!/bin/bash

# Script para comparar appsettings.json local vs servidor

HOST="farmaciasolidaria.somee.com"
USER="maikelpelaez"
REMOTE_PATH="/www.farmaciasolidaria.somee.com"

echo "=========================================="
echo "Comparar appsettings.json"
echo "Local vs Servidor (Somee)"
echo "=========================================="
echo ""

read -sp "🔑 Ingresa contraseña FTP: " PASSWORD
echo ""
echo ""

echo "📥 Descargando appsettings.json del servidor..."

# Descargar archivo del servidor
lftp -c "
set ftp:ssl-allow no
open -u $USER,$PASSWORD ftp://$HOST
cd $REMOTE_PATH
get appsettings.json -o /tmp/appsettings-servidor.json
bye
"

if [ $? -ne 0 ]; then
    echo "❌ Error al descargar archivo del servidor"
    exit 1
fi

echo "✅ Archivo descargado"
echo ""

# Comparar archivos
echo "=========================================="
echo "COMPARACIÓN"
echo "=========================================="
echo ""

if [ ! -f "publish/appsettings.json" ]; then
    echo "❌ Error: No existe publish/appsettings.json local"
    echo "Ejecuta primero: dotnet publish -c Release -o publish"
    exit 1
fi

# Usar diff para comparar
if diff -u publish/appsettings.json /tmp/appsettings-servidor.json > /tmp/appsettings-diff.txt 2>&1; then
    echo "✅ ¡Los archivos son IDÉNTICOS!"
    echo ""
    echo "Tu appsettings.json local y el del servidor son iguales."
else
    echo "⚠️  LOS ARCHIVOS SON DIFERENTES"
    echo ""
    echo "Diferencias encontradas:"
    echo "----------------------------------------"
    cat /tmp/appsettings-diff.txt
    echo "----------------------------------------"
    echo ""
    echo "Archivos guardados para revisión:"
    echo "  Local:    publish/appsettings.json"
    echo "  Servidor: /tmp/appsettings-servidor.json"
    echo "  Diff:     /tmp/appsettings-diff.txt"
    echo ""
    echo "¿Quieres ver los archivos completos? (s/n)"
    read -n 1 MOSTRAR
    echo ""
    if [[ $MOSTRAR =~ ^[Ss]$ ]]; then
        echo ""
        echo "====== ARCHIVO LOCAL (publish/appsettings.json) ======"
        cat publish/appsettings.json
        echo ""
        echo ""
        echo "====== ARCHIVO SERVIDOR (/tmp/appsettings-servidor.json) ======"
        cat /tmp/appsettings-servidor.json
    fi
fi

echo ""
echo "Para subir el local al servidor si es necesario:"
echo "  ./deploy-to-somee.sh"
echo ""
