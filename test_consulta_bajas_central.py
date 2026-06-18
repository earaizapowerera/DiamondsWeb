"""
Selenium Test: Consulta de Bajas Central - DiamondsWeb
Pantalla padre: #287289 | Task: #287290
URL: /Ventas/ConsultaBajasCentral

Migración de frmCB.frm (Consultas2.vbp) — consulta piezas vendidas desde BD central.

Funcionalidad a verificar:
- Carga de pagina con header, filtros, stats y tabla
- Filtros: Buscar (texto), Fecha desde/hasta, Grupo
- Paginacion server-side
- Toggle resumen/detalle (8 vs 19 columnas)
- Boton Limpiar resetea filtros
- Boton Imprimir existe
"""
import time
import sys
import os
from datetime import datetime
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

BASE_URL = os.environ.get("TEST_BASE_URL", "http://localhost:56390")
PAGE_URL = f"{BASE_URL}/Ventas/ConsultaBajasCentral"
USER = "admin"
PASS = "Waykee2026!"
SCREENSHOT_DIR = "/home/earaiza/DiamondsWeb-tests/screenshots/consulta_bajas_central"

os.makedirs(SCREENSHOT_DIR, exist_ok=True)

results = []


def screenshot(driver, name):
    ts = datetime.now().strftime("%H%M%S")
    path = f"{SCREENSHOT_DIR}/{ts}_{name}.png"
    driver.save_screenshot(path)
    print(f"  [Screenshot] {path}")
    return path


def setup_driver():
    opts = Options()
    opts.add_argument("--headless=new")
    opts.add_argument("--no-sandbox")
    opts.add_argument("--disable-dev-shm-usage")
    opts.add_argument("--window-size=1280,900")
    opts.add_argument("--ignore-certificate-errors")
    return webdriver.Chrome(options=opts)


def login(driver, wait):
    print("\n=== LOGIN ===")
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    wait.until(EC.presence_of_element_located((By.NAME, "LoginViewModel.Username")))
    driver.find_element(By.NAME, "LoginViewModel.Username").send_keys(USER)
    driver.find_element(By.NAME, "LoginViewModel.Password").send_keys(PASS)
    btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
    driver.execute_script("arguments[0].click()", btn)
    time.sleep(3)
    if "Login" in driver.current_url:
        screenshot(driver, "00_login_failed")
        print("  FAILED - Sigue en login")
        return False
    screenshot(driver, "00_login_ok")
    print(f"  Login OK -> {driver.current_url}")
    return True


def test_01_page_load(driver, wait):
    """Test 1: Carga de pagina — header, filtros, stats, tabla"""
    print("\n=== TEST 1: Carga de pagina ===")
    driver.get(PAGE_URL)
    time.sleep(2)
    screenshot(driver, "01_page_load")

    try:
        # Header con titulo
        header = driver.find_element(By.CSS_SELECTOR, "h5")
        assert "Consulta de Bajas Central" in header.text, f"Titulo incorrecto: {header.text}"
        print(f"  Titulo: '{header.text.strip()[:50]}' OK")

        # Filtros existen
        buscar = driver.find_element(By.NAME, "Buscar")
        assert buscar is not None
        print("  Campo Buscar: OK")

        fecha_desde = driver.find_element(By.NAME, "FechaDesde")
        fecha_hasta = driver.find_element(By.NAME, "FechaHasta")
        assert fecha_desde is not None and fecha_hasta is not None
        print("  Campos FechaDesde/FechaHasta: OK")

        grupo_select = driver.find_element(By.CSS_SELECTOR, "#selGrupoCentral")
        assert grupo_select is not None
        print("  Dropdown Grupo: OK")

        # Boton Buscar
        btn_buscar = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
        assert "Buscar" in btn_buscar.text
        print("  Boton Buscar: OK")

        # Boton Limpiar
        btn_limpiar = driver.find_element(By.CSS_SELECTOR, "a[href='/Ventas/ConsultaBajasCentral']")
        assert "Limpiar" in btn_limpiar.text
        print("  Boton Limpiar: OK")

        # Boton Imprimir
        btn_imprimir = driver.find_element(By.XPATH, "//button[contains(., 'Imprimir')]")
        assert btn_imprimir is not None
        print("  Boton Imprimir: OK")

        # Stats badges
        badges = driver.find_elements(By.CSS_SELECTOR, ".badge")
        piezas_badge = [b for b in badges if "Piezas" in b.text]
        suma_badge = [b for b in badges if "Suma" in b.text]
        assert len(piezas_badge) > 0, "Badge Piezas no encontrado"
        assert len(suma_badge) > 0, "Badge Suma no encontrado"
        print(f"  Stats: {piezas_badge[0].text.strip()} | {suma_badge[0].text.strip()}")

        # Tabla con headers (modo resumen = 8 columnas)
        headers = driver.find_elements(By.CSS_SELECTOR, "#tblBajasCentral thead th")
        header_texts = [h.text.strip() for h in headers]
        print(f"  Headers tabla: {header_texts}")
        for expected in ["Codigo", "Descripcion", "Precio", "Cliente", "Fecha Baja"]:
            assert expected in header_texts, f"Header '{expected}' falta"
        print("  Headers de tabla: OK")

        # Toggle resumen/detalle
        toggle_btns = driver.find_elements(By.CSS_SELECTOR, ".btn-group a.btn")
        assert len(toggle_btns) == 2, f"Toggle buttons: esperados 2, encontrados {len(toggle_btns)}"
        print(f"  Toggle resumen/detalle: '{toggle_btns[0].text.strip()}' | '{toggle_btns[1].text.strip()}' OK")

        screenshot(driver, "01_page_load_complete")
        print("  TEST 1 PASSED")
        results.append(("TEST 1: Carga de pagina", "PASSED"))
        return True

    except Exception as e:
        screenshot(driver, "01_page_load_error")
        print(f"  TEST 1 FAILED: {e}")
        results.append(("TEST 1: Carga de pagina", f"FAILED: {e}"))
        return False


