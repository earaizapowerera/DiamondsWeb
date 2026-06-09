"""
Selenium test — CRUD master-detail: TablasJerarquias → Jerarquias.
Ticket #286864: Migrar frmTablasJerarquias.frm a Razor Page.

Tests:
  1. Login
  2. Pagina carga con lista master
  3. Crear tabla (master)
  4. Seleccionar tabla y ver panel detail
  5. Agregar columna (detail)
  6. Editar columna (detail)
  7. Eliminar columna (detail)
  8. Editar tabla (master)
  9. Eliminar tabla (master, cascade)
"""
import sys
import time
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service

BASE_URL = "http://localhost:5350"
LOGIN_USER = "admin"
LOGIN_PASS = "u38a8fk3j0!"
TEST_TABLA = f"SeleniumTest_{int(time.time())}"
TEST_TABLA_EDIT = f"SeleniumEdit_{int(time.time())}"
CHROMEDRIVER = "/home/earaiza/.cache/selenium/chromedriver/linux64/145.0.7632.117/chromedriver"


def create_driver():
    opts = Options()
    opts.add_argument("--headless=new")
    opts.add_argument("--no-sandbox")
    opts.add_argument("--disable-dev-shm-usage")
    opts.add_argument("--disable-gpu")
    opts.add_argument("--window-size=1920,1080")
    opts.binary_location = "/usr/bin/google-chrome"
    svc = Service(CHROMEDRIVER)
    return webdriver.Chrome(service=svc, options=opts)


def login(driver, wait):
    """Login via UserPortal auth."""
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    wait.until(EC.presence_of_element_located((By.NAME, "LoginViewModel.Username")))
    driver.find_element(By.NAME, "LoginViewModel.Username").send_keys(LOGIN_USER)
    driver.find_element(By.NAME, "LoginViewModel.Password").send_keys(LOGIN_PASS)
    btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
    driver.execute_script("arguments[0].click();", btn)
    time.sleep(3)
    print(f"  Login OK — URL: {driver.current_url}")


def test_page_loads(driver, wait):
    """Test 1: La pagina de Jerarquias carga correctamente."""
    driver.get(f"{BASE_URL}/Jerarquias")
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".master-card")))
    header = driver.find_element(By.CSS_SELECTOR, ".info-panel h5").text
    assert "Jerarquias" in header, f"Expected 'Jerarquias' in header, got: {header}"
    print("  PASS: Pagina carga con panel master")


def test_create_tabla(driver, wait):
    """Test 2: Crear una tabla de jerarquia nueva (master)."""
    driver.get(f"{BASE_URL}/Jerarquias")
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".master-card")))

    # Abrir modal Nueva Tabla
    btn_nueva = driver.find_element(By.CSS_SELECTOR, "[data-bs-target='#modalNuevaTabla']")
    driver.execute_script("arguments[0].click();", btn_nueva)
    time.sleep(0.5)

    # Llenar y enviar form
    wait.until(EC.visibility_of_element_located((By.CSS_SELECTOR, "#modalNuevaTabla input[name='descripcion']")))
    inp = driver.find_element(By.CSS_SELECTOR, "#modalNuevaTabla input[name='descripcion']")
    inp.clear()
    inp.send_keys(TEST_TABLA)

    submit = driver.find_element(By.CSS_SELECTOR, "#modalNuevaTabla button[type='submit']")
    driver.execute_script("arguments[0].click();", submit)

    # Verificar success
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success")))
    alert = driver.find_element(By.CSS_SELECTOR, ".alert-success").text
    assert "creada" in alert.lower(), f"Expected 'creada' in alert, got: {alert}"

    # Verificar en lista master
    master_text = driver.find_element(By.CSS_SELECTOR, ".master-card").text
    assert TEST_TABLA in master_text, f"Tabla '{TEST_TABLA}' no encontrada en panel master"
    print(f"  PASS: Tabla '{TEST_TABLA}' creada exitosamente")


def test_select_tabla_shows_detail(driver, wait):
    """Test 3: Al seleccionar la tabla se muestra el panel de detalle."""
    driver.get(f"{BASE_URL}/Jerarquias")
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".master-card")))

    # Encontrar y clickear nuestra tabla via JS (evita interceptaciones del layout)
    items = driver.find_elements(By.CSS_SELECTOR, ".master-card .list-group-item")
    target = None
    for item in items:
        if TEST_TABLA in item.text:
            target = item
            break

    assert target is not None, f"No se encontro '{TEST_TABLA}' en lista master"
    driver.execute_script("arguments[0].click();", target)

    # Verificar que el panel de detalle aparece con el nombre correcto
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".detail-card")))
    detail_header = driver.find_element(By.CSS_SELECTOR, ".detail-card .card-header").text
    assert TEST_TABLA in detail_header, f"Expected '{TEST_TABLA}' in detail header, got: {detail_header}"
    print(f"  PASS: Panel detail muestra tabla '{TEST_TABLA}'")


