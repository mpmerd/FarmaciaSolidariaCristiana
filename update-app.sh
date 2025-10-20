#!/bin/bash

# Script para actualizar la aplicación Farmacia Solidaria Cristiana
# Para ejecutar en el servidor Ubuntu

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

APP_DIR="/var/www/farmacia"
BACKUP_DIR="/var/www/farmacia-backup-$(date +%Y%m%d-%H%M%S)"
NEW_FILES_DIR="$HOME/farmacia-new"
SERVICE_NAME="farmacia.service"

echo -e "${GREEN}=========================================="
echo "Actualización de Farmacia Solidaria"
echo "==========================================${NC}"
echo ""

# Verificar que existan archivos nuevos
if [ ! -d "$NEW_FILES_DIR" ]; then
    echo -e "${RED}Error: No se encontró el directorio $NEW_FILES_DIR${NC}"
    echo ""
    echo "Primero copia los archivos desde tu Mac:"
    echo "  rsync -avz --progress ./publish/ usuario@192.168.2.113:~/farmacia-new/"
    exit 1
fi

echo -e "${YELLOW}[1/7]${NC} Deteniendo servicio..."
sudo systemctl stop ${SERVICE_NAME}
echo -e "${GREEN}✓${NC} Servicio detenido"

echo ""
echo -e "${YELLOW}[2/7]${NC} Creando respaldo..."
sudo cp -r ${APP_DIR} ${BACKUP_DIR}
echo -e "${GREEN}✓${NC} Respaldo creado en: ${BACKUP_DIR}"

echo ""
echo -e "${YELLOW}[3/7]${NC} Guardando configuración actual..."
sudo cp ${APP_DIR}/appsettings.json /tmp/appsettings.json.backup
echo -e "${GREEN}✓${NC} Configuración guardada"

echo ""
echo -e "${YELLOW}[4/7]${NC} Eliminando archivos antiguos..."
sudo rm -rf ${APP_DIR}/*
echo -e "${GREEN}✓${NC} Archivos antiguos eliminados"

echo ""
echo -e "${YELLOW}[5/7]${NC} Copiando archivos nuevos..."
sudo cp -r ${NEW_FILES_DIR}/* ${APP_DIR}/
sudo cp /tmp/appsettings.json.backup ${APP_DIR}/appsettings.json
sudo chown -R www-data:www-data ${APP_DIR}
sudo chmod 750 ${APP_DIR}
echo -e "${GREEN}✓${NC} Archivos actualizados"

echo ""
echo -e "${YELLOW}[6/7]${NC} Aplicando migraciones de base de datos..."
cd ${APP_DIR}
if dotnet ef database update 2>/dev/null; then
    echo -e "${GREEN}✓${NC} Migraciones aplicadas"
else
    echo -e "${YELLOW}! No se pudieron aplicar migraciones automáticamente${NC}"
    echo "  Ejecuta manualmente: cd ${APP_DIR} && dotnet ef database update"
fi

echo ""
echo -e "${YELLOW}[7/7]${NC} Iniciando servicio..."
sudo systemctl start ${SERVICE_NAME}
sleep 2

if sudo systemctl is-active --quiet ${SERVICE_NAME}; then
    echo -e "${GREEN}✓${NC} Servicio iniciado correctamente"
else
    echo -e "${RED}✗ Error: El servicio no pudo iniciarse${NC}"
    echo "Ver logs con: sudo journalctl -u ${SERVICE_NAME} -n 50"
    echo ""
    echo "Restaurando respaldo..."
    sudo systemctl stop ${SERVICE_NAME}
    sudo rm -rf ${APP_DIR}/*
    sudo cp -r ${BACKUP_DIR}/* ${APP_DIR}/
    sudo systemctl start ${SERVICE_NAME}
    echo "Respaldo restaurado"
    exit 1
fi

echo ""
echo -e "${GREEN}=========================================="
echo "¡Actualización Completada!"
echo "==========================================${NC}"
echo ""
echo "Estado del servicio:"
sudo systemctl status ${SERVICE_NAME} --no-pager -l
echo ""
echo "Para limpiar archivos temporales:"
echo "  rm -rf ${NEW_FILES_DIR}"
echo ""
echo "Respaldo disponible en:"
echo "  ${BACKUP_DIR}"
echo ""
