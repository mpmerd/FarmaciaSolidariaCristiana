#!/bin/bash

# =====================================================================================
# Script de Backup Rápido - Usuarios Reales
# =====================================================================================
# Este script facilita el proceso de backup antes de migraciones
# =====================================================================================

echo "========================================================================="
echo "BACKUP DE USUARIOS REALES - Farmacia Solidaria Cristiana"
echo "========================================================================="
echo ""
echo "Este script te ayudará a hacer backup de los usuarios antes de migrar."
echo ""
echo "PASOS A SEGUIR:"
echo ""
echo "1. Ve al panel de Somee.com o abre SQL Server Management Studio"
echo "2. Conecta a tu base de datos de PRODUCCIÓN"
echo "3. Ejecuta el archivo: backup-users-real.sql"
echo "4. Copia TODO el output (los INSERT statements)"
echo "5. Pega el output en restore-users-real.sql en la sección indicada"
echo "6. Guarda restore-users-real.sql"
echo ""
echo "DESPUÉS de aplicar las migraciones:"
echo ""
echo "7. Ejecuta restore-users-real.sql en tu base de datos"
echo "8. Verifica que todos los usuarios estén restaurados"
echo ""
echo "========================================================================="
echo ""
read -p "¿Has completado el backup? (s/n): " completed

if [ "$completed" = "s" ] || [ "$completed" = "S" ]; then
    echo ""
    echo "✅ Excelente! Ahora puedes aplicar las migraciones de forma segura."
    echo ""
    echo "Para aplicar las migraciones localmente:"
    echo "  dotnet ef migrations add AddPatientIdentificationRequired"
    echo "  dotnet ef database update"
    echo ""
    echo "RECUERDA: Después de las migraciones, ejecuta restore-users-real.sql"
    echo ""
else
    echo ""
    echo "⚠️  Por favor completa el backup antes de continuar con las migraciones."
    echo ""
fi

echo "========================================================================="
