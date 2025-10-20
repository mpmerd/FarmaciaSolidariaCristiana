#!/bin/bash

# Script de instalación automática para Farmacia Solidaria Cristiana
# Para Ubuntu Server 20.04/22.04/24.04
# Uso: bash setup-ubuntu.sh

set -e

echo "=========================================="
echo "Farmacia Solidaria Cristiana"
echo "Script de Instalación Automática"
echo "=========================================="
echo ""

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Función para imprimir mensajes
print_message() {
    echo -e "${GREEN}[✓]${NC} $1"
}

print_error() {
    echo -e "${RED}[✗]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

# Verificar si se ejecuta con sudo
if [ "$EUID" -eq 0 ]; then 
    print_error "No ejecutes este script como root. Usa: bash setup-ubuntu.sh"
    exit 1
fi

# Variables
APP_NAME="farmacia"
APP_DIR="/var/www/${APP_NAME}"
APP_USER="www-data"
SERVICE_NAME="${APP_NAME}.service"

echo ""
print_message "Paso 1: Actualizando sistema..."
sudo apt update
sudo apt upgrade -y

echo ""
print_message "Paso 2: Instalando dependencias..."
sudo apt install -y wget curl apt-transport-https software-properties-common

echo ""
print_message "Paso 3: Instalando .NET 9..."

# Detectar versión de Ubuntu
UBUNTU_VERSION=$(lsb_release -rs)
print_message "Ubuntu ${UBUNTU_VERSION} detectado"

# Instalar repositorio de Microsoft
wget https://packages.microsoft.com/config/ubuntu/${UBUNTU_VERSION}/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-9.0 aspnetcore-runtime-9.0

# Verificar instalación
if command -v dotnet &> /dev/null; then
    print_message ".NET $(dotnet --version) instalado correctamente"
else
    print_error "Error al instalar .NET"
    exit 1
fi

echo ""
print_message "Paso 4: Instalando Nginx..."
sudo apt install -y nginx
sudo systemctl start nginx
sudo systemctl enable nginx

echo ""
print_message "Paso 5: Instalando herramientas de SQL Server..."
if ! command -v sqlcmd &> /dev/null; then
    curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
    curl https://packages.microsoft.com/config/ubuntu/${UBUNTU_VERSION}/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list
    sudo apt update
    ACCEPT_EULA=Y sudo apt install -y mssql-tools unixodbc-dev
    echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc
fi

echo ""
print_message "Paso 6: Instalando herramienta Entity Framework Core..."
if ! command -v dotnet-ef &> /dev/null; then
    dotnet tool install --global dotnet-ef
    echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
    export PATH="$PATH:$HOME/.dotnet/tools"
fi

echo ""
print_message "Paso 7: Creando directorio de aplicación..."
sudo mkdir -p ${APP_DIR}
sudo chown -R $USER:$USER ${APP_DIR}

echo ""
print_warning "IMPORTANTE: Ahora necesitas copiar los archivos de la aplicación"
echo "Desde tu Mac, ejecuta:"
echo ""
echo "  cd /Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana"
echo "  dotnet publish -c Release -o ./publish"
echo "  rsync -avz --progress ./publish/ usuario@192.168.2.113:~/farmacia-files/"
echo ""
read -p "Presiona ENTER cuando hayas copiado los archivos..."

if [ -d "$HOME/farmacia-files" ]; then
    print_message "Moviendo archivos a ${APP_DIR}..."
    sudo cp -r $HOME/farmacia-files/* ${APP_DIR}/
    sudo chown -R ${APP_USER}:${APP_USER} ${APP_DIR}
    sudo chmod 750 ${APP_DIR}
else
    print_error "No se encontró el directorio ~/farmacia-files"
    exit 1
fi

echo ""
print_message "Paso 8: Creando servicio systemd..."

# Obtener ruta de dotnet
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
Environment=DOTNET_ROOT=/usr/share/dotnet

[Install]
WantedBy=multi-user.target
EOF

print_message "Servicio systemd creado"

echo ""
print_message "Paso 9: Configurando Nginx..."

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

# Habilitar sitio
sudo ln -sf /etc/nginx/sites-available/${APP_NAME} /etc/nginx/sites-enabled/

# Desabilitar sitio por defecto
sudo rm -f /etc/nginx/sites-enabled/default

# Probar configuración
if sudo nginx -t; then
    print_message "Configuración de Nginx válida"
else
    print_error "Error en configuración de Nginx"
    exit 1
fi

echo ""
print_message "Paso 10: Configurando firewall..."
sudo ufw allow 80/tcp
sudo ufw allow 22/tcp
print_message "Firewall configurado (HTTP y SSH permitidos)"

echo ""
print_message "Paso 11: Iniciando servicios..."

# Recargar systemd
sudo systemctl daemon-reload

# Habilitar e iniciar servicio de la app
sudo systemctl enable ${SERVICE_NAME}
sudo systemctl start ${SERVICE_NAME}

# Reiniciar Nginx
sudo systemctl restart nginx

# Esperar un momento para que los servicios inicien
sleep 3

echo ""
print_message "Paso 12: Verificando servicios..."

# Verificar estado del servicio
if sudo systemctl is-active --quiet ${SERVICE_NAME}; then
    print_message "Servicio ${SERVICE_NAME} está corriendo"
else
    print_error "El servicio ${SERVICE_NAME} no está corriendo"
    echo "Ver logs con: sudo journalctl -u ${SERVICE_NAME} -n 50"
fi

# Verificar Nginx
if sudo systemctl is-active --quiet nginx; then
    print_message "Nginx está corriendo"
else
    print_error "Nginx no está corriendo"
fi

echo ""
echo "=========================================="
echo -e "${GREEN}¡Instalación Completada!${NC}"
echo "=========================================="
echo ""
echo "Tu aplicación debería estar accesible en:"
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
print_warning "NOTA: Asegúrate de que SQL Server esté corriendo y la base de datos configurada"
echo "Para aplicar migraciones: cd ${APP_DIR} && dotnet ef database update"
echo ""
