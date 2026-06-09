/**
 * lotes-repetidas.js — Lógica client-side para Alta de Lotes de Piezas Repetidas.
 * Migración de frmLotesRepetidas.frm (VB6).
 *
 * Fórmula de precio:
 *   Precio = CostoNeto × Utilidad × UtilidadExtra × Impuesto / Divisor × TCCotizacion
 *
 * Fórmula de costos:
 *   CostoBruto = Peso × PrecioGramo
 *   CostoNeto = CostoBruto × (1 - Descuento/100)
 */
(function () {
    'use strict';

    // ─── Elementos DOM ───────────────────────────────────────────
    var el = {
        codigoBarras: document.getElementById('txtCodigoBarras'),
        descripcion: document.getElementById('txtDescripcionPieza'),
        precioActual: document.getElementById('txtPrecioActual'),
        cantidad: document.getElementById('txtCantidad'),
        peso: document.getElementById('txtPeso'),
        precioGramo: document.getElementById('txtPrecioGramo'),
        costoBruto: document.getElementById('txtCostoBruto'),
        descuento: document.getElementById('txtDescuento'),
        costoNeto: document.getElementById('txtCostoNeto'),
        utilidad: document.getElementById('txtUtilidad'),
        utilidadExtra: document.getElementById('txtUtilidadExtra'),
        impuesto: document.getElementById('txtImpuesto'),
        divisor: document.getElementById('txtDivisor'),
        tcCotizacion: document.getElementById('txtTCCotizacion'),
        tcCosto: document.getElementById('txtTCCosto'),
        tcCotizacionHidden: document.getElementById('txtTCCotizacionHidden'),
        selMoneda: document.getElementById('selMoneda'),
        precioCalculado: document.getElementById('txtPrecioCalculado'),
        formulaDetalle: document.getElementById('formulaDetalle'),
        seccionAlta: document.getElementById('seccionAltaPieza'),
        form: document.getElementById('formAgregarPieza'),
        btnNueva: document.getElementById('btnNuevaPieza'),
        btnCancelar: document.getElementById('btnCancelarPieza'),
        btnCancelar2: document.getElementById('btnCancelarPieza2'),
        btnBuscarCodigo: document.getElementById('btnBuscarCodigo'),
        btnBuscarRemision: document.getElementById('btnEjecutarBusqueda'),
        txtBuscarRemision: document.getElementById('txtBuscarRemision'),
        bodyRemisiones: document.getElementById('bodyRemisiones'),
        selProveedorRemision: document.getElementById('selProveedorRemision')
    };

    // Estado del proveedor actual
    var proveedorDefaults = null;
    var rangosUtilidadExtra = [];

    // ─── Cálculos de Precio (migrado de VB6) ─────────────────────

    function num(input) {
        return parseFloat(input.value) || 0;
    }

    /**
     * Calcula CostoBruto y CostoNeto a partir de Peso, PrecioGramo y Descuento.
     * VB6: CalcularPesoNeto()
     */
    function calcularCostos() {
        var peso = num(el.peso);
        var precioGramo = num(el.precioGramo);
        var descuento = num(el.descuento);

        var costoBruto = peso * precioGramo;
        var costoNeto = costoBruto * (1 - (descuento * 0.01));

        el.costoBruto.value = costoBruto.toFixed(2);
        el.costoNeto.value = costoNeto.toFixed(2);

        calcularPrecio();
    }

    /**
     * Calcula el precio de venta.
     * VB6: CalcularPrecio()
     * Precio = CostoNeto × Utilidad × UtilidadExtra × Impuesto / Divisor × TCCotizacion
     */
    function calcularPrecio() {
        var costoNeto = num(el.costoNeto);
        var utilidad = num(el.utilidad) || 1;
        var utilidadExtra = num(el.utilidadExtra) || 1;
        var impuesto = num(el.impuesto) || 1;
        var divisor = num(el.divisor) || 1;
        var tcCotizacion = num(el.tcCotizacion) || 1;

        if (divisor === 0) divisor = 1;

        var precio = costoNeto * utilidad * utilidadExtra * impuesto / divisor * tcCotizacion;

        el.precioCalculado.value = Math.round(precio).toLocaleString('es-MX');

        // Sincronizar TC Cotización al campo hidden del form
        el.tcCotizacionHidden.value = el.tcCotizacion.value;

        // Mostrar detalle de fórmula
        if (el.formulaDetalle) {
            el.formulaDetalle.textContent =
                costoNeto.toFixed(2) + ' x ' + utilidad + ' x ' + utilidadExtra +
                ' x ' + impuesto + ' / ' + divisor + ' x ' + tcCotizacion +
                ' = ' + Math.round(precio);
        }
    }

    /**
     * Calcula la utilidad extra dinámica basada en precio/gramo.
     * VB6: txtPrecioGramo_LostFocus() → utilidadextra_preciogramo lookup
     */
    function calcularUtilidadExtraDinamica() {
        if (!proveedorDefaults || !proveedorDefaults.utilidadExtra) return;

        var precioGramo = num(el.precioGramo);
        var tcCotizacion = num(el.tcCotizacion);
        var precioGramoConvertido = precioGramo * tcCotizacion;

        for (var i = 0; i < rangosUtilidadExtra.length; i++) {
            var rango = rangosUtilidadExtra[i];
            if (precioGramoConvertido >= rango.desde && precioGramoConvertido <= rango.hasta) {
                el.utilidadExtra.value = rango.utilidad;
                el.utilidadExtra.classList.add('bg-info-subtle');
                return;
            }
        }
    }

    // ─── Event Listeners de Cálculos ─────────────────────────────

    // Campos que recalculan costos (Peso, PrecioGramo, Descuento)
    [el.peso, el.precioGramo, el.descuento].forEach(function (input) {
        if (!input) return;
        input.addEventListener('input', calcularCostos);
        input.addEventListener('change', function () {
            calcularUtilidadExtraDinamica();
            calcularCostos();
        });
    });

    // CostoNeto manual → solo recalcula precio
    if (el.costoNeto) {
        el.costoNeto.addEventListener('input', calcularPrecio);
    }

    // Factores de precio
    [el.utilidad, el.utilidadExtra, el.impuesto, el.divisor, el.tcCotizacion].forEach(function (input) {
        if (!input) return;
        input.addEventListener('input', calcularPrecio);
    });

    // ─── Búsqueda de Código de Barras ────────────────────────────

    function buscarCodigoBarras() {
        var codigo = el.codigoBarras.value.trim();
        if (!codigo) return;

        fetch(window.LotesConfig.pageUrl + '?handler=BuscarCatalogo&codigo=' + encodeURIComponent(codigo))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.found) {
                    el.descripcion.value = data.descripcion;
                    el.precioActual.value = data.precio;
                } else {
                    el.descripcion.value = '';
                    el.precioActual.value = '';
                    alert('El código de barras no es de una pieza repetida.');
                }
            })
            .catch(function (err) {
                console.error('Error buscando código:', err);
            });
    }

    if (el.codigoBarras) {
        el.codigoBarras.addEventListener('blur', buscarCodigoBarras);
    }
    if (el.btnBuscarCodigo) {
        el.btnBuscarCodigo.addEventListener('click', buscarCodigoBarras);
    }

    // ─── Cambio de Moneda ────────────────────────────────────────

    if (el.selMoneda) {
        el.selMoneda.addEventListener('change', function () {
            var idMoneda = this.value;
            if (!idMoneda) return;

            fetch(window.LotesConfig.pageUrl + '?handler=TipoCambio&idMoneda=' + idMoneda)
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    el.tcCotizacion.value = data.tipoCambioCotizacion || 1;
                    el.tcCotizacionHidden.value = el.tcCotizacion.value;
                    calcularUtilidadExtraDinamica();
                    calcularPrecio();
                });
        });
    }

    // ─── Mostrar/Ocultar sección de alta ─────────────────────────

    function mostrarAltaPieza() {
        if (el.seccionAlta) {
            el.seccionAlta.classList.add('show');
            if (el.codigoBarras) el.codigoBarras.focus();
        }
    }

    function ocultarAltaPieza() {
        if (el.seccionAlta) {
            el.seccionAlta.classList.remove('show');
        }
    }

    function resetFormPieza() {
        if (el.form) el.form.reset();
        if (el.descripcion) el.descripcion.value = '';
        if (el.precioActual) el.precioActual.value = '';
        if (el.precioCalculado) el.precioCalculado.value = '';
        if (el.formulaDetalle) el.formulaDetalle.textContent = '';
        // Restaurar defaults
        el.impuesto.value = window.LotesConfig.defaultImpuesto;
        el.divisor.value = window.LotesConfig.defaultDivisor;
        // Restaurar IdRemision/IdFactura
        var hiddenRemision = el.form.querySelector('[name="pieza.IdRemision"]');
        var hiddenFactura = el.form.querySelector('[name="pieza.IdFactura"]');
        if (hiddenRemision) hiddenRemision.value = window.LotesConfig.idRemision || '';
        if (hiddenFactura) hiddenFactura.value = window.LotesConfig.idFactura || '';
    }

    if (el.btnNueva) {
        el.btnNueva.addEventListener('click', function () {
            resetFormPieza();
            mostrarAltaPieza();
        });
    }
    if (el.btnCancelar) {
        el.btnCancelar.addEventListener('click', function () {
            ocultarAltaPieza();
        });
    }
    if (el.btnCancelar2) {
        el.btnCancelar2.addEventListener('click', function () {
            ocultarAltaPieza();
        });
    }

    // ─── Búsqueda de Remisiones (modal) ──────────────────────────

    function buscarRemisiones() {
        var filtro = el.txtBuscarRemision ? el.txtBuscarRemision.value : '';
        fetch(window.LotesConfig.pageUrl + '?handler=BuscarRemisiones&filtro=' + encodeURIComponent(filtro))
            .then(function (r) { return r.json(); })
            .then(function (data) {
                var html = '';
                if (data.length === 0) {
                    html = '<tr><td colspan="6" class="text-center text-muted">Sin resultados</td></tr>';
                } else {
                    data.forEach(function (r) {
                        html += '<tr>' +
                            '<td>' + r.idRemision + '</td>' +
                            '<td>' + (r.nombreProveedor || '') + '</td>' +
                            '<td>' + (r.numRemision || '') + '</td>' +
                            '<td>' + (r.fechaRemision || '') + '</td>' +
                            '<td>' + (r.consignacion ? 'Si' : 'No') + '</td>' +
                            '<td><a href="' + window.LotesConfig.pageUrl + '?IdRemision=' + r.idRemision +
                            '" class="btn btn-sm btn-primary">Seleccionar</a></td>' +
                            '</tr>';
                    });
                }
                el.bodyRemisiones.innerHTML = html;
            });
    }

    if (el.btnBuscarRemision) {
        el.btnBuscarRemision.addEventListener('click', buscarRemisiones);
    }
    if (el.txtBuscarRemision) {
        el.txtBuscarRemision.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); buscarRemisiones(); }
        });
    }

    // Cargar remisiones al abrir modal
    var modalBuscar = document.getElementById('modalBuscarRemision');
    if (modalBuscar) {
        modalBuscar.addEventListener('shown.bs.modal', function () {
            buscarRemisiones();
            if (el.txtBuscarRemision) el.txtBuscarRemision.focus();
        });
    }

    // ─── Inicializar TomSelect en dropdowns con > 5 opciones ─────

    if (typeof TomSelect !== 'undefined') {
        if (el.selProveedorRemision) {
            new TomSelect(el.selProveedorRemision, {
                allowEmptyOption: true,
                sortField: { field: 'text', direction: 'asc' }
            });
        }
        if (el.selMoneda) {
            new TomSelect(el.selMoneda, {
                allowEmptyOption: true,
                sortField: { field: 'text', direction: 'asc' }
            });
        }
    }

    // ─── Atajos de Teclado (F1, F12, Esc) ────────────────────────

    document.addEventListener('keydown', function (e) {
        // F1 = Nueva Pieza
        if (e.key === 'F1') {
            e.preventDefault();
            if (el.btnNueva) el.btnNueva.click();
        }
        // F12 = Aceptar (submit)
        if (e.key === 'F12') {
            e.preventDefault();
            var btnAceptar = document.getElementById('btnAceptar');
            if (btnAceptar && el.seccionAlta && el.seccionAlta.classList.contains('show')) {
                btnAceptar.click();
            }
        }
        // Escape = Cancelar
        if (e.key === 'Escape') {
            ocultarAltaPieza();
        }
    });

    // ─── Carga inicial: Si hay remisión activa, preparar estado ──

    if (window.LotesConfig.idRemision) {
        // La remisión ya está cargada, los botones están habilitados
    }

})();
