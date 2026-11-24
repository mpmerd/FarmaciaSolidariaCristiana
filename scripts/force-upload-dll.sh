#!/bin/bash

# Colores
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}Subiendo FarmaciaSolidariaCristiana.dll forzadamente...${NC}"
echo ""

read -s -p "Ingresa la contraseña FTP: " FTP_PASS
echo ""

lftp -u maikelpelaez,$FTP_PASS -e "
set ssl:verify-certificate no
cd //www.farmaciasolidaria.somee.com
put -O . publish/FarmaciaSolidariaCristiana.dll
put -O . publish/FarmaciaSolidariaCristiana.pdb
bye
" farmaciasolidaria.somee.com

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✓ DLL principal subido exitosamente${NC}"
    echo ""
    echo "Ahora debes REINICIAR la aplicación en el panel de Somee:"
    echo "  1. Ve a: https://somee.com/ControlPanel.aspx"
    echo "  2. Busca tu aplicación"
    echo "  3. Click en 'Restart' o 'Recycle App Pool'"
else
    echo -e "${YELLOW}⚠ Error al subir el DLL${NC}"
fi
