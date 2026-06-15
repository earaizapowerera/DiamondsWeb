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

// ==================== RECALCULO EN LOST FOCUS ====================
// RN-02: Al salir de CUALQUIER campo numérico que participe en cálculos,
// se recalcula el producto inmediatamente (equivalente a LostFocus del VB6).
// IDs de campos que al perder foco disparan recálculo de costos (sumas, productos, netos):
var CAMPOS_COSTOS = [
    'cbPieza', 'descPieza', 'peso', 'precioGramo', 'descPeso',
    'cbManoObra', 'descManoObra', 'cbFactura', 'descFactura', 'tcCosto',
    'brutoConIVA'
];
// IDs de campos que al perder foco disparan recálculo de precio final (factores multiplicativos):
var CAMPOS_PRECIO = [
    'utilidad', 'utilidadExtra', 'impuesto', 'tcCotizacion'
];

// Sync reverso: Oro -> Reloj/Diamante
document.addEventListener('DOMContentLoaded', function() {
    // --- Recálculo en blur (lost focus) para campos de costos ---
    CAMPOS_COSTOS.forEach(function(id) {
        var el = document.getElementById(id);
        if (el) {
            el.addEventListener('blur', function() {
                if (id === 'brutoConIVA') {
                    calcularDesdeIVA();
                } else {
                    calcularCostos();
                }
            });
        }
    });

    // --- Recálculo en blur para factores de precio ---
    CAMPOS_PRECIO.forEach(function(id) {
        var el = document.getElementById(id);
        if (el) {
            el.addEventListener('blur', function() {
                calcularPrecio();
            });
        }
    });

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
        precioGramo.addEventListener('blur', function() {
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

    // ==================== BUSQUEDA EN MODALES ====================

    // Buscar remisiones al escribir en el modal
    var txtBuscarRemision = document.getElementById('txtBuscarRemision');
    if (txtBuscarRemision) {
        var timerRemision = null;
        txtBuscarRemision.addEventListener('input', function() {
            clearTimeout(timerRemision);
            timerRemision = setTimeout(function() { buscarRemisionesAPI(txtBuscarRemision.value); }, 300);
        });
    }

    // Cargar remisiones al abrir modal
    var modalRemision = document.getElementById('modalBuscarRemision');
    if (modalRemision) {
        modalRemision.addEventListener('shown.bs.modal', function() {
            var txt = document.getElementById('txtBuscarRemision');
            if (txt) { txt.value = ''; txt.focus(); }
            buscarRemisionesAPI('');
        });
    }

    // Buscar facturas al escribir en el modal
    var txtBuscarFactura = document.getElementById('txtBuscarFactura');
    if (txtBuscarFactura) {
        var timerFactura = null;
        txtBuscarFactura.addEventListener('input', function() {
            clearTimeout(timerFactura);
            timerFactura = setTimeout(function() { buscarFacturasAPI(txtBuscarFactura.value); }, 300);
        });
    }

    // Cargar facturas al abrir modal
    var modalFactura = document.getElementById('modalBuscarFactura');
    if (modalFactura) {
        modalFactura.addEventListener('shown.bs.modal', function() {
            var txt = document.getElementById('txtBuscarFactura');
            if (txt) { txt.value = ''; txt.focus(); }
            buscarFacturasAPI('');
        });
    }

    // Cargar razones sociales cuando se selecciona proveedor de remision
    var selProvRem = document.getElementById('selProveedorRemision');
    if (selProvRem) {
        selProvRem.addEventListener('change', function() {
            cargarRazonesSociales(this.value);
        });
    }
});

// ==================== REMISION: Crear y Buscar ====================

function crearRemision() {
    var proveedor = document.getElementById('selProveedorRemision');
    if (!proveedor || !proveedor.value) {
        alert('Selecciona un proveedor');
        return;
    }
    var numRemision = document.getElementById('txtNumRemision');
    var fechaRemision = document.getElementById('txtFechaRemision');
    var chkConsignacion = document.getElementById('chkConsignacion');

    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    var body = new FormData();
    body.append('proveedor', proveedor.value);
    body.append('numeroRemision', numRemision ? numRemision.value : 'S/N');
    body.append('fechaRemision', fechaRemision ? fechaRemision.value : '');
    body.append('consignacion', chkConsignacion ? chkConsignacion.checked : false);
    if (token) body.append('__RequestVerificationToken', token.value);

    fetch('?handler=CrearRemision', { method: 'POST', body: body })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (data.success) {
                window.location.href = '/Piezas/Alta?IdRemision=' + data.idRemision;
            } else {
                alert('Error al crear remision: ' + (data.error || 'desconocido'));
            }
        })
        .catch(function(err) { alert('Error: ' + err.message); });
}

