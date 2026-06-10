#!/usr/bin/env python3
"""Selenium test: CRUD completo de Catálogo de Grupos (DiamondsWeb)
Ticket: 286812 — Migración de frmGrupos.frm a Razor Page
"""

from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import chromedriver_autoinstaller
import time
import sys
import os

BASE_URL = "http://localhost:5200"
SCREENSHOT_DIR = "/home/earaiza/DiamondsWeb/screenshots"
TEST_USER = "admin"
TEST_PASS = "u38a8fk3j0!"
TEST_GRUPO_NAME = "SeleniumTest_Grupo"
TEST_GRUPO_EDIT = "SeleniumTest_Editado"

def setup_driver():
    chromedriver_autoinstaller.install()
    options = Options()
    options.add_argument("--headless=new")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")
    options.add_argument("--disable-gpu")
    options.add_argument("--window-size=1920,1080")
    options.add_argument("--ignore-certificate-errors")
    options.set_capability("goog:loggingPrefs", {"browser": "ALL"})
    return webdriver.Chrome(options=options)


def login(driver, wait):
    """Login en UserPortal"""
    print("1. LOGIN")
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    wait.until(EC.presence_of_element_located((By.TAG_NAME, "form")))

    user_input = driver.find_elements(By.NAME, "LoginViewModel.Username")
    pwd_input = driver.find_elements(By.NAME, "LoginViewModel.Password")

    if user_input:
        user_input[0].clear()
        user_input[0].send_keys(TEST_USER)
    if pwd_input:
        pwd_input[0].clear()
        pwd_input[0].send_keys(TEST_PASS)

    submit = driver.find_elements(By.CSS_SELECTOR, "button[type='submit'], input[type='submit']")
    if submit:
        submit[0].click()

    time.sleep(2)
    if "/Security/Auth/Login" in driver.current_url:
        driver.save_screenshot(f"{SCREENSHOT_DIR}/FAIL_login.png")
        print("   FAIL: Login falló — no se pudo autenticar")
        return False

    print(f"   OK: Login exitoso → {driver.current_url}")
    return True


def test_navigate_to_grupos(driver, wait):
    """Navegar a la pantalla de Catálogo de Grupos"""
    print("2. NAVEGAR A CATÁLOGO DE GRUPOS")
    driver.get(f"{BASE_URL}/Catalogos/Grupos")
    time.sleep(2)

    # Verificar que cargó la página correcta
    title_el = driver.find_elements(By.XPATH, "//*[contains(text(), 'Catálogo de Grupos')]")
    if not title_el:
        driver.save_screenshot(f"{SCREENSHOT_DIR}/FAIL_navigate.png")
        print(f"   FAIL: No se encontró título 'Catálogo de Grupos'. URL: {driver.current_url}")
        return False

    driver.save_screenshot(f"{SCREENSHOT_DIR}/02_grupos_index.png")
    print("   OK: Página de Catálogo de Grupos cargada")
    return True


def test_create_grupo(driver, wait):
    """Crear un nuevo grupo"""
    print("3. CREAR NUEVO GRUPO")

    # Click en botón "Nuevo Grupo" (contiene <i> + texto)
    btn_nuevo = wait.until(EC.element_to_be_clickable(
        (By.XPATH, "//button[contains(., 'Nuevo Grupo')]")))
    btn_nuevo.click()
    time.sleep(1)

    # Esperar que el modal aparezca
    wait.until(EC.visibility_of_element_located((By.ID, "modalGrupo")))
    driver.save_screenshot(f"{SCREENSHOT_DIR}/03_modal_nuevo.png")

    # Llenar el nombre
    input_nombre = driver.find_element(By.ID, "nombreGrupo")
    input_nombre.clear()
    input_nombre.send_keys(TEST_GRUPO_NAME)

    # Verificar que el form action apunte a handler=Crear
    form = driver.find_element(By.ID, "formGrupo")
    action = form.get_attribute("action") or ""
    if "Crear" not in action:
        print(f"   WARN: form action={action}, esperaba handler=Crear")

    # Click Registrar
    btn_registrar = driver.find_element(By.ID, "btnRegistrar")
    btn_registrar.click()
    time.sleep(2)

    driver.save_screenshot(f"{SCREENSHOT_DIR}/04_after_create.png")

    # Verificar mensaje de éxito
    alerts = driver.find_elements(By.CSS_SELECTOR, ".alert-success")
    if alerts and TEST_GRUPO_NAME in alerts[0].text:
        print(f"   OK: Grupo '{TEST_GRUPO_NAME}' creado correctamente")
        return True

    # Verificar que aparece en la tabla
    rows = driver.find_elements(By.XPATH, f"//td/strong[contains(text(), '{TEST_GRUPO_NAME}')]")
    if rows:
        print(f"   OK: Grupo '{TEST_GRUPO_NAME}' visible en la tabla")
        return True

    errors = driver.find_elements(By.CSS_SELECTOR, ".alert-danger")
    if errors:
        print(f"   FAIL: Error al crear: {errors[0].text}")
    else:
        print("   FAIL: No se encontró el grupo creado ni mensaje de éxito")
    return False


def test_search_grupo(driver, wait):
    """Buscar el grupo recién creado"""
    print("4. BUSCAR GRUPO")

    search_input = driver.find_element(By.CSS_SELECTOR, "input[name='Buscar']")
    search_input.clear()
    search_input.send_keys(TEST_GRUPO_NAME)

    btn_buscar = driver.find_element(By.CSS_SELECTOR, "button[type='submit']")
    btn_buscar.click()
    time.sleep(2)

    driver.save_screenshot(f"{SCREENSHOT_DIR}/05_search_result.png")

    rows = driver.find_elements(By.XPATH, f"//td/strong[contains(text(), '{TEST_GRUPO_NAME}')]")
    if rows:
        print(f"   OK: Grupo encontrado en búsqueda")
        return True
    else:
        print("   FAIL: Grupo no encontrado en búsqueda")
        return False


