#!/bin/bash
# =====================================================================================
# SCRIPT DE LIMPIEZA DE ARCHIVOS SUBIDOS VÍA FTP - Farmacia Solidaria Cristiana
# =====================================================================================
# Versión 2.0
# Fecha: 15 de noviembre de 2025
# 
# ⚠️ ADVERTENCIA: Este script ELIMINARÁ archivos físicos del servidor vía FTP:
#    - Turnos: Recetas médicas, tarjetones, PDFs generados
#    - Pacientes: Documentos adjuntos
#    - Decoraciones: Imágenes personalizadas del navbar
# 
# ✅ PRESERVARÁ:
#    - Logos: logo-iglesia.png, logo-adriano.png
#    - Imágenes de patrocinadores (carpeta sponsors/)
#    - Archivos del sistema (css, js, lib, favicon, etc.)
# 
# 🎯 PROPÓSITO: 
#    Complementar el script SQL reset-production-data.sql eliminando archivos huérfanos
#    que quedan en el servidor después de limpiar la base de datos.
# 
# ⚡ EJECUCIÓN: 
#    1. Ejecutar primero reset-production-data.sql en Somee.com
#    2. Luego ejecutar este script: ./clean-uploaded-files.sh
# 
# 📝 REQUISITOS: lftp instalado (brew install lftp)
# =====================================================================================

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo "========================================================================="
echo -e "${BLUE}LIMPIEZA DE ARCHIVOS VÍA FTP - Farmacia Solidaria Cristiana${NC}"
echo "========================================================================="
echo ""
echo -e "${YELLOW}⚠️  ESTE SCRIPT ELIMINARÁ ARCHIVOS DEL SERVIDOR:${NC}"
echo "   • Turnos: Recetas, tarjetones, PDFs"
echo "   • Pacientes: Documentos adjuntos"
echo "   • Decoraciones navbar: Imágenes personalizadas"
echo ""
echo -e "${GREEN}✅ PRESERVARÁ:${NC}"
echo "   • Logos del sistema"
echo "   • Imágenes de patrocinadores"
echo "   • Archivos del sistema (css, js, lib, etc.)"
echo ""
echo "========================================================================="
echo ""

# Verificar que lftp esté instalado
if ! command -v lftp &> /dev/null; then
    echo -e "${RED}❌ ERROR: lftp no está instalado${NC}"
    echo "   Instala con: brew install lftp"
    exit 1
fi

# Configuración FTP (misma que deploy-to-somee.sh)
FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_PATH="//www.farmaciasolidaria.somee.com"

echo "Datos de conexión FTP:"
echo "  Host: $FTP_HOST"
echo "  Usuario: $FTP_USER"
echo "  Ruta remota: $FTP_PATH"
echo ""

# Solicitar contraseña FTP
read -sp "Ingresa la contraseña FTP: " FTP_PASS
echo ""
echo ""

# Confirmación interactiva
echo -e "${YELLOW}⚠️  ADVERTENCIA: Eliminarás archivos del servidor de producción${NC}"
echo ""
read -p "¿Deseas continuar? (escribe 'SI' para confirmar): " confirmacion

if [ "$confirmacion" != "SI" ]; then
    echo ""
    echo -e "${RED}❌ Operación cancelada por el usuario${NC}"
    exit 0
fi

echo ""
echo "========================================================================="
echo "PARTE 1: Conectando al servidor FTP..."
echo "========================================================================="
echo ""

# Conectar y ejecutar comandos vía lftp
echo "Conectando a Somee vía FTP..."
echo ""

lftp -c "
set ssl:verify-certificate no
set ftp:ssl-allow true
set ftp:ssl-protect-data true

echo 'Conectando al servidor...'
open -u $FTP_USER,$FTP_PASS $FTP_HOST

echo 'Cambiando a directorio raíz...'
cd $FTP_PATH || exit 1

echo ''
echo '========================================================================'
echo 'PARTE 2: Eliminando archivos de turnos...'
echo '========================================================================'
echo ''

# 1. Eliminar archivos de uploads/turnos (recetas y tarjetones)
echo 'Listando archivos en wwwroot/uploads/turnos/'
ls wwwroot/uploads/turnos/
echo 'Eliminando wwwroot/uploads/turnos/*'
cd wwwroot/uploads/turnos
mrm *.* || true
rm -f .gitkeep || true
echo '✓ Turnos eliminados'
cd $FTP_PATH

echo ''
echo '========================================================================'
echo 'PARTE 3: Eliminando documentos de pacientes...'
echo '========================================================================'
echo ''

