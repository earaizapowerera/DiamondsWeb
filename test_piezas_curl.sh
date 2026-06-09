#!/bin/bash
# ============================================================
# Test funcional: Alta de Piezas Sencillas - DiamondsWeb
# Usa curl + servidor remoto (no requiere Chrome)
# ============================================================

BASE="https://bot-286802.dev.powerera.com"
COOK="/tmp/test_piezas_cookies_$$"
PASSED=0
FAILED=0

pass() { echo "  PASSED"; PASSED=$((PASSED+1)); }
fail() { echo "  FAILED: $1"; FAILED=$((FAILED+1)); }

# LOGIN
echo "=== LOGIN ==="
# Get antiforgery token
LOGIN_PAGE=$(curl -sk -c "$COOK" -b "$COOK" "$BASE/Security/Auth/Login")
TOKEN=$(echo "$LOGIN_PAGE" | grep -oP '__RequestVerificationToken" .+?value="\K[^"]+' | head -1)
echo "  Token: ${TOKEN:0:20}..."

# Submit login
LOGIN_RESULT=$(curl -sk -c "$COOK" -b "$COOK" -L -o /dev/null -w "%{http_code}|%{url_effective}" \
  -d "LoginViewModel.Username=admin&LoginViewModel.Password=u38a8fk3j0!&__RequestVerificationToken=$TOKEN" \
  "$BASE/Security/Auth/Login")
HTTP_CODE=$(echo "$LOGIN_RESULT" | cut -d'|' -f1)
FINAL_URL=$(echo "$LOGIN_RESULT" | cut -d'|' -f2)
echo "  Login result: HTTP $HTTP_CODE -> $FINAL_URL"

if [[ "$HTTP_CODE" == "200" ]]; then
    echo "  Login OK"
else
    echo "  WARNING: Login returned $HTTP_CODE (may still work with cookies)"
fi

# TEST 1: Index page loads
echo ""
echo "=== TEST 1: Index page loads ==="
BODY=$(curl -sk -b "$COOK" "$BASE/Piezas")
if echo "$BODY" | grep -q "Piezas Sencillas"; then
    echo "  Title found: Piezas Sencillas"
    if echo "$BODY" | grep -q '<table'; then
        echo "  Table found"
        ROWS=$(echo "$BODY" | grep -c '<tr>')
        echo "  Rows: $ROWS"
        pass
    else
        fail "Table not found"
    fi
else
    # Check if redirected to login
    if echo "$BODY" | grep -q "Login"; then
        fail "Redirected to login - auth failed"
    else
        fail "Page title not found"
    fi
fi

# TEST 2: Alta form loads with all tabs and controls
echo ""
echo "=== TEST 2: Alta form loads ==="
BODY=$(curl -sk -b "$COOK" "$BASE/Piezas/Alta")
CHECKS=0
TOTAL=0

for CTRL in "formPieza" "Pieza_Descripcion" "tab-pieza-tab" "tab-peso-tab" "tab-extras-tab" "tab-factura-tab" "tab-oro-tab" "tab-diamante-tab" "tab-reloj-tab" "cbPieza" "peso" "precioGramo" "utilidad" "utilidadExtra" "impuesto" "precio" "selMoneda" "selDivisor" "formulaDisplay"; do
    TOTAL=$((TOTAL+1))
    if echo "$BODY" | grep -q "id=\"$CTRL\""; then
        CHECKS=$((CHECKS+1))
    else
        echo "  MISSING: $CTRL"
    fi
done
echo "  Controls found: $CHECKS/$TOTAL"
if [[ $CHECKS -eq $TOTAL ]]; then
    pass
else
    fail "$((TOTAL-CHECKS)) controls missing"
fi

# TEST 3: Alta form has characteristic fields
echo ""
echo "=== TEST 3: Characteristic fields present ==="
# Oro
ORO_FIELDS=0
for F in "selKilatesOro" "modeloOro" "lineaOro"; do
    echo "$BODY" | grep -q "id=\"$F\"" && ORO_FIELDS=$((ORO_FIELDS+1))
done
echo "  Oro fields: $ORO_FIELDS/3"

