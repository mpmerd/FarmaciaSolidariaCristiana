#!/bin/bash
# Guard anti-regresión de hora de Cuba (Fase 5 del plan de timezone).
#
# Falla (exit 1) si encuentra DateTime.Now / DateTime.Today fuera de la whitelist.
# Uso: ./check-no-datetime-now.sh   (recomendado correrlo antes de cada deploy)
#
# Regla: todo timestamp de negocio debe salir de CubaTime (Helpers/CubaTime.cs),
# calculado desde UTC hacia America/Havana — nunca de la hora del host ni del teléfono.
# DateTime.UtcNow SÍ está permitido (tokens JWT, notificaciones pendientes, etc.).
#
# Whitelist (usos intencionales de hora del host/dispositivo):
#   - Program.cs                        → log [DIAGNOSTICO-TZ] (compara host vs Cuba)
#   - DiagnosticsController.cs          → /api/diagnostics/ping (diagnóstico)
#   - PollingNotificationService.cs     → timeout de audio de 5s (reloj local del teléfono)

WHITELIST=(
  "FarmaciaSolidariaCristiana/Program.cs"
  "FarmaciaSolidariaCristiana/Api/Controllers/DiagnosticsController.cs"
  "FarmaciaSolidariaCristiana.Maui/Services/PollingNotificationService.cs"
)

cd "$(dirname "$0")"

matches=$(grep -rn --include='*.cs' --include='*.cshtml' --include='*.xaml' \
  -E 'DateTime\.(Now|Today)([^A-Za-z]|$)' \
  FarmaciaSolidariaCristiana FarmaciaSolidariaCristiana.Maui 2>/dev/null \
  | grep -v -E '/(bin|obj|publish|Migrations)/')

violations=""
while IFS= read -r line; do
  [ -z "$line" ] && continue
  file="${line%%:*}"
  allowed=0
  for w in "${WHITELIST[@]}"; do
    [ "$file" = "$w" ] && allowed=1
  done
  [ $allowed -eq 0 ] && violations="${violations}${line}"$'\n'
done <<< "$matches"

if [ -n "$violations" ]; then
  echo "✗ DateTime.Now/DateTime.Today fuera de la whitelist:"
  echo
  echo "$violations"
  echo "Reemplázalos por CubaTime.Now / CubaTime.Today (Helpers/CubaTime.cs de cada proyecto)."
  echo "Si el uso es genuinamente de hora local del host/dispositivo, agrégalo a la whitelist con justificación."
  exit 1
fi

echo "✓ Guard OK: sin DateTime.Now/DateTime.Today fuera de la whitelist."
exit 0