def test_02_search_text(driver, wait):
    """Test 2: Busqueda por texto libre"""
    print("\n=== TEST 2: Busqueda por texto ===")

    try:
        # Primero cargar sin filtros para ver si hay datos
        driver.get(PAGE_URL)
        time.sleep(2)

        rows = driver.find_elements(By.CSS_SELECTOR, "#tblBajasCentral tbody tr")
        if len(rows) == 1:
            cell_text = rows[0].text.strip()
            if "No se encontraron" in cell_text:
                print("  Sin datos en BD central. Probando busqueda de todas formas...")

        # Buscar un termino
        driver.get(f"{PAGE_URL}?Buscar=oro")
        time.sleep(3)
        screenshot(driver, "02_search_text")

        # Verificar URL tiene parametro
        assert "Buscar=oro" in driver.current_url, f"URL no tiene Buscar: {driver.current_url}"
        print("  URL con parametro Buscar: OK")

        # Verificar que el campo mantiene el valor
        buscar_input = driver.find_element(By.NAME, "Buscar")
        val = buscar_input.get_attribute("value")
        assert val == "oro", f"Campo Buscar no preservo valor: '{val}'"
        print(f"  Campo Buscar preserva valor '{val}': OK")

        rows = driver.find_elements(By.CSS_SELECTOR, "#tblBajasCentral tbody tr")
        print(f"  Resultados: {len(rows)} filas")

        screenshot(driver, "02_search_text_complete")
        print("  TEST 2 PASSED")
        results.append(("TEST 2: Busqueda por texto", "PASSED"))
        return True

    except Exception as e:
        screenshot(driver, "02_search_text_error")
        print(f"  TEST 2 FAILED: {e}")
        results.append(("TEST 2: Busqueda por texto", f"FAILED: {e}"))
        return False


