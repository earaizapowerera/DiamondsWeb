"""
Selenium tests for Actualización Pieza por Pieza (DiamondsWeb)
Ticket: 286844

Tests:
1. Login y acceso a /Procesos/ActualizacionPieza
2. Buscar pieza por código de barras
3. Buscar factura por folio (AJAX)
4. Actualizar costos en moneda nacional
5. Actualizar costos en moneda extranjera con tipo de cambio
"""

import time
import sys
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait, Select
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.keys import Keys

BASE_URL = "https://diamonds-bot-286844.dev.powerera.com"
LOGIN_USER = "admin"
LOGIN_PASS = "u38a8fk3j0!"

PAGE_PATH = "/Procesos/ActualizacionPieza"


def create_driver():
    options = Options()
    options.add_argument("--headless")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--disable-gpu")
    options.add_argument("--disable-software-rasterizer")
    options.add_argument("--window-size=1920,1080")
    options.add_argument("--disable-extensions")
    options.add_argument("--single-process")
    options.binary_location = "/usr/bin/google-chrome"

    service = Service(log_output="/dev/null")
    driver = webdriver.Chrome(options=options, service=service)
    driver.set_page_load_timeout(30)
    driver.implicitly_wait(5)
    return driver


def login(driver):
    """Login to DiamondsWeb via UserPortal auth."""
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    wait = WebDriverWait(driver, 10)

    username_input = wait.until(EC.presence_of_element_located(
        (By.ID, "LoginViewModel_Username")))
    username_input.clear()
    username_input.send_keys(LOGIN_USER)

    password_input = driver.find_element(By.ID, "LoginViewModel_Password")
    password_input.clear()
    password_input.send_keys(LOGIN_PASS)

    submit_btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
    submit_btn.click()

    wait.until(lambda d: "/Auth/Login" not in d.current_url)
    print("[OK] Login exitoso")


def test_01_acceso_pagina(driver):
    """Verifica acceso a la página de Actualización Pieza."""
    driver.get(f"{BASE_URL}{PAGE_PATH}")
    wait = WebDriverWait(driver, 10)

    header = wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".card-header")))
    assert "Actualización Pieza por Pieza" in header.text, \
        f"Título esperado no encontrado. Encontrado: {header.text}"

    cb_input = driver.find_element(By.NAME, "CodigoBarras")
    assert cb_input is not None, "Campo de código de barras no encontrado"

    print("[OK] Página de Actualización Pieza accesible")


def test_02_buscar_pieza(driver):
    """Busca una pieza por código de barras y verifica info."""
    driver.get(f"{BASE_URL}{PAGE_PATH}")
    wait = WebDriverWait(driver, 10)

    cb_input = wait.until(EC.presence_of_element_located(
        (By.NAME, "CodigoBarras")))
    cb_input.clear()
    cb_input.send_keys("E27")

    buscar_btn = driver.find_element(
        By.CSS_SELECTOR, "button[type='submit']")
    buscar_btn.click()

    # Wait for pieza info card to appear
    pieza_header = wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".card-header.bg-info")))
    assert "Pieza:" in pieza_header.text, \
        f"Encabezado de pieza no encontrado. Encontrado: {pieza_header.text}"

    # Verify costs table is displayed
    costs_table = driver.find_element(By.CSS_SELECTOR, ".table-bordered")
    assert costs_table is not None, "Tabla de costos actuales no encontrada"

    # Verify factura section is visible
    factura_section = driver.find_element(By.ID, "folioFactura")
    assert factura_section is not None, "Sección de factura no encontrada"

    print("[OK] Pieza encontrada y datos mostrados correctamente")


def test_03_pieza_no_encontrada(driver):
    """Verifica mensaje de error cuando pieza no existe."""
    driver.get(f"{BASE_URL}{PAGE_PATH}?CodigoBarras=NOEXISTE999")
    wait = WebDriverWait(driver, 10)

    error_alert = wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".alert-danger")))
    assert "no encontrada" in error_alert.text.lower(), \
        f"Mensaje de error esperado no encontrado: {error_alert.text}"

    print("[OK] Pieza no encontrada muestra error correctamente")


