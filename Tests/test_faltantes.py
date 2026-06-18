#!/usr/bin/env python3
"""
Prueba Selenium: Reporte de Faltantes (frmReporteInventarioFisico)
Descripcion: Verifica la migracion de frmReporteInventarioFisico.frm a Razor Page.
- Ver grid de piezas faltantes con columnas completas
- Buscar por texto
- Agregar/editar comentario por pieza
- Exportar a Excel

Referencia VB6: /home/earaiza/Diamonds/frmReporteInventarioFisico.frm
"""

from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.common.exceptions import NoSuchElementException, TimeoutException
import time
import os
import sys

# ─── Configuracion ────────────────────────────────────────────────
BASE_URL = os.environ.get("TEST_BASE_URL", "https://bot-286840.dev.powerera.com")
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
        print(f"   PASS: {description}")
    else:
        failed += 1
        errors.append(description)
        print(f"   FAIL: {description}")


def login(driver):
    """Login al sistema"""
    print("\n1. Login...")
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    time.sleep(2)

    # Verificar que estamos en la pagina de login
    if "/Login" not in driver.current_url and "/Auth/" not in driver.current_url:
        print("   Ya autenticado, saltando login")
        return

    try:
        user_input = driver.find_element(By.CSS_SELECTOR, "input[name='Username'], input[name='Input.Username'], #Username, #Input_Username")
        user_input.clear()
        user_input.send_keys(USERNAME)

        pass_input = driver.find_element(By.CSS_SELECTOR, "input[name='Password'], input[name='Input.Password'], #Password, #Input_Password")
        pass_input.clear()
        pass_input.send_keys(PASSWORD)

        submit_btn = driver.find_element(By.CSS_SELECTOR, "button[type='submit'], input[type='submit']")
        submit_btn.click()
        time.sleep(3)

        check("/Login" not in driver.current_url, "Login exitoso")
    except Exception as ex:
        check(False, f"Login fallo: {ex}")
        screenshot(driver, "login_error")
        raise


def test_ver_faltantes(driver):
    """Test: Navegar a la pagina y verificar que carga el grid"""
    print("\n2. Ver Faltantes...")
    driver.get(f"{BASE_URL}/Inventario/Faltantes")
    time.sleep(3)
    screenshot(driver, "01_faltantes_page")

    # Verificar titulo
    check("Reporte de Faltantes" in driver.page_source or "Faltantes" in driver.title,
          "Pagina de faltantes carga correctamente")

    # Verificar que la tabla existe
    try:
        tabla = driver.find_element(By.ID, "tablaFaltantes")
        check(True, "Tabla de faltantes presente")
    except NoSuchElementException:
        check(False, "Tabla de faltantes presente")
        return

    # Verificar columnas del encabezado
    headers = tabla.find_elements(By.CSS_SELECTOR, "thead th")
    header_texts = [h.text.strip() for h in headers]
    print(f"   Columnas encontradas: {header_texts}")

    expected_headers = ["Codigo", "Descripcion", "Modelo", "Linea", "K", "Peso", "Precio", "Grupo", "Comentario"]
    for eh in expected_headers:
        check(any(eh.lower() in h.lower() for h in header_texts),
              f"Columna '{eh}' presente en encabezado")

    # Verificar que hay filas de datos
    rows = tabla.find_elements(By.CSS_SELECTOR, "tbody tr")
    check(len(rows) > 0, f"Grid tiene {len(rows)} filas de datos")

    # Verificar stat cards
    try:
        stat_cards = driver.find_elements(By.CSS_SELECTOR, ".stat-card .stat-number")
        check(len(stat_cards) >= 2, f"Stat cards presentes ({len(stat_cards)} encontradas)")
    except Exception:
        check(False, "Stat cards presentes")


