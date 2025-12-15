#!/bin/bash

echo "Subiendo DLL actualizado..."
echo ""
read -s -p "Contraseña FTP: " FTP_PASS
echo ""
echo "Conectando..."

lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
open -u maikelpelaez,$FTP_PASS ftp://farmaciasolidaria.somee.com;
cd //www.farmaciasolidaria.somee.com;
put -O . FarmaciaSolidariaCristiana/bin/Release/net8.0/publish/FarmaciaSolidariaCristiana.dll;
put -O . FarmaciaSolidariaCristiana/bin/Release/net8.0/publish/FarmaciaSolidariaCristiana.pdb;
bye
"

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ DLL subido exitosamente"
    echo ""
    echo "⚠️  IMPORTANTE: Ahora debes REINICIAR la aplicación en Somee:"
    echo "   1. Ve a: https://somee.com/ControlPanel.aspx"
    echo "   2. Busca 'farmaciasolidaria'"
    echo "   3. Click en 'Restart' o 'Recycle App Pool'"
    echo ""
else
    echo ""
    echo "⚠ Error al subir"
fi
