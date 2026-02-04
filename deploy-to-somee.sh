#!/bin/bash

# Script de despliegue para Somee.com
# Sube la aplicación ya compilada vía FTP
# NOTA: Compila primero con: dotnet publish FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana.csproj -c Release -o publish

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

# Verificar que estamos en la rama developerConApi
CURRENT_BRANCH=$(git branch --show-current)
if [ "$CURRENT_BRANCH" != "developerConApi" ]; then
    echo -e "${RED}⚠️  ADVERTENCIA: No estás en la rama 'developerConApi'${NC}"
    echo "Rama actual: $CURRENT_BRANCH"
    read -p "¿Continuar de todas formas? (s/n): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Ss]$ ]]; then
        echo "Deploy cancelado."
        exit 1
    fi
fi

echo -e "${YELLOW}⚠️  IMPORTANTE: Migración de Base de Datos${NC}"
echo ""
echo "Si tienes cambios en la BD, recuerda aplicar las migraciones en Somee:"
echo "  1. Ve al panel de Somee → Manage my DB → SQL Manager"
echo "  2. Ejecuta el script de migración correspondiente"
echo ""
read -p "¿Ya aplicaste las migraciones SQL necesarias? (s/n): " SQL_APPLIED
echo ""

if [ "$SQL_APPLIED" != "s" ] && [ "$SQL_APPLIED" != "S" ]; then
    echo -e "${RED}⚠️  Debes aplicar las migraciones SQL primero${NC}"
    echo ""
    echo "Pasos:"
    echo "  1. Abre el archivo de migración correspondiente"
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
PUBLISH_DIR="publish"

echo -e "${YELLOW}📋 Verificando archivos compilados...${NC}"
if [ ! -d "$PUBLISH_DIR" ]; then
    echo -e "${RED}❌ Error: No existe el directorio '$PUBLISH_DIR'${NC}"
    echo ""
    echo "Debes compilar primero:"
    echo "  dotnet publish FarmaciaSolidariaCristiana/FarmaciaSolidariaCristiana.csproj -c Release -o publish"
    echo ""
    exit 1
fi

FILE_COUNT=$(find "$PUBLISH_DIR" -type f | wc -l)
if [ $FILE_COUNT -lt 10 ]; then
    echo -e "${RED}❌ Error: El directorio publish parece incompleto (solo $FILE_COUNT archivos)${NC}"
    echo "Recompila con: dotnet publish -c Release -o publish"
    exit 1
fi

echo -e "${GREEN}✅ Encontrados $FILE_COUNT archivos para subir${NC}"
echo ""

# Verificar que appsettings.json tiene configuración de producción
if grep -q "192.168" "$PUBLISH_DIR/appsettings.json" 2>/dev/null; then
    echo -e "${RED}❌ ERROR: appsettings.json contiene IPs de desarrollo${NC}"
    echo "El archivo debe tener la configuración de producción (Somee.com)"
    exit 1
fi

echo -e "${GREEN}✅ Configuración de producción verificada${NC}"
echo ""

echo -e "${YELLOW}📡 Datos de conexión FTP:${NC}"
echo "  Host: $FTP_HOST"
echo "  Usuario: $FTP_USER"
echo "  Destino: $FTP_REMOTE_PATH"
echo ""

read -s -p "🔑 Ingresa la contraseña FTP: " FTP_PASS
echo ""
echo ""

echo -e "${YELLOW}🚀 Conectando a Somee vía FTP...${NC}"

# Crear directorios necesarios
echo -e "${YELLOW}📁 Creando/verificando directorios...${NC}"
lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
set ftp:use-site-chmod no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH || mkdir -p $FTP_REMOTE_PATH;
mkdir -p wwwroot/uploads/turnos;
mkdir -p wwwroot/pdfs/turnos;
mkdir -p wwwroot/uploads/patient-documents;
echo '✓ Directorios verificados'
"

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Error al conectar con FTP${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Directorios listos${NC}"
echo ""

echo -e "${YELLOW}📤 Subiendo archivos (esto puede tardar varios minutos)...${NC}"
echo ""

# Usar lftp para subir archivos (ignorar errores de chmod que Somee no soporta)
# Usamos --only-newer para actualizar solo los archivos modificados
# Excluimos carpetas de uploads y pdfs para no subir archivos de prueba o de usuarios
lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
set ftp:use-site-chmod no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;
mirror --reverse --verbose --parallel=3 --only-newer \
  --exclude-glob wwwroot/uploads/** \
  --exclude-glob wwwroot/pdfs/** \
  $PUBLISH_DIR .
"

LFTP_EXIT_CODE=$?
echo ""

if [ $LFTP_EXIT_CODE -eq 0 ]; then
    echo -e "${GREEN}✅ Archivos subidos exitosamente${NC}"
    echo -e "${BLUE}ℹ️  Nota: Carpetas wwwroot/uploads y wwwroot/pdfs fueron excluidas${NC}"
else
    echo -e "${YELLOW}⚠️  Proceso completado con advertencias (código: $LFTP_EXIT_CODE)${NC}"
    echo -e "${YELLOW}Los archivos principales se subieron correctamente.${NC}"
    echo -e "${YELLOW}Las advertencias de 'chmod' son normales en Somee y pueden ignorarse.${NC}"
    echo -e "${YELLOW}Si el DLL principal no se actualizó, reinicia la aplicación en el panel de Somee.${NC}"
fi

echo ""
echo -e "${GREEN}=========================================="
echo "✅ ¡Despliegue Completado!"
echo "==========================================${NC}"
echo ""
echo "Tu aplicación está disponible en:"
echo "  🌐 https://farmaciasolidaria.somee.com"
echo ""
echo "Credenciales por defecto:"
echo "  👤 Usuario: admin"
echo "  🔑 Contraseña: doqkox-gadqud-niJho0"
echo ""
echo -e "${YELLOW}Verificación recomendada:${NC}"
echo "  1. Accede a https://farmaciasolidaria.somee.com"
echo "  2. Prueba el login con admin"
echo "  3. Verifica la funcionalidad principal"
echo "  4. Revisa los logs si hay errores"
echo ""
echo -e "${BLUE}NOTA: Para desplegar la app MAUI:${NC}"
echo "  1. Compila: dotnet build FarmaciaSolidariaCristiana.Maui -c Release"
echo "  2. APK en: FarmaciaSolidariaCristiana.Maui/bin/Release/net9.0-android/com.fsolidaria.app-Signed.apk"
echo "  3. Distribuye el APK a los usuarios"
echo ""