def test_create_columna(driver, wait):
    """Test 4: Agregar una columna (detail) a la tabla seleccionada."""
    # Ya estamos en la pagina con la tabla seleccionada
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".detail-card")))

    # Seleccionar columna "Diam" del dropdown
    select = driver.find_element(By.CSS_SELECTOR, ".detail-card select[name='columna']")
    driver.execute_script("arguments[0].value = 'Diam';", select)

    # Click Agregar (btn-success dentro del form de agregar columna)
    btn = driver.find_element(By.CSS_SELECTOR, ".detail-card .btn-success")
    driver.execute_script("arguments[0].click();", btn)

    # Verificar success
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success")))
    alert = driver.find_element(By.CSS_SELECTOR, ".alert-success").text
    assert "agregada" in alert.lower(), f"Expected 'agregada' in alert, got: {alert}"

    # Verificar que aparece en la tabla de detalle
    detail_table = driver.find_element(By.CSS_SELECTOR, ".detail-table").text
    assert "Diam" in detail_table, "Columna 'Diam' no aparece en tabla de detalle"
    print("  PASS: Columna 'Diam' agregada exitosamente")

    # Agregar segunda columna (Mod) para mas tests
    select = driver.find_element(By.CSS_SELECTOR, ".detail-card select[name='columna']")
    driver.execute_script("arguments[0].value = 'Mod';", select)
    btn = driver.find_element(By.CSS_SELECTOR, ".detail-card .btn-success")
    driver.execute_script("arguments[0].click();", btn)
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success")))
    print("  PASS: Columna 'Mod' agregada como segunda columna")


def test_edit_columna(driver, wait):
    """Test 5: Editar una columna existente."""
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".detail-table")))

    # Encontrar boton editar de la primera fila
    edit_btn = driver.find_element(By.CSS_SELECTOR,
        ".detail-table tbody tr:first-child button[title='Editar']")
    driver.execute_script("arguments[0].click();", edit_btn)
    time.sleep(0.5)

    # En el modal, cambiar a "Linea"
    wait.until(EC.visibility_of_element_located((By.ID, "editColSelect")))
    sel = driver.find_element(By.ID, "editColSelect")
    driver.execute_script("arguments[0].value = 'Linea';", sel)

    submit = driver.find_element(By.CSS_SELECTOR, "#modalEditarCol button[type='submit']")
    driver.execute_script("arguments[0].click();", submit)

    # Verificar success
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success")))
    alert = driver.find_element(By.CSS_SELECTOR, ".alert-success").text
    assert "actualizada" in alert.lower(), f"Expected 'actualizada' in alert, got: {alert}"

    # Verificar que ahora dice "Linea"
    detail_table = driver.find_element(By.CSS_SELECTOR, ".detail-table").text
    assert "Linea" in detail_table, "Columna 'Linea' no aparece despues de editar"
    print("  PASS: Columna editada a 'Linea' exitosamente")


def test_delete_columna(driver, wait):
    """Test 6: Eliminar una columna."""
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".detail-table")))

    # Contar filas antes
    rows_before = len(driver.find_elements(By.CSS_SELECTOR, ".detail-table tbody tr"))

    # Override confirm dialog
    driver.execute_script("window.confirm = function() { return true; }")

    # Click eliminar en la segunda fila (Mod)
    delete_btns = driver.find_elements(By.CSS_SELECTOR,
        ".detail-table tbody tr:last-child form button[title='Eliminar']")
    assert len(delete_btns) > 0, "No se encontro boton eliminar en la ultima fila"
    driver.execute_script("arguments[0].click();", delete_btns[0])

    # Verificar success
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success")))
    alert = driver.find_element(By.CSS_SELECTOR, ".alert-success").text
    assert "eliminada" in alert.lower(), f"Expected 'eliminada' in alert, got: {alert}"

    # Verificar que hay una fila menos
    rows_after = len(driver.find_elements(By.CSS_SELECTOR, ".detail-table tbody tr"))
    assert rows_after == rows_before - 1, \
        f"Expected {rows_before - 1} rows after delete, got {rows_after}"
    print(f"  PASS: Columna eliminada ({rows_before} -> {rows_after} filas)")


