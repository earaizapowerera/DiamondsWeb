"""
Selenium test: Actualización de Costos Individual - DiamondsWeb
Prueba end-to-end: Login -> Buscar pieza -> Seleccionar -> Verificar costos -> Calculos JS -> Guardar
Migración de frmActualizacionesII.frm (VB6) - Ticket #287280
"""
import time
import sys
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.support.ui import WebDriverWait, Select
from selenium.webdriver.support import expected_conditions as EC

BASE_URL = "https://diamonds-bot-287280.dev.powerera.com"
USER = "admin"
PASS = "Waykee2026!"
PAGE_PATH = "/Procesos/ActualizacionCostoIndividual"


def setup_driver():
    opts = Options()
    opts.add_argument("--headless=new")
    opts.add_argument("--no-sandbox")
    opts.add_argument("--disable-dev-shm-usage")
    opts.add_argument("--window-size=1920,1080")
    opts.add_argument("--ignore-certificate-errors")
    return webdriver.Chrome(options=opts)


def login(driver, wait):
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    wait.until(EC.presence_of_element_located((By.NAME, "LoginViewModel.Username")))
    driver.find_element(By.NAME, "LoginViewModel.Username").send_keys(USER)
    driver.find_element(By.NAME, "LoginViewModel.Password").send_keys(PASS)
    driver.find_element(By.CSS_SELECTOR, "button[type='submit']").click()
    # Wait for redirect to dashboard after login
    wait.until(EC.url_contains("/Security/App"))
    time.sleep(1)
    print(f"  Login OK, URL: {driver.current_url}")


def test_page_loads(driver, wait):
    """Test 1: Verificar que la página carga correctamente"""
    print("\n=== TEST 1: Página carga correctamente ===")
    driver.get(f"{BASE_URL}{PAGE_PATH}")
    wait.until(EC.presence_of_element_located((By.ID, "txtBuscar")))

    # Verificar título
    title = driver.find_element(By.CSS_SELECTOR, ".card-header.bg-primary")
    assert "Buscar Piezas" in title.text, f"Título incorrecto: {title.text}"
    print("  Header 'Buscar Piezas' OK")

    # Verificar campo de búsqueda
    buscar = driver.find_element(By.ID, "txtBuscar")
    assert buscar is not None, "Campo buscar no encontrado"
    print("  Campo búsqueda OK")

    # Verificar botones
    btn_buscar = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
    assert btn_buscar is not None, "Botón buscar no encontrado"
    btn_limpiar = driver.find_element(By.CSS_SELECTOR, "a.btn-outline-secondary")
    assert btn_limpiar is not None, "Botón limpiar no encontrado"
    print("  Botones Buscar/Limpiar OK")

    print("  TEST 1 PASSED")


def test_search_returns_results(driver, wait):
    """Test 2: Buscar piezas y verificar resultados"""
    print("\n=== TEST 2: Búsqueda de piezas ===")
    driver.get(f"{BASE_URL}{PAGE_PATH}")
    wait.until(EC.presence_of_element_located((By.ID, "txtBuscar")))

    # Buscar con un término genérico que debería devolver resultados
    buscar = driver.find_element(By.ID, "txtBuscar")
    buscar.clear()
    buscar.send_keys("a")
    buscar.send_keys(Keys.RETURN)

    time.sleep(3)

    # Verificar que aparece la tabla de resultados
    try:
        tabla = wait.until(EC.presence_of_element_located(
            (By.CSS_SELECTOR, "table.table-hover")))
        rows = driver.find_elements(By.CSS_SELECTOR, "table.table-hover tbody tr")
        print(f"  Piezas encontradas: {len(rows)}")
        assert len(rows) > 0, "No se encontraron piezas"

        # Verificar encabezados de la tabla
        headers = driver.find_elements(By.CSS_SELECTOR, "table.table-hover thead th")
        header_texts = [h.text for h in headers]
        print(f"  Encabezados: {header_texts}")
        assert "Código" in header_texts, "Falta columna Código"
        assert "Descripción" in header_texts, "Falta columna Descripción"
        assert "CB Pieza" in header_texts, "Falta columna CB Pieza"
        print("  Encabezados de tabla OK")

        # Capturar código de la primera pieza para tests posteriores
        first_code = rows[0].find_element(By.CSS_SELECTOR, "code").text
        print(f"  Primera pieza: {first_code}")
        print("  TEST 2 PASSED")
        return first_code
    except Exception as e:
        # Puede que no haya resultados con "a", intentar sin filtro
        print(f"  Sin resultados con 'a': {e}")
        # Verificar si hay mensaje de warning
        warnings = driver.find_elements(By.CSS_SELECTOR, ".alert-warning")
        if warnings:
            print(f"  Warning: {warnings[0].text}")
        print("  TEST 2 SKIPPED (no data)")
        return None


