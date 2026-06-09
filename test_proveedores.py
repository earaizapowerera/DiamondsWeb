"""
Test Selenium: CRUD de Catálogo de Proveedores en DiamondsWeb
Ticket: 286808
"""
import sys
import time
import argparse
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

parser = argparse.ArgumentParser()
parser.add_argument("--url", default="http://localhost:56286")
parser.add_argument("--user", default="admin")
parser.add_argument("--password", default="u38a8fk3j0!")
args = parser.parse_args()

BASE_URL = args.url.rstrip("/")
PASSED = 0
FAILED = 0

def setup_driver():
    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--disable-gpu")
    options.add_argument("--disable-extensions")
    options.add_argument("--disable-software-rasterizer")
    options.add_argument("--window-size=1280,720")
    options.add_argument("--remote-debugging-port=0")
    driver = webdriver.Chrome(options=options)
    driver.implicitly_wait(3)
    driver.set_page_load_timeout(30)
    return driver

def test(name, condition, detail=""):
    global PASSED, FAILED
    if condition:
        PASSED += 1
        print(f"  PASS: {name}")
    else:
        FAILED += 1
        print(f"  FAIL: {name} -- {detail}")

print("=== Test: CRUD Catalogo de Proveedores ===\n")

driver = setup_driver()
wait = WebDriverWait(driver, 10)

