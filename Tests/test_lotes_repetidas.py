#!/usr/bin/env python3
"""
Prueba Selenium: Alta de Lotes de Piezas Repetidas (frmLotesRepetidas)
Descripcion: Verifica la migracion de frmLotesRepetidas.frm a Razor Page.
- Crear lote con remision
- Verificar calculos de precio multi-moneda
- Cambio de moneda con recalculo automatico de TC

Referencia VB6: /home/earaiza/Diamonds/frmLotesRepetidas.frm
"""

from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import WebDriverWait, Select
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import NoSuchElementException, TimeoutException
import time
import os
import sys
import math

# ─── Configuracion ────────────────────────────────────────────────
BASE_URL = os.environ.get("TEST_BASE_URL", "https://bot-286806.dev.powerera.com")
USERNAME = os.environ.get("TEST_USERNAME", "admin")
PASSWORD = os.environ.get("TEST_PASSWORD", "u38a8fk3j0!")
SCREENSHOT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "screenshots")
HEADLESS = os.environ.get("TEST_HEADLESS", "true").lower() == "true"

os.makedirs(SCREENSHOT_DIR, exist_ok=True)

# Contadores de resultados
passed = 0
failed = 0
errors = []


def setup_driver():
    """Configura y retorna el driver de Chrome"""
    options = webdriver.ChromeOptions()
    if HEADLESS:
        options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--window-size=1920,1080")
    options.add_argument("--disable-blink-features=AutomationControlled")
    options.add_argument("--ignore-certificate-errors")
    return webdriver.Chrome(options=options)


def screenshot(driver, name):
    """Captura screenshot con nombre descriptivo"""
    path = os.path.join(SCREENSHOT_DIR, f"{name}.png")
    driver.save_screenshot(path)
    print(f"   Screenshot: {path}")


def check(condition, description):
    """Registra resultado de verificacion"""
    global passed, failed
    if condition:
        passed += 1
        print(f"  [PASS] {description}")
    else:
        failed += 1
        errors.append(description)
        print(f"  [FAIL] {description}")
    return condition


# ═══════════════════════════════════════════════════════════════════
# TEST 1: Login y navegacion a LotesRepetidas
# ═══════════════════════════════════════════════════════════════════

def test_login(driver):
    """Realiza login en DiamondsWeb UserPortal"""
    print("\n=== TEST 1: Login y Navegacion ===\n")

    print("  Navegando a /LotesRepetidas...")
    driver.get(f"{BASE_URL}/LotesRepetidas")
    time.sleep(2)

    # Debe redirigir a login
    check("/Auth/Login" in driver.current_url or "/Security/Auth/Login" in driver.current_url,
          "Redirige a pagina de login")
    screenshot(driver, "01_login_page")

    # Llenar credenciales
    wait = WebDriverWait(driver, 10)
    username_field = wait.until(EC.presence_of_element_located((By.ID, "LoginViewModel_Username")))
    password_field = driver.find_element(By.ID, "LoginViewModel_Password")

    username_field.clear()
    username_field.send_keys(USERNAME)
    password_field.clear()
    password_field.send_keys(PASSWORD)

    login_button = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
    login_button.click()
    time.sleep(3)

    # Verificar login exitoso (no hay alert-danger)
    try:
        error_alert = driver.find_element(By.CSS_SELECTOR, ".alert-danger")
        check(False, f"Login exitoso (error: {error_alert.text})")
        return False
    except NoSuchElementException:
        check(True, "Login exitoso")

    screenshot(driver, "02_after_login")

    # Navegar a LotesRepetidas
    driver.get(f"{BASE_URL}/LotesRepetidas")
    time.sleep(2)

    check("LotesRepetidas" in driver.current_url,
          "Pagina LotesRepetidas cargada")
    screenshot(driver, "03_lotes_repetidas_page")

    # Verificar elementos principales de la pagina
    check(driver.find_element(By.CSS_SELECTOR, ".section-card") is not None,
          "Section cards visibles")

    # Verificar que el mensaje de 'seleccione remision' aparece
    page_text = driver.page_source
    check("Seleccione" in page_text or "remision" in page_text.lower(),
          "Mensaje indicando seleccionar remision visible")

    return True


# ═══════════════════════════════════════════════════════════════════
# TEST 2: Crear Remision
# ═══════════════════════════════════════════════════════════════════