# Diamante
DIAM_FIELDS=0
for F in "Pieza_Quilates" "Pieza_Color" "Pieza_Pureza" "Pieza_Corte"; do
    echo "$BODY" | grep -q "id=\"$F\"" && DIAM_FIELDS=$((DIAM_FIELDS+1))
done
echo "  Diamante fields: $DIAM_FIELDS/4"

# Reloj
RELOJ_FIELDS=0
for F in "Pieza_NumSerie" "modeloReloj" "lineaReloj"; do
    echo "$BODY" | grep -q "id=\"$F\"" && RELOJ_FIELDS=$((RELOJ_FIELDS+1))
done
echo "  Reloj fields: $RELOJ_FIELDS/3"

if [[ $ORO_FIELDS -eq 3 && $DIAM_FIELDS -eq 4 && $RELOJ_FIELDS -eq 3 ]]; then
    pass
else
    fail "Missing characteristic fields"
fi

# TEST 4: JavaScript calculations loaded
echo ""
echo "=== TEST 4: JavaScript file loads ==="
JS_STATUS=$(curl -sk -o /dev/null -w "%{http_code}" -b "$COOK" "$BASE/js/piezas.js")
echo "  piezas.js HTTP status: $JS_STATUS"
JS_BODY=$(curl -sk -b "$COOK" "$BASE/js/piezas.js")
HAS_CALC_COSTOS=$(echo "$JS_BODY" | grep -c "function calcularCostos")
HAS_CALC_PRECIO=$(echo "$JS_BODY" | grep -c "function calcularPrecio")
HAS_SYNC=$(echo "$JS_BODY" | grep -c "function syncKilates")
echo "  calcularCostos: $HAS_CALC_COSTOS, calcularPrecio: $HAS_CALC_PRECIO, syncKilates: $HAS_SYNC"
if [[ "$JS_STATUS" == "200" && $HAS_CALC_COSTOS -gt 0 && $HAS_CALC_PRECIO -gt 0 ]]; then
    pass
else
    fail "JS file missing or incomplete"
fi

# TEST 5: CSS file loads
echo ""
echo "=== TEST 5: CSS file loads ==="
CSS_STATUS=$(curl -sk -o /dev/null -w "%{http_code}" -b "$COOK" "$BASE/css/piezas.css")
echo "  piezas.css HTTP status: $CSS_STATUS"
if [[ "$CSS_STATUS" == "200" ]]; then
    pass
else
    fail "CSS file not found"
fi

# TEST 6: Create piece (POST)
echo ""
echo "=== TEST 6: Create piece (POST) ==="
# Get antiforgery token from Alta page
ALTA_PAGE=$(curl -sk -b "$COOK" "$BASE/Piezas/Alta?IdRemision=111279")
AF_TOKEN=$(echo "$ALTA_PAGE" | grep -oP '__RequestVerificationToken" .+?value="\K[^"]+' | head -1)
echo "  AF Token: ${AF_TOKEN:0:20}..."

CREATE_RESULT=$(curl -sk -b "$COOK" -c "$COOK" -L -w "\n%{http_code}|%{url_effective}" \
  "$BASE/Piezas/Alta?handler=Guardar" \
  -d "IdRemision=111279" \
  -d "Pieza.IdRemision=111279" \
  -d "Pieza.Descripcion=TEST+curl+-+Anillo+oro+14k+prueba+automatica" \
  -d "Pieza.IdGrupo=3" \
  -d "Pieza.CBPieza=1800" \
  -d "Pieza.DescPieza=0" \
  -d "Pieza.CNPieza=1800" \
  -d "Pieza.Peso=0" \
  -d "Pieza.PrecioGramo=0" \
  -d "Pieza.CBPeso=0" \
  -d "Pieza.DescPeso=0" \
  -d "Pieza.CNPeso=0" \
  -d "Pieza.CBManoObra=0" \
  -d "Pieza.DescManoObra=0" \
  -d "Pieza.CNManoObra=0" \
  -d "Pieza.CBTotal=1800" \
  -d "Pieza.CNTotal=1800" \
  -d "Pieza.CBFactura=0" \
  -d "Pieza.DescFactura=0" \
  -d "Pieza.CNFactura=0" \
  -d "Pieza.IdMoneda=1" \
  -d "Pieza.TCCotizacion=1" \
  -d "Pieza.TCCosto=0" \
  -d "Pieza.Utilidad=1.667" \
  -d "Pieza.UtilidadExtra=1" \
  -d "Pieza.Impuesto=1.16" \
  -d "Pieza.Divisor=0.044" \
  -d "Pieza.Precio=79107" \
  -d "Pieza.Kilates=14" \
  -d "Pieza.Modelo=AN-TEST" \
  -d "Pieza.Linea=CurlTest" \
  -d "Pieza.IdDivisor=1" \
  -d "Pieza.IdTienda=1" \
  -d "Pieza.IdLocalizacion=1" \
  -d "IdEtiqueta=2" \
  -d "TabCaracteristica=Oro" \
  -d "TabCosto=Pieza" \
  -d "__RequestVerificationToken=$AF_TOKEN" \
  2>&1)

