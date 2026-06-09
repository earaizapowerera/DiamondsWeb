#!/bin/bash
# Test CRUD de Opciones de Pago via curl
# Se ejecuta en wk2 contra http://localhost:56018

set -e
COOKIE=/tmp/test-286818-cookies.txt
BASE=http://localhost:56018
PASSED=0
FAILED=0

pass() { echo "[OK] $1"; PASSED=$((PASSED+1)); }
fail() { echo "[FAIL] $1"; FAILED=$((FAILED+1)); }

# Cleanup
rm -f $COOKIE

echo "=== TEST 1: Login ==="
LOGIN_HTML=$(curl -sc $COOKIE "$BASE/Security/Auth/Login" 2>/dev/null)
TOKEN=$(echo "$LOGIN_HTML" | grep -oP '__RequestVerificationToken.*?value="[^"]*"' | head -1 | grep -oP 'value="[^"]*"' | tr -d '"' | sed 's/value=//')
if [ -z "$TOKEN" ]; then
    fail "No se obtuvo token antiforgery"
    exit 1
fi

curl -sb $COOKIE -c $COOKIE -X POST "$BASE/Security/Auth/Login" \
  -d "LoginViewModel.Username=admin&LoginViewModel.Password=u38a8fk3j0!&LoginViewModel.RememberMe=false&__RequestVerificationToken=$TOKEN" \
  -L -o /dev/null -w "" 2>/dev/null

# Verify login worked by accessing the page
PAGE=$(curl -sb $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)
if echo "$PAGE" | grep -q "tablaOpciones"; then
    pass "Login y carga de pagina OpcionesPago"
else
    fail "No se pudo acceder a OpcionesPago despues de login"
    echo "$PAGE" | head -50
    exit 1
fi

# Count initial rows
INITIAL_COUNT=$(echo "$PAGE" | grep -c 'IdOpcionPago' || true)
echo "    Opciones de pago iniciales: $INITIAL_COUNT"

echo "=== TEST 2: Crear opcion de pago ==="
# Get antiforgery token from the page
CREATE_TOKEN=$(echo "$PAGE" | grep -oP '__RequestVerificationToken.*?value="[^"]*"' | head -1 | grep -oP 'value="[^"]*"' | tr -d '"' | sed 's/value=//')

curl -sb $COOKIE -c $COOKIE -X POST "$BASE/Catalogos/OpcionesPago?handler=Guardar" \
  -d "IdOpcionPago=0&Nombre=TEST-Curl-Pago&IdMoneda=1&Logo=3&Activa=true&__RequestVerificationToken=$CREATE_TOKEN" \
  -o /dev/null 2>/dev/null

# Fetch page again to verify
RESULT=$(curl -sb $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)

if echo "$RESULT" | grep -q "TEST-Curl-Pago"; then
    pass "Crear opcion de pago TEST-Curl-Pago"
else
    fail "No se encontro TEST-Curl-Pago despues de crear"
fi

# Get the ID of the created record
PAGE=$(curl -sb $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)
# Extract the ID from the editarOpcion JS call for our test record
TEST_ID=$(echo "$PAGE" | grep -oP "editarOpcion\(\d+, 'TEST-Curl-Pago'" | grep -oP '\d+' | head -1)
echo "    ID creado: $TEST_ID"

if [ -z "$TEST_ID" ]; then
    fail "No se pudo obtener ID del registro creado"
    # Try to cleanup anyway
    exit 1
fi

echo "=== TEST 3: Editar opcion de pago ==="
EDIT_TOKEN=$(echo "$PAGE" | grep -oP '__RequestVerificationToken.*?value="[^"]*"' | head -1 | grep -oP 'value="[^"]*"' | tr -d '"' | sed 's/value=//')

curl -sb $COOKIE -c $COOKIE -X POST "$BASE/Catalogos/OpcionesPago?handler=Guardar" \
  -d "IdOpcionPago=$TEST_ID&Nombre=TEST-Curl-Editado&IdMoneda=2&Logo=1&Activa=true&__RequestVerificationToken=$EDIT_TOKEN" \
  -o /dev/null 2>/dev/null