function buscarRemisionesAPI(texto) {
    var lista = document.getElementById('listaRemisiones');
    if (!lista) return;
    lista.innerHTML = '<div class="text-center py-2"><i class="fa-solid fa-spinner fa-spin"></i></div>';

    fetch('?handler=BuscarRemisiones&texto=' + encodeURIComponent(texto || ''))
        .then(function(r) { return r.json(); })
        .then(function(items) {
            if (!items || items.length === 0) {
                lista.innerHTML = '<div class="text-center text-muted py-3"><small>No se encontraron remisiones</small></div>';
                return;
            }
            var currentIdRemision = new URLSearchParams(window.location.search).get('IdRemision');
            var html = '';
            items.forEach(function(r) {
                html += '<a href="/Piezas/Alta?IdRemision=' + r.idRemision + '"'
                    + ' class="list-group-item list-group-item-action py-1 small'
                    + (currentIdRemision == r.idRemision ? ' active' : '') + '">'
                    + '<strong>#' + r.idRemision + '</strong> - ' + (r.nombreProveedor || '')
                    + ' (' + (r.numeroRemision || 'S/N') + ')'
                    + (r.consignacion ? ' <span class="badge bg-warning text-dark">Cons.</span>' : '')
                    + '<span class="text-muted float-end">' + (r.fechaRemision || '')
                    + (r.cantidadPiezas > 0 ? ' | ' + r.cantidadPiezas + ' pzas' : '')
                    + '</span></a>';
            });
            lista.innerHTML = html;
        })
        .catch(function() { lista.innerHTML = '<div class="text-center text-danger py-2">Error de conexion</div>'; });
}

// ==================== FACTURA: Crear y Buscar ====================

function crearFactura() {
    var folioEl = document.getElementById('txtFolioFactura');
    var razonSocialEl = document.getElementById('selRazonSocial');
    var fechaFacturaEl = document.getElementById('txtFechaFactura');

    // Obtener proveedor de la remision actual (del hidden field)
    var idRemisionEl = document.querySelector('input[name="IdRemision"]');
    var idRemision = idRemisionEl ? idRemisionEl.value : '';

    // Necesitamos saber el proveedor de la remision.
    // Obtenerlo del badge o del select.
    var proveedorEl = document.getElementById('selProveedorRemision');
    var proveedor = proveedorEl ? proveedorEl.value : '';
    // Si la remision ya esta seleccionada, el proveedor no está en un select visible
    // Lo obtenemos del data attribute que inyectaremos
    if (!proveedor) {
        var provHidden = document.getElementById('hidProveedorRemision');
        proveedor = provHidden ? provHidden.value : '';
    }

    if (!proveedor) {
        alert('No se pudo determinar el proveedor de la remision');
        return;
    }

    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    var body = new FormData();
    body.append('proveedor', proveedor);
    body.append('folioFactura', folioEl ? folioEl.value : '');
    body.append('idRazonSocial', razonSocialEl ? razonSocialEl.value : '0');
    body.append('fechaFactura', fechaFacturaEl ? fechaFacturaEl.value : '');
    if (token) body.append('__RequestVerificationToken', token.value);

    fetch('?handler=CrearFactura', { method: 'POST', body: body })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (data.success) {
                // Vincular la factura a la remision
                var bodyVinc = new FormData();
                bodyVinc.append('idRemision', idRemision);
                bodyVinc.append('idFactura', data.idFactura);
                if (token) bodyVinc.append('__RequestVerificationToken', token.value);

                fetch('?handler=VincularFactura', { method: 'POST', body: bodyVinc })
                    .then(function() {
                        window.location.href = '/Piezas/Alta?IdRemision=' + idRemision + '&IdFactura=' + data.idFactura;
                    });
            } else {
                alert('Error al crear factura: ' + (data.error || 'desconocido'));
            }
        })
        .catch(function(err) { alert('Error: ' + err.message); });
}

