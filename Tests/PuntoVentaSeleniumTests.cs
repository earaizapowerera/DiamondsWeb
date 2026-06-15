using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DiamondsWeb.Tests;

/// <summary>
/// Selenium tests for Punto de Venta (POS) — full sales flow.
/// Target: https://diamonds.dev.powerera.com/PuntoVenta
/// Tests run sequentially (shared DB state).
/// </summary>
public class PuntoVentaSeleniumTests : IDisposable
{
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("DIAMONDS_TEST_URL") ?? "https://diamonds.dev.powerera.com";
    private static readonly string LoginUser = Environment.GetEnvironmentVariable("DIAMONDS_TEST_USER") ?? "admin";
    private static readonly string LoginPass = Environment.GetEnvironmentVariable("DIAMONDS_TEST_PASS") ?? "admin";
    private const string CodigoPiezaSencilla = "000270";
    private const string CodigoPiezaRepetida = "003946";
    private static readonly int IdUsuarioDiamonds = int.TryParse(Environment.GetEnvironmentVariable("DIAMONDS_TEST_USERID"), out var id) ? id : 1;

    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public PuntoVentaSeleniumTests()
    {
        var opts = new ChromeOptions();
        opts.AddArgument("--headless=new");
        opts.AddArgument("--no-sandbox");
        opts.AddArgument("--disable-dev-shm-usage");
        opts.AddArgument("--disable-gpu");
        opts.AddArgument("--window-size=1920,1080");
        opts.AddArgument("--ignore-certificate-errors");
        opts.AcceptInsecureCertificates = true;

        _driver = new ChromeDriver(opts);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }

    public void Dispose()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    // ─── Helpers ──────────────────────────────────────────────────

