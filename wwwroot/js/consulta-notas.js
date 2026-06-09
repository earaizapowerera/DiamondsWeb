// Consulta de Notas - JavaScript
// Master-detail: click en fila carga detalle via AJAX

(function () {
    'use strict';

    var panelDetalle = document.getElementById('panelDetalle');
    var lblNotaDetalle = document.getElementById('lblNotaDetalle');
    var tblPiezas = document.querySelector('#tblPiezas tbody');
    var tblPagos = document.querySelector('#tblPagos tbody');
    var tblTotales = document.querySelector('#tblTotales tbody');
    var filaActiva = null;

    // Toggle panel de filtros de pieza
    var btnToggle = document.getElementById('btnTogglePiezas');
    var filtrosPieza = document.getElementById('filtrosPieza');
    if (btnToggle && filtrosPieza) {
        // Mostrar si hay algun filtro de pieza activo
        var inputs = filtrosPieza.querySelectorAll('input');
        var tieneValor = false;
        inputs.forEach(function (input) {
            if (input.value) tieneValor = true;
        });
        if (tieneValor) filtrosPieza.style.display = 'block';

        btnToggle.addEventListener('click', function () {
            var visible = filtrosPieza.style.display !== 'none';
            filtrosPieza.style.display = visible ? 'none' : 'block';
            btnToggle.classList.toggle('btn-outline-secondary', visible);
            btnToggle.classList.toggle('btn-secondary', !visible);
        });
    }

    // Click en fila de nota -> cargar detalle
    var filas = document.querySelectorAll('.fila-nota');
    filas.forEach(function (fila) {
        fila.addEventListener('click', function () {
            var idNota = this.getAttribute('data-idnota');
            cargarDetalle(idNota, this);
        });
    });

    function cargarDetalle(idNota, filaElement) {
        // Highlight fila activa
        if (filaActiva) filaActiva.classList.remove('table-active');
        filaElement.classList.add('table-active');
        filaActiva = filaElement;

        // Mostrar loading
        if (panelDetalle) panelDetalle.style.display = 'block';
        if (lblNotaDetalle) lblNotaDetalle.textContent = '#' + idNota;
        limpiarTablas();
        mostrarLoading();

        fetch('/ConsultaNotas?handler=Detalle&idNota=' + idNota, {
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (res) { return res.json(); })
        .then(function (data) {
            if (data.error) {
                mostrarError(data.error);
                return;
            }
            renderPiezas(data.piezas);
            renderPagos(data.pagos);
            renderTotales(data.totales);

            // Scroll al detalle
            panelDetalle.scrollIntoView({ behavior: 'smooth', block: 'start' });
        })
        .catch(function (err) {
            mostrarError('Error de conexion: ' + err.message);
        });
    }

    function limpiarTablas() {
        if (tblPiezas) tblPiezas.innerHTML = '';
        if (tblPagos) tblPagos.innerHTML = '';
        if (tblTotales) tblTotales.innerHTML = '';
    }

    function mostrarLoading() {
        var loadingHtml = '<tr><td colspan="8" class="text-center text-muted py-3">' +
            '<i class="fa-solid fa-spinner fa-spin me-2"></i>Cargando...</td></tr>';
        if (tblPiezas) tblPiezas.innerHTML = loadingHtml;
        if (tblPagos) tblPagos.innerHTML = loadingHtml.replace('8', '5');
        if (tblTotales) tblTotales.innerHTML = loadingHtml.replace('8', '2');
    }

    function mostrarError(msg) {
        var html = '<tr><td colspan="8" class="text-center text-danger py-3">' +
            '<i class="fa-solid fa-exclamation-circle me-2"></i>' + msg + '</td></tr>';
        if (tblPiezas) tblPiezas.innerHTML = html;
    }

    function renderPiezas(piezas) {
        if (!tblPiezas) return;
        if (!piezas || piezas.length === 0) {
            tblPiezas.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">Sin piezas</td></tr>';
            return;
        }
        var html = '';
        piezas.forEach(function (p) {
            html += '<tr>' +
                '<td><code>' + esc(p.codigoBarras) + '</code></td>' +
                '<td>' + esc(p.descripcion) + '</td>' +
                '<td class="text-end">' + (p.cantidad || '') + '</td>' +
                '<td class="text-end">' + fmtMoney(p.subtotal) + '</td>' +
                '<td class="text-end fw-bold">' + fmtMoney(p.total) + '</td>' +
                '<td>' + esc(p.proveedor) + '</td>' +
                '<td class="text-end">' + fmtMoney(p.cnTotal) + '</td>' +
                '<td class="text-end">' + fmtMoney(p.precio) + '</td>' +
                '</tr>';
        });
        tblPiezas.innerHTML = html;
    }

    function renderPagos(pagos) {
        if (!tblPagos) return;
        if (!pagos || pagos.length === 0) {
            tblPagos.innerHTML = '<tr><td colspan="5" class="text-center text-muted py-3">Sin pagos</td></tr>';
            return;
        }
        var html = '';
        pagos.forEach(function (p) {
            html += '<tr>' +
                '<td>' + esc(p.opcionPago) + '</td>' +
                '<td class="text-end fw-bold">' + fmtMoney(p.importe) + '</td>' +
                '<td class="text-end">' + (p.tipoCambio || '') + '</td>' +
                '<td class="text-end">' + fmtMoney(p.importeOriginal) + '</td>' +
                '<td>' + fmtDate(p.fechaCaptura) + '</td>' +
                '</tr>';
        });
        tblPagos.innerHTML = html;
    }

    function renderTotales(totales) {
        if (!tblTotales) return;
        if (!totales || totales.length === 0) {
            tblTotales.innerHTML = '<tr><td colspan="2" class="text-center text-muted py-3">Sin datos</td></tr>';
            return;
        }
        var html = '';
        totales.forEach(function (t) {
            html += '<tr>' +
                '<td>' + esc(t.moneda) + '</td>' +
                '<td class="text-end fw-bold">' + fmtMoney(t.costoNeto) + '</td>' +
                '</tr>';
        });
        tblTotales.innerHTML = html;
    }

    // Helpers
    function esc(val) {
        if (val == null) return '';
        var div = document.createElement('div');
        div.appendChild(document.createTextNode(val));
        return div.innerHTML;
    }

    function fmtMoney(val) {
        if (val == null) return '';
        return new Intl.NumberFormat('es-MX', {
            style: 'currency', currency: 'MXN', minimumFractionDigits: 2
        }).format(val);
    }

    function fmtDate(val) {
        if (!val) return '';
        var d = new Date(val);
        return d.toLocaleDateString('es-MX', {
            day: '2-digit', month: '2-digit', year: 'numeric'
        });
    }
})();

// Confirmacion para cancelar nota (doble confirmacion como en VB6)
function confirmarCancelacion(idNota) {
    if (!confirm('Esta seguro de cancelar la nota #' + idNota + '?')) return false;
    return confirm('IMPORTANTE: Al cancelar esta nota se reabrira la sesion de baja. ' +
        'Las piezas podrian reingresar al inventario. Desea continuar?');
}
