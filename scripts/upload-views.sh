#!/bin/bash
HOST="farmaciasolidaria.somee.com"
USER="maikelpelaez"
REMOTE_PATH="//www.farmaciasolidaria.somee.com"

echo "Subiendo vistas actualizadas..."
read -sp "Contraseña FTP: " PASSWORD
echo

lftp -c "
set ftp:ssl-allow no
open -u $USER,$PASSWORD $HOST
cd $REMOTE_PATH
mirror -R --verbose --only-newer publish/Views Views
bye
"

echo "✓ Vistas actualizadas"
