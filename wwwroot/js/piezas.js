// ============================================================
// piezas.js — Calculos de precio para Alta de Piezas Sencillas
// Migrado de frmSencillas.frm (VB6)
// Formula: Precio = CNTotal * Utilidad * UtilidadExtra * Impuesto / Divisor * TCCotizacion
// ============================================================

function val(id) {
    var el = document.getElementById(id);
    if (!el) return 0;
    var v = parseFloat(el.value);
    return isNaN(v) ? 0 : v;
}

function setVal(id, v) {
    var el = document.getElementById(id);
    if (el) el.value = (typeof v === 'number') ? parseFloat(v.toFixed(4)) : v;
}

// ==================== CALCULOS DE COSTOS ====================

/**
 * Calcula todos los costos netos y totales.
 * Equivalente a CalcularCostos + CalcularPiezaNeto + CalcularPesoNeto + CalcularMONeto del VB6.
 */
function calcularCostos() {
    // Por Pieza: CNPieza = CBPieza * (1 - DescPieza/100)
    var cbPieza = val('cbPieza');
    var descPieza = val('descPieza');
    var cnPieza = cbPieza * (1 - descPieza / 100);
    setVal('cnPieza', cnPieza);

    // Por Peso: CBPeso = Peso * PrecioGramo, CNPeso = CBPeso * (1 - DescPeso/100)
    var peso = val('peso');
    var precioGramo = val('precioGramo');
    var cbPeso = peso * precioGramo;
    setVal('cbPeso', cbPeso);
    var descPeso = val('descPeso');
    var cnPeso = cbPeso * (1 - descPeso / 100);
    setVal('cnPeso', cnPeso);

    // Mano de Obra: CNManoObra = CBManoObra * (1 - DescManoObra/100)
    var cbManoObra = val('cbManoObra');
    var descManoObra = val('descManoObra');
    var cnManoObra = cbManoObra * (1 - descManoObra / 100);
    setVal('cnManoObra', cnManoObra);

    // Totales
    var cbTotal = cbPieza + cbPeso + cbManoObra;
    var cnTotal = cnPieza + cnPeso + cnManoObra;
    setVal('cbTotal', cbTotal);
    setVal('cnTotal', cnTotal);

    // Factura (si TCCosto > 0, auto-calcular)
    var tcCosto = val('tcCosto');
    if (tcCosto > 0) {
        var cbFactura = (cbPieza + cbPeso) * tcCosto;
        var cnFactura = (cnPieza + cnPeso) * tcCosto;
        setVal('cbFactura', cbFactura);
        setVal('cnFactura', cnFactura);
        if (cbFactura > 0)
            setVal('descFactura', (1 - cnFactura / cbFactura) * 100);
    } else {
        // Manual: CNFactura = CBFactura * (1 - DescFactura/100)
        var cbFactura2 = val('cbFactura');
        var descFactura = val('descFactura');
        var cnFactura2 = cbFactura2 * (1 - descFactura / 100);
        setVal('cnFactura', cnFactura2);
    }

    calcularPrecio();
}

/**
 * Calcula el precio final de venta.
 * Equivalente a CalcularPrecio del VB6.
 * Precio = CostoNeto * Utilidad * UtilidadExtra * Impuesto / Divisor * TCCotizacion
 */
function calcularPrecio() {
    var cnTotal = val('cnTotal');
    var utilidad = val('utilidad') || 1;
    var utilidadExtra = val('utilidadExtra') || 1;
    var impuesto = val('impuesto') || 1;
    var divisor = val('divisorValor') || 1;
    var tcCotizacion = val('tcCotizacion') || 1;

    var precio = cnTotal * utilidad * utilidadExtra * impuesto / divisor * tcCotizacion;
    var precioRedondeado = Math.round(precio);
    setVal('precio', precioRedondeado);

    // Actualizar formula visible
    actualizarFormulaDisplay(cnTotal, utilidad, utilidadExtra, impuesto, divisor, tcCotizacion, precioRedondeado);
}

function actualizarFormulaDisplay(cn, u, ue, imp, div, tc, precio) {
    var el = function(id) { return document.getElementById(id); };
    if (el('fvCN')) el('fvCN').textContent = cn.toFixed(2);
    if (el('fvUtil')) el('fvUtil').textContent = u.toFixed(3);
    if (el('fvUE')) el('fvUE').textContent = ue.toFixed(3);
    if (el('fvImp')) el('fvImp').textContent = imp.toFixed(3);
    if (el('fvDiv')) el('fvDiv').textContent = div.toFixed(4);
    if (el('fvTC')) el('fvTC').textContent = tc.toFixed(4);
    if (el('fvPrecio')) el('fvPrecio').textContent = '$' + precio.toLocaleString('es-MX');
}

// ==================== IVA ====================

/**
 * Calcula CBPieza desde un monto bruto con IVA (factor 1.15 del legacy).
 * Equivalente a PrecioIVA del VB6.
 */
