#!/bin/bash

# Script para forzar actualización COMPLETA incluyendo DLL principal

HOST="farmaciasolidaria.somee.com"
USER="maikelpelaez"
REMOTE_PATH="//www.farmaciasolidaria.somee.com"

echo "=========================================="
echo "ACTUALIZACIÓN FORZADA COMPLETA"
echo "=========================================="
echo ""
echo "Esto va a subir:"
echo "  ✓ DLL principal"
echo "  ✓ web.config (con control de caché)"
echo "  ✓ Vistas (eliminando viejas)"
echo ""

read -sp "🔑 Ingresa contraseña FTP: " PASSWORD
echo ""
echo ""

echo "📤 Conectando y actualizando..."
echo ""

lftp -c "
set ftp:ssl-allow no
set net:timeout 30
set net:max-retries 3
open -u $USER,$PASSWORD ftp://$HOST
cd $REMOTE_PATH

echo '>>> 1. Subiendo DLL principal y web.config...'
lcd publish
put FarmaciaSolidariaCristiana.dll
put web.config
put FarmaciaSolidariaCristiana.pdb 2>/dev/null || true

echo ''
echo '>>> 2. Eliminando carpeta Views antigua...'
rm -rf Views

echo ''
echo '>>> 3. Subiendo Views nuevas...'
mirror -R --verbose Views Views

echo ''
echo '>>> 4. Verificando archivos...'
ls -la Views/Donations/ | grep -E '(Create|Edit|Delete|Index)'

bye
"

RESULT=$?

echo ""
echo "=========================================="
if [ $RESULT -eq 0 ]; then
    echo "✅ ¡Actualización completada!"
    echo "=========================================="
    echo ""
    echo "⚠️  PASO FINAL CRÍTICO:"
    echo ""
    echo "1. Ve a https://dashboard.somee.com"
    echo "2. DETÉN la aplicación (Stop)"
    echo "3. ESPERA 15 segundos"
    echo "4. INICIA la aplicación (Start)"
    echo ""
    echo "5. Luego en tu navegador:"
    echo "   - Presiona: Cmd+Shift+Delete (Mac) o Ctrl+Shift+Delete (Windows)"
    echo "   - Selecciona TODO el tiempo"
    echo "   - Marca: Cookies | Cache | Archivos"
    echo "   - Haz clic: Borrar datos"
    echo ""
    echo "6. Cierra TODAS las pestañas de la aplicación"
    echo "7. Reabre: https://farmaciasolidaria.somee.com"
    echo ""
else
    echo "⚠️  Código de salida: $RESULT"
    echo "=========================================="
    echo "Verifica:"
    echo "  - La contraseña FTP"
    echo "  - La conexión a internet"
fi

echo ""