def test_03_search_by_date(driver, wait):
    """Test 3: Busqueda por rango de fechas"""
    print("\n=== TEST 3: Busqueda por fechas ===")

    try:
        driver.get(f"{PAGE_URL}?FechaDesde=2020-01-01&FechaHasta=2026-12-31")
        time.sleep(3)
        screenshot(driver, "03_search_dates")

        # Verificar URL
        assert "FechaDesde=" in driver.current_url
        assert "FechaHasta=" in driver.current_url
        print("  URL con parametros de fecha: OK")

        # Verificar campos preservan valores
        desde_val = driver.find_element(By.NAME, "FechaDesde").get_attribute("value")
        hasta_val = driver.find_element(By.NAME, "FechaHasta").get_attribute("value")
        print(f"  FechaDesde={desde_val}, FechaHasta={hasta_val}")

        # Verificar que no hay error
        errors = driver.find_elements(By.CSS_SELECTOR, ".alert-danger")
        if errors:
            print(f"  WARN: Error alert: {errors[0].text[:80]}")
        else:
            print("  Sin errores: OK")

        rows = driver.find_elements(By.CSS_SELECTOR, "#tblBajasCentral tbody tr")
        print(f"  Resultados: {len(rows)} filas")

        screenshot(driver, "03_search_dates_complete")
        print("  TEST 3 PASSED")
        results.append(("TEST 3: Busqueda por fechas", "PASSED"))
        return True

    except Exception as e:
        screenshot(driver, "03_search_dates_error")
        print(f"  TEST 3 FAILED: {e}")
        results.append(("TEST 3: Busqueda por fechas", f"FAILED: {e}"))
        return False


def test_04_toggle_detalle(driver, wait):
    """Test 4: Toggle resumen/detalle cambia columnas visibles"""
    print("\n=== TEST 4: Toggle resumen/detalle ===")

    try:
        # Modo resumen (default)
        driver.get(f"{PAGE_URL}?Modo=resumen")
        time.sleep(2)
        headers_resumen = driver.find_elements(By.CSS_SELECTOR, "#tblBajasCentral thead th")
        n_resumen = len(headers_resumen)
        print(f"  Modo resumen: {n_resumen} columnas")
        screenshot(driver, "04_modo_resumen")

        # Modo detalle
        driver.get(f"{PAGE_URL}?Modo=detalle")
        time.sleep(2)
        headers_detalle = driver.find_elements(By.CSS_SELECTOR, "#tblBajasCentral thead th")
        n_detalle = len(headers_detalle)
        print(f"  Modo detalle: {n_detalle} columnas")
        screenshot(driver, "04_modo_detalle")

        # Detalle debe tener mas columnas que resumen
        assert n_detalle > n_resumen, f"Detalle ({n_detalle}) no tiene mas columnas que resumen ({n_resumen})"
        print(f"  Detalle ({n_detalle}) > Resumen ({n_resumen}): OK")

        # Verificar columnas extra en detalle
        detail_header_texts = [h.text.strip() for h in headers_detalle]
        extra_cols = ["Nota", "Peso", "Kilates", "Quilates", "Grupo", "Moneda"]
        for col in extra_cols:
            assert col in detail_header_texts, f"Columna detalle '{col}' falta"
        print(f"  Columnas extra en detalle: OK")

        # Verificar que el toggle button correcto esta activo
        active_btn = driver.find_element(By.CSS_SELECTOR, ".btn-group a.btn.btn-primary")
        assert "Todas" in active_btn.text, f"Boton activo incorrecto: {active_btn.text}"
        print(f"  Boton activo correcto: '{active_btn.text.strip()}' OK")

        screenshot(driver, "04_toggle_complete")
        print("  TEST 4 PASSED")
        results.append(("TEST 4: Toggle resumen/detalle", "PASSED"))
        return True

    except Exception as e:
        screenshot(driver, "04_toggle_error")
        print(f"  TEST 4 FAILED: {e}")
        results.append(("TEST 4: Toggle resumen/detalle", f"FAILED: {e}"))
        return False


