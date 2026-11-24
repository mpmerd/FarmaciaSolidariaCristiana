#!/bin/bash

# Script para forzar la actualización de vistas (sin condición de fecha)
# Útil cuando el --only-newer no funciona correctamente

HOST="farmaciasolidaria.somee.com"
USER="maikelpelaez"
REMOTE_PATH="//www.farmaciasolidaria.somee.com"

echo "=========================================="
echo "Subiendo vistas (FUERZANDO SOBRESCRITURA)"
echo "=========================================="
echo ""
echo "Esto eliminará las vistas viejas en el servidor"
echo "y las reemplazará con las nuevas."
echo ""

# Verificar que publish/Views existe
if [ ! -d "publish/Views" ]; then
    echo "❌ Error: No existe publish/Views"
    echo "Ejecuta primero: cp -R FarmaciaSolidariaCristiana/Views publish/"
    exit 1
fi

read -sp "🔑 Ingresa contraseña FTP: " PASSWORD
echo ""
echo ""

echo "📤 Conectando a Somee y subiendo vistas..."
echo ""

lftp -c "
set ftp:ssl-allow no
set net:timeout 30
set net:max-retries 3
open -u $USER,$PASSWORD ftp://$HOST
cd $REMOTE_PATH

echo '>>> Eliminando carpeta Views antigua...'
rm -rf Views

echo '>>> Subiendo Views nuevas...'
mirror -R --verbose publish/Views Views

echo '>>> Verificando...'
ls -la Views/Donations/

bye
"

RESULT=$?

echo ""
if [ $RESULT -eq 0 ]; then
    echo "✅ ¡Vistas actualizadas exitosamente!"
    echo ""
    echo "Próximos pasos:"
    echo "1. Ve a https://dashboard.somee.com"
    echo "2. Detén la aplicación (Stop)"
    echo "3. Espera 10 segundos"
    echo "4. Inicia la aplicación (Start)"
    echo "5. Limpia el cache del navegador (Ctrl+Shift+Delete)"
    echo "6. Recarga la página"
else
    echo "⚠️  Proceso completado con código: $RESULT"
    echo "Verifica que la contraseña sea correcta"
fi

echo ""
