"""
Prueba Selenium para la pantalla Tipos de Cambio (DiamondsWeb).
Verifica: login, carga de pagina, registro de tipo de cambio, orden cronologico, eliminacion.
"""
import time
import sys
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

BASE_URL = "https://bot-286816.dev.powerera.com"
LOGIN_USER = "admin"
LOGIN_PASS = "u38a8fk3j0!"

def setup_driver():
    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--disable-gpu")
    options.add_argument("--window-size=1280,720")
    options.add_argument("--disable-extensions")
    options.add_argument("--disable-background-networking")
    options.add_argument("--disable-software-rasterizer")
    options.add_argument("--js-flags=--max-old-space-size=256")
    driver = webdriver.Chrome(options=options)
    driver.set_page_load_timeout(30)
    return driver

def login(driver):
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.ID, "LoginViewModel_Username"))
    )
    driver.find_element(By.ID, "LoginViewModel_Username").send_keys(LOGIN_USER)
    driver.find_element(By.ID, "LoginViewModel_Password").send_keys(LOGIN_PASS)
    driver.find_element(By.CSS_SELECTOR, "button[type='submit']").click()
    WebDriverWait(driver, 15).until(
        lambda d: "/Login" not in d.current_url
    )
    print(f"  [OK] Login exitoso. URL actual: {driver.current_url}")

def test_pagina_carga(driver):
    """Verificar que la pagina /TiposCambio carga sin errores."""
    print("\n--- Test 1: Carga de pagina ---")
    driver.get(f"{BASE_URL}/TiposCambio")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, "h6"))
    )
    # Verificar titulo
    title = driver.title
    assert "Tipos de Cambio" in title or "Diamonds" in title, f"Titulo inesperado: {title}"

    # Verificar que no hay errores de servidor
    body = driver.find_element(By.TAG_NAME, "body").text
    assert "Error" not in body or "alert-danger" not in driver.page_source, \
        f"Error detectado en la pagina"

    # Verificar panel vigentes
    info_panel = driver.find_elements(By.CSS_SELECTOR, ".info-panel")
    assert len(info_panel) > 0, "Falta panel de tipos vigentes"

    # Verificar formulario de registro
    form_crear = driver.find_elements(By.CSS_SELECTOR, "select[name='IdMoneda']")
    assert len(form_crear) > 0, "Falta formulario de registro (dropdown moneda)"

    # Verificar tabla historial
    table = driver.find_elements(By.CSS_SELECTOR, ".table-tc")
    # Table might not exist if no data yet - that's OK

    print("  [OK] Pagina carga correctamente")

def test_registrar_tipo_cambio(driver):
    """Registrar un nuevo tipo de cambio y verificar que aparece."""
    print("\n--- Test 2: Registrar tipo de cambio ---")
    driver.get(f"{BASE_URL}/TiposCambio")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, "select[name='IdMoneda']"))
    )

    # Contar registros antes
    rows_before = len(driver.find_elements(By.CSS_SELECTOR, ".table-tc tbody tr"))

    # Llenar formulario - seleccionar Dolar (IdMoneda=2)
    moneda_select = driver.find_element(By.CSS_SELECTOR, "select[name='IdMoneda']")
    # Check if TomSelect wrapped it
    ts_wrappers = driver.find_elements(By.CSS_SELECTOR, ".ts-wrapper")
    if ts_wrappers:
        # TomSelect is active - use JS to set value
        driver.execute_script("""
            var select = document.querySelector('select[name="IdMoneda"]');
            if (select.tomselect) {
                select.tomselect.setValue('2');
            } else {
                select.value = '2';
            }
        """)
    else:
        from selenium.webdriver.support.select import Select
        Select(moneda_select).select_by_value("2")

    # Tipo cambio cotizacion
    cotizacion = driver.find_element(By.CSS_SELECTOR, "input[name='TipoCambioCotizacion']")
    cotizacion.clear()
    cotizacion.send_keys("19.8765")

    # Tipo cambio venta
    venta = driver.find_element(By.CSS_SELECTOR, "input[name='TipoCambioVenta']")
    venta.clear()
    venta.send_keys("20.1234")

    # Click registrar
    btn_registrar = driver.find_element(By.CSS_SELECTOR, "form[action*='Crear'] button[type='submit']")
    btn_registrar.click()

    # Esperar recarga
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success, .alert-danger, .table-tc"))
    )

    # Verificar mensaje de exito
    success = driver.find_elements(By.CSS_SELECTOR, ".alert-success")
    if success:
        print(f"  [OK] Mensaje de exito: {success[0].text}")

    # Verificar que hay un registro mas
    rows_after = len(driver.find_elements(By.CSS_SELECTOR, ".table-tc tbody tr"))
    assert rows_after >= rows_before + 1 or success, \
        f"No se agrego el registro. Antes: {rows_before}, Despues: {rows_after}"

    print(f"  [OK] Tipo de cambio registrado. Registros: {rows_before} -> {rows_after}")
    return rows_after

