#!/bin/bash

# Script para subir compilación fresca desde la carpeta publish/
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=========================================="
echo "Subiendo Compilación Fresca"
echo "==========================================${NC}"
echo ""

# Verificar que existe el directorio publish
if [ ! -d "publish" ]; then
    echo -e "${RED}Error: No existe el directorio publish${NC}"
    echo "Ejecuta primero: dotnet publish FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana.csproj -c Release -o publish"
    exit 1
fi

# Verificar que existe el DLL
if [ ! -f "publish/FarmaciaSolidariaCristiana.dll" ]; then
    echo -e "${RED}Error: No existe publish/FarmaciaSolidariaCristiana.dll${NC}"
    exit 1
fi

# Mostrar timestamp del DLL
echo -e "${YELLOW}DLL a subir:${NC}"
ls -lh publish/FarmaciaSolidariaCristiana.dll
echo ""

# Configuración de Somee
FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="/www.farmaciasolidaria.somee.com"

echo -e "${YELLOW}Datos de conexión FTP:${NC}"
echo "  Host: $FTP_HOST"
echo "  Usuario: $FTP_USER"
echo "  Ruta remota: $FTP_REMOTE_PATH"
echo ""

read -s -p "Ingresa la contraseña FTP: " FTP_PASS
echo ""
echo ""

echo -e "${YELLOW}Subiendo archivos principales (DLL + PDB + configs)...${NC}"

lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
set ftp:use-site-chmod no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;
put -O . publish/FarmaciaSolidariaCristiana.dll;
put -O . publish/FarmaciaSolidariaCristiana.pdb;
put -O . publish/appsettings.json;
put -O . publish/web.config;
echo 'Archivos principales subidos'
"

echo ""
echo -e "${YELLOW}Subiendo carpeta wwwroot completa...${NC}"

lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
set ftp:use-site-chmod no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;
mirror --reverse --verbose --parallel=3 --only-newer publish/wwwroot wwwroot
"

LFTP_EXIT_CODE=$?
echo ""

if [ $LFTP_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✓ Archivos subidos exitosamente${NC}"
else
    echo -e "${YELLOW}⚠ Proceso completado con advertencias (código: $LFTP_EXIT_CODE)${NC}"
    echo -e "${YELLOW}Las advertencias de 'chmod' son normales en Somee y pueden ignorarse.${NC}"
fi

echo ""
echo -e "${GREEN}=========================================="
echo "¡Subida Completada!"
echo "==========================================${NC}"
echo ""
echo -e "${RED}⚠️  CRÍTICO: DEBES REINICIAR LA APLICACIÓN AHORA${NC}"
echo ""
echo "Pasos para reiniciar:"
echo "  1. Ve a: https://somee.com/ControlPanel.aspx"
echo "  2. Busca 'farmaciasolidaria'"
echo "  3. Click en 'Restart' o 'Recycle App Pool'"
echo "  4. Espera 1-2 minutos"
echo "  5. Limpia caché del navegador (Ctrl+Shift+R o Cmd+Shift+R)"
echo "  6. Prueba en: https://farmaciasolidaria.somee.com"
echo ""
echo -e "${YELLOW}Sin el reinicio, los cambios NO serán visibles${NC}"
echo ""
