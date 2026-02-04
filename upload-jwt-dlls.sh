#!/bin/bash

# Script para subir los DLLs de JWT/IdentityModel

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_DIR="$SCRIPT_DIR/FarmaciaSolidariaCristiana/publish"

FTP_HOST="farmaciasolidaria.somee.com"
FTP_USER="maikelpelaez"
FTP_REMOTE_PATH="/www.farmaciasolidaria.somee.com"

echo "Ingresa la contraseña FTP de Somee:"
read -s FTP_PASS
echo ""

echo "Subiendo DLLs de JWT/IdentityModel..."
lftp -u "$FTP_USER","$FTP_PASS" "$FTP_HOST" << FTPEOF
cd $FTP_REMOTE_PATH
put $PUBLISH_DIR/Microsoft.IdentityModel.Abstractions.dll
put $PUBLISH_DIR/Microsoft.IdentityModel.JsonWebTokens.dll
put $PUBLISH_DIR/Microsoft.IdentityModel.Logging.dll
put $PUBLISH_DIR/Microsoft.IdentityModel.Protocols.OpenIdConnect.dll
put $PUBLISH_DIR/Microsoft.IdentityModel.Protocols.dll
put $PUBLISH_DIR/Microsoft.IdentityModel.Tokens.dll
put $PUBLISH_DIR/System.IdentityModel.Tokens.Jwt.dll
ls -la *IdentityModel* *Jwt*
quit
FTPEOF

echo ""
echo "✅ DLLs de JWT subidos"
echo ""
