#!/usr/bin/env python3
"""
Selenium test: CRUD completo para Defaults Utilidad Extra.
Ticket: 286856 - Migrar frmDefaultsUtilidadExtra.frm a Razor Page.
Prueba: Create, Read, Edit, Delete.
"""
import sys
import time
from datetime import datetime
from selenium import webdriver
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

BASE_URL = "https://bot-286856.dev.powerera.com"
LOGIN_USER = "admin"
LOGIN_PASS = "u38a8fk3j0!"
CHROMEDRIVER_PATH = "/home/earaiza/.cache/selenium/chromedriver/linux64/145.0.7632.117/chromedriver"
TEST_VALUE = "1.7770"
TEST_EDIT_VALUE = "2.3330"
TIMESTAMP = datetime.utcnow().strftime("%Y%m%d%H%M%S")


def create_driver():
    opts = Options()
    opts.add_argument("--headless=new")
    opts.add_argument("--no-sandbox")
    opts.add_argument("--disable-dev-shm-usage")
    opts.add_argument("--disable-gpu")
    opts.add_argument("--window-size=1920,1080")
    opts.binary_location = "/usr/bin/google-chrome"
    svc = Service(executable_path=CHROMEDRIVER_PATH)
    driver = webdriver.Chrome(service=svc, options=opts)
    driver.set_page_load_timeout(120)
    return driver


def login(driver):
    driver.get(f"{BASE_URL}/Security/Auth/Login")
    wait = WebDriverWait(driver, 30)
    user_field = wait.until(EC.presence_of_element_located(
        (By.NAME, "LoginViewModel.Username")))
    user_field.clear()
    user_field.send_keys(LOGIN_USER)
    pass_field = driver.find_element(By.NAME, "LoginViewModel.Password")
    pass_field.clear()
    pass_field.send_keys(LOGIN_PASS)
    driver.find_element(By.CSS_SELECTOR, "button[type='submit']").click()
    wait.until(lambda d: "/Auth/Login" not in d.current_url)
    print(f"[OK] Login exitoso - redirigido a {driver.current_url}")


def navigate_to_page(driver):
    driver.get(f"{BASE_URL}/Catalogos/DefaultsUtilidadExtra")
    wait = WebDriverWait(driver, 30)
    wait.until(EC.presence_of_element_located((By.CSS_SELECTOR, "table.table")))
    print("[OK] Navegacion a Defaults Utilidad Extra")


def test_create(driver):
    wait = WebDriverWait(driver, 30)
    input_field = wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, "input[name='NuevaUtilidadExtra']")))
    input_field.clear()
    input_field.send_keys("1.777")
    # Submit create form
    driver.find_element(
        By.CSS_SELECTOR,
        "form[action*='Create'] button[type='submit']"
    ).click()
    wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".alert-success")))
    page_text = driver.find_element(By.CSS_SELECTOR, "table.table tbody").text
    assert TEST_VALUE in page_text, f"Valor {TEST_VALUE} no encontrado en la tabla"
    print(f"[OK] Create: valor {TEST_VALUE} creado")


def find_row_id(driver, value_text):
    """Find the ID of the row containing value_text in the utilidad extra column."""
    rows = driver.find_elements(By.CSS_SELECTOR, "table.table tbody tr")
    for row in rows:
        cells = row.find_elements(By.TAG_NAME, "td")
        if len(cells) >= 2:
            display_span = cells[1].find_elements(By.CSS_SELECTOR, "span.display-val")
            if display_span and value_text in display_span[0].text:
                return cells[0].text.strip()
    return None


def test_edit(driver):
    wait = WebDriverWait(driver, 30)
    row_id = find_row_id(driver, TEST_VALUE)
    assert row_id, f"No se encontro fila con valor {TEST_VALUE}"

    # Click the edit button to enter edit mode
    edit_btn = driver.find_element(
        By.CSS_SELECTOR, f"button[onclick='startEdit({row_id})']")
    edit_btn.click()
    time.sleep(0.5)

    # The visible input should now be shown
    visible_input = wait.until(EC.visibility_of_element_located(
        (By.CSS_SELECTOR, f"#input-ue-{row_id}")))
    visible_input.clear()
    visible_input.send_keys("2.333")

    # Trigger input event so hidden field syncs
    driver.execute_script(
        "arguments[0].dispatchEvent(new Event('input', {bubbles:true}));",
        visible_input
    )
    time.sleep(0.3)

    # Submit edit form
    edit_form = driver.find_element(By.CSS_SELECTOR, f"#edit-form-{row_id}")
    edit_form.find_element(By.CSS_SELECTOR, "button[type='submit']").click()
    wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".alert-success")))

    # Verify updated value in table
    page_text = driver.find_element(By.CSS_SELECTOR, "table.table tbody").text
    assert TEST_EDIT_VALUE in page_text, \
        f"Valor editado {TEST_EDIT_VALUE} no encontrado en la tabla"
    print(f"[OK] Edit: valor cambiado de {TEST_VALUE} a {TEST_EDIT_VALUE}")


def test_delete(driver):
    wait = WebDriverWait(driver, 30)
    row_id = find_row_id(driver, TEST_EDIT_VALUE)
    assert row_id, f"No se encontro fila con valor {TEST_EDIT_VALUE} para eliminar"

    old_table = driver.find_element(By.CSS_SELECTOR, "table.table")

    # Override confirm and click delete button
    driver.execute_script(f"""
        window.confirm = function() {{ return true; }};
        var inputs = document.querySelectorAll("input[name='id']");
        for (var i = 0; i < inputs.length; i++) {{
            if (inputs[i].value === '{row_id}') {{
                var btn = inputs[i].parentElement.querySelector('button[type="submit"]');
                if (btn) btn.click();
                break;
            }}
        }}
    """)

    try:
        wait.until(EC.staleness_of(old_table))
    except Exception:
        pass
    wait.until(EC.presence_of_element_located(
        (By.CSS_SELECTOR, ".alert-success")))

    page_text = driver.find_element(By.CSS_SELECTOR, "table.table tbody").text
    assert TEST_EDIT_VALUE not in page_text, \
        f"Valor {TEST_EDIT_VALUE} sigue en la tabla despues de eliminar"
    print(f"[OK] Delete: valor {TEST_EDIT_VALUE} eliminado exitosamente")


def main():
    driver = None
    try:
        driver = create_driver()
        login(driver)
        navigate_to_page(driver)
        test_create(driver)
        test_edit(driver)
        test_delete(driver)
        print("\n=== TODAS LAS PRUEBAS PASARON ===")
        return 0
    except Exception as e:
        print(f"\n[FAIL] {e}")
        if driver:
            screenshot_path = f"/home/earaiza/selenium_defaults_ue_{TIMESTAMP}.png"
            driver.save_screenshot(screenshot_path)
            print(f"Screenshot guardado: {screenshot_path}")
            print(f"URL actual: {driver.current_url}")
            print(f"Page source (primeros 2000 chars):\n{driver.page_source[:2000]}")
        return 1
    finally:
        if driver:
            driver.quit()


if __name__ == "__main__":
    sys.exit(main())