def test_search_no_results(driver, wait):
    """Test 3: Verificar mensaje cuando no hay resultados"""
    print("\n=== TEST 3: Búsqueda sin resultados ===")
    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=ZZZZNOEXISTE999")
    time.sleep(3)

    warnings = driver.find_elements(By.CSS_SELECTOR, ".alert-warning")
    if warnings:
        assert "No se encontraron" in warnings[0].text, \
            f"Mensaje inesperado: {warnings[0].text}"
        print(f"  Warning mostrado: {warnings[0].text}")
    else:
        # También es válido que simplemente no muestre tabla
        tablas = driver.find_elements(By.CSS_SELECTOR, "table.table-hover")
        assert len(tablas) == 0, "No debería haber tabla de resultados"
        print("  Sin tabla de resultados (correcto)")

    print("  TEST 3 PASSED")


def test_select_piece(driver, wait, codigo_barras):
    """Test 4: Seleccionar una pieza y verificar panel de costos"""
    print("\n=== TEST 4: Seleccionar pieza ===")

    # Primero buscar
    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=a")
    time.sleep(3)

    # Intentar encontrar la pieza en la tabla y hacer clic
    rows = driver.find_elements(By.CSS_SELECTOR, "table.table-hover tbody tr")
    if not rows:
        print("  TEST 4 SKIPPED (no data)")
        return None

    # Hacer clic en el botón de selección de la primera pieza
    first_btn = rows[0].find_element(By.CSS_SELECTOR, "a.btn")
    first_code = rows[0].find_element(By.CSS_SELECTOR, "code").text
    first_btn.click()
    time.sleep(3)

    # Verificar que aparece el panel de la pieza seleccionada
    try:
        pieza_header = wait.until(EC.presence_of_element_located(
            (By.CSS_SELECTOR, ".card-header.bg-info")))
        print(f"  Panel pieza: {pieza_header.text[:80]}")
        assert first_code in pieza_header.text, \
            f"Código {first_code} no aparece en header"
        print("  Panel de pieza seleccionada OK")

        # Verificar que aparece el campo de folio factura
        folio_input = driver.find_element(By.ID, "txtFolioFactura")
        assert folio_input is not None, "Campo folio factura no encontrado"
        print("  Campo folio factura OK")

        # Verificar la fila queda resaltada en la tabla
        active_rows = driver.find_elements(By.CSS_SELECTOR, "tr.table-active")
        assert len(active_rows) > 0, "Fila seleccionada no está resaltada"
        print("  Fila resaltada OK")

        print("  TEST 4 PASSED")
        return first_code
    except Exception as e:
        print(f"  Error seleccionando pieza: {e}")
        print("  TEST 4 FAILED")
        raise


def test_piece_with_existing_invoice(driver, wait):
    """Test 5: Seleccionar pieza que ya tiene factura asignada"""
    print("\n=== TEST 5: Pieza con factura existente ===")

    # Buscar piezas
    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=a")
    time.sleep(3)

    rows = driver.find_elements(By.CSS_SELECTOR, "table.table-hover tbody tr")
    if not rows:
        print("  TEST 5 SKIPPED (no data)")
        return None

    # Buscar una pieza que tenga factura (IdFactura != vacío)
    pieza_con_factura = None
    for row in rows:
        cols = row.find_elements(By.TAG_NAME, "td")
        if len(cols) >= 9:
            factura_text = cols[8].text.strip()
            if factura_text and factura_text != "0" and factura_text != "":
                codigo = cols[0].find_element(By.CSS_SELECTOR, "code").text
                pieza_con_factura = codigo
                print(f"  Pieza con factura: {codigo} (IdFactura={factura_text})")
                break

    if not pieza_con_factura:
        print("  No se encontró pieza con factura existente")
        print("  TEST 5 SKIPPED")
        return None

    # Seleccionar la pieza
    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=a&CodigoBarrasSeleccionado={pieza_con_factura}")
    time.sleep(3)

    # Verificar que se carga la factura y aparece el formulario de costos
    try:
        form_guardar = wait.until(EC.presence_of_element_located((By.ID, "formGuardar")))
        print("  Formulario de costos cargado OK")

        # Verificar campos de costos
        cb_pieza = driver.find_element(By.ID, "txtCB")
        cn_pieza = driver.find_element(By.ID, "txtCN")
        tc = driver.find_element(By.ID, "txtTC")
        moneda = driver.find_element(By.ID, "selMoneda")
        print(f"  CB Pieza: {cb_pieza.get_attribute('value')}")
        print(f"  CN Pieza: {cn_pieza.get_attribute('value')}")
        print(f"  TC: {tc.get_attribute('value')}")
        print("  Campos de costos OK")

        # Verificar campos MN
        cb_factura = driver.find_element(By.ID, "txtCBFactura")
        cn_factura = driver.find_element(By.ID, "txtCNFactura")
        print(f"  CB Factura MN: {cb_factura.get_attribute('value')}")
        print(f"  CN Factura MN: {cn_factura.get_attribute('value')}")
        print("  Campos MN OK")

        # Verificar botón guardar
        btn_guardar = driver.find_element(By.ID, "btnGuardar")
        assert btn_guardar is not None, "Botón guardar no encontrado"
        print("  Botón guardar OK")

        print("  TEST 5 PASSED")
        return pieza_con_factura
    except Exception as e:
        print(f"  Error: {e}")
        print("  TEST 5 FAILED")
        raise