def test_edit_grupo(driver, wait):
    """Editar el grupo creado"""
    print("5. EDITAR GRUPO")

    # Click en botón editar del grupo
    edit_btns = driver.find_elements(By.CSS_SELECTOR, ".btn-editar")
    target_btn = None
    for btn in edit_btns:
        if btn.get_attribute("data-nombre") == TEST_GRUPO_NAME:
            target_btn = btn
            break

    if not target_btn:
        print(f"   FAIL: No se encontró botón editar para '{TEST_GRUPO_NAME}'")
        return False

    target_btn.click()
    time.sleep(1)

    wait.until(EC.visibility_of_element_located((By.ID, "modalGrupo")))
    driver.save_screenshot(f"{SCREENSHOT_DIR}/06_modal_editar.png")

    # Verificar que los campos están llenos
    input_nombre = driver.find_element(By.ID, "nombreGrupo")
    current_val = input_nombre.get_attribute("value")
    if current_val != TEST_GRUPO_NAME:
        print(f"   WARN: Valor esperado '{TEST_GRUPO_NAME}', encontrado '{current_val}'")

    # Cambiar el nombre
    input_nombre.clear()
    input_nombre.send_keys(TEST_GRUPO_EDIT)

    btn_guardar = driver.find_element(By.ID, "btnRegistrar")
    btn_guardar.click()
    time.sleep(2)

    driver.save_screenshot(f"{SCREENSHOT_DIR}/07_after_edit.png")

    # Verificar éxito
    alerts = driver.find_elements(By.CSS_SELECTOR, ".alert-success")
    if alerts and TEST_GRUPO_EDIT in alerts[0].text:
        print(f"   OK: Grupo editado a '{TEST_GRUPO_EDIT}'")
        return True

    rows = driver.find_elements(By.XPATH, f"//td/strong[contains(text(), '{TEST_GRUPO_EDIT}')]")
    if rows:
        print(f"   OK: Grupo editado visible en la tabla")
        return True

    print("   FAIL: No se verificó la edición")
    return False


def test_delete_grupo(driver, wait):
    """Eliminar el grupo editado"""
    print("6. ELIMINAR GRUPO")

    # Primero buscar el grupo editado
    driver.get(f"{BASE_URL}/Catalogos/Grupos?Buscar={TEST_GRUPO_EDIT}")
    time.sleep(2)

    delete_btns = driver.find_elements(By.CSS_SELECTOR, ".btn-eliminar")
    target_btn = None
    for btn in delete_btns:
        if btn.get_attribute("data-nombre") == TEST_GRUPO_EDIT:
            target_btn = btn
            break

    if not target_btn:
        print(f"   FAIL: No se encontró botón eliminar para '{TEST_GRUPO_EDIT}'")
        return False

    # Aceptar el confirm dialog automáticamente
    driver.execute_script("window.confirm = function() { return true; }")

    # Submit the form containing the delete button
    parent_form = target_btn.find_element(By.XPATH, "./..")
    parent_form.submit()
    time.sleep(2)

    driver.save_screenshot(f"{SCREENSHOT_DIR}/08_after_delete.png")

    # Verificar que se eliminó
    alerts = driver.find_elements(By.CSS_SELECTOR, ".alert-success")
    if alerts and "eliminado" in alerts[0].text.lower():
        print(f"   OK: Grupo eliminado correctamente")
        return True

    # Verificar que ya no está en la tabla
    rows = driver.find_elements(By.XPATH, f"//td/strong[contains(text(), '{TEST_GRUPO_EDIT}')]")
    if not rows:
        print(f"   OK: Grupo ya no aparece en la tabla")
        return True

    print("   FAIL: El grupo aún aparece después de eliminar")
    return False


def main():
    os.makedirs(SCREENSHOT_DIR, exist_ok=True)
    driver = setup_driver()
    wait = WebDriverWait(driver, 15)

    results = {}
    try:
        # Login
        if not login(driver, wait):
            print("\nABORT: No se pudo autenticar")
            sys.exit(1)

        # CRUD tests
        results["Navigate"] = test_navigate_to_grupos(driver, wait)
        results["Create"] = test_create_grupo(driver, wait)
        results["Search"] = test_search_grupo(driver, wait)
        results["Edit"] = test_edit_grupo(driver, wait)
        results["Delete"] = test_delete_grupo(driver, wait)

        # Console logs
        print("\n=== CONSOLE LOGS ===")
        logs = driver.get_log("browser")
        for log in logs:
            level = log.get("level", "INFO")
            message = log.get("message", "")
            if "error" in message.lower() or level == "SEVERE":
                print(f"  [{level}] {message}")

    finally:
        driver.quit()

    # Resumen
    print("\n" + "=" * 50)
    print("RESUMEN DE PRUEBAS")
    print("=" * 50)
    passed = sum(1 for v in results.values() if v)
    total = len(results)
    for name, result in results.items():
        status = "✅ PASS" if result else "❌ FAIL"
        print(f"  {status} — {name}")
    print(f"\n  {passed}/{total} pruebas pasaron")

    if passed < total:
        sys.exit(1)
    print("\n🎉 Todas las pruebas CRUD pasaron exitosamente")


if __name__ == "__main__":
    main()