def test_04_buscar_factura_ajax(driver):
    """Busca factura por folio vía AJAX."""
    # First load a piece
    driver.get(f"{BASE_URL}{PAGE_PATH}?CodigoBarras=E27")
    wait = WebDriverWait(driver, 10)

    # Wait for piece to load
    wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".card-header.bg-info")))

    # Enter folio and search factura
    folio_input = driver.find_element(By.ID, "folioFactura")
    folio_input.clear()
    folio_input.send_keys("1")

    btn_buscar_factura = driver.find_element(By.ID, "btnBuscarFactura")
    btn_buscar_factura.click()

    # Wait for either facturaInfo or crearFacturaPanel to appear
    time.sleep(2)  # Wait for AJAX response

    factura_found = not driver.find_element(
        By.ID, "facturaInfo").get_attribute("class").count("d-none")
    crear_panel = not driver.find_element(
        By.ID, "crearFacturaPanel").get_attribute("class").count("d-none")

    assert factura_found or crear_panel, \
        "Ni factura encontrada ni panel de creación mostrado"

    if factura_found:
        factura_folio = driver.find_element(By.ID, "facturaFolio").text
        print(f"[OK] Factura encontrada: {factura_folio}")
    else:
        print("[OK] Factura no encontrada, panel de creación mostrado")


def test_05_costos_panel_visible(driver):
    """Verifica que el panel de costos se muestra tras seleccionar factura."""
    # Load piece that has a factura
    driver.get(f"{BASE_URL}{PAGE_PATH}?CodigoBarras=E27")
    wait = WebDriverWait(driver, 10)

    wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".card-header.bg-info")))

    # If piece already has a factura, the cost panel should auto-show
    time.sleep(2)
    card_costos = driver.find_element(By.ID, "cardCostos")
    is_visible = card_costos.value_of_css_property("display") != "none"

    if is_visible:
        # Verify fields exist
        txt_cb = driver.find_element(By.ID, "txtCB")
        txt_desc = driver.find_element(By.ID, "txtDesc")
        txt_cn = driver.find_element(By.ID, "txtCN")
        sel_moneda = driver.find_element(By.ID, "selMoneda")

        assert txt_cb is not None, "Campo CB no encontrado"
        assert txt_desc is not None, "Campo Desc no encontrado"
        assert txt_cn is not None, "Campo CN no encontrado"
        assert sel_moneda is not None, "Selector de moneda no encontrado"

        print("[OK] Panel de costos visible con todos los campos")
    else:
        # Need to search a factura first
        folio_input = driver.find_element(By.ID, "folioFactura")
        folio_input.clear()
        folio_input.send_keys("1")
        driver.find_element(By.ID, "btnBuscarFactura").click()
        time.sleep(2)

        card_costos = driver.find_element(By.ID, "cardCostos")
        is_visible = card_costos.value_of_css_property("display") != "none"
        assert is_visible, "Panel de costos no se mostró después de buscar factura"
        print("[OK] Panel de costos visible después de buscar factura")


def test_06_calculo_nacional(driver):
    """Verifica auto-cálculo en moneda nacional (TC=1).
    CB=100, Desc=10 → CN=90, CBFactura=100, CNFactura=90"""
    driver.get(f"{BASE_URL}{PAGE_PATH}?CodigoBarras=E27")
    wait = WebDriverWait(driver, 10)

    wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".card-header.bg-info")))

    # Ensure factura is loaded (auto-load or manual search)
    time.sleep(2)
    card_costos = driver.find_element(By.ID, "cardCostos")
    if card_costos.value_of_css_property("display") == "none":
        folio_input = driver.find_element(By.ID, "folioFactura")
        folio_input.clear()
        folio_input.send_keys("1")
        driver.find_element(By.ID, "btnBuscarFactura").click()
        time.sleep(2)

    # Set moneda to 1 (Nacional)
    sel_moneda = Select(driver.find_element(By.ID, "selMoneda"))
    sel_moneda.select_by_value("1")

    # Extranjera panel should be hidden
    panel_ext = driver.find_element(By.ID, "panelExtranjera")
    assert "d-none" in panel_ext.get_attribute("class"), \
        "Panel extranjera debería estar oculto para moneda nacional"

    # Set CB = 100
    txt_cb = driver.find_element(By.ID, "txtCB")
    txt_cb.clear()
    txt_cb.send_keys("100")

    # Set Desc = 10
    txt_desc = driver.find_element(By.ID, "txtDesc")
    txt_desc.clear()
    txt_desc.send_keys("10")

    # Trigger change on CB to recalculate
    txt_cb.clear()
    txt_cb.send_keys("100")
    driver.execute_script(
        "document.getElementById('txtCB').dispatchEvent(new Event('change'))")
    time.sleep(0.5)

    # Verify CN = 100 * (1 - 10/100) = 90
    txt_cn = driver.find_element(By.ID, "txtCN")
    cn_value = float(txt_cn.get_attribute("value") or 0)
    assert abs(cn_value - 90.0) < 0.01, \
        f"CN esperado 90.00, obtenido {cn_value}"

    # Verify hidden fields
    hdn_cb_factura = driver.find_element(By.ID, "hdnCBFactura")
    hdn_cn_factura = driver.find_element(By.ID, "hdnCNFactura")
    cb_factura = float(hdn_cb_factura.get_attribute("value") or 0)
    cn_factura = float(hdn_cn_factura.get_attribute("value") or 0)

    assert abs(cb_factura - 100.0) < 0.01, \
        f"CBFactura esperado 100, obtenido {cb_factura}"
    assert abs(cn_factura - 90.0) < 0.01, \
        f"CNFactura esperado 90, obtenido {cn_factura}"

    # Verify Guardar button is enabled
    btn_guardar = driver.find_element(By.ID, "btnGuardar")
    assert not btn_guardar.get_attribute("disabled"), \
        "Botón Guardar debería estar habilitado"

    print("[OK] Cálculo nacional correcto: CB=100, Desc=10%, CN=90")