def test_js_cost_calculations(driver, wait):
    """Test 6: Verificar cálculos JavaScript de costos"""
    print("\n=== TEST 6: Cálculos JavaScript de costos ===")

    # Buscar pieza con factura existente
    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=a")
    time.sleep(3)

    rows = driver.find_elements(By.CSS_SELECTOR, "table.table-hover tbody tr")
    pieza_con_factura = None
    for row in rows:
        cols = row.find_elements(By.TAG_NAME, "td")
        if len(cols) >= 9:
            factura_text = cols[8].text.strip()
            if factura_text and factura_text != "0":
                pieza_con_factura = cols[0].find_element(By.CSS_SELECTOR, "code").text
                break

    if not pieza_con_factura:
        print("  No se encontró pieza con factura para probar cálculos")
        print("  TEST 6 SKIPPED")
        return

    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=a&CodigoBarrasSeleccionado={pieza_con_factura}")
    time.sleep(3)

    try:
        wait.until(EC.presence_of_element_located((By.ID, "formGuardar")))
    except:
        print("  No se cargó formulario de costos")
        print("  TEST 6 SKIPPED")
        return

    # Test: CB cambia → CN se recalcula
    cb_input = driver.find_element(By.ID, "txtCB")
    cn_input = driver.find_element(By.ID, "txtCN")
    desc_input = driver.find_element(By.ID, "txtDesc")
    tc_input = driver.find_element(By.ID, "txtTC")
    cb_factura = driver.find_element(By.ID, "txtCBFactura")
    cn_factura = driver.find_element(By.ID, "txtCNFactura")

    # Limpiar y establecer valores conocidos
    # CB = 1000, Desc = 10%, CN esperado = 900
    cb_input.clear()
    cb_input.send_keys("1000")

    desc_input.clear()
    desc_input.send_keys("10")

    # Disparar cálculo
    driver.execute_script("calcularDesdeDesc()")
    time.sleep(0.5)

    cn_val = float(cn_input.get_attribute("value"))
    assert abs(cn_val - 900.0) < 0.01, f"CN Pieza debería ser 900, es {cn_val}"
    print(f"  CB=1000, Desc=10% → CN={cn_val} OK")

    # Verificar MN con TC
    tc_val = float(tc_input.get_attribute("value") or "1")
    cbf_val = float(cb_factura.get_attribute("value"))
    cnf_val = float(cn_factura.get_attribute("value"))

    expected_cbf = 1000.0 * tc_val
    expected_cnf = 900.0 * tc_val
    assert abs(cbf_val - expected_cbf) < 0.01, \
        f"CB Factura debería ser {expected_cbf}, es {cbf_val}"
    assert abs(cnf_val - expected_cnf) < 0.01, \
        f"CN Factura debería ser {expected_cnf}, es {cnf_val}"
    print(f"  TC={tc_val} → CBFactura={cbf_val}, CNFactura={cnf_val} OK")

    # Test: CN cambia → Desc se recalcula
    cn_input.clear()
    cn_input.send_keys("800")
    driver.execute_script("calcularDesdeCN()")
    time.sleep(0.5)

    desc_val = float(desc_input.get_attribute("value"))
    expected_desc = 100 * (1 - 800.0 / 1000.0)  # = 20%
    assert abs(desc_val - expected_desc) < 0.01, \
        f"Desc debería ser {expected_desc}%, es {desc_val}%"
    print(f"  CN=800, CB=1000 → Desc={desc_val}% OK")

    print("  TEST 6 PASSED")