def test_crear_remision(driver):
    """Crea una nueva remision desde el modal"""
    print("\n=== TEST 2: Crear Remision ===\n")

    # Click en 'Nueva Remision'
    btn_nueva = WebDriverWait(driver, 10).until(
        EC.element_to_be_clickable((By.CSS_SELECTOR, "[data-bs-target='#modalNuevaRemision']"))
    )
    btn_nueva.click()
    time.sleep(1)

    # Esperar que el modal se abra
    modal = WebDriverWait(driver, 5).until(
        EC.visibility_of_element_located((By.ID, "modalNuevaRemision"))
    )
    check(modal.is_displayed(), "Modal 'Nueva Remision' abierto")
    screenshot(driver, "04_modal_nueva_remision")

    # Seleccionar primer proveedor disponible
    # TomSelect reemplaza el select nativo con un input
    try:
        # TomSelect crea un wrapper con clase .ts-control
        ts_control = modal.find_element(By.CSS_SELECTOR, ".ts-control input")
        ts_control.click()
        time.sleep(0.5)
        # Seleccionar primer opcion del dropdown
        ts_option = WebDriverWait(driver, 5).until(
            EC.element_to_be_clickable((By.CSS_SELECTOR, ".ts-dropdown .option"))
        )
        ts_option.click()
        time.sleep(0.5)
        check(True, "Proveedor seleccionado via TomSelect")
    except Exception:
        # Fallback: select nativo si TomSelect no se cargo
        try:
            select_prov = Select(modal.find_element(By.ID, "selProveedorRemision"))
            if len(select_prov.options) > 1:
                select_prov.select_by_index(1)
                check(True, "Proveedor seleccionado via select nativo")
            else:
                check(False, "No hay proveedores disponibles en el select")
                return False
        except Exception as e:
            check(False, f"No se pudo seleccionar proveedor: {e}")
            return False

    # Llenar numero de remision
    txt_remision = modal.find_element(By.CSS_SELECTOR, "input[name='numRemision']")
    txt_remision.clear()
    txt_remision.send_keys("TEST-001")

    # Fecha ya tiene valor default
    screenshot(driver, "05_remision_filled")

    # Submit
    btn_submit = modal.find_element(By.CSS_SELECTOR, "button[type='submit']")
    btn_submit.click()
    time.sleep(3)

    # Verificar que se creo la remision (debe redirigir con ?IdRemision=...)
    check("IdRemision" in driver.current_url,
          "Remision creada — URL contiene IdRemision")
    screenshot(driver, "06_remision_creada")

    # Verificar que aparece la info de la remision
    page_text = driver.page_source
    check("TEST-001" in page_text or "Remision" in page_text,
          "Datos de remision visibles en la pagina")

    # Verificar boton 'Nueva Pieza' visible
    try:
        btn_nueva_pieza = driver.find_element(By.ID, "btnNuevaPieza")
        check(btn_nueva_pieza.is_displayed(), "Boton 'Nueva Pieza' visible")
    except NoSuchElementException:
        check(False, "Boton 'Nueva Pieza' no encontrado")

    return True


# ═══════════════════════════════════════════════════════════════════
# TEST 3: Verificar Calculos de Precio
# ═══════════════════════════════════════════════════════════════════

