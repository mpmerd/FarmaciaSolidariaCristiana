#!/bin/bash

# Script rápido de actualización
# Uso: bash quick-update.sh

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${YELLOW}Actualizando aplicación en el servidor...${NC}"
echo ""

echo "1. Deteniendo servicio..."
sudo systemctl stop farmacia.service

echo "2. Copiando archivos nuevos..."
sudo rm -rf /var/www/farmacia/*
sudo cp -r ~/farmacia-files/* /var/www/farmacia/
sudo chown -R www-data:www-data /var/www/farmacia

echo "3. Reiniciando servicio..."
sudo systemctl start farmacia.service

sleep 2

echo "4. Verificando estado..."
if sudo systemctl is-active --quiet farmacia.service; then
    echo -e "${GREEN}✓ Servicio corriendo correctamente${NC}"
    sudo systemctl status farmacia.service --no-pager -l
else
    echo -e "${YELLOW}! Error: Servicio no está corriendo${NC}"
    echo "Ver logs: sudo journalctl -u farmacia.service -n 50"
fi

echo ""
echo -e "${GREEN}Actualización completada${NC}"
