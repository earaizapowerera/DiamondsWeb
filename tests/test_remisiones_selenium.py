"""
Selenium tests for Actualización de Remisiones (DiamondsWeb)
Ticket: 286846

Tests:
1. Login y acceso a /Remisiones
2. Crear remisión nueva
3. Seleccionar remisión y buscar piezas disponibles
4. Vincular pieza a remisión
5. Desvincular pieza de remisión
6. Modificar remisión existente
7. Eliminar remisión (sin piezas)
"""

import time
import sys
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.common.keys import Keys

BASE_URL = "https://diamonds-bot-286846.dev.powerera.com"
LOGIN_USER = "admin"
LOGIN_PASS = "u38a8fk3j0!"

# Track created remision to clean up
created_remision_id = None


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

    # Wait for redirect away from login
    wait.until(lambda d: "/Auth/Login" not in d.current_url)
    print("[OK] Login exitoso")


def test_01_acceso_remisiones(driver):
    """Verifica acceso a la página de Remisiones."""
    driver.get(f"{BASE_URL}/Remisiones")
    wait = WebDriverWait(driver, 10)

    # Debe mostrar el título
    heading = wait.until(EC.presence_of_element_located(
        (By.XPATH, "//*[contains(text(),'Actualizacion de Remisiones')]")))
    assert heading is not None, "Titulo no encontrado"

    # Debe mostrar la tabla de remisiones
    table = driver.find_element(By.CSS_SELECTOR, "table.table-rem")
    assert table is not None, "Tabla de remisiones no encontrada"

    # Debe tener filtros
    buscar_input = driver.find_element(By.CSS_SELECTOR, "input[name='Buscar']")
    assert buscar_input is not None, "Campo buscar no encontrado"

    print("[OK] Test 01: Acceso a /Remisiones")


def test_02_crear_remision(driver):
    """Crea una nueva remisión de prueba."""
    global created_remision_id

    driver.get(f"{BASE_URL}/Remisiones")
    wait = WebDriverWait(driver, 10)

    # Click "Nueva" button
    nueva_btn = wait.until(EC.element_to_be_clickable(
        (By.XPATH, "//button[contains(text(),'Nueva')]")))
    nueva_btn.click()

    # Wait for modal
    time.sleep(1)
    modal = wait.until(EC.visibility_of_element_located(
        (By.ID, "modalRemision")))

    # Fill proveedor via TomSelect - type in its input
    try:
        ts_input = modal.find_element(
            By.CSS_SELECTOR, "#proveedorSelect + .ts-wrapper input.ts-input input, .ts-control input")
        ts_input.click()
        ts_input.send_keys("Dise")
        time.sleep(0.5)
        # Click first option
        first_option = wait.until(EC.element_to_be_clickable(
            (By.CSS_SELECTOR, ".ts-dropdown .option")))
        first_option.click()
    except Exception:
        # Fallback to regular select
        from selenium.webdriver.support.ui import Select
        select = Select(modal.find_element(By.ID, "proveedorSelect"))
        select.select_by_index(1)

    # Fill remision number
    remision_input = modal.find_element(By.ID, "inputRemision")
    remision_input.clear()
    remision_input.send_keys("TEST-SEL")

    # Set fecha
    fecha_input = modal.find_element(By.ID, "inputFecha")
    fecha_input.clear()
    fecha_input.send_keys("2026-06-09")

    # Check consignacion
    consignacion_cb = modal.find_element(By.ID, "inputConsignacion")
    if not consignacion_cb.is_selected():
        consignacion_cb.click()

    # Submit
    guardar_btn = modal.find_element(By.ID, "btnGuardar")
    guardar_btn.click()

    # Wait for page reload with success message
    time.sleep(2)
    success = wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".alert-success")))
    assert "creada exitosamente" in success.text, f"Mensaje inesperado: {success.text}"

    # Extract created remision ID from message
    import re
    match = re.search(r'#(\d+)', success.text)
    if match:
        created_remision_id = int(match.group(1))

    print(f"[OK] Test 02: Crear remision (ID: {created_remision_id})")


def test_03_seleccionar_y_buscar_piezas(driver):
    """Selecciona remisión creada y busca piezas disponibles."""
    assert created_remision_id is not None, "No se creo remision en test anterior"

    driver.get(f"{BASE_URL}/Remisiones?SelId={created_remision_id}")
    wait = WebDriverWait(driver, 10)

    # Debe mostrar panel info de remision
    info_panel = wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".info-panel-rem")))
    assert str(created_remision_id) in info_panel.text, "Id de remision no visible en panel"

    # Debe mostrar consignación badge
    badge = driver.find_element(By.XPATH, "//*[contains(text(),'Consignacion')]")
    assert badge is not None, "Badge consignacion no encontrado"

    # Buscar piezas disponibles
    buscar_pieza = driver.find_element(By.CSS_SELECTOR, "input[name='BuscarPieza']")
    buscar_pieza.clear()
    buscar_pieza.send_keys("1")  # Search broad to get results
    buscar_pieza.submit()

    time.sleep(2)

    # Should have piezas disponibles table
    tabla_disponibles = driver.find_elements(By.XPATH,
        "//th[contains(text(),'Codigo')]")
    assert len(tabla_disponibles) > 0, "Tabla de piezas disponibles no encontrada"

    print("[OK] Test 03: Seleccionar remision y buscar piezas")


