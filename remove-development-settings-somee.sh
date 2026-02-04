#!/bin/bash

# Script para eliminar appsettings.Development.json del servidor Somee
# Este archivo no debe existir en producción

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

echo -e "${YELLOW}🗑️  Eliminando appsettings.Development.json del servidor Somee${NC}"
echo ""

FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="/www.farmaciasolidaria.somee.com"

echo "Ingresa la contraseña FTP de Somee:"
read -s FTP_PASS
echo ""

echo "Eliminando archivo..."
lftp -u "$FTP_USER","$FTP_PASS" "$FTP_HOST" << FTPEOF
cd $FTP_REMOTE_PATH
rm -f appsettings.Development.json
ls -la appsettings*.json
quit
FTPEOF

echo ""
echo -e "${GREEN}✅ Archivo eliminado${NC}"
echo ""
echo -e "${YELLOW}⚠️  IMPORTANTE: Debes reiniciar la aplicación en Somee${NC}"
echo "1. Ve al panel de Somee"
echo "2. My Websites → farmaciasolidaria.somee.com"
echo "3. Restart Application"
echo ""