def test_07_calculo_extranjera(driver):
    """Verifica auto-cálculo en moneda extranjera.
    CB=50(USD), Desc=10, CN=45, TC=20 → NuevoBruto=1000, NuevoNeto=900"""
    driver.get(f"{BASE_URL}{PAGE_PATH}?CodigoBarras=E27")
    wait = WebDriverWait(driver, 10)

    wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".card-header.bg-info")))

    # Ensure factura is loaded
    time.sleep(2)
    card_costos = driver.find_element(By.ID, "cardCostos")
    if card_costos.value_of_css_property("display") == "none":
        folio_input = driver.find_element(By.ID, "folioFactura")
        folio_input.clear()
        folio_input.send_keys("1")
        driver.find_element(By.ID, "btnBuscarFactura").click()
        time.sleep(2)

    # Set moneda to 2 (Dolar)
    sel_moneda = Select(driver.find_element(By.ID, "selMoneda"))
    sel_moneda.select_by_value("2")
    driver.execute_script(
        "document.getElementById('selMoneda').dispatchEvent(new Event('change'))")
    time.sleep(0.5)

    # Extranjera panel should be visible
    panel_ext = driver.find_element(By.ID, "panelExtranjera")
    assert "d-none" not in panel_ext.get_attribute("class"), \
        "Panel extranjera debería estar visible para moneda 2"

    # Set TC = 20
    txt_tc = driver.find_element(By.ID, "txtTC")
    txt_tc.clear()
    txt_tc.send_keys("20")

    # Set Desc = 10
    txt_desc = driver.find_element(By.ID, "txtDesc")
    txt_desc.clear()
    txt_desc.send_keys("10")

    # Set CB = 50 (in foreign currency)
    txt_cb = driver.find_element(By.ID, "txtCB")
    txt_cb.clear()
    txt_cb.send_keys("50")
    driver.execute_script(
        "document.getElementById('txtCB').dispatchEvent(new Event('change'))")
    time.sleep(0.5)

    # Verify CN = 50 * (1 - 10/100) = 45
    txt_cn = driver.find_element(By.ID, "txtCN")
    cn_value = float(txt_cn.get_attribute("value") or 0)
    assert abs(cn_value - 45.0) < 0.01, \
        f"CN esperado 45.00, obtenido {cn_value}"

    # Verify NuevoBruto = 20 * 50 = 1000
    txt_nuevo_bruto = driver.find_element(By.ID, "txtNuevoBruto")
    nuevo_bruto = float(txt_nuevo_bruto.get_attribute("value") or 0)
    assert abs(nuevo_bruto - 1000.0) < 0.01, \
        f"NuevoBruto esperado 1000, obtenido {nuevo_bruto}"

    # Verify NuevoNeto = 20 * 45 = 900
    txt_nuevo_neto = driver.find_element(By.ID, "txtNuevoNeto")
    nuevo_neto = float(txt_nuevo_neto.get_attribute("value") or 0)
    assert abs(nuevo_neto - 900.0) < 0.01, \
        f"NuevoNeto esperado 900, obtenido {nuevo_neto}"

    # Verify hidden fields: for extranjera, CBFactura=NuevoBruto, CNFactura=NuevoNeto
    hdn_cb_factura = driver.find_element(By.ID, "hdnCBFactura")
    hdn_cn_factura = driver.find_element(By.ID, "hdnCNFactura")
    hdn_id_moneda = driver.find_element(By.ID, "hdnIdMoneda")
    hdn_tc = driver.find_element(By.ID, "hdnTC")

    assert hdn_id_moneda.get_attribute("value") == "2", \
        f"IdMoneda esperado 2, obtenido {hdn_id_moneda.get_attribute('value')}"
    assert hdn_tc.get_attribute("value") == "20", \
        f"TC esperado 20, obtenido {hdn_tc.get_attribute('value')}"
    assert abs(float(hdn_cb_factura.get_attribute("value") or 0) - 1000.0) < 0.01, \
        "CBFactura debería ser 1000 (MN)"
    assert abs(float(hdn_cn_factura.get_attribute("value") or 0) - 900.0) < 0.01, \
        "CNFactura debería ser 900 (MN)"

    print("[OK] Cálculo extranjera correcto: CB=50 USD, TC=20, "
          "NuevoBruto=1000 MN, NuevoNeto=900 MN")


