"""
Selenium test: Inventario Físico (DiamondsWeb)
Test dividido en sesiones independientes para evitar chromedriver timeouts.
"""
import time
import sys
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC

BASE = "https://diamonds.dev.powerera.com"
USER = "admin"
PASS = "u38a8fk3j0!"
results = []

def get_driver():
    opts = Options()
    opts.add_argument("--headless=new")
    opts.add_argument("--no-sandbox")
    opts.add_argument("--disable-dev-shm-usage")
    opts.add_argument("--disable-gpu")
    opts.add_argument("--window-size=1920,1080")
    opts.add_argument("--ignore-certificate-errors")
    opts.add_argument("--single-process")
    return webdriver.Chrome(options=opts)

def login(d):
    d.get(BASE + "/Security/Auth/Login")
    time.sleep(3)
    d.find_element(By.ID, "LoginViewModel_Username").send_keys(USER)
    d.find_element(By.ID, "LoginViewModel_Password").send_keys(PASS)
    d.execute_script("arguments[0].click();",
        d.find_element(By.CSS_SELECTOR, "button[type=submit]"))
    time.sleep(3)
    return "Login" not in d.current_url

def run_test(name, fn):
    d = None
    try:
        d = get_driver()
        ok = login(d)
        if not ok:
            results.append(("FAIL", name, "Login failed"))
            return
        fn(d)
        results.append(("PASS", name, ""))
    except Exception as e:
        results.append(("FAIL", name, str(e)[:100]))
        if d:
            try: d.save_screenshot("/tmp/fail_" + name.replace(" ", "_") + ".png")
            except: pass
    finally:
        if d:
            try: d.quit()
            except: pass

def test_page_load(d):
    d.get(BASE + "/Inventario/InventarioFisico")
    time.sleep(3)
    assert d.find_element(By.ID, "txtCB").is_displayed()
    assert d.find_element(By.ID, "btnEscanear").is_displayed()
    assert "InventarioFisico" in d.current_url

def test_stats(d):
    d.get(BASE + "/Inventario/InventarioFisico")
    time.sleep(3)
    for sid in ["statHoy", "statTotal", "statSobrantes", "statCancelados"]:
        el = d.find_element(By.ID, sid)
        assert el.is_displayed(), sid + " not visible"
        assert el.text.strip() != "", sid + " empty"

def test_tabs(d):
    d.get(BASE + "/Inventario/InventarioFisico")
    time.sleep(3)
    tabs = d.find_elements(By.CSS_SELECTOR, ".nav-pills .nav-link")
    assert len(tabs) == 3, "Expected 3 tabs"
    active = d.find_element(By.CSS_SELECTOR, ".nav-pills .nav-link.active")
    assert "registros" in active.text.lower()

def test_tab_sobrantes(d):
    d.get(BASE + "/Inventario/InventarioFisico?Tab=sobrantes")
    time.sleep(3)
    hdr = d.find_element(By.CSS_SELECTOR, ".card-header.bg-warning")
    assert hdr.is_displayed()

def test_tab_faltantes(d):
    d.get(BASE + "/Inventario/InventarioFisico?Tab=faltantes")
    time.sleep(3)
    hdr = d.find_element(By.CSS_SELECTOR, ".card-header.bg-danger")
    assert hdr.is_displayed()

def test_scan_ajax(d):
    d.get(BASE + "/Inventario/InventarioFisico")
    time.sleep(3)
    inp = d.find_element(By.ID, "txtCB")
    inp.clear()
    inp.send_keys("TEST01")
    inp.send_keys(Keys.ENTER)
    time.sleep(3)
    fb = d.find_element(By.ID, "scanFeedback")
    # Feedback should be visible with some text
    for _ in range(10):
        if fb.is_displayed() and fb.text.strip():
            break
        time.sleep(0.5)
    assert fb.text.strip(), "Feedback empty after scan"

def test_export_link(d):
    d.get(BASE + "/Inventario/InventarioFisico")
    time.sleep(3)
    exp = d.find_element(By.CSS_SELECTOR, "a[href*='handler=Exportar']")
    assert exp.is_displayed()
    assert "Exportar" in exp.get_attribute("href")

def test_iniciar_modal(d):
    d.get(BASE + "/Inventario/InventarioFisico")
    time.sleep(3)
    btn = d.find_element(By.CSS_SELECTOR, "button[data-bs-target='#modalIniciar']")
    d.execute_script("arguments[0].click();", btn)
    time.sleep(1)
    modal = d.find_element(By.ID, "modalIniciar")
    assert modal.is_displayed(), "Modal not visible"
    warn = modal.find_element(By.CSS_SELECTOR, ".alert-warning")
    assert warn.is_displayed()

def test_search(d):
    d.get(BASE + "/Inventario/InventarioFisico")
    time.sleep(3)
    search = d.find_element(By.CSS_SELECTOR, "input[name='Buscar']")
    search.send_keys("167")
    d.execute_script("arguments[0].click();",
        d.find_element(By.CSS_SELECTOR, ".filter-panel button[type='submit']"))
    time.sleep(3)
    assert "Buscar" in d.current_url

def main():
    tests = [
        ("Page load + scan panel", test_page_load),
        ("Stats cards", test_stats),
        ("Tabs (3 tabs, Registros active)", test_tabs),
        ("Tab Sobrantes", test_tab_sobrantes),
        ("Tab Faltantes", test_tab_faltantes),
        ("Scan AJAX", test_scan_ajax),
        ("Export Excel link", test_export_link),
        ("Iniciar Inventario modal", test_iniciar_modal),
        ("Search", test_search),
    ]

    for name, fn in tests:
        run_test(name, fn)
        time.sleep(1)

    print("")
    print("=" * 60)
    passed = sum(1 for r in results if r[0] == "PASS")
    failed = sum(1 for r in results if r[0] == "FAIL")
    for status, name, err in results:
        line = status + " - " + name
        if err:
            line += " | " + err
        print(line)
    print("=" * 60)
    print("Resultado: " + str(passed) + "/" + str(len(results)) + " pasaron")
    if failed > 0:
        print(str(failed) + " test(s) fallaron")
    else:
        print("TODAS LAS PRUEBAS PASARON")
    return 1 if failed > 0 else 0

if __name__ == "__main__":
    sys.exit(main())
