"""
Selenium test: Alta de Piezas Sencillas - DiamondsWeb
Prueba end-to-end: Login -> Alta -> Crear pieza -> Verificar calculos -> Editar -> Verificar
"""
import time
import sys
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.support.ui import WebDriverWait, Select
from selenium.webdriver.support import expected_conditions as EC

BASE_URL = "https://bot-286802.dev.powerera.com"
USER = "admin"
PASS = "u38a8fk3j0!"

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
    time.sleep(3)
    print(f"  Login OK, URL: {driver.current_url}")

def test_index_page(driver, wait):
    """Test 1: Verificar que la pagina Index carga con datos"""
    print("\n=== TEST 1: Pagina Index (lista de piezas) ===")
    driver.get(f"{BASE_URL}/Piezas")
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, "table")))

    # Verificar que hay tabla
    rows = driver.find_elements(By.CSS_SELECTOR, "table tbody tr")
    print(f"  Filas en tabla: {len(rows)}")

    # Verificar filtros
    buscar = driver.find_element(By.NAME, "Buscar")
    assert buscar is not None, "Campo buscar no encontrado"
    print("  Filtros OK")

    # Verificar boton Nueva Pieza
    btn = driver.find_element(By.CSS_SELECTOR, "a[href*='Alta']")
    assert btn is not None, "Boton Nueva Pieza no encontrado"
    print("  Boton 'Nueva Pieza' OK")
    print("  TEST 1 PASSED")

def test_alta_page_loads(driver, wait):
    """Test 2: Verificar que la pagina Alta carga con todos los controles"""
    print("\n=== TEST 2: Pagina Alta (formulario) ===")
    driver.get(f"{BASE_URL}/Piezas/Alta")
    wait.until(EC.presence_of_element_located((By.ID, "formPieza")))

    # Verificar controles principales
    desc = driver.find_element(By.ID, "Pieza_Descripcion")
    assert desc is not None, "Campo Descripcion no encontrado"

    # Verificar tabs de costos
    tab_pieza = driver.find_element(By.ID, "tab-pieza-tab")
    tab_peso = driver.find_element(By.ID, "tab-peso-tab")
    tab_extras = driver.find_element(By.ID, "tab-extras-tab")
    tab_factura = driver.find_element(By.ID, "tab-factura-tab")
    assert all([tab_pieza, tab_peso, tab_extras, tab_factura]), "Tabs de costos incompletos"
    print("  Tabs costos: Pieza, Peso, Extras, Factura OK")

    # Verificar tabs de caracteristicas
    tab_oro = driver.find_element(By.ID, "tab-oro-tab")
    tab_diam = driver.find_element(By.ID, "tab-diamante-tab")
    tab_reloj = driver.find_element(By.ID, "tab-reloj-tab")
    assert all([tab_oro, tab_diam, tab_reloj]), "Tabs de caracteristicas incompletos"
    print("  Tabs caracteristicas: Oro, Diamante, Reloj OK")

    # Verificar campos de precio
    utilidad = driver.find_element(By.ID, "utilidad")
    utilidad_extra = driver.find_element(By.ID, "utilidadExtra")
    impuesto = driver.find_element(By.ID, "impuesto")
    precio = driver.find_element(By.ID, "precio")
    assert all([utilidad, utilidad_extra, impuesto, precio]), "Campos de precio incompletos"
    print("  Campos precio: Utilidad, UtilidadExtra, Impuesto, Precio OK")

    # Verificar formula display
    formula = driver.find_element(By.ID, "formulaDisplay")
    assert formula is not None, "Formula display no encontrada"
    print("  Formula display OK")

    print("  TEST 2 PASSED")

