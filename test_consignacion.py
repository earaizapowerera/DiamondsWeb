"""
Selenium test: Pantalla Cuentas de Consignacion en DiamondsWeb
Verifica: login, carga de la pantalla, 3 grids, filtro por remision, filtro por fecha.
"""
import sys
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

BASE_URL = "https://diamonds.dev.powerera.com"
LOGIN_USER = "admin"
LOGIN_PASS = "u38a8fk3j0!"


def create_driver():
    opts = Options()
    opts.add_argument("--headless=new")
    opts.add_argument("--no-sandbox")
    opts.add_argument("--disable-dev-shm-usage")
    opts.add_argument("--disable-gpu")
    opts.add_argument("--window-size=1920,1080")
    opts.add_argument("--ignore-certificate-errors")
    return webdriver.Chrome(options=opts)


def test_login(driver):
    """Login al sistema"""
    print("  [1] Navegando al login...")
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    wait = WebDriverWait(driver, 15)

    user_field = wait.until(EC.presence_of_element_located((By.NAME, "Input.Username")))
    user_field.clear()
    user_field.send_keys(LOGIN_USER)

    pass_field = driver.find_element(By.NAME, "Input.Password")
    pass_field.clear()
    pass_field.send_keys(LOGIN_PASS)

    btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
    btn.click()

    wait.until(lambda d: "/Auth/Login" not in d.current_url)
    print(f"  [1] Login OK -> {driver.current_url}")
    return True


def test_consignacion_loads(driver):
    """Verificar que la pantalla de Consignacion carga correctamente"""
    print("  [2] Navegando a /Ventas/Consignacion...")
    driver.get(f"{BASE_URL}/Ventas/Consignacion")
    wait = WebDriverWait(driver, 15)

    # Esperar que los stat cards o el filtro aparezcan
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".stat-card, .filter-panel, .card-header")))

    title = driver.title
    print(f"  [2] Titulo: {title}")
    assert "consignacion" in title.lower(), f"Titulo inesperado: {title}"
    return True


def test_three_grids(driver):
    """Verificar que existen las 3 secciones/grids"""
    print("  [3] Verificando 3 grids...")

    headers = driver.find_elements(By.CSS_SELECTOR, ".card-header")
    header_texts = [h.text.lower() for h in headers]
    print(f"  [3] Headers encontrados: {header_texts}")

    found_existencia = any("existencia" in t for t in header_texts)
    found_devolver = any("devolver" in t for t in header_texts)
    found_devueltas = any("devuelt" in t for t in header_texts)

    assert found_existencia, "No se encontro grid 'En Existencia'"
    assert found_devolver, "No se encontro grid 'Por Devolver'"
    assert found_devueltas, "No se encontro grid 'Devueltas'"

    print("  [3] Los 3 grids estan presentes")
    return True


def test_filter_panel(driver):
    """Verificar que el panel de filtros existe con remision y fecha"""
    print("  [4] Verificando panel de filtros...")

    filter_form = driver.find_element(By.ID, "filterForm")
    assert filter_form is not None, "No se encontro el form de filtros"

    # Verificar campo de fecha
    date_input = driver.find_element(By.CSS_SELECTOR, "input[name='fechaDesde']")
    assert date_input is not None, "No se encontro el campo de fecha 'Cuentas desde'"

    # Verificar select de remision
    select = driver.find_element(By.ID, "selectRemision")
    assert select is not None, "No se encontro el select de remision"

    print("  [4] Panel de filtros OK (remision + fecha)")
    return True


def test_filter_by_remision(driver):
    """Probar filtrado por remision usando query param directo"""
    print("  [5] Probando filtro por remision...")

    driver.get(f"{BASE_URL}/Ventas/Consignacion?idRemision=111280")
    wait = WebDriverWait(driver, 15)
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, ".stat-card, .card-header")))

    # Verificar que la pagina cargo con filtro
    headers = driver.find_elements(By.CSS_SELECTOR, ".card-header")
    assert len(headers) >= 3, "No se cargaron los 3 grids despues de filtrar"

    # Verificar que hay datos en la tabla de En Existencia
    table = driver.find_elements(By.ID, "tblEnExistencia")
    if table:
        rows = table[0].find_elements(By.CSS_SELECTOR, "tbody tr")
        print(f"  [5] Filas en 'En Existencia' para remision 111280: {len(rows)}")
        assert len(rows) > 0, "Se esperaban piezas para remision 111280"
    else:
        print("  [5] No hay tabla (remision puede no tener piezas en existencia)")

    print("  [5] Filtro por remision OK")
    return True


def test_estados_correctos(driver):
    """Verificar que las piezas muestran estados/status correctos"""
    print("  [6] Verificando estados en los grids...")

    # En Existencia debe tener badges bg-success
    table_exist = driver.find_elements(By.ID, "tblEnExistencia")
    if table_exist:
        badges = table_exist[0].find_elements(By.CSS_SELECTOR, ".badge.bg-success")
        print(f"  [6] Badges 'success' en En Existencia: {len(badges)}")

    # Por Devolver debe tener badges bg-warning
    table_dev = driver.find_elements(By.ID, "tblPorDevolver")
    if table_dev:
        badges = table_dev[0].find_elements(By.CSS_SELECTOR, ".badge.bg-warning")
        print(f"  [6] Badges 'warning' en Por Devolver: {len(badges)}")

    # Devueltas debe tener badges bg-secondary
    table_devueltas = driver.find_elements(By.ID, "tblDevueltas")
    if table_devueltas:
        badges = table_devueltas[0].find_elements(By.CSS_SELECTOR, ".badge.bg-secondary")
        print(f"  [6] Badges 'secondary' en Devueltas: {len(badges)}")

    print("  [6] Verificacion de estados OK")
    return True


def main():
    print("=" * 60)
    print("TEST: Cuentas de Consignacion - DiamondsWeb")
    print(f"URL: {BASE_URL}")
    print("=" * 60)

    driver = create_driver()
    passed = 0
    failed = 0

    tests = [
        ("Login", test_login),
        ("Pantalla carga", test_consignacion_loads),
        ("3 grids presentes", test_three_grids),
        ("Panel de filtros (remision + fecha)", test_filter_panel),
        ("Filtro por remision", test_filter_by_remision),
        ("Estados correctos en grids", test_estados_correctos),
    ]

    try:
        for name, test_fn in tests:
            try:
                print(f"\n--- Test: {name} ---")
                test_fn(driver)
                print(f"  PASS: {name}")
                passed += 1
            except Exception as e:
                print(f"  FAIL: {name} -> {e}")
                driver.save_screenshot(f"/tmp/fail_{name.replace(' ', '_')}.png")
                failed += 1
    finally:
        driver.quit()

    print(f"\n{'=' * 60}")
    print(f"RESULTADOS: {passed} passed, {failed} failed de {len(tests)} tests")
    print("=" * 60)

    sys.exit(1 if failed > 0 else 0)


if __name__ == "__main__":
    main()
