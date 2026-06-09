#!/usr/bin/env python3
"""
HTTP test: Cambio de Status de Piezas (DiamondsWeb)
Usa requests + BeautifulSoup en lugar de Selenium para evitar el overhead de Chrome.
Flujo: Login → GET CambioStatus → AJAX buscar pieza → POST cambiar status → Verificar bitacora
"""

import requests
import re
import sys

BASE_URL = "http://localhost:56023"
TEST_USER = "admin"
TEST_PASS = "u38a8fk3j0!"
TEST_CB = "167269"  # Pieza real en exhibicion

passed = 0
failed = 0
errors = []


def check(label, condition):
    global passed, failed
    if condition:
        passed += 1
        print(f"  PASS: {label}")
    else:
        failed += 1
        errors.append(label)
        print(f"  FAIL: {label}")


def extract_antiforgery(html):
    """Extrae __RequestVerificationToken del HTML"""
    match = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', html)
    return match.group(1) if match else None


def main():
    global passed, failed
    session = requests.Session()
    session.verify = False

    print("=== Test HTTP Cambio de Status de Piezas ===")
    print(f"URL: {BASE_URL}")
    print(f"Pieza: {TEST_CB}")

    # --- 1. Login ---
    print("\n--- 1. Login ---")
    try:
        r = session.get(f"{BASE_URL}/Security/Auth/Login", timeout=15)
        check("Login page carga (200)", r.status_code == 200)
        check("Login page tiene formulario", "password" in r.text.lower())

        token = extract_antiforgery(r.text)
        check("AntiForgery token encontrado", token is not None)

        data = {
            "LoginViewModel.Username": TEST_USER,
            "LoginViewModel.Password": TEST_PASS,
            "LoginViewModel.RememberMe": "false",
            "__RequestVerificationToken": token or "",
        }
        r = session.post(
            f"{BASE_URL}/Security/Auth/Login", data=data,
            allow_redirects=True, timeout=15
        )
        check("Login redirect exitoso", "/Login" not in r.url)
        print(f"   URL final: {r.url}")
    except Exception as e:
        failed += 1
        errors.append(f"Login: {e}")
        print(f"  EXCEPTION: {e}")
        print_summary()
        return

    # --- 2. Carga pagina CambioStatus ---
    print("\n--- 2. Carga pagina CambioStatus ---")
    try:
        r = session.get(f"{BASE_URL}/CambioStatus", timeout=15)
        check("Pagina CambioStatus carga (200)", r.status_code == 200)
        check("Tiene input codigo barras", 'id="txtCodigoBarras"' in r.text)
        check("Tiene boton buscar", 'id="btnBuscar"' in r.text)
        check("Tiene select nuevo status", 'id="selectNuevoStatus"' in r.text)
        check("Tiene grid piezas", 'table-aml' in r.text)
        check("Tiene tab bitacora", 'tabBitacora' in r.text)

        # Contar opciones de status
        status_options = re.findall(r'<option value="(\d+)">([^<]+)</option>', r.text)
        check("Catalogo de status cargado", len(status_options) > 0)
        print(f"   Status disponibles: {len(status_options)}")
        for sid, sname in status_options[:5]:
            print(f"     {sid}: {sname}")

        # Verificar grid tiene filas
        grid_rows = r.text.count('btn-escanear')
        print(f"   Filas en grid: {grid_rows}")
    except Exception as e:
        failed += 1
        errors.append(f"CambioStatus page: {e}")
        print(f"  EXCEPTION: {e}")

    # --- 3. Buscar pieza (AJAX handler) ---
    print("\n--- 3. Buscar pieza via AJAX ---")
    try:
        r = session.get(
            f"{BASE_URL}/CambioStatus?handler=BuscarPieza&cb={TEST_CB}",
            timeout=15
        )
        check("AJAX buscar pieza (200)", r.status_code == 200)
        data = r.json()
        check("Pieza encontrada", data.get("found") == True)
        if data.get("found"):
            print(f"   CodigoBarras: {data.get('codigoBarras')}")
            print(f"   Descripcion: {data.get('descripcion', '')[:60]}")
            print(f"   Status: {data.get('nombreStatus')}")
            print(f"   Fecha Ult. Cambio: {data.get('fechaUltimoCambio')}")
            original_status = data.get("nombreStatus")
            original_status_id = data.get("idStatus")
        else:
            original_status = None
            original_status_id = None
    except Exception as e:
        failed += 1
        errors.append(f"BuscarPieza AJAX: {e}")
        print(f"  EXCEPTION: {e}")
        original_status = None
        original_status_id = None

    # --- 4. Buscar pieza inexistente ---
    print("\n--- 4. Buscar pieza inexistente ---")
    try:
        r = session.get(
            f"{BASE_URL}/CambioStatus?handler=BuscarPieza&cb=999999",
            timeout=15
        )
        data = r.json()
        check("Pieza inexistente retorna found=false", data.get("found") == False)
    except Exception as e:
        failed += 1
        errors.append(f"BuscarPieza inexistente: {e}")
        print(f"  EXCEPTION: {e}")

    # --- 5. Cambiar status ---
    print("\n--- 5. Cambiar status ---")
    if original_status_id is None:
        print("   Saltando - no se encontro pieza")
    else:
        try:
            # Obtener token antiforgery de la pagina
            r = session.get(f"{BASE_URL}/CambioStatus", timeout=15)
            token = extract_antiforgery(r.text)

            # Seleccionar status diferente al actual (Guardado=5)
            nuevo_status_id = 5 if original_status_id != 5 else 2
            print(f"   Cambiando de {original_status} (id={original_status_id}) a id={nuevo_status_id}")

            data = {
                "codigoBarras": TEST_CB,
                "nuevoStatusId": nuevo_status_id,
                "__RequestVerificationToken": token or "",
            }
            r = session.post(
                f"{BASE_URL}/CambioStatus?handler=CambiarStatus",
                data=data, allow_redirects=True, timeout=60
            )
            check("POST cambiar status (200)", r.status_code == 200)
            check("Mensaje de exito visible", "Id de Cambio" in r.text)
            check("Alert success presente", "alert-success" in r.text)

            # Extraer Id de Cambio del mensaje
            id_match = re.search(r'Id de Cambio:\s*(\d+)', r.text)
            if id_match:
                print(f"   Id de Cambio: {id_match.group(1)}")
        except Exception as e:
            failed += 1
            errors.append(f"CambiarStatus: {e}")
            print(f"  EXCEPTION: {e}")

    # --- 6. Verificar bitacora ---
    print("\n--- 6. Verificar bitacora ---")
    try:
        r = session.get(f"{BASE_URL}/CambioStatus", timeout=15)
        check("Pagina recarga correctamente", r.status_code == 200)
        check("Bitacora contiene codigo de barras",
              f'<code>{TEST_CB}</code>' in r.text and 'tabBitacora' in r.text)
    except Exception as e:
        failed += 1
        errors.append(f"Bitacora: {e}")
        print(f"  EXCEPTION: {e}")

    # --- 7. Revertir status (limpieza) ---
    print("\n--- 7. Revertir status (limpieza) ---")
    if original_status_id is not None:
        try:
            r = session.get(f"{BASE_URL}/CambioStatus", timeout=15)
            token = extract_antiforgery(r.text)

            data = {
                "codigoBarras": TEST_CB,
                "nuevoStatusId": original_status_id,
                "__RequestVerificationToken": token or "",
            }
            r = session.post(
                f"{BASE_URL}/CambioStatus?handler=CambiarStatus",
                data=data, allow_redirects=True, timeout=60
            )
            check("Status revertido exitosamente", "Id de Cambio" in r.text)
        except Exception as e:
            failed += 1
            errors.append(f"Revertir: {e}")
            print(f"  EXCEPTION: {e}")

    # --- 8. Filtro del grid ---
    print("\n--- 8. Filtro del grid por status ---")
    try:
        r_all = session.get(f"{BASE_URL}/CambioStatus", timeout=15)
        rows_all = r_all.text.count('btn-escanear')

        r_filtered = session.get(f"{BASE_URL}/CambioStatus?FiltroStatus=3", timeout=15)
        check("Filtro GET responde (200)", r_filtered.status_code == 200)
        rows_filtered = r_filtered.text.count('btn-escanear')
        check("Filtro reduce o iguala resultados", rows_filtered <= rows_all)
        print(f"   Sin filtro: {rows_all} | Con filtro (status=3): {rows_filtered}")
    except Exception as e:
        failed += 1
        errors.append(f"Filtro: {e}")
        print(f"  EXCEPTION: {e}")

    print_summary()


def print_summary():
    print(f"\n{'='*50}")
    print(f"RESULTADO: {passed} passed, {failed} failed")
    if errors:
        print("Fallos:")
        for e in errors:
            print(f"  - {e}")
    print(f"{'='*50}")
    sys.exit(0 if failed == 0 else 1)


if __name__ == "__main__":
    main()