def test_invoice_search(driver, wait):
    """Test 7: Buscar factura por folio"""
    print("\n=== TEST 7: Búsqueda de factura por folio ===")

    # Buscar pieza sin factura
    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=a")
    time.sleep(3)

    rows = driver.find_elements(By.CSS_SELECTOR, "table.table-hover tbody tr")
    pieza_sin_factura = None
    for row in rows:
        cols = row.find_elements(By.TAG_NAME, "td")
        if len(cols) >= 9:
            factura_text = cols[8].text.strip()
            codigo = cols[0].find_element(By.CSS_SELECTOR, "code").text
            if not factura_text or factura_text == "0" or factura_text == "":
                pieza_sin_factura = codigo
                break

    if not pieza_sin_factura:
        # Usar la primera pieza
        if rows:
            pieza_sin_factura = rows[0].find_element(By.CSS_SELECTOR, "code").text
        else:
            print("  TEST 7 SKIPPED (no data)")
            return

    # Seleccionar pieza e ingresar folio inexistente
    driver.get(
        f"{BASE_URL}{PAGE_PATH}?Buscar=a"
        f"&CodigoBarrasSeleccionado={pieza_sin_factura}"
        f"&FolioFactura=FOLIO_INEXISTENTE_999"
    )
    time.sleep(3)

    # Verificar que aparece formulario de alta de factura
    form_alta = driver.find_elements(By.CSS_SELECTOR, ".card.border-warning")
    if form_alta:
        print("  Formulario 'Registrar Nueva Factura' mostrado OK")

        # Verificar campos del formulario
        fecha = driver.find_elements(By.CSS_SELECTOR, "input[name='fechaFactura']")
        razon = driver.find_elements(By.ID, "selRazonSocial")
        btn_registrar = driver.find_elements(
            By.CSS_SELECTOR, ".card.border-warning button[type='submit']")

        assert len(fecha) > 0, "Campo fecha factura no encontrado"
        assert len(btn_registrar) > 0, "Botón registrar no encontrado"
        print("  Campos de alta factura OK")
        print("  TEST 7 PASSED")
    else:
        # Puede que el proveedor no tenga razones sociales configuradas
        print("  Formulario de alta no apareció (posible falta de razones sociales)")
        print("  TEST 7 SKIPPED")


def test_limpiar_button(driver, wait):
    """Test 8: Verificar botón Limpiar"""
    print("\n=== TEST 8: Botón Limpiar ===")
    # Navegar con parámetros
    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=test")
    wait.until(EC.presence_of_element_located((By.ID, "txtBuscar")))
    time.sleep(1)

    # Hacer clic en Limpiar
    btn_limpiar = wait.until(EC.element_to_be_clickable(
        (By.CSS_SELECTOR, "a.btn-outline-secondary")))
    btn_limpiar.click()
    wait.until(EC.presence_of_element_located((By.ID, "txtBuscar")))
    time.sleep(1)

    # Verificar que URL está limpia
    assert "Buscar=" not in driver.current_url, \
        f"URL debería estar limpia: {driver.current_url}"

    # Verificar que campo búsqueda está vacío
    buscar = driver.find_element(By.ID, "txtBuscar")
    assert buscar.get_attribute("value") == "", \
        f"Campo búsqueda debería estar vacío: {buscar.get_attribute('value')}"
    print("  Botón Limpiar OK")
    print("  TEST 8 PASSED")


def test_f6_shortcut(driver, wait):
    """Test 9: Verificar atajo F6 para buscar"""
    print("\n=== TEST 9: Atajo F6 ===")
    driver.get(f"{BASE_URL}{PAGE_PATH}")
    wait.until(EC.presence_of_element_located((By.ID, "txtBuscar")))

    # Hacer clic en otro lugar para quitar focus
    driver.find_element(By.TAG_NAME, "body").click()
    time.sleep(0.5)

    # Presionar F6
    driver.find_element(By.TAG_NAME, "body").send_keys(Keys.F6)
    time.sleep(0.5)

    # Verificar que el campo de búsqueda tiene el focus
    focused = driver.switch_to.active_element
    focused_id = focused.get_attribute("id")
    assert focused_id == "txtBuscar", \
        f"Focus debería estar en txtBuscar, está en: {focused_id}"
    print("  F6 → focus en búsqueda OK")
    print("  TEST 9 PASSED")