def test_edit_tabla(driver, wait):
    """Test 7: Editar nombre de la tabla (master)."""
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".detail-card")))

    # Click boton editar tabla
    edit_btn = driver.find_element(By.CSS_SELECTOR,
        ".detail-card .card-header button[data-bs-target='#modalEditarTabla']")
    driver.execute_script("arguments[0].click();", edit_btn)
    time.sleep(0.5)

    # Cambiar nombre
    wait.until(EC.visibility_of_element_located(
        (By.CSS_SELECTOR, "#modalEditarTabla input[name='descripcion']")))
    inp = driver.find_element(By.CSS_SELECTOR, "#modalEditarTabla input[name='descripcion']")
    inp.clear()
    inp.send_keys(TEST_TABLA_EDIT)

    submit = driver.find_element(By.CSS_SELECTOR, "#modalEditarTabla button[type='submit']")
    driver.execute_script("arguments[0].click();", submit)

    # Verificar success
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success")))
    alert = driver.find_element(By.CSS_SELECTOR, ".alert-success").text
    assert "actualizada" in alert.lower(), f"Expected 'actualizada' in alert, got: {alert}"

    # Verificar nuevo nombre en master
    master_text = driver.find_element(By.CSS_SELECTOR, ".master-card").text
    assert TEST_TABLA_EDIT in master_text, f"Tabla editada '{TEST_TABLA_EDIT}' no en panel master"
    print(f"  PASS: Tabla renombrada a '{TEST_TABLA_EDIT}'")


def test_delete_tabla(driver, wait):
    """Test 8: Eliminar tabla (cascade elimina columnas hijas)."""
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".detail-card")))

    # Override confirm
    driver.execute_script("window.confirm = function() { return true; }")

    # Click eliminar tabla
    delete_btn = driver.find_element(By.CSS_SELECTOR,
        ".detail-card .card-header form button[title='Eliminar tabla']")
    driver.execute_script("arguments[0].click();", delete_btn)

    # Verificar success
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".alert-success")))
    alert = driver.find_element(By.CSS_SELECTOR, ".alert-success").text
    assert "eliminad" in alert.lower(), f"Expected 'eliminad' in alert, got: {alert}"

    # Verificar que ya no aparece en lista
    page_text = driver.page_source
    assert TEST_TABLA_EDIT not in page_text, \
        f"Tabla '{TEST_TABLA_EDIT}' aun aparece despues de eliminar"
    print(f"  PASS: Tabla '{TEST_TABLA_EDIT}' y columnas eliminadas (cascade)")


def test_buscar_filter(driver, wait):
    """Test 9: El filtro de busqueda funciona."""
    driver.get(f"{BASE_URL}/Jerarquias?Buscar=Normal")
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".master-card")))

    items = driver.find_elements(By.CSS_SELECTOR, ".master-card .list-group-item")
    # Verificar que al menos aparece "Normal" si existe en los datos
    if len(items) > 0:
        found = any("Normal" in item.text for item in items)
        assert found, "Filtro 'Normal' no mostro la tabla 'Normal'"
        print("  PASS: Filtro de busqueda funciona correctamente")
    else:
        print("  SKIP: No hay tablas que coincidan con 'Normal' (aceptable en BD limpia)")


def main():
    driver = None
    try:
        print("=== Selenium CRUD Test: Tablas de Jerarquias (Master-Detail) ===")
        driver = create_driver()
        wait = WebDriverWait(driver, 15)

        print("\n[1/10] Login...")
        login(driver, wait)

        print("\n[2/10] Test: Pagina carga correctamente...")
        test_page_loads(driver, wait)

        print("\n[3/10] Test: Crear tabla (master)...")
        test_create_tabla(driver, wait)

        print("\n[4/10] Test: Seleccionar tabla muestra detail...")
        test_select_tabla_shows_detail(driver, wait)

        print("\n[5/10] Test: Agregar columnas (detail)...")
        test_create_columna(driver, wait)

        print("\n[6/10] Test: Editar columna (detail)...")
        test_edit_columna(driver, wait)

        print("\n[7/10] Test: Eliminar columna (detail)...")
        test_delete_columna(driver, wait)

        print("\n[8/10] Test: Editar tabla (master)...")
        test_edit_tabla(driver, wait)

        print("\n[9/10] Test: Eliminar tabla con cascade (master)...")
        test_delete_tabla(driver, wait)

        print("\n[10/10] Test: Filtro de busqueda...")
        test_buscar_filter(driver, wait)

        print("\n=== TODOS LOS TESTS PASARON ===")
        return 0

    except Exception as e:
        print(f"\n*** TEST FALLO: {e}")
        if driver:
            driver.save_screenshot("/tmp/selenium-jerarquias-fail.png")
            print(f"  Screenshot: /tmp/selenium-jerarquias-fail.png")
            print(f"  Current URL: {driver.current_url}")
            # Print page source for debugging
            src = driver.page_source
            if "error" in src.lower() or "exception" in src.lower():
                print(f"  Page source (first 1000 chars): {src[:1000]}")
        return 1
    finally:
        if driver:
            driver.quit()


if __name__ == "__main__":
    sys.exit(main())
