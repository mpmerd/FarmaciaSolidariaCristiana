#!/bin/bash

# Script simple para subir solo el DLL, PDB y web.config
# Ejecutar después de detener la aplicación en Somee

PUBLISH_DIR="../publish"
FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="//www.farmaciasolidaria.somee.com"

echo "=========================================="
echo "Subiendo archivos principales"
echo "=========================================="
echo ""
echo "⚠️  Asegúrate que la aplicación esté DETENIDA en Somee"
echo ""
read -p "Ingresa la contraseña FTP: " -s FTP_PASS
echo ""
echo ""

echo "Subiendo FarmaciaSolidariaCristiana.dll..."
lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;
put -O . $PUBLISH_DIR/FarmaciaSolidariaCristiana.dll;
echo 'DLL subido'
"

echo "Subiendo FarmaciaSolidariaCristiana.pdb..."
lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;
put -O . $PUBLISH_DIR/FarmaciaSolidariaCristiana.pdb;
echo 'PDB subido'
"

echo "Subiendo web.config..."
lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;
put -O . $PUBLISH_DIR/web.config;
echo 'web.config subido'
"

echo ""
echo "✅ Archivos principales subidos"
echo ""
echo "Ahora en Somee:"
echo "1. Inicia la aplicación (Start o Restart Application Pool)"
echo "2. Espera 30 segundos"
echo "3. Abre: https://farmaciasolidaria.somee.com"
echo "4. Presiona Ctrl+Shift+R para limpiar caché"
echo "5. Inicia sesión como Admin y verifica el menú 'Gestión Avanzada'"
