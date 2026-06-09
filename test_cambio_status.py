#!/usr/bin/env python3
"""
Selenium test: Cambio de Status de Piezas (DiamondsWeb)
Flujo: Login → Navegar a /CambioStatus → Escanear pieza → Cambiar status → Verificar bitácora
"""

from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import WebDriverWait, Select
from selenium.webdriver.support import expected_conditions as EC
import time
import sys
import os

BASE_URL = "http://localhost:56023"
SCREENSHOT_DIR = "/home/earaiza/DiamondsWeb-worktrees/bot-286848/screenshots"
TEST_USER = "admin"
TEST_PASS = "u38a8fk3j0!"

# Pieza real de la BD (exhibición, status 1)
TEST_CB = "167269"  # Medalla oro amarillo 14k Virgen Milagrosa chica

passed = 0
failed = 0
errors = []


def setup_driver():
    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--disable-gpu")
    options.add_argument("--window-size=1280,720")
    options.add_argument("--ignore-certificate-errors")
    options.add_argument("--disable-extensions")
    options.add_argument("--disable-background-networking")
    options.add_argument("--disable-sync")
    options.add_argument("--disable-translate")
    options.add_argument("--single-process")
    options.add_argument("--js-flags=--max-old-space-size=128")
    options.set_capability("goog:loggingPrefs", {"browser": "ALL"})
    return webdriver.Chrome(options=options)


def screenshot(driver, name):
    os.makedirs(SCREENSHOT_DIR, exist_ok=True)
    path = f"{SCREENSHOT_DIR}/{name}.png"
    driver.save_screenshot(path)
    print(f"   Screenshot: {path}")


def check(label, condition, driver=None):
    global passed, failed
    if condition:
        passed += 1
        print(f"  PASS: {label}")
    else:
        failed += 1
        errors.append(label)
        print(f"  FAIL: {label}")
        if driver:
            screenshot(driver, f"FAIL_{label.replace(' ', '_')[:40]}")


def login(driver):
    """Login via UserPortal auth"""
    print("\n--- 1. Login ---")
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.TAG_NAME, "form"))
    )
    screenshot(driver, "01_login_page")

    user_fields = driver.find_elements(
        By.CSS_SELECTOR,
        "input[type='text'], input[name*='user'], input[name*='User'], "
        "input[id*='user'], input[id*='User']"
    )
    pass_fields = driver.find_elements(By.CSS_SELECTOR, "input[type='password']")

    if user_fields:
        user_fields[0].clear()
        user_fields[0].send_keys(TEST_USER)
    if pass_fields:
        pass_fields[0].clear()
        pass_fields[0].send_keys(TEST_PASS)

    submit = driver.find_elements(
        By.CSS_SELECTOR, "button[type='submit'], input[type='submit']"
    )
    if submit:
        submit[0].click()

    WebDriverWait(driver, 15).until(
        lambda d: "/Login" not in d.current_url
    )
    check("Login exitoso", "/Login" not in driver.current_url, driver)
    screenshot(driver, "02_post_login")


def test_pagina_carga(driver):
    """Verificar que la página CambioStatus carga correctamente"""
    print("\n--- 2. Carga de pagina ---")
    driver.get(f"{BASE_URL}/CambioStatus")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.ID, "txtCodigoBarras"))
    )
    screenshot(driver, "03_cambio_status_page")

    title = driver.title
    check("Titulo contiene 'Cambio de Status'",
          "Cambio de Status" in driver.page_source or "CambioStatus" in driver.current_url,
          driver)

    # Verificar elementos clave
    cb_input = driver.find_element(By.ID, "txtCodigoBarras")
    check("Input codigo barras presente", cb_input is not None, driver)

    btn_buscar = driver.find_element(By.ID, "btnBuscar")
    check("Boton buscar presente", btn_buscar is not None, driver)

    # Verificar grid de piezas
    tabla = driver.find_elements(By.CSS_SELECTOR, ".table-aml")
    check("Grid de piezas visible", len(tabla) > 0, driver)

    # Verificar tabs
    tabs = driver.find_elements(By.CSS_SELECTOR, ".nav-tabs .nav-link")
    check("Tabs presentes (piezas + bitacora)", len(tabs) >= 2, driver)

    # Verificar panel nuevo status oculto
    panel = driver.find_element(By.ID, "panelNuevoStatus")
    check("Panel nuevo status oculto inicialmente",
          panel.value_of_css_property("display") == "none", driver)