function buscarFacturasAPI(texto) {
    var lista = document.getElementById('listaFacturas');
    if (!lista) return;
    lista.innerHTML = '<div class="text-center py-2"><i class="fa-solid fa-spinner fa-spin"></i></div>';

    fetch('?handler=BuscarFacturas&texto=' + encodeURIComponent(texto || ''))
        .then(function(r) { return r.json(); })
        .then(function(items) {
            if (!items || items.length === 0) {
                lista.innerHTML = '<div class="text-center text-muted py-3"><small>No se encontraron facturas</small></div>';
                return;
            }
            var idRemision = new URLSearchParams(window.location.search).get('IdRemision') || '';
            var html = '';
            items.forEach(function(f) {
                var href = '/Piezas/Alta?IdFactura=' + f.idFactura;
                if (idRemision) href += '&IdRemision=' + idRemision;
                html += '<a href="' + href + '"'
                    + ' class="list-group-item list-group-item-action py-1 small">'
                    + '<strong>#' + f.idFactura + '</strong>'
                    + (f.folioFactura ? ' - Folio: ' + f.folioFactura : '')
                    + ' - ' + (f.nombreProveedor || '')
                    + '<span class="text-muted float-end">' + (f.fechaFactura || '') + '</span>'
                    + '</a>';
            });
            lista.innerHTML = html;
        })
        .catch(function() { lista.innerHTML = '<div class="text-center text-danger py-2">Error de conexion</div>'; });
}

function cargarRazonesSociales(proveedorId) {
    var sel = document.getElementById('selRazonSocial');
    if (!sel || !proveedorId) return;
    sel.innerHTML = '<option value="0">Cargando...</option>';

    fetch('?handler=RazonesSociales&proveedor=' + proveedorId)
        .then(function(r) { return r.json(); })
        .then(function(items) {
            sel.innerHTML = '<option value="0">--</option>';
            (items || []).forEach(function(rs) {
                var opt = document.createElement('option');
                opt.value = rs.idRazonSocialProveedor;
                opt.textContent = rs.razonSocial;
                sel.appendChild(opt);
            });
        })
        .catch(function() { sel.innerHTML = '<option value="0">--</option>'; });
}

// ==================== FOTO DE PIEZA ====================

/**
 * Sube una foto desde el input file del navegador.
 */
