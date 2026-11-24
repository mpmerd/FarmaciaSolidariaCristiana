#!/bin/bash

# Script rápido para subir solo el DLL principal

set -e

FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="/www.farmaciasolidaria.somee.com"

echo "Ingresa la contraseña FTP de Somee:"
read -s FTP_PASS
echo ""

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_DIR="$SCRIPT_DIR/FarmaciaSolidariaCristiana/publish"

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