HTTP_LINE=$(echo "$CREATE_RESULT" | tail -1)
HTTP_CODE=$(echo "$HTTP_LINE" | cut -d'|' -f1)
BODY_RESULT=$(echo "$CREATE_RESULT" | head -n-1)

if echo "$BODY_RESULT" | grep -q "creada exitosamente"; then
    CB=$(echo "$BODY_RESULT" | grep -oP 'Pieza \K\d+' | head -1)
    echo "  Piece created! CB=$CB"
    pass

    # TEST 7: Edit piece (verify data loads)
    echo ""
    echo "=== TEST 7: Edit piece $CB ==="
    EDIT_PAGE=$(curl -sk -b "$COOK" "$BASE/Piezas/Alta?cb=$CB")
    if echo "$EDIT_PAGE" | grep -q "TEST curl"; then
        echo "  Description loaded correctly"
        if echo "$EDIT_PAGE" | grep -q "AN-TEST"; then
            echo "  Modelo loaded correctly"
        fi
        if echo "$EDIT_PAGE" | grep -q "Editar Pieza"; then
            echo "  Edit mode detected"
        fi
        pass
    else
        fail "Piece data not loaded"
    fi

    # TEST 8: Verify piece in DB via Index page
    echo ""
    echo "=== TEST 8: Piece visible in index ==="
    INDEX_PAGE=$(curl -sk -b "$COOK" "$BASE/Piezas?IdRemision=111279")
    if echo "$INDEX_PAGE" | grep -q "$CB"; then
        echo "  CB $CB found in index grid"
        pass
    else
        fail "CB $CB not found in index"
    fi

elif echo "$BODY_RESULT" | grep -q "alert-danger"; then
    ERROR=$(echo "$BODY_RESULT" | grep -oP 'alert-danger[^<]*<[^>]*>[^<]*\K[^<]+' | head -1)
    echo "  Error: $ERROR"
    fail "Server error during create"
else
    echo "  HTTP: $HTTP_CODE"
    echo "  Response preview: $(echo "$BODY_RESULT" | head -5)"
    fail "Unexpected response"
fi

# TEST 9: API endpoints
echo ""
echo "=== TEST 9: API endpoints ==="
# Tipo de cambio
TC_RESULT=$(curl -sk -b "$COOK" "$BASE/Piezas/Alta?handler=TipoCambio&idMoneda=2")
echo "  TipoCambio USD: $TC_RESULT"
if echo "$TC_RESULT" | grep -q "tipoCambioCotizacion"; then
    echo "  TipoCambio API OK"
    pass
else
    fail "TipoCambio API failed"
fi

# TEST 10: Moneda change API
echo ""
echo "=== TEST 10: Proveedor info API ==="
PROV_RESULT=$(curl -sk -b "$COOK" "$BASE/Piezas/Alta?handler=Proveedor&id=33")
echo "  Proveedor 33: $(echo "$PROV_RESULT" | head -c 200)"
if echo "$PROV_RESULT" | grep -q "nombreProveedor\|NombreProveedor"; then
    echo "  Proveedor API OK"
    pass
else
    fail "Proveedor API failed"
fi

# CLEANUP
rm -f "$COOK"

# SUMMARY
echo ""
echo "============================================================"
echo "RESULTS: $PASSED passed, $FAILED failed, $((PASSED+FAILED)) total"
echo "============================================================"

exit $FAILED
