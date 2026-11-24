#!/bin/bash

# Script de instalación simplificado - .NET ya instalado
# Para Ubuntu con .NET 8 ya presente

set -e

echo "=========================================="
echo "Farmacia Solidaria Cristiana"
echo "Instalación Simplificada"
echo "=========================================="
echo ""

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

print_message() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

if [ "$EUID" -eq 0 ]; then 
    print_error "No ejecutes este script como root. Usa: bash setup-simple.sh"
    exit 1
fi

APP_NAME="farmacia"
APP_DIR="/var/www/${APP_NAME}"
APP_USER="www-data"
SERVICE_NAME="${APP_NAME}.service"

echo ""
print_message "Verificando .NET..."
DOTNET_VERSION=$(dotnet --version)
print_message ".NET ${DOTNET_VERSION} detectado"

echo ""
print_message "Paso 1: Instalando Nginx..."
if command -v nginx &> /dev/null; then
    print_message "Nginx ya está instalado"
else
    sudo apt update
    sudo apt install -y nginx
    sudo systemctl start nginx
    sudo systemctl enable nginx
    print_message "Nginx instalado"
fi

echo ""
print_message "Paso 2: Creando directorio de aplicación..."
sudo mkdir -p ${APP_DIR}
sudo chown -R $USER:$USER ${APP_DIR}

if [ ! -d "$HOME/farmacia-files" ]; then
    print_error "No se encontró ~/farmacia-files"
    print_warning "Asegúrate de haber copiado los archivos primero"
    exit 1
fi

print_message "Copiando archivos de aplicación..."
sudo cp -r $HOME/farmacia-files/* ${APP_DIR}/
sudo chown -R ${APP_USER}:${APP_USER} ${APP_DIR}
sudo chmod 750 ${APP_DIR}
print_message "Archivos copiados"

echo ""
print_message "Paso 3: Creando servicio systemd..."

DOTNET_PATH=$(which dotnet)

sudo tee /etc/systemd/system/${SERVICE_NAME} > /dev/null <<EOF
[Unit]
Description=Farmacia Solidaria Cristiana - ASP.NET Core App
After=network.target

[Service]
WorkingDirectory=${APP_DIR}
ExecStart=${DOTNET_PATH} ${APP_DIR}/FarmaciaSolidariaCristiana.dll --urls="http://localhost:5000"
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=farmacia-app
User=${APP_USER}
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_ROOT=/usr/lib/dotnet

[Install]
WantedBy=multi-user.target
EOF

print_message "Servicio systemd creado"

echo ""
print_message "Paso 4: Configurando Nginx..."

sudo tee /etc/nginx/sites-available/${APP_NAME} > /dev/null <<'EOF'
server {
    listen 80;
    listen [::]:80;
    server_name 192.168.2.113 MPMESCRITORIO farmacia.local;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }

    location ~* \.(css|js|gif|jpe?g|png|ico|svg|woff|woff2|ttf|eot)$ {
        proxy_pass http://localhost:5000;
        proxy_cache_valid 200 1d;
        expires 1d;
        add_header Cache-Control "public, immutable";
    }

    client_max_body_size 10M;
}
EOF

sudo ln -sf /etc/nginx/sites-available/${APP_NAME} /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default

if sudo nginx -t; then
    print_message "Configuración de Nginx válida"
else
    print_error "Error en configuración de Nginx"
    exit 1
fi

echo ""
print_message "Paso 5: Configurando firewall..."
sudo ufw allow 80/tcp 2>/dev/null || true
sudo ufw allow 22/tcp 2>/dev/null || true
print_message "Firewall configurado"

echo ""
print_message "Paso 6: Iniciando servicios..."

sudo systemctl daemon-reload
sudo systemctl enable ${SERVICE_NAME}
sudo systemctl start ${SERVICE_NAME}
sudo systemctl restart nginx

sleep 3

echo ""
print_message "Paso 7: Verificando servicios..."

if sudo systemctl is-active --quiet ${SERVICE_NAME}; then
    print_message "Servicio ${SERVICE_NAME} está corriendo"
else
    print_error "El servicio ${SERVICE_NAME} no está corriendo"
    echo "Ver logs con: sudo journalctl -u ${SERVICE_NAME} -n 50"
    exit 1
fi

if sudo systemctl is-active --quiet nginx; then
    print_message "Nginx está corriendo"
else
    print_error "Nginx no está corriendo"
    exit 1
fi

echo ""
echo "=========================================="
echo -e "${GREEN}¡Instalación Completada!${NC}"
echo "=========================================="
echo ""
echo "Tu aplicación está disponible en:"
echo "  - http://192.168.2.113"
echo "  - http://MPMESCRITORIO"
echo ""
echo "Credenciales de administrador:"
echo "  Usuario: admin"
echo "  Contraseña: doqkox-gadqud-niJho0"
echo ""
echo "Comandos útiles:"
echo "  - Ver logs: sudo journalctl -u ${SERVICE_NAME} -f"
echo "  - Reiniciar: sudo systemctl restart ${SERVICE_NAME}"
echo "  - Estado: sudo systemctl status ${SERVICE_NAME}"
echo ""