def test_08_calculo_cn_inverso(driver):
    """Verifica cálculo inverso: editar CN recalcula Desc.
    CB=200, CN=180 → Desc = 100*(1-180/200) = 10%"""
    driver.get(f"{BASE_URL}{PAGE_PATH}?CodigoBarras=E27")
    wait = WebDriverWait(driver, 10)

    wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".card-header.bg-info")))

    time.sleep(2)
    card_costos = driver.find_element(By.ID, "cardCostos")
    if card_costos.value_of_css_property("display") == "none":
        folio_input = driver.find_element(By.ID, "folioFactura")
        folio_input.clear()
        folio_input.send_keys("1")
        driver.find_element(By.ID, "btnBuscarFactura").click()
        time.sleep(2)

    # Set moneda nacional
    sel_moneda = Select(driver.find_element(By.ID, "selMoneda"))
    sel_moneda.select_by_value("1")

    # Set CB = 200
    txt_cb = driver.find_element(By.ID, "txtCB")
    txt_cb.clear()
    txt_cb.send_keys("200")
    driver.execute_script(
        "document.getElementById('txtCB').dispatchEvent(new Event('change'))")
    time.sleep(0.3)

    # Set CN = 180 (this should recalculate Desc)
    txt_cn = driver.find_element(By.ID, "txtCN")
    txt_cn.clear()
    txt_cn.send_keys("180")
    driver.execute_script(
        "document.getElementById('txtCN').dispatchEvent(new Event('change'))")
    time.sleep(0.3)

    # Verify Desc = 100 * (1 - 180/200) = 10
    txt_desc = driver.find_element(By.ID, "txtDesc")
    desc_value = float(txt_desc.get_attribute("value") or 0)
    assert abs(desc_value - 10.0) < 0.01, \
        f"Desc esperado 10.00, obtenido {desc_value}"

    print("[OK] Cálculo inverso CN→Desc correcto: CB=200, CN=180, Desc=10%")


def main():
    tests = [
        test_01_acceso_pagina,
        test_02_buscar_pieza,
        test_03_pieza_no_encontrada,
        test_04_buscar_factura_ajax,
        test_05_costos_panel_visible,
        test_06_calculo_nacional,
        test_07_calculo_extranjera,
        test_08_calculo_cn_inverso,
    ]

    driver = None
    passed = 0
    failed = 0
    errors = []

    try:
        driver = create_driver()
        login(driver)

        for test_fn in tests:
            try:
                test_fn(driver)
                passed += 1
            except Exception as e:
                failed += 1
                test_name = test_fn.__name__
                errors.append(f"{test_name}: {e}")
                print(f"[FAIL] {test_name}: {e}")
                try:
                    driver.save_screenshot(f"/tmp/{test_name}.png")
                except Exception:
                    pass
    except Exception as e:
        print(f"[ERROR] Setup failed: {e}")
        return 1
    finally:
        if driver:
            driver.quit()

    print(f"\n{'='*50}")
    print(f"Results: {passed} passed, {failed} failed, {passed + failed} total")
    if errors:
        print("\nFailures:")
        for err in errors:
            print(f"  - {err}")
    print(f"{'='*50}")

    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