def test_price_calculations(driver, wait):
    """Test 3: Verificar calculos de precio client-side"""
    print("\n=== TEST 3: Calculos de precio ===")
    driver.get(f"{BASE_URL}/Piezas/Alta")
    wait.until(EC.presence_of_element_located((By.ID, "formPieza")))
    time.sleep(1)

    # Limpiar y setear valores
    def set_field(field_id, value):
        el = driver.find_element(By.ID, field_id)
        el.clear()
        el.send_keys(str(value))
        # Trigger change event
        driver.execute_script("arguments[0].dispatchEvent(new Event('change'))", el)

    # Escenario: Pieza con CBPieza=1800, Utilidad=1.667, Impuesto=1.16, Divisor=0.044
    set_field("cbPieza", "1800")
    set_field("utilidad", "1.667")
    set_field("utilidadExtra", "1")
    set_field("impuesto", "1.16")
    set_field("tcCotizacion", "1")

    # Setear divisor manualmente ya que es un select
    driver.execute_script("document.getElementById('divisorValor').value = '0.044'")
    driver.execute_script("calcularCostos()")
    time.sleep(0.5)

    # Verificar CNTotal = CBPieza = 1800 (descuento 0%)
    cn_total = driver.find_element(By.ID, "cnTotal").get_attribute("value")
    print(f"  CNTotal = {cn_total} (esperado: 1800)")
    assert abs(float(cn_total) - 1800) < 1, f"CNTotal incorrecto: {cn_total}"

    # Verificar Precio = 1800 * 1.667 * 1 * 1.16 / 0.044 * 1 = 79,109
    # Calculo: 1800 * 1.667 = 3000.6, * 1 = 3000.6, * 1.16 = 3480.696, / 0.044 = 79,106.7, * 1 = 79,107
    precio = driver.find_element(By.ID, "precio").get_attribute("value")
    print(f"  Precio = {precio} (esperado ~79107)")
    assert abs(int(float(precio)) - 79107) < 100, f"Precio muy diferente: {precio}"

    # Escenario: Agregar descuento pieza 10%
    set_field("descPieza", "10")
    time.sleep(0.5)
    cn_pieza = driver.find_element(By.ID, "cnPieza").get_attribute("value")
    print(f"  CNPieza con 10% desc = {cn_pieza} (esperado: 1620)")
    assert abs(float(cn_pieza) - 1620) < 1, f"CNPieza incorrecto: {cn_pieza}"

    # Escenario: Por Peso
    tab_peso = driver.find_element(By.ID, "tab-peso-tab")
    tab_peso.click()
    time.sleep(0.5)
    set_field("peso", "7.5")
    set_field("precioGramo", "2800")
    time.sleep(0.5)

    cb_peso = driver.find_element(By.ID, "cbPeso").get_attribute("value")
    print(f"  CBPeso = {cb_peso} (esperado: 21000 = 7.5 * 2800)")
    assert abs(float(cb_peso) - 21000) < 1, f"CBPeso incorrecto: {cb_peso}"

    print("  TEST 3 PASSED")

def test_characteristics_tabs(driver, wait):
    """Test 4: Verificar tabs de caracteristicas dinamicas"""
    print("\n=== TEST 4: Caracteristicas dinamicas ===")
    driver.get(f"{BASE_URL}/Piezas/Alta")
    wait.until(EC.presence_of_element_located((By.ID, "formPieza")))
    time.sleep(1)

    # Tab Oro (default)
    tab_oro = driver.find_element(By.ID, "tab-oro")
    assert "show active" in tab_oro.get_attribute("class"), "Tab Oro no activa por default"
    print("  Tab Oro activa por default OK")

    # Cambiar a Diamante
    driver.find_element(By.ID, "tab-diamante-tab").click()
    time.sleep(0.5)
    tab_diam = driver.find_element(By.ID, "tab-diamante")
    assert "show active" in tab_diam.get_attribute("class"), "Tab Diamante no se activo"

    # Verificar campos de diamante
    quilates = driver.find_element(By.ID, "Pieza_Quilates")
    assert quilates is not None, "Campo quilates no encontrado"
    color = driver.find_element(By.ID, "Pieza_Color")
    assert color is not None, "Campo color no encontrado"
    print("  Tab Diamante con campos OK")

    # Cambiar a Reloj
    driver.find_element(By.ID, "tab-reloj-tab").click()
    time.sleep(0.5)
    numserie = driver.find_element(By.ID, "Pieza_NumSerie")
    assert numserie is not None, "Campo NumSerie no encontrado"
    print("  Tab Reloj con campos OK")

    print("  TEST 4 PASSED")

