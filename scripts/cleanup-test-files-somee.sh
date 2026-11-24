#!/bin/bash

# Script para eliminar archivos de prueba del servidor Somee.com

set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}=========================================="
echo "Limpieza de archivos de prueba"
echo "Servidor: farmaciasolidaria.somee.com"
echo "==========================================${NC}"
echo ""

FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="/www.farmaciasolidaria.somee.com"

echo -e "${YELLOW}⚠️  ADVERTENCIA${NC}"
echo "Este script eliminará los siguientes archivos de prueba:"
echo ""
echo "  📁 wwwroot/pdfs/turnos/turno_*.pdf (turnos 92-1101)"
echo "  📁 wwwroot/uploads/patient-documents/patient_21_*"
echo "  📁 wwwroot/uploads/patient-documents/patient_32_*"
echo "  📁 wwwroot/uploads/turnos/* (todos los archivos)"
echo ""
read -p "¿Estás seguro de continuar? (s/n): " -n 1 -r
echo ""
if [[ ! $REPLY =~ ^[Ss]$ ]]; then
    echo "Operación cancelada."
    exit 0
fi

read -s -p "🔑 Ingresa la contraseña FTP: " FTP_PASS
echo ""
echo ""

echo -e "${YELLOW}🗑️  Eliminando archivos de prueba...${NC}"

lftp -c "
set ssl:verify-certificate no;
set ftp:use-feat no;
set ftp:use-site-chmod no;
open -u $FTP_USER,$FTP_PASS ftp://$FTP_HOST;
cd $FTP_REMOTE_PATH;

echo 'Eliminando PDFs de turnos de prueba...';
mrm wwwroot/pdfs/turnos/turno_92_*.pdf;
mrm wwwroot/pdfs/turnos/turno_93_*.pdf;
mrm wwwroot/pdfs/turnos/turno_94_*.pdf;
mrm wwwroot/pdfs/turnos/turno_95_*.pdf;
mrm wwwroot/pdfs/turnos/turno_96_*.pdf;
mrm wwwroot/pdfs/turnos/turno_97_*.pdf;
mrm wwwroot/pdfs/turnos/turno_98_*.pdf;
mrm wwwroot/pdfs/turnos/turno_99_*.pdf;
mrm wwwroot/pdfs/turnos/turno_100_*.pdf;
mrm wwwroot/pdfs/turnos/turno_109*.pdf;
mrm wwwroot/pdfs/turnos/turno_110*.pdf;

echo 'Eliminando documentos del paciente 21...';
mrm wwwroot/uploads/patient-documents/patient_21_*.jpg;
mrm wwwroot/uploads/patient-documents/patient_21_*.pdf;
mrm wwwroot/uploads/patient-documents/21_*.jpg;

echo 'Eliminando documentos del paciente 32...';
mrm wwwroot/uploads/patient-documents/patient_32_*.jpg;
mrm wwwroot/uploads/patient-documents/patient_32_*.pdf;

echo 'Eliminando archivos de turnos...';
mrm wwwroot/uploads/turnos/*.jpg;
mrm wwwroot/uploads/turnos/*.pdf;

echo 'Limpieza completada';
"

LFTP_EXIT_CODE=$?

if [ $LFTP_EXIT_CODE -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✅ Archivos de prueba eliminados exitosamente${NC}"
    echo ""
    echo -e "${BLUE}Espacio liberado en el servidor${NC}"
else
    echo ""
    echo -e "${YELLOW}⚠️  Proceso completado con advertencias (código: $LFTP_EXIT_CODE)${NC}"
    echo "Algunos archivos pueden no haber existido o ya fueron eliminados."
fi

echo ""
echo -e "${GREEN}=========================================="
echo "✅ Limpieza completada"
echo "==========================================${NC}"