function subirFoto(inputEl) {
    if (!inputEl.files || !inputEl.files[0]) return;

    var file = inputEl.files[0];

    // Validar tipo
    var tiposPermitidos = ['image/jpeg', 'image/png', 'image/webp', 'image/gif', 'image/bmp'];
    if (tiposPermitidos.indexOf(file.type) === -1) {
        alert('Tipo de archivo no permitido. Use JPG, PNG, WebP, GIF o BMP.');
        inputEl.value = '';
        return;
    }

    // Validar tamano (10 MB)
    if (file.size > 10 * 1024 * 1024) {
        alert('El archivo es muy grande. Maximo 10 MB.');
        inputEl.value = '';
        return;
    }

    // Mostrar spinner
    var dropzone = document.getElementById('fotoDropzone');
    var loading = document.getElementById('fotoLoading');
    if (dropzone) dropzone.classList.add('d-none');
    if (loading) loading.classList.remove('d-none');

    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    var formData = new FormData();
    formData.append('foto', file);
    if (token) formData.append('__RequestVerificationToken', token.value);

    fetch('?handler=SubirFoto', { method: 'POST', body: formData })
        .then(function(r) { return r.json(); })
        .then(function(data) {
            if (loading) loading.classList.add('d-none');
            if (data.success) {
                mostrarFotoPreview(data.url, data.storedFileName);
            } else {
                if (dropzone) dropzone.classList.remove('d-none');
                alert('Error al subir foto: ' + (data.error || 'desconocido'));
            }
        })
        .catch(function(err) {
            if (loading) loading.classList.add('d-none');
            if (dropzone) dropzone.classList.remove('d-none');
            alert('Error de conexion: ' + err.message);
        });

    // Limpiar input para permitir re-subir el mismo archivo
    inputEl.value = '';
}

/**
 * Selecciona una foto de las recientes del movil.
 */
function seleccionarFotoMovil(storedFileName, url) {
    mostrarFotoPreview(url, storedFileName);

    // Marcar thumbnail como seleccionada
    document.querySelectorAll('.foto-thumb').forEach(function(t) {
        t.classList.remove('selected');
    });
    var thumbs = document.querySelectorAll('.foto-thumb');
    thumbs.forEach(function(t) {
        if (t.src.indexOf(storedFileName) !== -1) {
            t.classList.add('selected');
        }
    });
}

/**
 * Muestra el preview de la foto y actualiza el hidden field.
 */
function mostrarFotoPreview(url, storedFileName) {
    var preview = document.getElementById('fotoPreview');
    var img = document.getElementById('imgPreview');
    var uploadArea = document.getElementById('fotoUploadArea');
    var hidArchivoFoto = document.getElementById('hidArchivoFoto');

    if (img) img.src = url;
    if (preview) preview.classList.remove('d-none');
    if (uploadArea) uploadArea.classList.add('d-none');
    if (hidArchivoFoto) hidArchivoFoto.value = storedFileName;
}

/**
 * Quita la foto seleccionada.
 */
function quitarFoto() {
    var preview = document.getElementById('fotoPreview');
    var img = document.getElementById('imgPreview');
    var uploadArea = document.getElementById('fotoUploadArea');
    var dropzone = document.getElementById('fotoDropzone');
    var hidArchivoFoto = document.getElementById('hidArchivoFoto');

    if (preview) preview.classList.add('d-none');
    if (img) img.src = '';
    if (uploadArea) uploadArea.classList.remove('d-none');
    if (dropzone) dropzone.classList.remove('d-none');
    if (hidArchivoFoto) hidArchivoFoto.value = '';

    // Quitar seleccion de thumbnails
    document.querySelectorAll('.foto-thumb').forEach(function(t) {
        t.classList.remove('selected');
    });
}

// ==================== DRAG & DROP ====================

document.addEventListener('DOMContentLoaded', function() {
    var dropzone = document.getElementById('fotoDropzone');
    if (!dropzone) return;

    dropzone.addEventListener('dragover', function(e) {
        e.preventDefault();
        e.stopPropagation();
        this.classList.add('drag-over');
    });

    dropzone.addEventListener('dragleave', function(e) {
        e.preventDefault();
        e.stopPropagation();
        this.classList.remove('drag-over');
    });

    dropzone.addEventListener('drop', function(e) {
        e.preventDefault();
        e.stopPropagation();
        this.classList.remove('drag-over');

        if (e.dataTransfer.files && e.dataTransfer.files[0]) {
            var inputFoto = document.getElementById('inputFoto');
            if (inputFoto) {
                // Crear un DataTransfer para asignar los archivos al input
                var dt = new DataTransfer();
                dt.items.add(e.dataTransfer.files[0]);
                inputFoto.files = dt.files;
                subirFoto(inputFoto);
            }
        }
    });
});