# 2. Eliminar documentos de pacientes
echo 'Listando archivos en wwwroot/uploads/patient-documents/'
ls wwwroot/uploads/patient-documents/
echo 'Eliminando wwwroot/uploads/patient-documents/*'
cd wwwroot/uploads/patient-documents
mrm *.* || true
rm -f .gitkeep || true
echo '✓ Documentos eliminados'
cd $FTP_PATH

echo ''
echo '========================================================================'
echo 'PARTE 4: Eliminando decoraciones personalizadas...'
echo '========================================================================'
echo ''

# 3. Eliminar decoraciones personalizadas
echo 'Listando archivos en wwwroot/uploads/decorations/'
ls wwwroot/uploads/decorations/
echo 'Eliminando wwwroot/uploads/decorations/*'
cd wwwroot/uploads/decorations
mrm *.* || true
rm -f .gitkeep || true
echo '✓ Decoraciones eliminadas'
cd $FTP_PATH

echo ''
echo '========================================================================'
echo 'PARTE 5: Eliminando PDFs de turnos...'
echo '========================================================================'
echo ''

# 4. Eliminar PDFs de turnos
echo 'Listando archivos en wwwroot/pdfs/turnos/'
ls wwwroot/pdfs/turnos/
echo 'Eliminando wwwroot/pdfs/turnos/*'
cd wwwroot/pdfs/turnos
mrm *.pdf || true
echo '✓ PDFs eliminados (algunos pueden estar en uso)'
cd $FTP_PATH

echo ''
echo '========================================================================'
echo 'PARTE 6: Verificando eliminación...'
echo '========================================================================'
echo ''

echo 'Verificando que las carpetas estén vacías...'
echo 'Turnos restantes:'
ls wwwroot/uploads/turnos/ | wc -l
echo 'Patient-documents restantes:'
ls wwwroot/uploads/patient-documents/ | wc -l
echo 'Decorations restantes:'
ls wwwroot/uploads/decorations/ | wc -l
echo 'PDFs turnos restantes:'
ls wwwroot/pdfs/turnos/ | wc -l

echo ''
echo '========================================================================'
echo 'PARTE 7: Verificando archivos preservados...'
echo '========================================================================'
echo ''

# Verificar logos
echo 'Verificando logos en wwwroot/images/...'
ls wwwroot/images/logo-*.png

echo ''
echo 'Verificando patrocinadores en wwwroot/images/sponsors/...'
ls wwwroot/images/sponsors/ | wc -l
echo 'archivos de patrocinadores preservados'

echo ''
echo '========================================================================'
echo '✅ LIMPIEZA COMPLETADA'
echo '========================================================================'
echo ''

bye
"

LFTP_EXIT=$?

echo ""
echo "========================================================================="

if [ $LFTP_EXIT -eq 0 ]; then
    echo -e "${GREEN}✅ ✅ ✅ LIMPIEZA COMPLETADA EXITOSAMENTE ✅ ✅ ✅${NC}"
    echo ""
    echo "🎯 RESULTADO:"
    echo "  • Archivos de turnos eliminados del servidor"
    echo "  • Documentos de pacientes eliminados"
    echo "  • Decoraciones personalizadas eliminadas"
    echo "  • PDFs de turnos eliminados"
    echo "  • Logos y patrocinadores preservados"
    echo ""
    echo -e "${GREEN}✅ SERVIDOR LISTO PARA PRODUCCIÓN${NC}"
else
    echo -e "${RED}❌ ERROR DURANTE LA LIMPIEZA${NC}"
    echo ""
    echo "Código de salida: $LFTP_EXIT"
    echo ""
    echo "Posibles causas:"
    echo "  • Contraseña FTP incorrecta"
    echo "  • Problemas de conexión"
    echo "  • Permisos insuficientes"
    echo ""
    echo "Intenta ejecutar el script nuevamente"
fi

echo ""
echo "========================================================================="
echo "Finalizado: $(date '+%Y-%m-%d %H:%M:%S')"
echo "========================================================================="
echo ""
echo "📝 PRÓXIMOS PASOS:"
echo "  1. ✅ Ejecutar reset-production-data.sql en Somee.com"
echo "  2. ✅ Ejecutar este script para limpiar archivos"
echo "  3. Verificar en FileZilla que los archivos se eliminaron"
echo "  4. Verificar que logos y patrocinadores están intactos"
echo "  5. Probar el registro de nuevos pacientes"
echo "  6. Probar la solicitud de turnos con subida de archivos"
echo ""