    private void Login()
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Security/Auth/Login");
        WaitFor(By.Id("LoginViewModel_Username"));
        _driver.FindElement(By.Id("LoginViewModel_Username")).SendKeys(LoginUser);
        _driver.FindElement(By.Id("LoginViewModel_Password")).SendKeys(LoginPass);
        _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
        _wait.Until(d => !d.Url.Contains("/Login"));
    }

    private void NavigatePOS()
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/PuntoVenta");
        WaitFor(By.Id("txtUsuario"));
    }

    private IWebElement WaitFor(By by)
    {
        return _wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(by);
                return el.Displayed ? el : null;
            }
            catch (NoSuchElementException) { return null; }
        })!;
    }

    private IWebElement WaitForText(By by, Func<string, bool> predicate)
    {
        return _wait.Until(d =>
        {
            try
            {
                var el = d.FindElement(by);
                return el.Displayed && predicate(el.Text) ? el : null;
            }
            catch (NoSuchElementException) { return null; }
        })!;
    }

    private void CrearSesion()
    {
        var txtUsuario = _driver.FindElement(By.Id("txtUsuario"));
        txtUsuario.Clear();
        txtUsuario.SendKeys(IdUsuarioDiamonds.ToString());
        _driver.FindElement(By.Id("btnNuevaSesion")).Click();
        Thread.Sleep(3000);

        // Check for error alert
        var alerts = _driver.FindElements(By.CssSelector(".alert-danger"));
        if (alerts.Count > 0)
            throw new Exception($"CrearSesion error: {alerts[0].Text}");

        // Check JS console for errors
        var logs = ((IJavaScriptExecutor)_driver).ExecuteScript(
            "return document.getElementById('lblUsuario')?.textContent || 'EMPTY'");
        var lblText = logs?.ToString() ?? "NULL";

        if (string.IsNullOrWhiteSpace(lblText) || lblText == "EMPTY")
        {
            // Try waiting a bit more — the AJAX might be slow
            Thread.Sleep(3000);
            lblText = ((IJavaScriptExecutor)_driver).ExecuteScript(
                "return document.getElementById('lblUsuario')?.textContent || 'EMPTY'")?.ToString() ?? "NULL";
        }

        if (string.IsNullOrWhiteSpace(lblText) || lblText == "EMPTY")
            throw new Exception($"CrearSesion: lblUsuario never populated. URL={_driver.Url}");
    }

    private void CancelarSesionSiExiste()
    {
        Thread.Sleep(500);
        try
        {
            // Check if cancel button exists and is enabled
            var btns = _driver.FindElements(By.Id("btnCancelar"));
            if (btns.Count > 0 && btns[0].Enabled)
            {
                // Override confirm dialog and click cancel
                ((IJavaScriptExecutor)_driver).ExecuteScript(
                    "window.confirm = () => true;");
                btns[0].Click();
                Thread.Sleep(2000);
            }
        }
        catch { }
    }

    private void EscanearPieza(string codigo)
    {
        var txtCB = _driver.FindElement(By.Id("txtCodigoBarras"));
        txtCB.Clear();
        txtCB.SendKeys(codigo);
        txtCB.SendKeys(Keys.Enter);
        Thread.Sleep(1000);
    }

    // ═══════════════════════════════════════════════════════════════
    //  TESTS
    // ═══════════════════════════════════════════════════════════════

    [Fact]
    public void T01_PaginaPOSCargaCorrectamente()
    {
        Login();
        NavigatePOS();

        Assert.Contains("Punto de Venta", _driver.Title);
        Assert.True(_driver.FindElement(By.Id("txtUsuario")).Displayed);
        Assert.True(_driver.FindElement(By.Id("btnNuevaSesion")).Displayed);
        Assert.True(_driver.FindElement(By.Id("txtCodigoBarras")).Displayed);
        Assert.True(_driver.FindElement(By.Id("tblPiezas")).Displayed);
        Assert.True(_driver.FindElement(By.Id("tblPagos")).Displayed);
    }

    [Fact]
    public void T02_CrearNuevaSesion()
    {
        Login();
        NavigatePOS();
        CancelarSesionSiExiste();

        CrearSesion();

        var lbl = _driver.FindElement(By.Id("lblUsuario"));
        Assert.False(string.IsNullOrWhiteSpace(lbl.Text), "lblUsuario should show the user name");

        var txtCB = _driver.FindElement(By.Id("txtCodigoBarras"));
        Assert.False(txtCB.GetDomProperty("disabled") == "true", "Barcode input should be enabled");

        CancelarSesionSiExiste();
    }

    [Fact]
    public void T03_AgregarPiezaSencilla()
    {
        Login();
        NavigatePOS();
        CancelarSesionSiExiste();
        CrearSesion();

        EscanearPieza(CodigoPiezaSencilla);

        var row = _wait.Until(d =>
        {
            var rows = d.FindElements(By.CssSelector("#tbodyPiezas .pieza-row"));
            return rows.Count > 0 ? rows[0] : null;
        });

        Assert.NotNull(row);
        Assert.Contains(CodigoPiezaSencilla, row!.Text);

        var txtTotal = _driver.FindElement(By.Id("txtTotal"));
        var total = txtTotal.GetDomProperty("value");
        Assert.False(string.IsNullOrWhiteSpace(total), "Total should have a value");

        CancelarSesionSiExiste();
    }

    [Fact]
    public void T04_AgregarPiezaRepetida()
    {
        Login();
        NavigatePOS();
        CancelarSesionSiExiste();
        CrearSesion();

        EscanearPieza(CodigoPiezaRepetida);

        // For repetida, the quantity prompt should appear
        var divCantidad = _wait.Until(d =>
        {
            try
            {
                var div = d.FindElement(By.Id("lblRepetidaCantidad"));
                return div.Displayed ? div : null;
            }
            catch { return null; }
        });

        if (divCantidad != null)
        {
            var txtCant = _driver.FindElement(By.Id("txtCantidadRepetida"));
            txtCant.Clear();
            txtCant.SendKeys("3");
            _driver.FindElement(By.Id("btnConfirmarRepetida")).Click();
            Thread.Sleep(1000);
        }

        var rows = _driver.FindElements(By.CssSelector("#tbodyPiezas .pieza-row"));
        Assert.True(rows.Count > 0, "At least one piece should be in the grid");

        CancelarSesionSiExiste();
    }

    [Fact]
    public void T05_EliminarPieza()
    {
        Login();
        NavigatePOS();
        CancelarSesionSiExiste();
        CrearSesion();

        EscanearPieza(CodigoPiezaSencilla);
        _wait.Until(d => d.FindElements(By.CssSelector("#tbodyPiezas .pieza-row")).Count > 0);

        var btnEliminar = _driver.FindElement(By.CssSelector("#tbodyPiezas .btn-eliminar-pieza"));
        btnEliminar.Click();
        Thread.Sleep(1000);

        var rows = _driver.FindElements(By.CssSelector("#tbodyPiezas .pieza-row"));
        Assert.Empty(rows);

        CancelarSesionSiExiste();
    }

    [Fact]
    public void T06_AplicarDescuento()
    {
        Login();
        NavigatePOS();
        CancelarSesionSiExiste();
        CrearSesion();

        EscanearPieza(CodigoPiezaSencilla);
        _wait.Until(d => d.FindElements(By.CssSelector("#tbodyPiezas .pieza-row")).Count > 0);
        Thread.Sleep(500);

        var txtTotal = _driver.FindElement(By.Id("txtTotal"));
        var originalTotal = txtTotal.GetDomProperty("value");

        var txtDescuento = _driver.FindElement(By.Id("txtDescuento"));
        txtDescuento.Clear();
        txtDescuento.SendKeys("10");
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.getElementById('txtDescuento').dispatchEvent(new Event('change'));");
        Thread.Sleep(1000);

        var newTotal = txtTotal.GetDomProperty("value");
        Assert.NotEqual(originalTotal, newTotal);

        CancelarSesionSiExiste();
    }

    [Fact]
    public void T07_RegistrarPago()
    {
        Login();
        NavigatePOS();
        CancelarSesionSiExiste();
        CrearSesion();

        EscanearPieza(CodigoPiezaSencilla);
        _wait.Until(d => d.FindElements(By.CssSelector("#tbodyPiezas .pieza-row")).Count > 0);
        Thread.Sleep(500);

        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.dispatchEvent(new KeyboardEvent('keydown', {key: 'F5', cancelable: true}));");
        Thread.Sleep(500);

        var modal = WaitFor(By.CssSelector("#modalPago.show"));
        Assert.NotNull(modal);

        var btnPago = _wait.Until(d =>
        {
            var btns = d.FindElements(By.CssSelector("#paymentGrid .btn-pago-opcion"));
            return btns.Count > 0 ? btns[0] : null;
        });
        btnPago!.Click();
        Thread.Sleep(300);

        var pagoImporte = _driver.FindElement(By.Id("pagoImporte"));
        pagoImporte.Clear();
        pagoImporte.SendKeys("100");

        _driver.FindElement(By.Id("btnRegistrarPago")).Click();
        Thread.Sleep(1000);

        var pagosRows = _driver.FindElements(By.CssSelector("#tbodyPagos .pago-row"));
        Assert.True(pagosRows.Count > 0, "At least one payment should appear in grid");

        CancelarSesionSiExiste();
    }

    [Fact]
    public void T08_FlujoCompletoDeVenta()
    {
        Login();
        NavigatePOS();
        CancelarSesionSiExiste();

        // 1. Create session
        CrearSesion();

        // 2. Set date
        var dtFecha = _driver.FindElement(By.Id("dtFechaBaja"));
        dtFecha.Clear();
        dtFecha.SendKeys(DateTime.Now.ToString("MM/dd/yyyy"));
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.getElementById('dtFechaBaja').dispatchEvent(new Event('change'));");

        // 3. Add piece
        EscanearPieza(CodigoPiezaSencilla);
        _wait.Until(d => d.FindElements(By.CssSelector("#tbodyPiezas .pieza-row")).Count > 0);
        Thread.Sleep(500);

        // 4. Set client name
        var txtNombre = _driver.FindElement(By.Id("txtNombre"));
        txtNombre.Clear();
        txtNombre.SendKeys("CLIENTE PRUEBA SELENIUM");
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.getElementById('txtNombre').dispatchEvent(new Event('blur'));");
        Thread.Sleep(300);

        // 5. Get total
        var txtTotal = _driver.FindElement(By.Id("txtTotal"));
        var totalStr = txtTotal.GetDomProperty("value")!.Replace(",", "");
        var total = decimal.Parse(totalStr, System.Globalization.CultureInfo.InvariantCulture);

        // 6. Open payment modal and pay the full amount
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.dispatchEvent(new KeyboardEvent('keydown', {key: 'F5', cancelable: true}));");
        Thread.Sleep(500);

        WaitFor(By.CssSelector("#modalPago.show"));
        var firstPayBtn = _wait.Until(d =>
        {
            var btns = d.FindElements(By.CssSelector("#paymentGrid .btn-pago-opcion"));
            return btns.Count > 0 ? btns[0] : null;
        });
        firstPayBtn!.Click();
        Thread.Sleep(300);

        var pagoImporte = _driver.FindElement(By.Id("pagoImporte"));
        pagoImporte.Clear();
        pagoImporte.SendKeys(total.ToString("F2"));

        _driver.FindElement(By.Id("btnRegistrarPago")).Click();
        Thread.Sleep(1500);

        // 7. Handle the auto "close note" confirm dialog
        try
        {
            var alert = _driver.SwitchTo().Alert();
            alert.Accept();
            Thread.Sleep(2000);
        }
        catch (NoAlertPresentException)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "document.dispatchEvent(new KeyboardEvent('keydown', {key: 'F2', cancelable: true}));");
            Thread.Sleep(500);
            try
            {
                var btnConfirmar = WaitFor(By.Id("btnConfirmarCerrar"));
                btnConfirmar.Click();
                Thread.Sleep(2000);
            }
            catch { }
        }

        // 8. Verify success
        Thread.Sleep(1000);
        var successAlert = _driver.FindElements(By.CssSelector(".alert-success"));
        var lblUsuario = _driver.FindElement(By.Id("lblUsuario"));
        var closed = successAlert.Count > 0 || string.IsNullOrWhiteSpace(lblUsuario.Text);
        Assert.True(closed, "Note should be closed (success alert or session reset)");
    }

    [Fact]
    public void T09_TeclasDeFuncion()
    {
        Login();
        NavigatePOS();
        CancelarSesionSiExiste();
        CrearSesion();

        // F4 -> focus barcode
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.dispatchEvent(new KeyboardEvent('keydown', {key: 'F4', cancelable: true}));");
        Thread.Sleep(300);
        var activeId = ((IJavaScriptExecutor)_driver).ExecuteScript(
            "return document.activeElement?.id;")?.ToString();
        Assert.Equal("txtCodigoBarras", activeId);

        // F7 -> focus client name
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.dispatchEvent(new KeyboardEvent('keydown', {key: 'F7', cancelable: true}));");
        Thread.Sleep(300);
        activeId = ((IJavaScriptExecutor)_driver).ExecuteScript(
            "return document.activeElement?.id;")?.ToString();
        Assert.Equal("txtNombre", activeId);

        // F8 -> focus sessions
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.dispatchEvent(new KeyboardEvent('keydown', {key: 'F8', cancelable: true}));");
        Thread.Sleep(300);
        activeId = ((IJavaScriptExecutor)_driver).ExecuteScript(
            "return document.activeElement?.id;")?.ToString();
        Assert.Equal("cmbSesion", activeId);

        // Ctrl+D -> focus discount
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.dispatchEvent(new KeyboardEvent('keydown', {key: 'd', ctrlKey: true, cancelable: true}));");
        Thread.Sleep(300);
        activeId = ((IJavaScriptExecutor)_driver).ExecuteScript(
            "return document.activeElement?.id;")?.ToString();
        Assert.Equal("txtDescuento", activeId);

        CancelarSesionSiExiste();
    }

    [Fact]
    public void T10_DescuentoMaximo20Porciento()
    {
        Login();
        NavigatePOS();
        CancelarSesionSiExiste();
        CrearSesion();

        EscanearPieza(CodigoPiezaSencilla);
        _wait.Until(d => d.FindElements(By.CssSelector("#tbodyPiezas .pieza-row")).Count > 0);
        Thread.Sleep(500);

        var txtDescuento = _driver.FindElement(By.Id("txtDescuento"));
        txtDescuento.Clear();
        txtDescuento.SendKeys("25");
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "document.getElementById('txtDescuento').dispatchEvent(new Event('change'));");
        Thread.Sleep(1000);

        var alerts = _driver.FindElements(By.CssSelector(".alert-danger"));
        Assert.True(alerts.Count > 0, "An error alert should appear when discount > 20%");

        var descValue = txtDescuento.GetDomProperty("value");
        Assert.Equal("20", descValue);

        CancelarSesionSiExiste();
    }
}