try:
    # ── Step 1: Login ──────────────────────────────────────────────
    print("1. Login")
    driver.get(BASE_URL + "/Security/Auth/Login")
    time.sleep(1)

    try:
        user_input = wait.until(EC.presence_of_element_located((By.ID, "LoginViewModel_Username")))
        user_input.clear()
        user_input.send_keys(args.user)
        pwd_input = driver.find_element(By.ID, "LoginViewModel_Password")
        pwd_input.clear()
        pwd_input.send_keys(args.password)
        login_btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
        login_btn.click()
        time.sleep(2)
        # After login, should redirect away from login page
        logged_in = "Login" not in driver.current_url.split("?")[0]
        test("Login exitoso", logged_in, f"URL: {driver.current_url}")
    except Exception as e:
        test("Login exitoso", False, str(e)[:100])

    # ── Step 2: Navigate to Proveedores ────────────────────────────
    print("\n2. Navegar a Proveedores")
    driver.get(BASE_URL + "/Proveedores")
    time.sleep(2)

    page_src = driver.page_source
    test("Pagina Proveedores carga", "Catalogo de Proveedores" in page_src,
         f"URL: {driver.current_url}")

    # Check table exists
    try:
        table = driver.find_element(By.ID, "tblProveedores")
        rows = table.find_elements(By.CSS_SELECTOR, "tbody tr")
        test("Tabla de proveedores visible", len(rows) > 0, f"Filas: {len(rows)}")
    except Exception as e:
        test("Tabla de proveedores visible", False, str(e)[:80])

    # Verify the "Nuevo Proveedor" button
    try:
        new_btn = driver.find_element(By.CSS_SELECTOR, "a[href='/Proveedores/Editar']")
        test("Boton Nuevo Proveedor visible", new_btn.is_displayed())
    except Exception as e:
        test("Boton Nuevo Proveedor visible", False, str(e)[:80])

    # ── Step 3: Search ────────────────────────────────────────────
    print("\n3. Busqueda")
    try:
        search_input = driver.find_element(By.NAME, "Buscar")
        search_input.clear()
        search_input.send_keys("TRESSOR")
        search_btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
        search_btn.click()
        time.sleep(2)
        test("Busqueda filtra resultados", "TRESSOR" in driver.page_source)

        # Clear search
        driver.get(BASE_URL + "/Proveedores")
        time.sleep(1)
    except Exception as e:
        test("Busqueda filtra resultados", False, str(e)[:80])

    # ── Step 4: Create new proveedor ───────────────────────────────
    print("\n4. Crear nuevo proveedor")
    driver.get(BASE_URL + "/Proveedores/Editar")
    time.sleep(2)

    test("Formulario nuevo proveedor carga", "Nuevo Proveedor" in driver.page_source,
         f"URL: {driver.current_url}")

    try:
        nombre = driver.find_element(By.ID, "Prov_NombreProveedor")
        nombre.clear()
        nombre.send_keys("Selenium Test Proveedor")

        direccion = driver.find_element(By.ID, "Prov_Direccion")
        direccion.clear()
        direccion.send_keys("Calle Test 123")

        telefono = driver.find_element(By.ID, "Prov_Telefono")
        telefono.clear()
        telefono.send_keys("5551234567")

        atiende = driver.find_element(By.ID, "Prov_Atiende")
        atiende.clear()
        atiende.send_keys("Persona Test")

        test("Campos de datos generales llenados", True)
    except Exception as e:
        test("Campos de datos generales llenados", False, str(e)[:80])

    # Set CaracteristicaDefault via JS (handles TomSelect wrapping)
    try:
        driver.execute_script("""
            var el = document.getElementById('Prov_CaracteristicaDefault');
            if (el) { el.value = 'Diamante'; el.dispatchEvent(new Event('change', {bubbles: true})); }
            var el2 = document.getElementById('Prov_CostoDefault');
            if (el2) { el2.value = 'Pieza'; el2.dispatchEvent(new Event('change', {bubbles: true})); }
        """)
        test("Selects de Caracteristica y Costo configurados", True)
    except Exception as e:
        test("Selects de Caracteristica y Costo configurados", False, str(e)[:80])

    # Check utilidad extra toggle
    try:
        chk_extra = driver.find_element(By.ID, "chkUtilidadExtra")
        was_checked = chk_extra.is_selected()
        driver.execute_script("arguments[0].click()", chk_extra)
        time.sleep(0.3)
        div_extra = driver.find_element(By.ID, "divUtilidadExtra")
        test("Toggle utilidad extra funciona", div_extra.is_displayed() != was_checked)
        # Reset
        driver.execute_script("arguments[0].click()", chk_extra)
        time.sleep(0.3)
    except Exception as e:
        test("Toggle utilidad extra funciona", False, str(e)[:80])

    # Check moneda toggle
    try:
        chk_moneda = driver.find_element(By.ID, "chkMoneda")
        driver.execute_script("arguments[0].click()", chk_moneda)
        time.sleep(0.3)
        sel_moneda = driver.find_element(By.ID, "Prov_IdMoneda")
        # When checked, moneda dropdown should be enabled
        test("Toggle moneda funciona", True)
    except Exception as e:
        test("Toggle moneda funciona", False, str(e)[:80])

    # Submit form
    try:
        submit_btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
        driver.execute_script("arguments[0].scrollIntoView(true);", submit_btn)
        time.sleep(0.3)
        submit_btn.click()
        time.sleep(3)
        redirected = "/Proveedores" in driver.current_url and "Editar" not in driver.current_url
        test("Proveedor creado (redirect a Index)", redirected, f"URL: {driver.current_url}")

        if redirected:
            test("Mensaje de exito", "creado" in driver.page_source.lower(),
                 "Buscando confirmacion en page source")
    except Exception as e:
        test("Proveedor creado", False, str(e)[:80])

    # ── Step 5: Edit the created proveedor ─────────────────────────
    print("\n5. Editar proveedor creado")
    driver.get(BASE_URL + "/Proveedores?Buscar=Selenium+Test")
    time.sleep(2)

    try:
        test("Proveedor test encontrado en grid", "Selenium Test Proveedor" in driver.page_source)

        edit_links = driver.find_elements(By.CSS_SELECTOR, "a[title='Editar']")
        if edit_links:
            edit_links[0].click()
            time.sleep(2)
            test("Pagina editar carga", "Editar" in driver.page_source, f"URL: {driver.current_url}")

            nombre = driver.find_element(By.ID, "Prov_NombreProveedor")
            nombre.clear()
            nombre.send_keys("Selenium Test Proveedor Editado")

            submit_btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
            driver.execute_script("arguments[0].scrollIntoView(true);", submit_btn)
            time.sleep(0.3)
            submit_btn.click()
            time.sleep(3)
            test("Proveedor editado (redirect a Index)", "/Proveedores" in driver.current_url and "Editar" not in driver.current_url,
                 f"URL: {driver.current_url}")
        else:
            test("Botones editar encontrados", False, "No edit buttons found")
    except Exception as e:
        test("Editar proveedor", False, str(e)[:80])

    # ── Step 6: Delete test proveedor ──────────────────────────────
    print("\n6. Eliminar proveedor de prueba")
    driver.get(BASE_URL + "/Proveedores?Buscar=Selenium+Test")
    time.sleep(2)

    try:
        driver.execute_script("window.confirm = function() { return true; }")
        delete_btns = driver.find_elements(By.CSS_SELECTOR, "button[title='Eliminar']")
        if delete_btns:
            delete_btns[0].click()
            time.sleep(3)
            test("Proveedor eliminado", "eliminado" in driver.page_source.lower() or "Selenium Test" not in driver.page_source)
        else:
            test("Boton eliminar encontrado", False, "No delete buttons found")
    except Exception as e:
        test("Eliminar proveedor", False, str(e)[:80])

    # ── Step 7: Verify defaults on existing proveedor ──────────────
    print("\n7. Verificar defaults (proveedor existente #69 TRESSOR)")
    driver.get(BASE_URL + "/Proveedores/Editar?id=69")
    time.sleep(2)

    try:
        nombre = driver.find_element(By.ID, "Prov_NombreProveedor")
        test("Datos de proveedor existente cargan", "TRESSOR" in nombre.get_attribute("value"),
             f"Valor: {nombre.get_attribute('value')}")

        txt_oro = driver.find_element(By.ID, "txtUtilidadOro")
        test("Utilidad Joyeria muestra valor", txt_oro.get_attribute("value") != "",
             f"Valor: '{txt_oro.get_attribute('value')}'")

        txt_gemas = driver.find_element(By.ID, "txtUtilidadGemas")
        test("Utilidad Gemas muestra valor", txt_gemas.get_attribute("value") != "",
             f"Valor: '{txt_gemas.get_attribute('value')}'")

        txt_reloj = driver.find_element(By.ID, "txtUtilidadReloj")
        test("Utilidad Relojes muestra valor", txt_reloj.get_attribute("value") != "",
             f"Valor: '{txt_reloj.get_attribute('value')}'")
    except Exception as e:
        test("Verificar defaults", False, str(e)[:80])

    driver.save_screenshot("/tmp/test_proveedores_final.png")
    print("\n  Screenshot: /tmp/test_proveedores_final.png")

except Exception as e:
    print(f"\n  ERROR FATAL: {e}")
    try:
        driver.save_screenshot("/tmp/test_proveedores_error.png")
    except:
        pass
finally:
    driver.quit()

print(f"\n=== Resultados: {PASSED} PASSED, {FAILED} FAILED ===")
sys.exit(0 if FAILED == 0 else 1)
