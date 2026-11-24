#!/bin/bash

# Script interactivo de despliegue
# Guía paso a paso para transferir y desplegar la aplicación

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=========================================="
echo "Farmacia Solidaria Cristiana"
echo "Asistente de Despliegue Interactivo"
echo "==========================================${NC}"
echo ""

# Solicitar información del servidor
echo -e "${YELLOW}Configuración del Servidor${NC}"
echo ""
read -p "Nombre de usuario SSH en Ubuntu [usuario]: " SSH_USER
SSH_USER=${SSH_USER:-usuario}

read -p "IP del servidor [192.168.2.113]: " SERVER_IP
SERVER_IP=${SERVER_IP:-192.168.2.113}

echo ""
echo -e "${GREEN}Probando conexión SSH...${NC}"
if ssh -o BatchMode=yes -o ConnectTimeout=5 ${SSH_USER}@${SERVER_IP} exit 2>/dev/null; then
    echo -e "${GREEN}✓ Conexión SSH exitosa (usando clave SSH)${NC}"
    USE_PASSWORD="no"
else
    echo -e "${YELLOW}! Se necesitará contraseña para SSH${NC}"
    USE_PASSWORD="yes"
    echo ""
    echo "Para evitar escribir la contraseña múltiples veces, puedes configurar SSH keys:"
    echo "  ssh-keygen -t ed25519"
    echo "  ssh-copy-id ${SSH_USER}@${SERVER_IP}"
    echo ""
    read -p "¿Quieres continuar con contraseña? (y/n): " CONTINUE
    if [ "$CONTINUE" != "y" ] && [ "$CONTINUE" != "Y" ]; then
        echo "Abortado por el usuario"
        exit 0
    fi
fi

# Directorios
PROJECT_DIR="/Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana"
APP_DIR="${PROJECT_DIR}/FarmaciaSolidariaCristiana"

echo ""
echo -e "${YELLOW}[1/6] Publicando aplicación...${NC}"
cd "${APP_DIR}"
if [ -d "./publish" ]; then
    echo "Limpiando publicación anterior..."
    rm -rf ./publish
fi
dotnet publish -c Release -o ./publish
echo -e "${GREEN}✓ Aplicación publicada${NC}"

echo ""
echo -e "${YELLOW}[2/6] Transfiriendo script de instalación...${NC}"
cd "${PROJECT_DIR}"
scp setup-ubuntu.sh ${SSH_USER}@${SERVER_IP}:~/
echo -e "${GREEN}✓ Script transferido${NC}"

echo ""
echo -e "${YELLOW}[3/6] Transfiriendo archivos de aplicación...${NC}"
echo "Esto puede tardar varios minutos dependiendo de tu conexión..."
rsync -avz --progress ./FarmaciaSolidariaCristiana/publish/ ${SSH_USER}@${SERVER_IP}:~/farmacia-files/
echo -e "${GREEN}✓ Archivos transferidos${NC}"

echo ""
echo -e "${YELLOW}[4/6] Verificando archivos en el servidor...${NC}"
ssh ${SSH_USER}@${SERVER_IP} "ls -lh ~/farmacia-files/ | head -10"
echo -e "${GREEN}✓ Archivos verificados${NC}"

echo ""
echo -e "${YELLOW}[5/6] Información importante antes de instalar:${NC}"
echo ""
echo "El script de instalación realizará:"
echo "  • Instalación de .NET 9 SDK y Runtime"
echo "  • Instalación de Nginx"
echo "  • Configuración de servicio systemd"
echo "  • Configuración de Nginx como proxy reverso"
echo "  • Configuración de firewall"
echo ""
echo "Necesitarás permisos sudo en el servidor."
echo ""

read -p "¿Ejecutar instalación ahora? (y/n): " RUN_INSTALL

if [ "$RUN_INSTALL" == "y" ] || [ "$RUN_INSTALL" == "Y" ]; then
    echo ""
    echo -e "${YELLOW}[6/6] Ejecutando instalación en el servidor...${NC}"
    echo ""
    echo -e "${BLUE}====== SALIDA DEL SERVIDOR ======${NC}"
    ssh -t ${SSH_USER}@${SERVER_IP} "bash ~/setup-ubuntu.sh"
    echo -e "${BLUE}====== FIN SALIDA DEL SERVIDOR ======${NC}"
    echo ""
    
    echo -e "${GREEN}=========================================="
    echo "¡Despliegue Completado!"
    echo "==========================================${NC}"
    echo ""
    echo "Tu aplicación debería estar disponible en:"
    echo "  🌐 http://${SERVER_IP}"
    echo ""
    echo "Credenciales de acceso:"
    echo "  👤 Usuario: admin"
    echo "  🔑 Contraseña: doqkox-gadqud-niJho0"
    echo ""
    echo "Para ver logs:"
    echo "  ssh ${SSH_USER}@${SERVER_IP}"
    echo "  sudo journalctl -u farmacia.service -f"
    echo ""
else
    echo ""
    echo -e "${YELLOW}Instalación manual:${NC}"
    echo ""
    echo "Conéctate al servidor:"
    echo "  ssh ${SSH_USER}@${SERVER_IP}"
    echo ""
    echo "Ejecuta la instalación:"
    echo "  bash ~/setup-ubuntu.sh"
    echo ""
    echo "Sigue las instrucciones en pantalla."
    echo ""
fi

echo -e "${GREEN}=========================================="
echo "Proceso Finalizado"
echo "==========================================${NC}"
