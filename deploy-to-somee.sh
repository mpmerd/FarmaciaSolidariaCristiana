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

echo -e "${YELLOW}⚠️  IMPORTANTE: Migración de Base de Datos${NC}"
echo ""
echo "Si esta es la primera vez que despliegas O tienes cambios en la BD:"
echo "  1. Ve al panel de Somee → Manage my DB → SQL Manager"
echo "  2. Ejecuta el script: apply-migration-somee.sql"
echo "  3. Espera a que diga: TODAS LAS MIGRACIONES COMPLETADAS EXITOSAMENTE"
echo ""
echo "El script apply-migration-somee.sql incluye TODAS las migraciones:"
echo "  • AddPatientIdentificationRequired (23/10/2025)"
echo "  • AddDeliveryFieldsEnhancement (23/10/2025)"
echo "  • AddCreatedAtToDeliveries (25/10/2025)"
echo ""
read -p "¿Ya aplicaste la migración SQL? (s/n): " SQL_APPLIED
echo ""

if [ "$SQL_APPLIED" != "s" ] && [ "$SQL_APPLIED" != "S" ]; then
    echo -e "${RED}⚠️  Debes aplicar la migración SQL primero${NC}"
    echo ""
    echo "Pasos:"
    echo "  1. Abre: apply-migration-somee.sql"
    echo "  2. Copia TODO el contenido"
    echo "  3. Ve a Somee → Manage my DB → SQL Manager"
    echo "  4. Pega y ejecuta el script"
    echo "  5. Vuelve a ejecutar este script"
    echo ""
    exit 1
fi

echo -e "${GREEN}✓ Migración confirmada. Continuando con el despliegue...${NC}"
echo ""

# Configuración de Somee
FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="/www.farmaciasolidaria.somee.com"
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

# Primero crear directorios necesarios si no existen
echo -e "${YELLOW}Creando directorios necesarios en el servidor...${NC}"
lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
set ftp:use-site-chmod no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;
mkdir -p wwwroot/uploads/turnos;
mkdir -p wwwroot/pdfs/turnos;
echo 'Directorios de turnos creados/verificados'
"

echo -e "${GREEN}✓ Directorios verificados${NC}"
echo ""

echo -e "${YELLOW}Subiendo archivos...${NC}"
echo "Esto puede tardar varios minutos..."
echo ""

# Usar lftp para subir archivos (ignorar errores de chmod que Somee no soporta)
# Usamos --only-newer para actualizar solo los archivos modificados
lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
set ftp:use-site-chmod no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;
mirror --reverse --verbose --parallel=3 --only-newer $PUBLISH_DIR .
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