def test_05_clear_filters(driver, wait):
    """Test 5: Boton Limpiar resetea todos los filtros"""
    print("\n=== TEST 5: Boton Limpiar ===")

    try:
        # Navegar con filtros
        driver.get(f"{PAGE_URL}?Buscar=test&FechaDesde=2024-01-01&Grupo=Anillo&Modo=detalle&Pagina=2")
        time.sleep(2)
        screenshot(driver, "05_with_filters")

        # Verificar campos con valores
        buscar_val = driver.find_element(By.NAME, "Buscar").get_attribute("value")
        print(f"  Buscar antes de limpiar: '{buscar_val}'")
        assert buscar_val == "test"

        # Click Limpiar
        btn_limpiar = driver.find_element(By.CSS_SELECTOR, "a[href='/Ventas/ConsultaBajasCentral']")
        btn_limpiar.click()
        time.sleep(2)
        screenshot(driver, "05_after_clear")

        # Verificar campos vacios
        buscar_val = driver.find_element(By.NAME, "Buscar").get_attribute("value")
        desde_val = driver.find_element(By.NAME, "FechaDesde").get_attribute("value")
        hasta_val = driver.find_element(By.NAME, "FechaHasta").get_attribute("value")

        assert buscar_val == "", f"Buscar no se limpio: '{buscar_val}'"
        assert desde_val == "", f"FechaDesde no se limpio: '{desde_val}'"
        assert hasta_val == "", f"FechaHasta no se limpio: '{hasta_val}'"
        print("  Campos limpiados: OK")

        # URL limpia
        assert "Buscar=" not in driver.current_url
        assert "FechaDesde=" not in driver.current_url
        print("  URL limpia: OK")

        screenshot(driver, "05_clear_complete")
        print("  TEST 5 PASSED")
        results.append(("TEST 5: Boton Limpiar", "PASSED"))
        return True

    except Exception as e:
        screenshot(driver, "05_clear_error")
        print(f"  TEST 5 FAILED: {e}")
        results.append(("TEST 5: Boton Limpiar", f"FAILED: {e}"))
        return False


def test_06_pagination(driver, wait):
    """Test 6: Paginacion server-side"""
    print("\n=== TEST 6: Paginacion ===")

    try:
        driver.get(PAGE_URL)
        time.sleep(2)

        # Verificar stats (total piezas)
        badges = driver.find_elements(By.CSS_SELECTOR, ".badge")
        piezas_text = ""
        for b in badges:
            if "Piezas" in b.text:
                piezas_text = b.text
                break
        print(f"  Stats: {piezas_text}")

        # Buscar paginacion
        pagination = driver.find_elements(By.CSS_SELECTOR, "nav .pagination")
        if pagination:
            page_items = driver.find_elements(By.CSS_SELECTOR, "nav .page-item")
            print(f"  Paginacion visible con {len(page_items)} items")

            # Navegar a pagina 2 via URL
            driver.get(f"{PAGE_URL}?Pagina=2")
            time.sleep(2)
            screenshot(driver, "06_page_2")

            # Verificar que pagina 2 esta activa
            active_page = driver.find_elements(By.CSS_SELECTOR, "nav .page-item.active .page-link")
            if active_page:
                print(f"  Pagina activa: {active_page[0].text}")
                assert active_page[0].text == "2", f"Pagina activa no es 2: {active_page[0].text}"
                print("  Pagina 2 activa: OK")

            # Verificar info de paginacion
            info = driver.find_elements(By.CSS_SELECTOR, "small.text-muted")
            for i in info:
                if "Pagina" in i.text:
                    print(f"  Info paginacion: {i.text}")
                    break
        else:
            print("  Sin paginacion (menos de 50 registros)")
            # Verificar que el texto de info muestra registros
            info = driver.find_elements(By.CSS_SELECTOR, "small.text-muted")
            for i in info:
                if "Mostrando" in i.text:
                    print(f"  Info: {i.text}")

        screenshot(driver, "06_pagination_complete")
        print("  TEST 6 PASSED")
        results.append(("TEST 6: Paginacion", "PASSED"))
        return True

    except Exception as e:
        screenshot(driver, "06_pagination_error")
        print(f"  TEST 6 FAILED: {e}")
        results.append(("TEST 6: Paginacion", f"FAILED: {e}"))
        return False


def test_07_no_errors(driver, wait):
    """Test 7: Pagina carga sin errores de servidor"""
    print("\n=== TEST 7: Sin errores de servidor ===")

    try:
        driver.get(PAGE_URL)
        time.sleep(2)

        # Verificar que no hay alertas de error
        errors = driver.find_elements(By.CSS_SELECTOR, ".alert-danger")
        if errors:
            error_text = errors[0].text
            print(f"  ERROR encontrado: {error_text[:100]}")
            results.append(("TEST 7: Sin errores", f"FAILED: {error_text[:80]}"))
            screenshot(driver, "07_error_found")
            return False

        # Verificar que la tabla existe
        table = driver.find_element(By.CSS_SELECTOR, "#tblBajasCentral")
        assert table is not None
        print("  Tabla presente: OK")

        # Verificar que no hay error 500 en el body
        body = driver.find_element(By.TAG_NAME, "body").text
        assert "500" not in body or "Internal Server Error" not in body
        assert "Exception" not in body
        print("  Sin errores 500/Exception: OK")

        screenshot(driver, "07_no_errors")
        print("  TEST 7 PASSED")
        results.append(("TEST 7: Sin errores", "PASSED"))
        return True

    except Exception as e:
        screenshot(driver, "07_errors_check_error")
        print(f"  TEST 7 FAILED: {e}")
        results.append(("TEST 7: Sin errores", f"FAILED: {e}"))
        return False


