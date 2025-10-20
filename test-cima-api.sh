#!/bin/bash

# Script para probar conectividad con CIMA API
# Uso: bash test-cima-api.sh [codigo_nacional]

CN=${1:-"662883"}  # Por defecto prueba con código de ejemplo

echo "=========================================="
echo "Prueba de Conectividad con CIMA API"
echo "=========================================="
echo ""
echo "Código Nacional a probar: ${CN}"
echo ""

# Test 1: Ping al servidor
echo "Test 1: Verificando conectividad con cima.aemps.es..."
if ping -c 3 cima.aemps.es > /dev/null 2>&1; then
    echo "✓ Servidor alcanzable"
else
    echo "✗ No se puede alcanzar el servidor (esto puede ser normal si bloquea ping)"
fi
echo ""

# Test 2: Resolución DNS
echo "Test 2: Verificando resolución DNS..."
if nslookup cima.aemps.es > /dev/null 2>&1; then
    echo "✓ DNS resuelve correctamente"
    nslookup cima.aemps.es | grep -A 2 "Name:"
else
    echo "✗ Error en resolución DNS"
fi
echo ""

# Test 3: Conexión HTTPS
echo "Test 3: Probando conexión HTTPS al API..."
URL="https://cima.aemps.es/cima/rest/medicamento?cn=${CN}"
echo "URL: ${URL}"
echo ""

HTTP_CODE=$(curl -s -o /tmp/cima_response.json -w "%{http_code}" \
    -H "User-Agent: FarmaciaSolidariaCristiana/1.0" \
    -H "Accept: application/json" \
    --max-time 30 \
    "${URL}")

echo "HTTP Status Code: ${HTTP_CODE}"
echo ""

if [ "$HTTP_CODE" = "200" ]; then
    echo "✓ Conexión exitosa"
    echo ""
    echo "Respuesta del API:"
    cat /tmp/cima_response.json | python3 -m json.tool 2>/dev/null || cat /tmp/cima_response.json
    echo ""
elif [ "$HTTP_CODE" = "000" ]; then
    echo "✗ Error de conexión (timeout o SSL)"
    echo ""
    echo "Intentando con más detalles:"
    curl -v "https://cima.aemps.es/cima/rest/medicamento?cn=${CN}" 2>&1 | head -20
else
    echo "✗ Error HTTP: ${HTTP_CODE}"
    echo ""
    echo "Respuesta:"
    cat /tmp/cima_response.json
fi

echo ""
echo "=========================================="
echo "Prueba Completada"
echo "=========================================="

# Limpiar
rm -f /tmp/cima_response.json