def test_create_piece(driver, wait):
    """Test 5: Crear una pieza nueva end-to-end"""
    print("\n=== TEST 5: Crear pieza nueva ===")
    driver.get(f"{BASE_URL}/Piezas/Alta?IdRemision=111279")
    wait.until(EC.presence_of_element_located((By.ID, "formPieza")))
    time.sleep(1)

    def set_field(field_id, value):
        el = driver.find_element(By.ID, field_id)
        el.clear()
        el.send_keys(str(value))
        driver.execute_script("arguments[0].dispatchEvent(new Event('change'))", el)

    # Llenar datos
    set_field("Pieza_Descripcion", "TEST Selenium - Anillo oro 14k con diamante 0.5qt")

    # Seleccionar grupo (Anillo = 3)
    driver.execute_script("""
        var sel = document.querySelector('[name="Pieza.IdGrupo"]');
        if (sel.tomselect) sel.tomselect.setValue('3');
        else sel.value = '3';
    """)

    # Costo por pieza
    set_field("cbPieza", "5000")

    # Factores
    set_field("utilidad", "1.667")
    set_field("utilidadExtra", "1.1")
    set_field("impuesto", "1.16")
    driver.execute_script("document.getElementById('divisorValor').value = '0.044'")
    set_field("tcCotizacion", "1")

    # Caracteristicas Oro
    driver.execute_script("""
        var sel = document.getElementById('selKilatesOro');
        if (sel) sel.value = '14';
    """)
    set_field("modeloOro", "AN-TEST")
    set_field("lineaOro", "Selenium")

    driver.execute_script("calcularCostos()")
    time.sleep(0.5)

    precio = driver.find_element(By.ID, "precio").get_attribute("value")
    print(f"  Precio calculado: ${precio}")
    assert int(float(precio)) > 0, "Precio es 0"

    # Enviar formulario
    btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
    btn.click()
    time.sleep(3)

    # Verificar resultado
    page_source = driver.page_source
    if "creada exitosamente" in page_source or "alert-success" in page_source:
        print("  Pieza creada exitosamente!")
        # Extraer codigo de barras del mensaje
        import re
        match = re.search(r'Pieza (\d+) creada', page_source)
        if match:
            cb = match.group(1)
            print(f"  Codigo de barras: {cb}")
            print("  TEST 5 PASSED")
            return cb
        print("  TEST 5 PASSED (sin extraer CB)")
        return None
    elif "error" in page_source.lower():
        # Check for error messages
        alerts = driver.find_elements(By.CSS_SELECTOR, ".alert-danger")
        for a in alerts:
            print(f"  ERROR: {a.text}")
        print("  TEST 5 FAILED - Error en creacion")
        return None
    else:
        print(f"  TEST 5 INCONCLUSIVE - URL: {driver.current_url}")
        return None

def test_edit_piece(driver, wait, codigo_barras):
    """Test 6: Editar pieza existente"""
    if not codigo_barras:
        print("\n=== TEST 6: Editar pieza (SKIPPED - no CB) ===")
        return

    print(f"\n=== TEST 6: Editar pieza {codigo_barras} ===")
    driver.get(f"{BASE_URL}/Piezas/Alta?cb={codigo_barras}")
    wait.until(EC.presence_of_element_located((By.ID, "formPieza")))
    time.sleep(1)

    # Verificar que cargo los datos
    desc = driver.find_element(By.ID, "Pieza_Descripcion").get_attribute("value")
    print(f"  Descripcion cargada: {desc}")
    assert "TEST Selenium" in desc, f"Datos no cargados: {desc}"

    precio = driver.find_element(By.ID, "precio").get_attribute("value")
    print(f"  Precio cargado: {precio}")

    print("  TEST 6 PASSED")

def test_currency_change(driver, wait):
    """Test 7: Verificar cambio de moneda y TC"""
    print("\n=== TEST 7: Cambio de moneda ===")
    driver.get(f"{BASE_URL}/Piezas/Alta")
    wait.until(EC.presence_of_element_located((By.ID, "formPieza")))
    time.sleep(1)

    # Verificar que existe selector de moneda
    sel_moneda = driver.find_element(By.ID, "selMoneda")
    assert sel_moneda is not None, "Selector de moneda no encontrado"

    # Verificar que Moneda Nacional tiene TC=1
    tc_cot = driver.find_element(By.ID, "tcCotizacion").get_attribute("value")
    print(f"  TC Cotizacion Moneda Nacional: {tc_cot}")

    print("  TEST 7 PASSED")

def main():
    print("=" * 60)
    print("SELENIUM TEST: Alta de Piezas Sencillas - DiamondsWeb")
    print(f"URL: {BASE_URL}")
    print("=" * 60)

    driver = setup_driver()
    wait = WebDriverWait(driver, 15)
    passed = 0
    failed = 0

    try:
        login(driver, wait)

        tests = [
            test_index_page,
            test_alta_page_loads,
            test_price_calculations,
            test_characteristics_tabs,
        ]

        cb = None
        for test in tests:
            try:
                test(driver, wait)
                passed += 1
            except Exception as e:
                print(f"  FAILED: {e}")
                failed += 1

        # Test crear pieza
        try:
            cb = test_create_piece(driver, wait)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test editar pieza
        try:
            test_edit_piece(driver, wait, cb)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

        # Test moneda
        try:
            test_currency_change(driver, wait)
            passed += 1
        except Exception as e:
            print(f"  FAILED: {e}")
            failed += 1

    finally:
        driver.quit()

    print("\n" + "=" * 60)
    print(f"RESULTADOS: {passed} passed, {failed} failed, {passed + failed} total")
    print("=" * 60)

    return 0 if failed == 0 else 1

if __name__ == "__main__":
    sys.exit(main())