def test_buscar_pieza(driver):
    """Escanear/buscar pieza por código de barras"""
    print("\n--- 3. Buscar pieza ---")
    cb_input = driver.find_element(By.ID, "txtCodigoBarras")
    cb_input.clear()
    cb_input.send_keys(TEST_CB)
    screenshot(driver, "04_codigo_ingresado")

    # Simular Enter (busca via AJAX)
    cb_input.send_keys(Keys.RETURN)
    time.sleep(2)  # Esperar AJAX
    screenshot(driver, "05_pieza_encontrada")

    # Verificar que el status actual se muestra
    status_input = driver.find_element(By.ID, "txtStatusActual")
    status_value = status_input.get_attribute("value")
    check("Status actual mostrado", len(status_value) > 0, driver)
    print(f"   Status actual: '{status_value}'")

    # Verificar que el panel de nuevo status está visible
    panel = driver.find_element(By.ID, "panelNuevoStatus")
    check("Panel nuevo status visible despues de buscar",
          panel.value_of_css_property("display") != "none", driver)

    # Verificar descripción visible
    desc_span = driver.find_element(By.ID, "spanDescripcion")
    check("Descripcion de pieza mostrada",
          len(desc_span.text) > 0, driver)
    print(f"   Descripcion: '{desc_span.text[:60]}'")

    return status_value


def test_cambiar_status(driver, status_anterior):
    """Cambiar el status de la pieza"""
    print("\n--- 4. Cambiar status ---")

    # Seleccionar un status diferente al actual
    select_el = driver.find_element(By.ID, "selectNuevoStatus")
    select = Select(select_el)
    opciones = [o for o in select.options if o.get_attribute("value") and o.text != status_anterior]
    check("Hay opciones de status disponibles", len(opciones) > 0, driver)

    if len(opciones) == 0:
        print("   No hay opciones para cambiar, saltando...")
        return None

    # Seleccionar "Guardado" (id=5) si existe, o la primera opción diferente
    nuevo_status = None
    for o in opciones:
        if o.get_attribute("value") == "5":
            nuevo_status = o
            break
    if nuevo_status is None:
        nuevo_status = opciones[0]

    nuevo_status_nombre = nuevo_status.text
    nuevo_status_id = nuevo_status.get_attribute("value")
    print(f"   Cambiando de '{status_anterior}' a '{nuevo_status_nombre}' (id={nuevo_status_id})")

    select.select_by_value(nuevo_status_id)
    screenshot(driver, "06_status_seleccionado")

    # Aceptar el confirm dialog
    btn_cambiar = driver.find_element(By.ID, "btnCambiar")

    # Interceptar el confirm() para que retorne true automáticamente
    driver.execute_script("window.confirm = function() { return true; }")
    btn_cambiar.click()

    # Esperar recarga de la página (form POST)
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success, .alert-danger"))
    )
    screenshot(driver, "07_resultado_cambio")

    # Verificar mensaje de éxito
    alerts_success = driver.find_elements(By.CSS_SELECTOR, ".alert-success")
    check("Mensaje de exito mostrado", len(alerts_success) > 0, driver)

    if alerts_success:
        msg = alerts_success[0].text
        print(f"   Mensaje: '{msg}'")
        check("Mensaje contiene Id de Cambio", "Id de Cambio" in msg, driver)

    return nuevo_status_nombre, nuevo_status_id


def test_verificar_bitacora(driver):
    """Verificar que el cambio aparece en la bitácora"""
    print("\n--- 5. Verificar bitacora ---")

    # Click en la tab de bitácora
    tab_bitacora = driver.find_element(
        By.CSS_SELECTOR, "a[href='#tabBitacora']"
    )
    tab_bitacora.click()
    time.sleep(1)
    screenshot(driver, "08_tab_bitacora")

    # Verificar que la tabla de bitácora tiene registros
    rows = driver.find_elements(
        By.CSS_SELECTOR, "#tabBitacora .table-aml tbody tr"
    )
    check("Bitacora tiene registros", len(rows) > 0, driver)

    if rows:
        # Verificar que el primer registro es el cambio recién hecho
        first_row_text = rows[0].text
        check("Primer registro contiene el codigo de barras",
              TEST_CB in first_row_text, driver)
        print(f"   Primera fila: '{first_row_text[:80]}'")