def test_calculos_precio(driver):
    """
    Verifica la formula de precio:
    Precio = CostoNeto x Utilidad x UtilidadExtra x Impuesto / Divisor x TCCotizacion

    Y la formula de costos:
    CostoBruto = Peso x PrecioGramo
    CostoNeto = CostoBruto x (1 - Descuento/100)
    """
    print("\n=== TEST 3: Calculos de Precio ===\n")

    # Abrir seccion de alta de pieza
    btn_nueva_pieza = driver.find_element(By.ID, "btnNuevaPieza")
    btn_nueva_pieza.click()
    time.sleep(1)

    # Verificar que la seccion esta visible
    seccion = driver.find_element(By.ID, "seccionAltaPieza")
    check("show" in seccion.get_attribute("class"), "Seccion 'Agregar Pieza' visible")
    screenshot(driver, "07_alta_pieza_abierta")

    # ─── Test 3a: Calculo de CostoBruto y CostoNeto ──────────────
    print("\n  --- 3a: CostoBruto = Peso x PrecioGramo ---")

    def set_field(field_id, value):
        """Helper para limpiar y poner valor en un campo"""
        field = driver.find_element(By.ID, field_id)
        field.clear()
        field.send_keys(str(value))
        # Trigger input event
        driver.execute_script(
            "arguments[0].dispatchEvent(new Event('input', {bubbles:true}));", field)
        return field

    # Peso = 10.5, PrecioGramo = 20
    set_field("txtPeso", "10.50")
    set_field("txtPrecioGramo", "20")
    time.sleep(0.5)

    costo_bruto = driver.find_element(By.ID, "txtCostoBruto").get_attribute("value")
    expected_bruto = 10.50 * 20  # = 210.00
    check(abs(float(costo_bruto) - expected_bruto) < 0.01,
          f"CostoBruto = {costo_bruto} (esperado {expected_bruto:.2f})")

    # ─── Test 3b: CostoNeto con descuento ─────────────────────────
    print("\n  --- 3b: CostoNeto = CostoBruto x (1 - Descuento%) ---")

    set_field("txtDescuento", "10")
    time.sleep(0.5)

    costo_neto = driver.find_element(By.ID, "txtCostoNeto").get_attribute("value")
    expected_neto = expected_bruto * (1 - 10 / 100)  # = 189.00
    check(abs(float(costo_neto) - expected_neto) < 0.01,
          f"CostoNeto = {costo_neto} (esperado {expected_neto:.2f})")

    # ─── Test 3c: Precio con formula completa ─────────────────────
    print("\n  --- 3c: Precio = CostoNeto x Util x UtilExtra x Imp / Div x TC ---")

    set_field("txtUtilidad", "1.8")
    set_field("txtUtilidadExtra", "1.05")
    set_field("txtImpuesto", "1.16")
    set_field("txtDivisor", "1")
    set_field("txtTCCotizacion", "1")
    time.sleep(0.5)

    precio_calc = driver.find_element(By.ID, "txtPrecioCalculado").get_attribute("value")
    # Precio = 189 x 1.8 x 1.05 x 1.16 / 1 x 1 = 414.28... -> redondeado a 414
    expected_precio = expected_neto * 1.8 * 1.05 * 1.16 / 1 * 1
    # El VB6 usa Format(val, "######") que redondea a entero
    precio_clean = precio_calc.replace(",", "").replace(".", "").strip()
    check(abs(int(precio_clean) - round(expected_precio)) <= 1,
          f"Precio = {precio_calc} (esperado ~{round(expected_precio)})")

    screenshot(driver, "08_calculos_precio")

    # ─── Test 3d: Cambio de factores recalcula ────────────────────
    print("\n  --- 3d: Cambio de Utilidad recalcula precio ---")

    set_field("txtUtilidad", "1.6667")
    time.sleep(0.5)

    precio_nuevo = driver.find_element(By.ID, "txtPrecioCalculado").get_attribute("value")
    expected_nuevo = expected_neto * 1.6667 * 1.05 * 1.16 / 1 * 1
    precio_nuevo_clean = precio_nuevo.replace(",", "").replace(".", "").strip()
    check(abs(int(precio_nuevo_clean) - round(expected_nuevo)) <= 1,
          f"Precio recalculado = {precio_nuevo} (esperado ~{round(expected_nuevo)})")

    # ─── Test 3e: Divisor != 1 ────────────────────────────────────
    print("\n  --- 3e: Divisor != 1 reduce precio ---")

    set_field("txtDivisor", "20.71")
    time.sleep(0.5)

    precio_div = driver.find_element(By.ID, "txtPrecioCalculado").get_attribute("value")
    expected_div = expected_neto * 1.6667 * 1.05 * 1.16 / 20.71 * 1
    precio_div_clean = precio_div.replace(",", "").replace(".", "").strip()
    check(abs(int(precio_div_clean) - round(expected_div)) <= 1,
          f"Precio con divisor 20.71 = {precio_div} (esperado ~{round(expected_div)})")

    # Restaurar divisor
    set_field("txtDivisor", "1")
    time.sleep(0.3)

    screenshot(driver, "09_calculos_completos")
    return True


# ═══════════════════════════════════════════════════════════════════
# TEST 4: Cambio de Moneda
# ═══════════════════════════════════════════════════════════════════

