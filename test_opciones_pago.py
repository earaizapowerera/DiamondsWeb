"""
Selenium test: CRUD completo de Opciones de Pago en DiamondsWeb.
Prueba: crear, editar, activar/desactivar, eliminar opciones de pago.
"""
import time
import sys
import traceback
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait, Select
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.chrome.options import Options

BASE_URL = "https://bot-286818.dev.powerera.com"
LOGIN_USER = "admin"
LOGIN_PASS = "u38a8fk3j0!"

def setup_driver():
    opts = Options()
    opts.add_argument("--headless=new")
    opts.add_argument("--no-sandbox")
    opts.add_argument("--disable-dev-shm-usage")
    opts.add_argument("--disable-gpu")
    opts.add_argument("--disable-extensions")
    opts.add_argument("--disable-background-networking")
    opts.add_argument("--disable-sync")
    opts.add_argument("--disable-translate")
    opts.add_argument("--no-first-run")
    opts.add_argument("--window-size=1920,1080")
    opts.add_argument("--ignore-certificate-errors")
    opts.page_load_strategy = "eager"  # Don't wait for all resources
    d = webdriver.Chrome(options=opts)
    d.set_page_load_timeout(30)
    d.set_script_timeout(15)
    return d

def login(driver, wait):
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    wait.until(EC.presence_of_element_located((By.NAME, "LoginViewModel.Username")))
    driver.find_element(By.NAME, "LoginViewModel.Username").send_keys(LOGIN_USER)
    driver.find_element(By.NAME, "LoginViewModel.Password").send_keys(LOGIN_PASS)
    # Use JS submit instead of click (more reliable in headless)
    driver.execute_script("document.querySelector('form').submit()")
    time.sleep(5)
    print(f"[OK] Login exitoso (URL: {driver.current_url})")

def test_page_loads(driver, wait):
    """Verifica que la pagina carga correctamente con datos."""
    driver.get(f"{BASE_URL}/Catalogos/OpcionesPago")
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))
    rows = driver.find_elements(By.CSS_SELECTOR, "#tablaOpciones tbody tr")
    assert len(rows) > 0, "No hay filas en la tabla de opciones de pago"
    print(f"[OK] Pagina carga correctamente con {len(rows)} opciones de pago")
    return len(rows)

def test_create(driver, wait):
    """Crea una opcion de pago de prueba."""
    driver.get(f"{BASE_URL}/Catalogos/OpcionesPago")
    wait.until(EC.presence_of_element_located((By.ID, "formOpcionPago")))

    nombre_input = driver.find_element(By.ID, "Nombre")
    nombre_input.clear()
    nombre_input.send_keys("TEST-Selenium-Pago")

    moneda_select = Select(driver.find_element(By.ID, "IdMoneda"))
    moneda_select.select_by_index(0)

    logo_select = Select(driver.find_element(By.ID, "Logo"))
    logo_select.select_by_value("3")  # Visa

    activa_check = driver.find_element(By.ID, "Activa")
    assert activa_check.is_selected(), "Checkbox Activa deberia estar checked por default"

    driver.execute_script("document.getElementById('formOpcionPago').submit()")
    time.sleep(3)
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))

    page_source = driver.page_source
    assert "TEST-Selenium-Pago" in page_source, "El registro creado no aparece en la tabla"
    print("[OK] Opcion de pago creada correctamente")

def find_test_row(driver, name="TEST-Selenium-Pago"):
    """Busca la fila del registro de prueba en la tabla."""
    rows = driver.find_elements(By.CSS_SELECTOR, "#tablaOpciones tbody tr")
    for row in rows:
        if name in row.text:
            return row
    return None

def test_edit(driver, wait):
    """Edita la opcion de pago de prueba."""
    driver.get(f"{BASE_URL}/Catalogos/OpcionesPago")
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))
    time.sleep(1)

    row = find_test_row(driver)
    assert row is not None, "No se encontro el registro de prueba para editar"

    edit_btn = row.find_element(By.CSS_SELECTOR, ".btn-outline-warning")
    driver.execute_script("arguments[0].click()", edit_btn)
    time.sleep(1)

    nombre_input = driver.find_element(By.ID, "Nombre")
    assert nombre_input.get_attribute("value") == "TEST-Selenium-Pago", \
        f"El nombre no se cargo: '{nombre_input.get_attribute('value')}'"

    nombre_input.clear()
    nombre_input.send_keys("TEST-Selenium-Editado")

    logo_select = Select(driver.find_element(By.ID, "Logo"))
    logo_select.select_by_value("1")  # Amex

    driver.execute_script("document.getElementById('formOpcionPago').submit()")
    time.sleep(3)
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))

    page_source = driver.page_source
    assert "TEST-Selenium-Editado" in page_source, "El registro editado no aparece"
    print("[OK] Opcion de pago editada correctamente")