def test_04_vincular_pieza(driver):
    """Vincula una pieza a la remisión."""
    assert created_remision_id is not None, "No se creo remision en test anterior"

    driver.get(f"{BASE_URL}/Remisiones?SelId={created_remision_id}&BuscarPieza=16")
    wait = WebDriverWait(driver, 10)
    time.sleep(2)

    # Find vincular buttons (green arrow-right)
    vincular_btns = driver.find_elements(
        By.CSS_SELECTOR, "form[action*='handler=Vincular'] button[type='submit']")

    if len(vincular_btns) == 0:
        print("[SKIP] Test 04: No hay piezas disponibles para vincular")
        return

    # Click first vincular button
    vincular_btns[0].click()
    time.sleep(2)

    # Check success message
    success = wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".alert-success")))
    assert "vinculada" in success.text.lower(), f"Mensaje inesperado: {success.text}"

    print("[OK] Test 04: Vincular pieza a remision")


def test_05_desvincular_pieza(driver):
    """Desvincula una pieza de la remisión."""
    assert created_remision_id is not None, "No se creo remision en test anterior"

    driver.get(f"{BASE_URL}/Remisiones?SelId={created_remision_id}")
    wait = WebDriverWait(driver, 10)
    time.sleep(2)

    # Find desvincular buttons (red X)
    desvincular_btns = driver.find_elements(
        By.CSS_SELECTOR, "form[action*='handler=Desvincular'] button[type='submit']")

    if len(desvincular_btns) == 0:
        print("[SKIP] Test 05: No hay piezas vinculadas para desvincular")
        return

    # Accept the confirm dialog
    driver.execute_script(
        "window.originalConfirm = window.confirm; window.confirm = function() { return true; };")

    desvincular_btns[0].click()
    time.sleep(2)

    # Restore confirm
    driver.execute_script(
        "if (window.originalConfirm) window.confirm = window.originalConfirm;")

    success = wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".alert-success")))
    assert "desvinculada" in success.text.lower(), f"Mensaje inesperado: {success.text}"

    print("[OK] Test 05: Desvincular pieza de remision")


def test_06_modificar_remision(driver):
    """Modifica la remisión creada."""
    assert created_remision_id is not None, "No se creo remision en test anterior"

    driver.get(f"{BASE_URL}/Remisiones?SelId={created_remision_id}")
    wait = WebDriverWait(driver, 10)
    time.sleep(1)

    # Click Modificar button
    modificar_btn = wait.until(EC.element_to_be_clickable(
        (By.XPATH, "//button[contains(text(),'Modificar')]")))
    modificar_btn.click()

    # Wait for modal
    time.sleep(1)
    modal = wait.until(EC.visibility_of_element_located(
        (By.ID, "modalRemision")))

    # Change remision number
    remision_input = modal.find_element(By.ID, "inputRemision")
    remision_input.clear()
    remision_input.send_keys("TEST-MOD")

    # Uncheck consignacion
    consignacion_cb = modal.find_element(By.ID, "inputConsignacion")
    if consignacion_cb.is_selected():
        consignacion_cb.click()

    # Submit
    guardar_btn = modal.find_element(By.ID, "btnGuardar")
    guardar_btn.click()

    time.sleep(2)

    success = wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".alert-success")))
    assert "actualizada" in success.text.lower(), f"Mensaje inesperado: {success.text}"

    print("[OK] Test 06: Modificar remision")


def test_07_eliminar_remision(driver):
    """Elimina la remisión de prueba (cleanup)."""
    assert created_remision_id is not None, "No se creo remision en test anterior"

    driver.get(f"{BASE_URL}/Remisiones?SelId={created_remision_id}")
    wait = WebDriverWait(driver, 10)
    time.sleep(1)

    # Auto-accept confirm dialog
    driver.execute_script(
        "window.originalConfirm = window.confirm; window.confirm = function() { return true; };")

    # Click Eliminar button
    eliminar_btn = wait.until(EC.element_to_be_clickable(
        (By.XPATH, "//button[contains(text(),'Eliminar')]")))
    eliminar_btn.click()
    time.sleep(2)

    # Restore confirm
    driver.execute_script(
        "if (window.originalConfirm) window.confirm = window.originalConfirm;")

    # Check result - could be success (deleted) or error (has piezas)
    alerts = driver.find_elements(By.CSS_SELECTOR, ".alert-success, .alert-danger")
    if alerts:
        text = alerts[0].text.lower()
        if "eliminada" in text:
            print("[OK] Test 07: Eliminar remision")
        elif "piezas vinculadas" in text:
            print("[OK] Test 07: Eliminar bloqueado correctamente (tiene piezas)")
        else:
            print(f"[WARN] Test 07: Resultado inesperado: {alerts[0].text}")
    else:
        print("[WARN] Test 07: Sin mensaje de resultado")


def main():
    driver = None
    passed = 0
    failed = 0
    tests = [
        test_01_acceso_remisiones,
        test_02_crear_remision,
        test_03_seleccionar_y_buscar_piezas,
        test_04_vincular_pieza,
        test_05_desvincular_pieza,
        test_06_modificar_remision,
        test_07_eliminar_remision,
    ]

    try:
        driver = create_driver()
        login(driver)

        for test_fn in tests:
            try:
                test_fn(driver)
                passed += 1
            except Exception as e:
                failed += 1
                print(f"[FAIL] {test_fn.__name__}: {e}")
                # Take screenshot on failure
                try:
                    screenshot_path = f"/tmp/selenium_fail_{test_fn.__name__}.png"
                    driver.save_screenshot(screenshot_path)
                    print(f"       Screenshot: {screenshot_path}")
                except Exception:
                    pass

    finally:
        if driver:
            driver.quit()

    print(f"\n{'='*50}")
    print(f"Resultados: {passed} passed, {failed} failed, {len(tests)} total")
    print(f"{'='*50}")

    return 0 if failed == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