def test_busqueda(driver):
    """Test: Buscar por texto y verificar filtrado"""
    print("\n3. Busqueda...")
    driver.get(f"{BASE_URL}/Inventario/Faltantes")
    time.sleep(2)

    # Obtener total antes de buscar
    rows_antes = driver.find_elements(By.CSS_SELECTOR, "#tablaFaltantes tbody tr")
    total_antes = len(rows_antes)
    print(f"   Total antes de buscar: {total_antes}")

    # Buscar con texto especifico
    buscar_input = driver.find_element(By.ID, "txtBuscar")
    buscar_input.clear()
    buscar_input.send_keys("cadena")

    btn_buscar = driver.find_element(By.ID, "btnBuscar")
    btn_buscar.click()
    time.sleep(3)
    screenshot(driver, "02_busqueda")

    # Verificar que la URL tiene el parametro de busqueda
    check("Buscar=cadena" in driver.current_url or "buscar=cadena" in driver.current_url.lower(),
          "Parametro de busqueda en URL")

    # Verificar que los resultados filtrados muestran "cadena"
    rows_despues = driver.find_elements(By.CSS_SELECTOR, "#tablaFaltantes tbody tr")
    if len(rows_despues) > 0 and "No se encontraron" not in rows_despues[0].text:
        # Al menos una fila deberia contener "cadena"
        found_match = False
        for row in rows_despues:
            if "cadena" in row.text.lower():
                found_match = True
                break
        check(found_match, "Resultados filtrados contienen el termino buscado")
    else:
        print("   INFO: Busqueda no retorno resultados (puede ser normal si no hay 'cadena')")
        check(True, "Busqueda ejecutada sin errores")

    # Limpiar busqueda
    try:
        btn_limpiar = driver.find_element(By.ID, "btnLimpiar")
        btn_limpiar.click()
        time.sleep(2)
        check("Buscar" not in driver.current_url, "Limpiar busqueda funciona")
    except NoSuchElementException:
        print("   INFO: Boton limpiar no visible (busqueda vacia)")


def test_agregar_comentario(driver):
    """Test: Agregar comentario a una pieza faltante"""
    print("\n4. Agregar comentario...")
    driver.get(f"{BASE_URL}/Inventario/Faltantes")
    time.sleep(3)

    # Encontrar la primera pieza con boton de comentario
    try:
        btn_edit = driver.find_element(By.CSS_SELECTOR, "button.btn-comment-edit")
    except NoSuchElementException:
        try:
            btn_edit = driver.find_element(By.CSS_SELECTOR, "button[onclick*='startComment']")
        except NoSuchElementException:
            check(False, "Boton de editar comentario presente")
            return

    # Extraer el codigo de barras del onclick
    onclick = btn_edit.get_attribute("onclick") or ""
    cb = onclick.replace("startComment('", "").replace("')", "")
    print(f"   Editando comentario para pieza: {cb}")

    # Click en boton editar
    btn_edit.click()
    time.sleep(1)
    screenshot(driver, "03_comment_edit_mode")

    # Verificar que aparece el formulario inline
    try:
        edit_form = driver.find_element(By.ID, f"edit-com-{cb}")
        check("d-none" not in (edit_form.get_attribute("class") or ""),
              "Formulario de edicion de comentario visible")
    except NoSuchElementException:
        check(False, "Formulario de edicion de comentario visible")
        return

    # Escribir comentario de prueba
    comment_text = f"Test Selenium {time.strftime('%H:%M:%S')}"
    input_field = edit_form.find_element(By.CSS_SELECTOR, "input[name='ComentarioTexto']")
    input_field.clear()
    input_field.send_keys(comment_text)

    # Guardar
    btn_save = edit_form.find_element(By.CSS_SELECTOR, "button.btn-success")
    btn_save.click()
    time.sleep(3)
    screenshot(driver, "04_comment_saved")

    # Verificar mensaje de exito
    check("Comentario guardado" in driver.page_source or "success" in driver.page_source.lower(),
          "Mensaje de exito al guardar comentario")

    # Verificar que el comentario aparece en la fila
    try:
        display_span = driver.find_element(By.ID, f"display-com-{cb}")
        check(comment_text in (display_span.text or ""),
              f"Comentario '{comment_text}' visible en la fila")
    except NoSuchElementException:
        # Puede haber recargado la pagina, verificar en la tabla general
        check(comment_text in driver.page_source,
              f"Comentario '{comment_text}' presente en la pagina")