function calcularDesdeIVA() {
    var brutoIVA = val('brutoConIVA');
    if (brutoIVA > 0) {
        var cbSinIVA = brutoIVA / 1.16; // Actualizado a IVA 16% (legacy usaba 15%)
        setVal('cbPieza', cbSinIVA);
        calcularCostos();
    }
}

// ==================== DIVISOR ====================

function onDivisorChange() {
    var sel = document.getElementById('selDivisor');
    if (!sel) return;
    var opt = sel.options[sel.selectedIndex];
    if (opt && opt.dataset.divisor) {
        setVal('divisorValor', parseFloat(opt.dataset.divisor));
    }
    calcularPrecio();
}

// ==================== MONEDA ====================

function onMonedaChange() {
    var sel = document.getElementById('selMoneda');
    if (!sel) return;
    var idMoneda = sel.value;

    // Consultar tipo de cambio via API
    fetch('?handler=TipoCambio&idMoneda=' + idMoneda)
        .then(function(r) { return r.json(); })
        .then(function(tc) {
            if (tc) {
                setVal('tcCotizacion', tc.tipoCambioCotizacion || 1);
                calcularPrecio();
            }
        })
        .catch(function() {
            setVal('tcCotizacion', 1);
            calcularPrecio();
        });
}

// ==================== UTILIDAD EXTRA AUTOMATICA ====================

/**
 * Busca utilidad extra automatica basada en PrecioGramo * TCCotizacion.
 * Solo se activa cuando el proveedor tiene UtilidadExtra=-1.
 */
function buscarUtilidadExtra() {
    var precioGramo = val('precioGramo');
    var tcCotizacion = val('tcCotizacion');
    if (precioGramo <= 0) return;

    fetch('?handler=UtilidadExtra&precioGramo=' + precioGramo + '&tcCotizacion=' + tcCotizacion)
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (data && data.utilidadExtra) {
                setVal('utilidadExtra', data.utilidadExtra);
                // Indicar visualmente que fue auto-calculado
                var el = document.getElementById('utilidadExtra');
                if (el) {
                    el.classList.add('bg-warning-subtle');
                    el.title = 'Auto-calculado desde tabla UtilidadExtra_PrecioGramo';
                }
                calcularPrecio();
            }
        })
        .catch(function() {});
}

// ==================== SINCRONIZACION ENTRE TABS ====================
// En VB6, los campos Modelo/Linea/Kilates/Obs1 se comparten entre tabs Oro/Diamante/Reloj

function syncKilates(value) {
    var selOro = document.getElementById('selKilatesOro');
    if (selOro) selOro.value = value;
    // Actualizar el hidden field que se envia al server
    var hidden = document.querySelector('[name="Pieza.Kilates"]');
    if (hidden) hidden.value = value;
}

function syncModelo(value) {
    var modeloOro = document.getElementById('modeloOro');
    if (modeloOro) modeloOro.value = value;
}

function syncLinea(value) {
    var lineaOro = document.getElementById('lineaOro');
    if (lineaOro) lineaOro.value = value;
}

function syncObs1(value) {
    var obs1Diam = document.getElementById('obs1Diam');
    if (obs1Diam) obs1Diam.value = value;
    var hidden = document.querySelector('[name="Pieza.Obs1"]');
    if (hidden) hidden.value = value;
}

// Sync reverso: Oro -> Reloj/Diamante
document.addEventListener('DOMContentLoaded', function() {
    var modeloOro = document.getElementById('modeloOro');
    if (modeloOro) {
        modeloOro.addEventListener('change', function() {
            var modeloReloj = document.getElementById('modeloReloj');
            if (modeloReloj) modeloReloj.value = this.value;
        });
    }

    var lineaOro = document.getElementById('lineaOro');
    if (lineaOro) {
        lineaOro.addEventListener('change', function() {
            var lineaReloj = document.getElementById('lineaReloj');
            if (lineaReloj) lineaReloj.value = this.value;
        });
    }

    var selKilatesOro = document.getElementById('selKilatesOro');
    if (selKilatesOro) {
        selKilatesOro.addEventListener('change', function() {
            var selKilatesDiam = document.getElementById('selKilatesDiam');
            if (selKilatesDiam) selKilatesDiam.value = this.value;
        });
    }

    // Auto-calcular utilidad extra cuando cambia precio gramo
    var precioGramo = document.getElementById('precioGramo');
    if (precioGramo) {
        precioGramo.addEventListener('change', function() {
            // Solo auto-buscar si el campo utilidadExtra tiene indicador de auto
            var ue = document.getElementById('utilidadExtra');
            if (ue && ue.classList.contains('bg-warning-subtle')) {
                buscarUtilidadExtra();
            }
        });
    }

    // Trigger calc en inputs numericos con tecla Enter
    document.querySelectorAll('#formPieza input[type="number"]').forEach(function(input) {
        input.addEventListener('keydown', function(e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                calcularCostos();
                // Mover al siguiente input
                var inputs = Array.from(document.querySelectorAll('#formPieza input:not([readonly]):not([type="hidden"])'));
                var idx = inputs.indexOf(this);
                if (idx >= 0 && idx < inputs.length - 1) {
                    inputs[idx + 1].focus();
                }
            }
        });
    });
});
