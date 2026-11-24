#!/bin/bash

# Script para preparar paquete de despliegue en USB
# Uso: bash prepare-usb-package.sh

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}=========================================="
echo "Preparando Paquete para USB"
echo "==========================================${NC}"
echo ""

# Directorio del proyecto
PROJECT_DIR="/Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana"
APP_DIR="${PROJECT_DIR}/FarmaciaSolidariaCristiana"
USB_PKG_DIR="${PROJECT_DIR}/usb-deployment"

echo -e "${YELLOW}[1/5]${NC} Limpiando directorio temporal..."
rm -rf "${USB_PKG_DIR}"
mkdir -p "${USB_PKG_DIR}"

echo -e "${YELLOW}[2/5]${NC} Publicando aplicación..."
cd "${APP_DIR}"
dotnet publish -c Release -o ./publish

echo -e "${YELLOW}[3/5]${NC} Copiando archivos de aplicación..."
cp -r "${APP_DIR}/publish" "${USB_PKG_DIR}/farmacia-app"

echo -e "${YELLOW}[4/5]${NC} Copiando scripts y documentación..."
cd "${PROJECT_DIR}"
cp setup-ubuntu.sh "${USB_PKG_DIR}/"
cp update-app.sh "${USB_PKG_DIR}/"
cp DEPLOYMENT_UBUNTU.md "${USB_PKG_DIR}/"
cp DEPLOYMENT_MANUAL_USB.md "${USB_PKG_DIR}/"
cp QUICK_COMMANDS.md "${USB_PKG_DIR}/"

# Crear README para USB
cat > "${USB_PKG_DIR}/README_USB.txt" << 'EOF'
==============================================
FARMACIA SOLIDARIA CRISTIANA
Paquete de Instalación para Ubuntu Server
==============================================

CONTENIDO:
----------
📁 farmacia-app/          - Aplicación publicada
📄 setup-ubuntu.sh        - Script de instalación automática
📄 update-app.sh          - Script de actualización
📄 DEPLOYMENT_*.md        - Documentación detallada
📄 QUICK_COMMANDS.md      - Comandos de referencia

INSTALACIÓN RÁPIDA:
-------------------
1. Copia esta carpeta al servidor Ubuntu:
   cp -r usb-deployment/* ~/

2. Instala .NET 9:
   wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb
   sudo dpkg -i packages-microsoft-prod.deb
   sudo apt update
   sudo apt install -y dotnet-sdk-9.0 aspnetcore-runtime-9.0

3. Instala Nginx:
   sudo apt install -y nginx

4. Copia archivos de la app:
   sudo mkdir -p /var/www/farmacia
   sudo cp -r ~/farmacia-app/* /var/www/farmacia/
   sudo chown -R www-data:www-data /var/www/farmacia

5. Sigue los pasos en DEPLOYMENT_MANUAL_USB.md

ACCESO A LA APLICACIÓN:
------------------------
URL: http://192.168.2.113
Usuario: admin
Contraseña: doqkox-gadqud-niJho0

SOPORTE:
--------
Ver documentación completa en los archivos .md incluidos.

==============================================
Iglesia Metodista de Cárdenas 
y Adriano Solidario
==============================================
EOF

echo -e "${YELLOW}[5/5]${NC} Creando archivo comprimido..."
cd "${PROJECT_DIR}"
tar -czf farmacia-deployment.tar.gz -C usb-deployment .
zip -r farmacia-deployment.zip usb-deployment/ > /dev/null 2>&1

echo ""
echo -e "${GREEN}=========================================="
echo "¡Paquete Preparado!"
echo "==========================================${NC}"
echo ""
echo "Archivos creados:"
echo "  📁 ${USB_PKG_DIR}/"
echo "  📦 ${PROJECT_DIR}/farmacia-deployment.tar.gz"
echo "  📦 ${PROJECT_DIR}/farmacia-deployment.zip"
echo ""
echo "Tamaño del paquete:"
du -sh "${USB_PKG_DIR}"
du -sh "${PROJECT_DIR}/farmacia-deployment.tar.gz"
du -sh "${PROJECT_DIR}/farmacia-deployment.zip"
echo ""
echo "Para copiar a USB:"
echo "  1. Conecta tu USB"
echo "  2. Identifica el punto de montaje: ls /Volumes/"
echo "  3. Copia: cp -r usb-deployment /Volumes/TU_USB/farmacia/"
echo ""
echo "O descomprime el archivo .tar.gz/.zip directamente en la USB"
echo ""