def test_toggle_deactivate(driver, wait):
    """Desactiva la opcion de pago de prueba."""
    driver.get(f"{BASE_URL}/Catalogos/OpcionesPago")
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))
    time.sleep(1)

    row = find_test_row(driver, "TEST-Selenium-Editado")
    assert row is not None, "No se encontro el registro para desactivar"

    toggle_form = row.find_element(By.CSS_SELECTOR, "form[action*='ToggleActiva']")
    driver.execute_script("arguments[0].submit()", toggle_form)
    time.sleep(3)
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))

    row = find_test_row(driver, "TEST-Selenium-Editado")
    assert row is not None, "No se encontro registro despues de desactivar"
    assert "table-secondary" in row.get_attribute("class"), \
        "La fila deberia tener clase table-secondary (inactiva)"
    print("[OK] Opcion de pago desactivada correctamente")

def test_toggle_activate(driver, wait):
    """Reactiva la opcion de pago de prueba."""
    driver.get(f"{BASE_URL}/Catalogos/OpcionesPago")
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))
    time.sleep(1)

    row = find_test_row(driver, "TEST-Selenium-Editado")
    assert row is not None, "No se encontro el registro para activar"

    toggle_form = row.find_element(By.CSS_SELECTOR, "form[action*='ToggleActiva']")
    driver.execute_script("arguments[0].submit()", toggle_form)
    time.sleep(3)
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))

    row = find_test_row(driver, "TEST-Selenium-Editado")
    assert row is not None, "No se encontro registro despues de activar"
    assert "table-secondary" not in row.get_attribute("class"), \
        "La fila NO deberia tener clase table-secondary"
    print("[OK] Opcion de pago reactivada correctamente")

def test_delete(driver, wait):
    """Elimina la opcion de pago de prueba."""
    driver.get(f"{BASE_URL}/Catalogos/OpcionesPago")
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))
    time.sleep(1)

    row = find_test_row(driver, "TEST-Selenium-Editado")
    assert row is not None, "No se encontro el registro para eliminar"

    delete_form = row.find_element(By.CSS_SELECTOR, "form[action*='Eliminar']")
    driver.execute_script("arguments[0].submit()", delete_form)
    time.sleep(3)
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))

    page_source = driver.page_source
    assert "TEST-Selenium-Editado" not in page_source, "El registro eliminado sigue apareciendo"
    print("[OK] Opcion de pago eliminada correctamente")

def cleanup_test_data(driver, wait):
    """Limpia datos de prueba residuales."""
    driver.get(f"{BASE_URL}/Catalogos/OpcionesPago")
    wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))
    time.sleep(1)
    rows = driver.find_elements(By.CSS_SELECTOR, "#tablaOpciones tbody tr")
    for row in rows:
        if "TEST-Selenium" in row.text:
            delete_form = row.find_element(By.CSS_SELECTOR, "form[action*='Eliminar']")
            driver.execute_script("arguments[0].submit()", delete_form)
            time.sleep(3)
            driver.get(f"{BASE_URL}/Catalogos/OpcionesPago")
            wait.until(EC.presence_of_element_located((By.ID, "tablaOpciones")))
            time.sleep(1)

def main():
    driver = setup_driver()
    wait = WebDriverWait(driver, 20)
    passed = 0
    failed = 0
    tests = [
        ("page_loads", test_page_loads),
        ("create", test_create),
        ("edit", test_edit),
        ("toggle_deactivate", test_toggle_deactivate),
        ("toggle_activate", test_toggle_activate),
        ("delete", test_delete),
    ]

    try:
        login(driver, wait)
        try:
            cleanup_test_data(driver, wait)
        except Exception as e:
            print(f"[WARN] Cleanup inicial: {e}")

        for name, test_fn in tests:
            try:
                test_fn(driver, wait)
                passed += 1
            except Exception as e:
                print(f"[FAIL] {name}: {e}")
                traceback.print_exc()
                failed += 1
    except Exception as e:
        print(f"[FATAL] {e}")
        traceback.print_exc()
        failed += 1
    finally:
        try:
            cleanup_test_data(driver, wait)
        except:
            pass
        driver.quit()

    print(f"\n{'='*50}")
    print(f"Resultados: {passed} passed, {failed} failed")
    print(f"{'='*50}")
    sys.exit(0 if failed == 0 else 1)

if __name__ == "__main__":
    main()
