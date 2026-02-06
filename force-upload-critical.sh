#!/bin/bash

# Script para forzar subida de DLLs críticos y vistas
# Usa esto cuando sospechas que el deploy normal no actualizó los archivos

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=========================================="
echo "Forzar Subida de Archivos Críticos"
echo "==========================================${NC}"
echo ""

FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="/www.farmaciasolidaria.somee.com"
PUBLISH_DIR="publish"

if [ ! -f "$PUBLISH_DIR/FarmaciaSolidariaCristiana.dll" ]; then
    echo -e "${RED}Error: No se encuentra el DLL principal${NC}"
    exit 1
fi

echo -e "${YELLOW}Archivos a subir:${NC}"
ls -la "$PUBLISH_DIR/FarmaciaSolidariaCristiana.dll"
ls -la "$PUBLISH_DIR/FarmaciaSolidariaCristiana.pdb" 2>/dev/null || true
echo ""

read -s -p "🔑 Ingresa la contraseña FTP: " FTP_PASS
echo ""
echo ""

echo -e "${YELLOW}🚀 Subiendo archivos críticos...${NC}"

lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
set ftp:use-site-chmod no;
set net:timeout 60;
set net:max-retries 5;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;

echo '>>> Eliminando DLL antiguo para forzar actualización...';
rm -f FarmaciaSolidariaCristiana.dll 2>/dev/null || true;
rm -f FarmaciaSolidariaCristiana.pdb 2>/dev/null || true;

echo '>>> Subiendo DLL principal...';
lcd $PUBLISH_DIR;
put FarmaciaSolidariaCristiana.dll;
put FarmaciaSolidariaCristiana.pdb;

echo '>>> Subiendo carpeta Views (vistas Razor)...';
mirror --reverse --delete --parallel=2 --verbose Views Views;

echo '>>> Subida completada';
bye
"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Archivos críticos subidos exitosamente${NC}"
else
    echo -e "${YELLOW}⚠️  Proceso completado (pueden haber advertencias normales)${NC}"
fi

echo ""
echo -e "${YELLOW}⚠️  IMPORTANTE: Reinicia la aplicación en el panel de Somee${NC}"
echo "  1. Ve a Somee → Site Management"
echo "  2. Click en 'Stop' y luego 'Start'"
echo ""
echo -e "${BLUE}Verificación:${NC}"
echo "  curl https://farmaciasolidaria.somee.com/api/diagnostics/ping"
echo ""
