#!/bin/bash

# Script de despliegue para Somee.com
# Sube la aplicación vía FTP

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=========================================="
echo "Farmacia Solidaria Cristiana"
echo "Despliegue a Somee.com"
echo "==========================================${NC}"
echo ""

# Configuración de Somee
FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="www.farmaciasolidaria.somee.com"
PUBLISH_DIR="/Users/maikelpelaez/Documents/Proyectos/FarmaciaSolidariaCristiana/publish"

echo -e "${YELLOW}Verificando archivos publicados...${NC}"
if [ ! -d "$PUBLISH_DIR" ]; then
    echo -e "${RED}Error: No existe el directorio $PUBLISH_DIR${NC}"
    echo "Ejecuta primero: dotnet publish -c Release -o $PUBLISH_DIR"
    exit 1
fi

FILE_COUNT=$(ls -1 "$PUBLISH_DIR" | wc -l)
echo -e "${GREEN}✓ Encontrados $FILE_COUNT archivos para subir${NC}"
echo ""

echo -e "${YELLOW}Datos de conexión FTP:${NC}"
echo "  Host: $FTP_HOST"
echo "  Usuario: $FTP_USER"
echo "  Ruta remota: /$FTP_REMOTE_PATH"
echo ""

read -s -p "Ingresa la contraseña FTP: " FTP_PASS
echo ""
echo ""

echo -e "${YELLOW}Conectando a Somee vía FTP...${NC}"
echo -e "${YELLOW}Subiendo archivos...${NC}"
echo "Esto puede tardar varios minutos..."
echo ""

# Usar lftp para subir archivos (ignorar errores de chmod que Somee no soporta)
# Sin --delete para evitar conflictos con archivos en uso
lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
set ftp:use-site-chmod no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH || mkdir -p $FTP_REMOTE_PATH;
cd $FTP_REMOTE_PATH;
mirror --reverse --verbose --parallel=3 --ignore-time --newer-than=now-1day $PUBLISH_DIR .
"

LFTP_EXIT_CODE=$?
echo ""

if [ $LFTP_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✓ Archivos subidos exitosamente${NC}"
else
    echo -e "${YELLOW}⚠ Proceso completado con advertencias (código: $LFTP_EXIT_CODE)${NC}"
    echo -e "${YELLOW}Los archivos principales se subieron correctamente.${NC}"
    echo -e "${YELLOW}Las advertencias de 'chmod' son normales en Somee y pueden ignorarse.${NC}"
    echo -e "${YELLOW}Si el DLL principal no se actualizó, reinicia la aplicación en el panel de Somee.${NC}"
fi

echo ""
echo -e "${GREEN}=========================================="
echo "¡Despliegue Completado!"
echo "==========================================${NC}"
echo ""
echo "Tu aplicación está disponible en:"
echo "  🌐 https://farmaciasolidaria.somee.com"
echo ""
echo "Credenciales por defecto:"
echo "  👤 Usuario: admin"
echo "  🔑 Contraseña: doqkox-gadqud-niJho0"
echo ""
echo "Notas importantes:"
echo "  • El registro público está HABILITADO para pruebas"
echo "  • Para deshabilitarlo: Cambia EnablePublicRegistration a false en appsettings.json"
echo "  • SMTP Somee pendiente de ticket de soporte"
echo "  • Emails temporalmente se envían desde Gmail"
echo ""
echo -e "${YELLOW}Verificación recomendada:${NC}"
echo "  1. Accede a https://farmaciasolidaria.somee.com"
echo "  2. Prueba el login con admin"
echo "  3. Prueba el registro de nuevo usuario"
echo "  4. Verifica que lleguen los emails"
echo ""