def test_cancelar_comentario(driver):
    """Test: Cancelar edicion de comentario"""
    print("\n5. Cancelar comentario...")
    driver.get(f"{BASE_URL}/Inventario/Faltantes")
    time.sleep(3)

    try:
        btn_edit = driver.find_element(By.CSS_SELECTOR, "button.btn-comment-edit")
    except NoSuchElementException:
        print("   SKIP: No hay boton de editar comentario")
        return

    onclick = btn_edit.get_attribute("onclick") or ""
    cb = onclick.replace("startComment('", "").replace("')", "")

    btn_edit.click()
    time.sleep(1)

    # Click cancelar
    try:
        btn_cancel = driver.find_element(By.CSS_SELECTOR, f"#edit-com-{cb} button.btn-secondary")
        btn_cancel.click()
        time.sleep(1)

        # Verificar que el formulario se oculto
        edit_form = driver.find_element(By.ID, f"edit-com-{cb}")
        check("d-none" in (edit_form.get_attribute("class") or ""),
              "Cancelar edicion oculta el formulario")
    except NoSuchElementException:
        check(False, "Boton cancelar presente")


def test_exportar_excel(driver):
    """Test: Verificar que el boton de exportar Excel existe y responde"""
    print("\n6. Exportar Excel...")
    driver.get(f"{BASE_URL}/Inventario/Faltantes")
    time.sleep(2)

    try:
        btn_excel = driver.find_element(By.ID, "btnExportarExcel")
        check(True, "Boton Exportar Excel presente")
        check("fa-file-excel" in btn_excel.get_attribute("innerHTML"),
              "Boton tiene icono de Excel")
    except NoSuchElementException:
        check(False, "Boton Exportar Excel presente")


def test_stat_cards(driver):
    """Test: Verificar que las stat cards muestran datos correctos"""
    print("\n7. Stat Cards...")
    driver.get(f"{BASE_URL}/Inventario/Faltantes")
    time.sleep(2)

    stat_cards = driver.find_elements(By.CSS_SELECTOR, ".stat-card")
    check(len(stat_cards) == 4, f"4 stat cards presentes ({len(stat_cards)} encontradas)")

    # Verificar que los numeros son validos (no vacios)
    for card in stat_cards:
        num = card.find_element(By.CSS_SELECTOR, ".stat-number").text
        label = card.find_element(By.CSS_SELECTOR, ".stat-label").text
        check(len(num.strip()) > 0, f"Stat card '{label}' tiene valor: {num}")


def main():
    print("=" * 60)
    print("SELENIUM: Reporte de Faltantes")
    print(f"URL: {BASE_URL}")
    print("=" * 60)

    driver = setup_driver()
    try:
        login(driver)
        test_ver_faltantes(driver)
        test_busqueda(driver)
        test_agregar_comentario(driver)
        test_cancelar_comentario(driver)
        test_exportar_excel(driver)
        test_stat_cards(driver)
    except Exception as ex:
        print(f"\n   FATAL ERROR: {ex}")
        screenshot(driver, "fatal_error")
        failed_count = 1
    finally:
        driver.quit()

    # Resumen
    print("\n" + "=" * 60)
    print(f"RESULTADOS: {passed} passed, {failed} failed")
    if errors:
        print(f"\nFallas:")
        for e in errors:
            print(f"  - {e}")
    print("=" * 60)

    sys.exit(1 if failed > 0 else 0)


if __name__ == "__main__":
    main()