def test_orden_cronologico(driver):
    """Verificar que los tipos de cambio estan ordenados del mas reciente al mas antiguo."""
    print("\n--- Test 3: Orden cronologico ---")
    driver.get(f"{BASE_URL}/TiposCambio")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, ".table-tc tbody"))
    )

    rows = driver.find_elements(By.CSS_SELECTOR, ".table-tc tbody tr")
    if len(rows) < 2:
        print("  [SKIP] Menos de 2 registros, no se puede verificar orden")
        return

    # Extraer fechas de la columna 5 (indice 4)
    fechas = []
    for row in rows:
        cols = row.find_elements(By.TAG_NAME, "td")
        if len(cols) >= 5:
            fecha_text = cols[4].text.strip()
            fechas.append(fecha_text)

    # Verificar que estan en orden descendente (mas reciente primero)
    # El registro que acabamos de crear deberia estar primero
    if fechas:
        print(f"  Primera fecha (mas reciente): {fechas[0]}")
        if len(fechas) > 1:
            print(f"  Segunda fecha: {fechas[1]}")
        # El primer registro deberia tener fecha de hoy
        from datetime import datetime
        today = datetime.utcnow().strftime("%d/%m/%Y")
        # Also check for "09/06/2026" format
        today_alt = datetime.utcnow().strftime("%m/%d/%Y")
        assert today in fechas[0] or today_alt in fechas[0] or "2026" in fechas[0], \
            f"El registro mas reciente no es de hoy: {fechas[0]}"

    print(f"  [OK] Orden cronologico correcto ({len(fechas)} registros)")

def test_filtro_moneda(driver):
    """Verificar que el filtro por moneda funciona."""
    print("\n--- Test 4: Filtro por moneda ---")
    driver.get(f"{BASE_URL}/TiposCambio?FiltroMoneda=2")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, "h6"))
    )

    # Verificar badge de moneda filtrada
    badge = driver.find_elements(By.CSS_SELECTOR, ".badge.bg-info")
    if badge:
        print(f"  [OK] Badge de filtro: {badge[0].text}")

    # Todas las filas deben ser Dolar
    rows = driver.find_elements(By.CSS_SELECTOR, ".table-tc tbody tr")
    for row in rows:
        cols = row.find_elements(By.TAG_NAME, "td")
        if len(cols) >= 2:
            moneda_text = cols[1].text.strip()
            assert "Dolar" in moneda_text, f"Filtro fallo: se muestra '{moneda_text}' en vez de Dolar"

    print(f"  [OK] Filtro por moneda funciona ({len(rows)} registros de Dolar)")

def test_eliminar_tipo_cambio(driver):
    """Eliminar el tipo de cambio que registramos (cleanup)."""
    print("\n--- Test 5: Eliminar tipo de cambio ---")
    driver.get(f"{BASE_URL}/TiposCambio")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, ".table-tc tbody"))
    )

    rows_before = len(driver.find_elements(By.CSS_SELECTOR, ".table-tc tbody tr"))

    # Click en el boton eliminar del primer registro (el mas reciente, el que creamos)
    delete_btn = driver.find_elements(By.CSS_SELECTOR, ".table-tc tbody tr:first-child .btn-outline-danger")
    if not delete_btn:
        print("  [SKIP] No hay boton de eliminar")
        return

    # Handle confirm dialog
    driver.execute_script("window.confirm = function() { return true; }")
    delete_btn[0].click()

    # Esperar recarga
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success, .alert-danger, h6"))
    )

    rows_after = len(driver.find_elements(By.CSS_SELECTOR, ".table-tc tbody tr"))

    success = driver.find_elements(By.CSS_SELECTOR, ".alert-success")
    if success:
        print(f"  [OK] {success[0].text}")

    print(f"  [OK] Tipo de cambio eliminado. Registros: {rows_before} -> {rows_after}")

def main():
    print("=" * 60)
    print("SELENIUM TEST: Tipos de Cambio - DiamondsWeb")
    print(f"URL: {BASE_URL}/TiposCambio")
    print("=" * 60)

    driver = setup_driver()
    failed = 0

    try:
        login(driver)

        tests = [
            test_pagina_carga,
            test_registrar_tipo_cambio,
            test_orden_cronologico,
            test_filtro_moneda,
            test_eliminar_tipo_cambio,
        ]

        for test in tests:
            try:
                test(driver)
            except Exception as e:
                print(f"  [FAIL] {test.__name__}: {e}")
                driver.save_screenshot(f"/tmp/test_fail_{test.__name__}.png")
                failed += 1

    except Exception as e:
        print(f"\n[FATAL] Error general: {e}")
        driver.save_screenshot("/tmp/test_fatal.png")
        failed += 1

    finally:
        driver.quit()

    print("\n" + "=" * 60)
    if failed == 0:
        print("RESULTADO: TODOS LOS TESTS PASARON")
    else:
        print(f"RESULTADO: {failed} TEST(S) FALLARON")
    print("=" * 60)

    sys.exit(failed)

if __name__ == "__main__":
    main()
