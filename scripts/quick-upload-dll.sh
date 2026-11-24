#!/bin/bash

# Script rápido para subir solo el DLL principal

set -e

FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="/www.farmaciasolidaria.somee.com"

echo "Ingresa la contraseña FTP de Somee:"
read -s FTP_PASS
echo ""

# El script vive en scripts/ pero opera desde la raíz del repo
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIR="$REPO_DIR/FarmaciaSolidariaCristiana/publish"

echo "Subiendo archivos desde: $PUBLISH_DIR"
lftp -u "$FTP_USER","$FTP_PASS" "$FTP_HOST" << FTPEOF
cd $FTP_REMOTE_PATH
put $PUBLISH_DIR/FarmaciaSolidariaCristiana.dll
put $PUBLISH_DIR/FarmaciaSolidariaCristiana.pdb
ls -la FarmaciaSolidariaCristiana.*
quit
FTPEOF

echo ""
echo "✅ Archivos subidos"
echo ""