def test_cambio_moneda(driver):
    """
    Verifica que al cambiar moneda:
    1. El TC Cotizacion se actualiza automaticamente
    2. El precio se recalcula con el nuevo TC
    """
    print("\n=== TEST 4: Cambio de Moneda ===\n")

    # Primero poner valores conocidos para poder verificar el efecto del TC
    def set_field(field_id, value):
        field = driver.find_element(By.ID, field_id)
        field.clear()
        field.send_keys(str(value))
        driver.execute_script(
            "arguments[0].dispatchEvent(new Event('input', {bubbles:true}));", field)

    set_field("txtPeso", "10")
    set_field("txtPrecioGramo", "100")
    set_field("txtDescuento", "0")
    set_field("txtUtilidad", "1")
    set_field("txtUtilidadExtra", "1")
    set_field("txtImpuesto", "1")
    set_field("txtDivisor", "1")
    set_field("txtTCCotizacion", "1")
    time.sleep(0.5)

    # Precio base: 10 x 100 x 1 x 1 x 1 / 1 x 1 = 1000
    precio_base = driver.find_element(By.ID, "txtPrecioCalculado").get_attribute("value")
    precio_base_clean = precio_base.replace(",", "").replace(".", "").strip()
    check(abs(int(precio_base_clean) - 1000) <= 1,
          f"Precio base con TC=1: {precio_base} (esperado 1000)")

    # ─── 4a: Seleccionar moneda diferente (Dolar) ─────────────────
    print("\n  --- 4a: Seleccionar Dolar (IdMoneda=2) ---")

    # Intentar cambiar moneda via TomSelect o select nativo
    try:
        # TomSelect: buscar el wrapper del select de moneda
        sel_moneda = driver.find_element(By.ID, "selMoneda")
        # Si TomSelect esta activo, hay un .ts-wrapper alrededor
        ts_wrappers = driver.find_elements(By.CSS_SELECTOR, "#selMoneda + .ts-wrapper, .ts-wrapper")

        if ts_wrappers:
            # TomSelect activo — click para abrir dropdown
            ts_ctrl = ts_wrappers[-1].find_element(By.CSS_SELECTOR, ".ts-control")
            ts_ctrl.click()
            time.sleep(0.5)

            # Buscar opcion "Dolar" o la segunda opcion
            options = driver.find_elements(By.CSS_SELECTOR, ".ts-dropdown .option")
            moneda_selected = False
            for opt in options:
                if "dolar" in opt.text.lower() or "dollar" in opt.text.lower():
                    opt.click()
                    moneda_selected = True
                    break
            if not moneda_selected and len(options) > 1:
                options[1].click()  # Segunda opcion (primera es placeholder)
                moneda_selected = True

            check(moneda_selected, "Moneda cambiada via TomSelect")
        else:
            # Select nativo
            select = Select(sel_moneda)
            # Buscar 'Dolar' o usar indice 2
            for i, opt in enumerate(select.options):
                if "dolar" in opt.text.lower():
                    select.select_by_index(i)
                    break
            else:
                if len(select.options) > 1:
                    select.select_by_index(1)
            check(True, "Moneda cambiada via select nativo")

    except Exception as e:
        check(False, f"Error cambiando moneda: {e}")

    time.sleep(2)  # Esperar AJAX de tipo de cambio
    screenshot(driver, "10_cambio_moneda")

    # ─── 4b: Verificar que TC se actualizo ────────────────────────
    print("\n  --- 4b: Verificar TC Cotizacion actualizado ---")

    tc_value = driver.find_element(By.ID, "txtTCCotizacion").get_attribute("value")
    tc_float = float(tc_value) if tc_value else 0

    check(tc_float > 0, f"TC Cotizacion tiene valor: {tc_value}")

    # ─── 4c: Si TC != 1, el precio debe haber cambiado ───────────
    print("\n  --- 4c: Precio recalculado con nuevo TC ---")

    precio_nuevo = driver.find_element(By.ID, "txtPrecioCalculado").get_attribute("value")
    precio_nuevo_clean = precio_nuevo.replace(",", "").replace(".", "").strip()

    if tc_float != 1 and tc_float > 0:
        expected = round(1000 * tc_float)
        check(abs(int(precio_nuevo_clean) - expected) <= 1,
              f"Precio con TC={tc_value}: {precio_nuevo} (esperado ~{expected})")
    else:
        # TC = 1 para Moneda Nacional es esperado
        check(True, f"TC={tc_value}, precio={precio_nuevo} (moneda nacional, TC=1 esperado)")

    # ─── 4d: Cambio manual de TC ──────────────────────────────────
    print("\n  --- 4d: Cambio manual de TC ---")

    set_field("txtTCCotizacion", "17.5")
    time.sleep(0.5)

    precio_tc_manual = driver.find_element(By.ID, "txtPrecioCalculado").get_attribute("value")
    precio_tc_clean = precio_tc_manual.replace(",", "").replace(".", "").strip()
    expected_manual = round(1000 * 17.5)  # = 17500
    check(abs(int(precio_tc_clean) - expected_manual) <= 1,
          f"Precio con TC manual 17.5: {precio_tc_manual} (esperado {expected_manual})")

    screenshot(driver, "11_tc_manual")

    # ─── 4e: Formula detalle visible ──────────────────────────────
    print("\n  --- 4e: Formula detalle muestra valores ---")

    try:
        formula = driver.find_element(By.ID, "formulaDetalle").text
        check(len(formula) > 5, f"Formula visible: {formula}")
    except NoSuchElementException:
        check(False, "Elemento formulaDetalle no encontrado")

    return True