def test_revertir_status(driver, status_original_nombre):
    """Revertir al status original para limpiar datos de prueba"""
    print("\n--- 6. Revertir status (limpieza) ---")

    # Buscar la pieza de nuevo
    cb_input = driver.find_element(By.ID, "txtCodigoBarras")
    cb_input.clear()
    cb_input.send_keys(TEST_CB)
    cb_input.send_keys(Keys.RETURN)
    time.sleep(2)

    # Seleccionar el status original
    select_el = driver.find_element(By.ID, "selectNuevoStatus")
    select = Select(select_el)

    # Encontrar la opción que coincide con el nombre original
    original_option = None
    for o in select.options:
        if o.text == status_original_nombre:
            original_option = o
            break

    if original_option:
        select.select_by_value(original_option.get_attribute("value"))
        driver.execute_script("window.confirm = function() { return true; }")
        driver.find_element(By.ID, "btnCambiar").click()
        WebDriverWait(driver, 15).until(
            EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success, .alert-danger"))
        )
        alerts = driver.find_elements(By.CSS_SELECTOR, ".alert-success")
        check("Status revertido exitosamente", len(alerts) > 0, driver)
        screenshot(driver, "09_status_revertido")
    else:
        print(f"   No se encontro opcion para '{status_original_nombre}', omitiendo reversion")


def test_filtro_grid(driver):
    """Probar el filtro del grid por status"""
    print("\n--- 7. Filtro del grid ---")
    driver.get(f"{BASE_URL}/CambioStatus")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.ID, "txtCodigoBarras"))
    )

    # Contar filas sin filtro
    rows_sin_filtro = driver.find_elements(
        By.CSS_SELECTOR, "#tabPiezas .table-aml tbody tr"
    )
    count_sin_filtro = len(rows_sin_filtro)
    print(f"   Filas sin filtro: {count_sin_filtro}")

    # Aplicar filtro (seleccionar primer status disponible)
    filtro_select = driver.find_elements(
        By.CSS_SELECTOR, "select[name='FiltroStatus']"
    )
    if filtro_select:
        select = Select(filtro_select[0])
        opciones = [o for o in select.options if o.get_attribute("value")]
        if opciones:
            opciones[0].click()
            WebDriverWait(driver, 15).until(
                EC.presence_of_element_located((By.ID, "txtCodigoBarras"))
            )
            rows_con_filtro = driver.find_elements(
                By.CSS_SELECTOR, "#tabPiezas .table-aml tbody tr"
            )
            screenshot(driver, "10_grid_filtrado")
            check("Filtro del grid funciona",
                  len(rows_con_filtro) <= count_sin_filtro, driver)
            print(f"   Filas con filtro: {len(rows_con_filtro)}")


def test_boton_escanear_grid(driver):
    """Probar el botón de escanear desde el grid"""
    print("\n--- 8. Boton escanear desde grid ---")
    driver.get(f"{BASE_URL}/CambioStatus")
    WebDriverWait(driver, 15).until(
        EC.presence_of_element_located((By.ID, "txtCodigoBarras"))
    )

    btns = driver.find_elements(By.CSS_SELECTOR, ".btn-escanear")
    if btns:
        first_cb = btns[0].get_attribute("data-cb")
        print(f"   Clicking escanear para CB={first_cb}")
        btns[0].click()
        time.sleep(2)

        cb_input = driver.find_element(By.ID, "txtCodigoBarras")
        check("Codigo de barras cargado desde grid",
              cb_input.get_attribute("value") == first_cb, driver)

        status_input = driver.find_element(By.ID, "txtStatusActual")
        check("Status actualizado al escanear desde grid",
              len(status_input.get_attribute("value")) > 0, driver)
        screenshot(driver, "11_escanear_desde_grid")
    else:
        print("   No hay botones de escanear en el grid (puede estar vacío)")


def main():
    global passed, failed
    print(f"=== Test Cambio de Status de Piezas ===")
    print(f"URL: {BASE_URL}")
    print(f"Pieza de prueba: {TEST_CB}")

    driver = setup_driver()
    try:
        login(driver)
        test_pagina_carga(driver)
        status_anterior = test_buscar_pieza(driver)
        resultado = test_cambiar_status(driver, status_anterior)
        test_verificar_bitacora(driver)

        if resultado and status_anterior:
            test_revertir_status(driver, status_anterior)

        test_filtro_grid(driver)
        test_boton_escanear_grid(driver)

        # Check JS console errors
        print("\n--- JS Console Errors ---")
        logs = driver.get_log("browser")
        js_errors = [l for l in logs if l["level"] == "SEVERE"]
        if js_errors:
            for err in js_errors[:5]:
                print(f"   JS ERROR: {err['message'][:100]}")
        else:
            print("   Sin errores JS criticos")

    except Exception as e:
        failed += 1
        errors.append(f"Exception: {str(e)}")
        print(f"\n  EXCEPTION: {e}")
        screenshot(driver, "99_exception")
    finally:
        driver.quit()

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