RESULT=$(curl -sb $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)

if echo "$RESULT" | grep -q "TEST-Curl-Editado"; then
    pass "Editar opcion de pago a TEST-Curl-Editado"
else
    fail "No se encontro TEST-Curl-Editado despues de editar"
fi

echo "=== TEST 4: Desactivar opcion de pago ==="
PAGE=$(curl -sb $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)
TOGGLE_TOKEN=$(echo "$PAGE" | grep -oP '__RequestVerificationToken.*?value="[^"]*"' | head -1 | grep -oP 'value="[^"]*"' | tr -d '"' | sed 's/value=//')

curl -sb $COOKIE -c $COOKIE -X POST "$BASE/Catalogos/OpcionesPago?handler=ToggleActiva" \
  -d "IdOpcionPago=$TEST_ID&ActivaActual=True&__RequestVerificationToken=$TOGGLE_TOKEN" \
  -o /dev/null 2>/dev/null

RESULT=$(curl -sb $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)
ROW=$(echo "$RESULT" | grep -B2 "TEST-Curl-Editado" | head -3)
if echo "$ROW" | grep -q "table-secondary"; then
    pass "Desactivar opcion de pago (fila inactiva)"
else
    # Check if Inactiva badge is present
    ROW2=$(echo "$RESULT" | grep -A5 "TEST-Curl-Editado" | head -8)
    if echo "$ROW2" | grep -q "Inactiva"; then
        pass "Desactivar opcion de pago (badge Inactiva)"
    else
        fail "No se confirmo desactivacion"
        echo "$ROW2"
    fi
fi

echo "=== TEST 5: Reactivar opcion de pago ==="
PAGE=$(curl -sb $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)
TOGGLE_TOKEN2=$(echo "$PAGE" | grep -oP '__RequestVerificationToken.*?value="[^"]*"' | head -1 | grep -oP 'value="[^"]*"' | tr -d '"' | sed 's/value=//')

curl -sb $COOKIE -c $COOKIE -X POST "$BASE/Catalogos/OpcionesPago?handler=ToggleActiva" \
  -d "IdOpcionPago=$TEST_ID&ActivaActual=False&__RequestVerificationToken=$TOGGLE_TOKEN2" \
  -o /dev/null 2>/dev/null

RESULT=$(curl -sb $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)
ROW=$(echo "$RESULT" | grep -B2 "TEST-Curl-Editado" | head -3)
if echo "$ROW" | grep -q "table-secondary"; then
    fail "La fila sigue inactiva despues de reactivar"
else
    pass "Reactivar opcion de pago"
fi

echo "=== TEST 6: Eliminar opcion de pago ==="
PAGE=$(curl -sb $COOKIE -c $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)
DEL_TOKEN=$(echo "$PAGE" | grep -oP '__RequestVerificationToken.*?value="[^"]*"' | head -1 | grep -oP 'value="[^"]*"' | tr -d '"' | sed 's/value=//')

DEL_HTTP=$(curl -sb $COOKIE -c $COOKIE -X POST "$BASE/Catalogos/OpcionesPago?handler=Eliminar" \
  -d "IdOpcionPago=$TEST_ID&__RequestVerificationToken=$DEL_TOKEN" \
  -o /dev/null -w "%{http_code}" 2>/dev/null)
echo "    Delete HTTP: $DEL_HTTP (esperado: 302)"

RESULT=$(curl -sb $COOKIE "$BASE/Catalogos/OpcionesPago" 2>/dev/null)

# Check specifically for editarOpcion(TEST_ID, ... to avoid false positives from other test data
if echo "$RESULT" | grep -q "editarOpcion($TEST_ID,"; then
    fail "El registro eliminado (ID=$TEST_ID) sigue apareciendo"
else
    pass "Eliminar opcion de pago (ID=$TEST_ID)"
fi

# Cleanup
rm -f $COOKIE

echo ""
echo "=================================================="
echo "Resultados: $PASSED passed, $FAILED failed"
echo "=================================================="

[ $FAILED -eq 0 ] && exit 0 || exit 1
