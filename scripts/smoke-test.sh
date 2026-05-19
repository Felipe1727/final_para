#!/usr/bin/env bash
# Smoke test end-to-end para validar el refactor de condiciones dinámicas
# y el fix de solución trivial en FEM.
#
# Uso:
#   bash scripts/smoke-test.sh
#
# Requisitos:
#   - dotnet SDK
#   - curl
#   - (opcional) jq para verificaciones estructurales del JSON
#
# Este script depende de la integración de las Units 1-6 del plan
# "polymorphic-painting-stardust". En el estado actual de `main` fallará;
# eso es esperado y sirve como criterio de aceptación cuando todas las
# unidades estén integradas.

set -euo pipefail

cd "$(dirname "$0")/.."

# 1. Build
dotnet build final_para/final_para.csproj
dotnet build app/app.csproj

# 2. Arranca app en background, captura puerto y pid
PORT=5099
ASPNETCORE_URLS="http://localhost:$PORT" dotnet run --project app/app.csproj &
APP_PID=$!
trap "kill $APP_PID 2>/dev/null || true" EXIT

# 3. Espera readiness (poll hasta 30s)
echo "Esperando a que la app levante en puerto $PORT..."
for i in $(seq 1 30); do
  if curl -sf "http://localhost:$PORT/" -o /dev/null; then
    echo "App lista tras ${i}s"
    break
  fi
  sleep 1
done

# 4. POST a /Ecuacion/Parsear con Laplace 2D
RESP=$(curl -sf -X POST "http://localhost:$PORT/Ecuacion/Parsear" \
  -H "Content-Type: application/json" \
  -d '{"textoPlano":"u_xx + u_yy = 0"}')
echo "Parse response: $RESP"

# Verifica que OrdenesPorVariable contiene x:2 y y:2 (con jq si existe)
if command -v jq &>/dev/null; then
  echo "$RESP" | jq -e '.ordenesPorVariable.x == 2 and .ordenesPorVariable.y == 2' \
    || { echo "FAIL: OrdenesPorVariable incorrectos"; exit 1; }
fi

# 5. POST a /Wizard/GuardarConfiguracion con 4 C.F.
curl -sf -X POST "http://localhost:$PORT/Wizard/GuardarConfiguracion" \
  -H "Content-Type: application/json" \
  -d '{
    "Min":{"x":0,"y":0},"Max":{"x":1,"y":1},"N":{"x":20,"y":20},
    "CondicionesIniciales":{},
    "CondicionesFrontera":{
      "u(xMin)":"sin(pi*y)",
      "u(xMax)":"0",
      "u(yMin)":"0",
      "u(yMax)":"0"
    },
    "EsquemaTemporal":"Implicito",
    "AlgoritmoFDM":"GaussSeidel",
    "TipoElemento":"Cuadrilateral",
    "AlgoritmoFEM":"Galerkin"
  }'

# 6. POST a /Resolucion/Iniciar y captura malla FEM
RESULT=$(curl -sf -X POST "http://localhost:$PORT/Resolucion/Iniciar" \
  -H "Content-Type: application/json" \
  -d '{}')

# 7. Verifica que MallaFEM tiene al menos un valor no-cero
if command -v jq &>/dev/null; then
  NONZERO=$(echo "$RESULT" | jq '[.. | numbers] | map(select(. != 0)) | length')
  if [ "$NONZERO" -lt 1 ]; then
    echo "FAIL: MallaFEM es trivial (todos ceros)"
    exit 1
  fi
fi

echo "SMOKE OK"