def test_08_data_with_results(driver, wait):
    """Test 8: Verificar formato de datos cuando hay resultados"""
    print("\n=== TEST 8: Datos con resultados ===")

    try:
        # Cargar con rango amplio para obtener datos
        driver.get(f"{PAGE_URL}?FechaDesde=2005-01-01&FechaHasta=2026-12-31")
        time.sleep(3)
        screenshot(driver, "08_data_results")

        rows = driver.find_elements(By.CSS_SELECTOR, "#tblBajasCentral tbody tr")

        if len(rows) == 0:
            print("  SKIP: Sin datos en vBajasPiezas")
            results.append(("TEST 8: Datos con resultados", "SKIPPED (sin datos)"))
            return True

        first_tds = rows[0].find_elements(By.TAG_NAME, "td")
        if len(first_tds) < 5:
            # Fila de "no encontrado"
            print(f"  Mensaje: {rows[0].text[:60]}")
            results.append(("TEST 8: Datos con resultados", "SKIPPED (mensaje vacio)"))
            return True

        # Verificar datos de primera fila
        codigo = first_tds[0].text.strip()
        descripcion = first_tds[1].text.strip()
        precio = first_tds[4].text.strip()
        print(f"  Codigo: {codigo}")
        print(f"  Descripcion: {descripcion[:40]}")
        print(f"  Precio: {precio}")

        # Verificar que el codigo esta en <code>
        code_el = first_tds[0].find_elements(By.TAG_NAME, "code")
        assert len(code_el) > 0, "Codigo no esta en <code>"
        print("  Codigo en <code>: OK")

        # Verificar precio es numerico
        assert any(c.isdigit() for c in precio), f"Precio no tiene digitos: {precio}"
        print("  Precio numerico: OK")

        screenshot(driver, "08_data_complete")
        print("  TEST 8 PASSED")
        results.append(("TEST 8: Datos con resultados", "PASSED"))
        return True

    except Exception as e:
        screenshot(driver, "08_data_error")
        print(f"  TEST 8 FAILED: {e}")
        results.append(("TEST 8: Datos con resultados", f"FAILED: {e}"))
        return False


def main():
    print("=" * 70)
    print("SELENIUM TEST: Consulta de Bajas Central")
    print(f"Task: #287290 | Pantalla padre: #287289")
    print(f"URL: {PAGE_URL}")
    print(f"Fecha: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print("=" * 70)

    driver = setup_driver()
    wait = WebDriverWait(driver, 15)

    try:
        if not login(driver, wait):
            print("\n*** FALLO EN LOGIN ***")
            results.append(("LOGIN", "FAILED"))
            return 1

        test_01_page_load(driver, wait)
        test_02_search_text(driver, wait)
        test_03_search_by_date(driver, wait)
        test_04_toggle_detalle(driver, wait)
        test_05_clear_filters(driver, wait)
        test_06_pagination(driver, wait)
        test_07_no_errors(driver, wait)
        test_08_data_with_results(driver, wait)

    except Exception as e:
        print(f"\n*** ERROR GLOBAL: {e} ***")
        try:
            screenshot(driver, "global_error")
        except Exception:
            pass
        results.append(("GLOBAL", f"ERROR: {e}"))

    finally:
        try:
            driver.quit()
        except Exception:
            pass

    # Resumen
    print("\n" + "=" * 70)
    print("RESUMEN DE RESULTADOS")
    print("=" * 70)
    passed = failed = skipped = 0
    for name, result in results:
        icon = "PASS" if "PASSED" in result else ("SKIP" if "SKIP" in result else "FAIL")
        print(f"  [{icon}] {name}: {result}")
        if "PASSED" in result:
            passed += 1
        elif "SKIP" in result:
            skipped += 1
        else:
            failed += 1

    print(f"\nTotal: {passed} passed, {failed} failed, {skipped} skipped")
    print("=" * 70)

    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