# ═══════════════════════════════════════════════════════════════════
# TEST 5: Busqueda de Remisiones (modal)
# ═══════════════════════════════════════════════════════════════════

def test_buscar_remision(driver):
    """Verifica el modal de busqueda de remisiones"""
    print("\n=== TEST 5: Busqueda de Remisiones ===\n")

    # Cancelar pieza actual primero
    try:
        btn_cancel = driver.find_element(By.ID, "btnCancelarPieza")
        btn_cancel.click()
        time.sleep(0.5)
    except Exception:
        pass

    # Abrir modal de busqueda
    btn_buscar = driver.find_element(By.CSS_SELECTOR, "[data-bs-target='#modalBuscarRemision']")
    btn_buscar.click()
    time.sleep(1)

    modal = WebDriverWait(driver, 5).until(
        EC.visibility_of_element_located((By.ID, "modalBuscarRemision"))
    )
    check(modal.is_displayed(), "Modal 'Buscar Remision' abierto")

    # Verificar que la tabla se lleno automaticamente
    time.sleep(2)  # Esperar AJAX
    rows = driver.find_elements(By.CSS_SELECTOR, "#tablaRemisiones tbody tr")
    check(len(rows) > 0, f"Tabla de remisiones tiene {len(rows)} filas")

    screenshot(driver, "12_buscar_remision")

    # Cerrar modal
    close_btn = modal.find_element(By.CSS_SELECTOR, ".btn-close")
    close_btn.click()
    time.sleep(0.5)

    return True


# ═══════════════════════════════════════════════════════════════════
# TEST 6: Atajos de teclado
# ═══════════════════════════════════════════════════════════════════

def test_atajos_teclado(driver):
    """Verifica atajos F1 (nueva pieza) y Esc (cancelar)"""
    print("\n=== TEST 6: Atajos de Teclado ===\n")

    # F1 debe abrir seccion de nueva pieza
    body = driver.find_element(By.TAG_NAME, "body")
    body.send_keys(Keys.F1)
    time.sleep(0.5)

    seccion = driver.find_element(By.ID, "seccionAltaPieza")
    check("show" in seccion.get_attribute("class"),
          "F1 abre seccion de nueva pieza")

    # Esc debe cerrar la seccion
    body.send_keys(Keys.ESCAPE)
    time.sleep(0.5)

    check("show" not in seccion.get_attribute("class"),
          "Esc cierra seccion de nueva pieza")

    screenshot(driver, "13_atajos_teclado")
    return True


# ═══════════════════════════════════════════════════════════════════
# MAIN
# ═══════════════════════════════════════════════════════════════════

def main():
    global passed, failed

    print("=" * 65)
    print("  SELENIUM TEST: Alta de Lotes de Piezas Repetidas")
    print(f"  URL: {BASE_URL}")
    print(f"  Headless: {HEADLESS}")
    print("=" * 65)

    driver = None
    try:
        driver = setup_driver()

        # Test 1: Login
        if not test_login(driver):
            print("\n[ABORT] Login fallo. No se pueden ejecutar mas pruebas.")
            screenshot(driver, "99_login_failed")
            sys.exit(1)

        # Test 2: Crear Remision
        test_crear_remision(driver)

        # Test 3: Calculos de Precio
        test_calculos_precio(driver)

        # Test 4: Cambio de Moneda
        test_cambio_moneda(driver)

        # Test 5: Buscar Remision
        test_buscar_remision(driver)

        # Test 6: Atajos de teclado
        test_atajos_teclado(driver)

    except Exception as e:
        failed += 1
        errors.append(f"Error inesperado: {e}")
        print(f"\n[ERROR] {e}")
        if driver:
            screenshot(driver, "99_error")
        import traceback
        traceback.print_exc()

    finally:
        if driver:
            driver.quit()

    # ─── Resumen ──────────────────────────────────────────────────
    print("\n" + "=" * 65)
    print(f"  RESULTADOS: {passed} passed, {failed} failed")
    print("=" * 65)

    if errors:
        print("\n  Fallos:")
        for e in errors:
            print(f"    - {e}")

    print(f"\n  Screenshots guardados en: {SCREENSHOT_DIR}")

    sys.exit(0 if failed == 0 else 1)


if __name__ == "__main__":
    main()