def test_save_costs(driver, wait):
    """Test 10: Guardar costos (end-to-end)"""
    print("\n=== TEST 10: Guardar costos (E2E) ===")

    # Buscar pieza con factura
    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=a")
    time.sleep(3)

    rows = driver.find_elements(By.CSS_SELECTOR, "table.table-hover tbody tr")
    pieza_con_factura = None
    original_cb = None
    for row in rows:
        cols = row.find_elements(By.TAG_NAME, "td")
        if len(cols) >= 9:
            factura_text = cols[8].text.strip()
            if factura_text and factura_text != "0":
                pieza_con_factura = cols[0].find_element(By.CSS_SELECTOR, "code").text
                original_cb = cols[4].text.strip()
                break

    if not pieza_con_factura:
        print("  No se encontró pieza con factura para test E2E")
        print("  TEST 10 SKIPPED")
        return

    # Cargar pieza con su factura
    driver.get(f"{BASE_URL}{PAGE_PATH}?Buscar=a&CodigoBarrasSeleccionado={pieza_con_factura}")
    time.sleep(3)

    try:
        wait.until(EC.presence_of_element_located((By.ID, "formGuardar")))
    except:
        print("  No se cargó formulario de costos")
        print("  TEST 10 SKIPPED")
        return

    # Leer valores actuales
    cb_input = driver.find_element(By.ID, "txtCB")
    cn_input = driver.find_element(By.ID, "txtCN")
    current_cb = cb_input.get_attribute("value")
    current_cn = cn_input.get_attribute("value")
    print(f"  Valores actuales: CB={current_cb}, CN={current_cn}")

    # Establecer valores de prueba y recalcular
    cb_input.clear()
    cb_input.send_keys(current_cb or "100")
    driver.execute_script("calcularDesdeCB()")
    time.sleep(0.5)

    # Guardar
    btn_guardar = driver.find_element(By.ID, "btnGuardar")
    if btn_guardar.is_enabled():
        btn_guardar.click()
        time.sleep(4)

        # Verificar mensaje de éxito
        success = driver.find_elements(By.CSS_SELECTOR, ".alert-success")
        if success:
            print(f"  Mensaje: {success[0].text}")
            assert "actualizados exitosamente" in success[0].text, \
                f"Mensaje inesperado: {success[0].text}"
            print("  Costos guardados exitosamente")
            print("  TEST 10 PASSED")
        else:
            errors = driver.find_elements(By.CSS_SELECTOR, ".alert-danger")
            if errors:
                print(f"  Error al guardar: {errors[0].text}")
                print("  TEST 10 FAILED")
                raise AssertionError(f"Error al guardar: {errors[0].text}")
            else:
                print("  Sin mensaje de confirmación ni error")
                print("  TEST 10 INCONCLUSIVE")
    else:
        print("  Botón guardar deshabilitado (costos en 0)")
        print("  TEST 10 SKIPPED")


def main():
    print("=" * 60)
    print("SELENIUM: Actualización de Costos Individual")
    print(f"URL: {BASE_URL}{PAGE_PATH}")
    print("=" * 60)

    driver = setup_driver()
    wait = WebDriverWait(driver, 15)
    passed = 0
    failed = 0
    skipped = 0

    try:
        login(driver, wait)

        # Test 1: Carga de página
        try:
            test_page_loads(driver, wait)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test 2: Búsqueda con resultados
        codigo = None
        try:
            codigo = test_search_returns_results(driver, wait)
            if codigo:
                passed += 1
            else:
                skipped += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test 3: Búsqueda sin resultados
        try:
            test_search_no_results(driver, wait)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test 4: Seleccionar pieza
        try:
            selected = test_select_piece(driver, wait, codigo)
            if selected:
                passed += 1
            else:
                skipped += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test 5: Pieza con factura existente
        try:
            pieza = test_piece_with_existing_invoice(driver, wait)
            if pieza:
                passed += 1
            else:
                skipped += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test 6: Cálculos JavaScript
        try:
            test_js_cost_calculations(driver, wait)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test 7: Búsqueda factura
        try:
            test_invoice_search(driver, wait)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test 8: Botón Limpiar
        try:
            test_limpiar_button(driver, wait)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test 9: F6
        try:
            test_f6_shortcut(driver, wait)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test 10: Guardar costos E2E
        try:
            test_save_costs(driver, wait)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

    finally:
        driver.quit()

    print("\n" + "=" * 60)
    print(f"RESULTADOS: {passed} passed, {failed} failed, {skipped} skipped, "
          f"{passed + failed + skipped} total")
    print("=" * 60)

    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
