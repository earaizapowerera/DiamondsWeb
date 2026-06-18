// ═══════════════════════════════════════════════════════════════
// Punto de Venta Apartados — JavaScript
// Migración de frmPuntodeVentaApartados.frm (VB6)
// ═══════════════════════════════════════════════════════════════

(function () {
    'use strict';

    // ─── Estado global ─────────────────────────────────────────
    let state = {
        idNota: null,
        idUsuario: null,
        idVendedor: null,
        esFactura: false,
        descuentoPct: 0,
        sobrePrecio: 0,
        sesionActiva: false,
        pendingRepetida: null
    };

    // ─── Elementos DOM ─────────────────────────────────────────
    const $ = id => document.getElementById(id);
    const el = {
        txtUsuario: $('txtUsuario'),
        lblUsuario: $('lblUsuario'),
        txtVendedor: $('txtVendedor'),
        cmbSesion: $('cmbSesion'),
        btnNuevaSesion: $('btnNuevaSesion'),
        btnCerrarNota: $('btnCerrarNota'),
        btnCancelar: $('btnCancelar'),
        txtCodigoBarras: $('txtCodigoBarras'),
        lblRepetidaCantidad: $('lblRepetidaCantidad'),
        txtCantidadRepetida: $('txtCantidadRepetida'),
        btnConfirmarRepetida: $('btnConfirmarRepetida'),
        cmbRepetida: $('cmbRepetida'),
        txtSubTotal: $('txtSubTotal'),
        txtDescuento: $('txtDescuento'),
        txtSobrePrecio: $('txtSobrePrecio'),
        txtTotal: $('txtTotal'),
        txtSubTotalFactura: $('txtSubTotalFactura'),
        txtTotalFactura: $('txtTotalFactura'),
        txtTotalPagado: $('txtTotalPagado'),
        txtCambio: $('txtCambio'),
        chkFactura: $('chkFactura'),
        divFactura: $('divFactura'),
        tbodyPiezas: $('tbodyPiezas'),
        tbodyPagos: $('tbodyPagos'),
        // Cliente
        txtNombre: $('txtNombre'),
        txtTelefonos: $('txtTelefonos'),
        txtRFC: $('txtRFC'),
        txtCalle: $('txtCalle'),
        txtCP: $('txtCP'),
        cmbColonia: $('cmbColonia'),
        txtCiudad: $('txtCiudad'),
        txtEstado: $('txtEstado'),
        txtMunicipio: $('txtMunicipio'),
        txtCBCliente: $('txtCBCliente'),
        // Modal pago
        paymentGrid: $('paymentGrid'),
        pagoIdOpcionPago: $('pagoIdOpcionPago'),
        pagoIdMoneda: $('pagoIdMoneda'),
        pagoExtranjera: $('pagoExtranjera'),
        pagoImporte: $('pagoImporte'),
        pagoImporteOriginal: $('pagoImporteOriginal'),
        pagoTipoCambio: $('pagoTipoCambio'),
        pagoEquivalenteMXN: $('pagoEquivalenteMXN'),
        divMonedaExtranjera: $('divMonedaExtranjera'),
        btnRegistrarPago: $('btnRegistrarPago'),
        btnConfirmarCerrar: $('btnConfirmarCerrar'),
        printArea: $('printArea')
    };

    // ─── CSRF Token ────────────────────────────────────────────
    function getToken() {
        const t = document.querySelector('input[name="__RequestVerificationToken"]');
        return t ? t.value : '';
    }

    // ─── API Helper ────────────────────────────────────────────
    const BASE = '/Ventas/Apartados';
    async function api(handler, method, body) {
        const url = `${BASE}?handler=${handler}`;
        const opts = { method, headers: {} };
        if (method === 'POST') {
            opts.headers['Content-Type'] = 'application/json';
            opts.headers['RequestVerificationToken'] = getToken();
            opts.body = JSON.stringify(body);
        }
        const resp = await fetch(method === 'GET' ? url + (body || '') : url, opts);
        return resp.json();
    }
    async function apiGet(handler, params) {
        return api(handler, 'GET', params ? '&' + new URLSearchParams(params).toString() : '');
    }
    async function apiPost(handler, body) {
        return api(handler, 'POST', body);
    }

    // ─── Formateo ──────────────────────────────────────────────
    function fmt(n) {
        return Number(n || 0).toLocaleString('es-MX', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    // ═══════════════════════════════════════════════════════════════
    //  SESIONES
    // ═══════════════════════════════════════════════════════════════

    async function cargarSesiones() {
        const r = await apiGet('Sesiones');
        if (!r.ok) return;
        el.cmbSesion.innerHTML = '<option value="">— Seleccionar sesión —</option>';
        r.sesiones.forEach(s => {
            const opt = document.createElement('option');
            opt.value = s.idNota;
            opt.textContent = `${s.nombreUsuario} (Nota #${s.idNota})`;
            el.cmbSesion.appendChild(opt);
        });
        // Si solo hay una, seleccionarla
        if (r.sesiones.length === 1) {
            el.cmbSesion.value = r.sesiones[0].idNota;
            await seleccionarSesion(r.sesiones[0].idNota);
        }
    }

    async function seleccionarSesion(idNota) {
        const r = await apiGet('Sesion', { idNota });
        if (!r.ok) { alert(r.error); return; }
        state.idNota = idNota;
        state.idUsuario = r.sesion.idUsuario;
        state.idVendedor = r.sesion.idVendedor;
        state.esFactura = r.sesion.factura;
        state.sesionActiva = true;

        el.txtUsuario.value = r.sesion.idUsuario;
        el.lblUsuario.textContent = r.sesion.nombreUsuario;
        el.txtVendedor.value = r.sesion.idVendedor;
        // Datos del cliente
        el.txtNombre.value = r.sesion.nombreCliente || '';
        el.txtTelefonos.value = r.sesion.telefonos || '';
        el.txtRFC.value = r.sesion.rfc || '';
        el.txtCalle.value = r.sesion.calle || '';
        el.txtCP.value = r.sesion.codigoPostal || '';
        el.txtCiudad.value = r.sesion.ciudad || '';
        el.txtEstado.value = r.sesion.estado || '';
        el.txtMunicipio.value = r.sesion.municipio || '';
        el.txtCBCliente.value = r.sesion.codigoBarrasCliente || '';
        el.chkFactura.checked = r.sesion.factura;

        renderPiezas(r.piezas);
        renderPagos(r.pagos);
        actualizarResumen(r.resumen);
        habilitarSesion(true);
        el.txtCodigoBarras.focus();
    }

    async function nuevaSesion() {
        const idUsuario = parseInt(el.txtUsuario.value);
        if (!idUsuario || idUsuario < 1) {
            el.txtUsuario.focus();
            alert('Ingrese un ID de usuario válido.');
            return;
        }
        const r = await apiPost('CrearSesion', { idUsuario });
        if (!r.ok) { alert(r.error); return; }
        state.idNota = r.sesion.idNota;
        state.idUsuario = r.sesion.idUsuario;
        state.idVendedor = r.sesion.idVendedor;
        state.sesionActiva = true;

        el.lblUsuario.textContent = r.sesion.nombreUsuario;
        el.txtVendedor.value = r.sesion.idVendedor;
        habilitarSesion(true);
        await cargarSesiones();
        el.cmbSesion.value = state.idNota;
        el.txtCodigoBarras.focus();
    }

    async function cancelarSesion() {
        if (!state.idNota) return;
        if (!confirm('¿Cancelar esta sesión? Se perderán todas las piezas y pagos.')) return;
        await apiPost('CancelarSesion', { idNota: state.idNota });
        resetearVista();
        await cargarSesiones();
    }

    function habilitarSesion(enabled) {
        el.txtCodigoBarras.disabled = !enabled;
        el.txtDescuento.disabled = !enabled;
        el.txtSobrePrecio.disabled = !enabled;
        el.chkFactura.disabled = !enabled;
        el.btnCerrarNota.disabled = !enabled;
        el.btnCancelar.disabled = !enabled;
        el.txtNombre.disabled = !enabled;
        el.txtTelefonos.disabled = !enabled;
        el.txtRFC.disabled = !enabled;
        el.txtCalle.disabled = !enabled;
        el.txtCP.disabled = !enabled;
        el.cmbColonia.disabled = !enabled;
        el.txtCiudad.disabled = !enabled;
        el.txtEstado.disabled = !enabled;
        el.txtMunicipio.disabled = !enabled;
    }

    function resetearVista() {
        state = { idNota: null, idUsuario: null, idVendedor: null, esFactura: false, descuentoPct: 0, sobrePrecio: 0, sesionActiva: false, pendingRepetida: null };
        el.txtUsuario.value = '';
        el.lblUsuario.textContent = '';
        el.txtVendedor.value = '';
        el.txtNombre.value = '';
        el.txtTelefonos.value = '';
        el.txtRFC.value = '';
        el.txtCalle.value = '';
        el.txtCP.value = '';
        el.cmbColonia.innerHTML = '<option value="">—</option>';
        el.txtCiudad.value = '';
        el.txtEstado.value = '';
        el.txtMunicipio.value = '';
        el.txtCBCliente.value = '';
        el.txtCodigoBarras.value = '';
        el.txtSubTotal.value = '';
        el.txtDescuento.value = '0';
        el.txtSobrePrecio.value = '0';
        el.txtTotal.value = '';
        el.txtTotalPagado.value = '';
        el.txtCambio.value = '';
        el.chkFactura.checked = false;
        el.tbodyPiezas.innerHTML = '<tr id="trPiezasVacio"><td colspan="6" class="text-center text-muted py-4"><i class="fa-solid fa-barcode me-1"></i>Escanee un código de barras para comenzar</td></tr>';
        el.tbodyPagos.innerHTML = '<tr id="trPagosVacio"><td colspan="5" class="text-center text-muted py-3">Sin pagos registrados</td></tr>';
        habilitarSesion(false);
    }

    // ═══════════════════════════════════════════════════════════════
    //  PIEZAS
    // ═══════════════════════════════════════════════════════════════

    async function agregarPieza(codigoBarras, cantidad) {
        if (!codigoBarras) return;
        const r = await apiPost('AgregarPieza', {
            idNota: state.idNota,
            codigoBarras,
            cantidad: cantidad || 1,
            esFactura: state.esFactura
        });
        if (!r.ok) { alert(r.error); el.txtCodigoBarras.value = ''; el.txtCodigoBarras.focus(); return; }
        renderPiezas(r.piezas);
        actualizarResumen(r.resumen);
        el.txtCodigoBarras.value = '';
        el.txtCodigoBarras.focus();
    }

    async function eliminarPieza(codigoBarras) {
        if (!confirm('¿Eliminar esta pieza del apartado?')) return;
        const r = await apiPost('EliminarPieza', {
            idNota: state.idNota,
            codigoBarras,
            descuentoPct: state.descuentoPct,
            sobrePrecio: state.sobrePrecio,
            esFactura: state.esFactura
        });
        if (!r.ok) return;
        renderPiezas(r.piezas);
        actualizarResumen(r.resumen);
    }

    function renderPiezas(piezas) {
        if (!piezas || piezas.length === 0) {
            el.tbodyPiezas.innerHTML = '<tr id="trPiezasVacio"><td colspan="6" class="text-center text-muted py-4"><i class="fa-solid fa-barcode me-1"></i>Escanee un código de barras para comenzar</td></tr>';
            return;
        }
        el.tbodyPiezas.innerHTML = piezas.map(p => `
            <tr>
                <td class="small">${p.codigoBarras}</td>
                <td class="small">${p.descripcion}</td>
                <td class="text-center">${p.cantidad}</td>
                <td class="text-end">${fmt(p.subTotal)}</td>
                <td class="text-end fw-bold">${fmt(p.total)}</td>
                <td><button class="btn btn-sm btn-outline-danger py-0 px-1 btn-del-pieza" data-cb="${p.codigoBarras}" title="Eliminar"><i class="fa-solid fa-xmark"></i></button></td>
            </tr>
        `).join('');
    }

    // ═══════════════════════════════════════════════════════════════
    //  PAGOS
    // ═══════════════════════════════════════════════════════════════

    async function registrarPago() {
        const idOpcionPago = parseInt(el.pagoIdOpcionPago.value);
        if (!idOpcionPago) { alert('Seleccione una forma de pago.'); return; }

        const esExtranjera = el.pagoExtranjera.value === 'true';
        let importe, tipoCambio = 0, importeOriginal = 0;

        if (esExtranjera) {
            importeOriginal = parseFloat(el.pagoImporteOriginal.value) || 0;
            tipoCambio = parseFloat(el.pagoTipoCambio.value) || 0;
            importe = importeOriginal * tipoCambio;
        } else {
            importe = parseFloat(el.pagoImporte.value) || 0;
        }
        if (importe <= 0) { alert('El importe debe ser mayor a 0.'); return; }

        const r = await apiPost('RegistrarPago', {
            idNota: state.idNota,
            idOpcionPago,
            importe,
            tipoCambio,
            importeOriginal
        });
        if (!r.ok) { alert(r.error); return; }
        renderPagos(r.pagos);
        actualizarResumen(r.resumen);
        bootstrap.Modal.getInstance(document.getElementById('modalPago')).hide();
    }

    async function eliminarPago(idOpcionPago, importe) {
        if (!confirm('¿Eliminar este pago?')) return;
        const r = await apiPost('EliminarPago', {
            idNota: state.idNota,
            idOpcionPago: parseInt(idOpcionPago),
            importe: parseFloat(importe)
        });
        if (!r.ok) return;
        renderPagos(r.pagos);
        actualizarResumen(r.resumen);
    }

    function renderPagos(pagos) {
        if (!pagos || pagos.length === 0) {
            el.tbodyPagos.innerHTML = '<tr id="trPagosVacio"><td colspan="5" class="text-center text-muted py-3">Sin pagos registrados</td></tr>';
            return;
        }
        el.tbodyPagos.innerHTML = pagos.map(p => `
            <tr>
                <td>${p.opcionPago}</td>
                <td class="text-end">${fmt(p.importe)}</td>
                <td class="text-end">${p.tipoCambio > 0 ? fmt(p.tipoCambio) : '-'}</td>
                <td class="text-end">${p.importeOriginal > 0 ? fmt(p.importeOriginal) : '-'}</td>
                <td><button class="btn btn-sm btn-outline-danger py-0 px-1 btn-del-pago" data-op="${p.idOpcionPago}" data-imp="${p.importe}" title="Eliminar"><i class="fa-solid fa-xmark"></i></button></td>
            </tr>
        `).join('');
    }

    // ═══════════════════════════════════════════════════════════════
    //  RESUMEN / TOTALES
    // ═══════════════════════════════════════════════════════════════

    function actualizarResumen(resumen) {
        if (!resumen) return;
        el.txtSubTotal.value = fmt(resumen.subTotal);
        el.txtTotal.value = fmt(resumen.total);
        el.txtTotalPagado.value = fmt(resumen.totalPagado);
        el.txtCambio.value = fmt(resumen.cambio);
        state.descuentoPct = resumen.descuentoPct || 0;
        state.sobrePrecio = resumen.sobrePrecio || 0;

        if (resumen.esFactura) {
            el.divFactura.style.display = '';
            el.txtSubTotalFactura.value = fmt(resumen.subTotal);
            el.txtTotalFactura.value = fmt(resumen.totalFactura);
        } else {
            el.divFactura.style.display = 'none';
        }

        // Color cambio
        const cambioVal = parseFloat(resumen.cambio) || 0;
        el.txtCambio.style.color = cambioVal < 0 ? '#dc3545' : '#198754';
    }

    async function recalcularResumen() {
        if (!state.idNota) return;
        const desc = parseFloat(el.txtDescuento.value) || 0;
        const sp = parseFloat(el.txtSobrePrecio.value) || 0;
        if (desc > 20) {
            alert('Imposible realizar descuentos de esta magnitud (max 20%). Favor de rectificar.');
            el.txtDescuento.value = '20';
            return;
        }
        state.descuentoPct = desc;
        state.sobrePrecio = sp;
        const r = await apiGet('Resumen', {
            idNota: state.idNota,
            descuento: desc,
            sobrePrecio: sp,
            factura: state.esFactura
        });
        if (r.ok) actualizarResumen(r.resumen);
    }

    // ═══════════════════════════════════════════════════════════════
    //  CERRAR NOTA
    // ═══════════════════════════════════════════════════════════════

    function mostrarModalCerrar() {
        if (!state.idNota) return;
        if (!el.txtNombre.value.trim()) { alert('Falta el Nombre del Cliente.'); el.txtNombre.focus(); return; }
        $('modalTotal').textContent = '$' + (el.txtTotal.value || '0.00');
        $('modalPagado').textContent = '$' + (el.txtTotalPagado.value || '0.00');
        $('modalCambio').textContent = '$' + (el.txtCambio.value || '0.00');
        new bootstrap.Modal(document.getElementById('modalCerrarNota')).show();
    }

    async function cerrarNota() {
        const r = await apiPost('CerrarNota', {
            idNota: state.idNota,
            nombreCliente: el.txtNombre.value.trim(),
            telefonos: el.txtTelefonos.value.trim() || null,
            factura: state.esFactura,
            descuento: state.descuentoPct,
            sobrePrecio: state.sobrePrecio,
            total: parseFloat(el.txtTotal.value.replace(/,/g, '')) || 0,
            formaPago: '',
            idVendedor: state.idVendedor || state.idUsuario
        });
        bootstrap.Modal.getInstance(document.getElementById('modalCerrarNota')).hide();
        if (!r.ok) { alert(r.error); return; }
        alert('Nota de apartado cerrada exitosamente. Nota #' + r.idNota);
        resetearVista();
        await cargarSesiones();
    }

    // ═══════════════════════════════════════════════════════════════
    //  MODAL PAGO
    // ═══════════════════════════════════════════════════════════════

    async function abrirModalPago() {
        if (!state.idNota) return;
        // Cargar opciones de pago
        const r = await apiGet('OpcionesPago');
        if (!r.ok) return;
        el.paymentGrid.innerHTML = r.opciones.map(op => `
            <button type="button" class="btn btn-outline-primary btn-sm btn-opcion-pago"
                    data-id="${op.idOpcionPago}" data-moneda="${op.idMoneda}" data-ext="${op.extranjera}">
                ${op.opcionPago}
            </button>
        `).join('');
        // Reset
        el.pagoIdOpcionPago.value = '';
        el.pagoImporte.value = '';
        el.pagoImporteOriginal.value = '';
        el.pagoTipoCambio.value = '';
        el.pagoEquivalenteMXN.value = '';
        el.divMonedaExtranjera.style.display = 'none';
        // Default importe = lo que falta
        const cambio = parseFloat(el.txtCambio.value.replace(/,/g, '')) || 0;
        if (cambio < 0) el.pagoImporte.value = Math.abs(cambio).toFixed(2);

        new bootstrap.Modal(document.getElementById('modalPago')).show();
    }

    // ═══════════════════════════════════════════════════════════════
    //  COLONIAS POR CP
    // ═══════════════════════════════════════════════════════════════

    async function buscarColonias() {
        const cp = el.txtCP.value.trim();
        if (cp.length !== 5) return;
        const r = await apiGet('Colonias', { cp });
        if (!r.ok) return;
        el.cmbColonia.innerHTML = '<option value="">—</option>';
        if (r.colonias && r.colonias.length > 0) {
            r.colonias.forEach(c => {
                const opt = document.createElement('option');
                opt.value = c.fcnombrecolonia || c.fcNombreColonia || '';
                opt.textContent = opt.value;
                el.cmbColonia.appendChild(opt);
            });
            // Auto-fill city/state/municipality from first result
            const first = r.colonias[0];
            el.txtCiudad.value = first.fcnombreciudad || first.fcNombreCiudad || '';
            el.txtEstado.value = first.fcnombreestado || first.fcNombreEstado || '';
            el.txtMunicipio.value = first.fcnombremunicipio || first.fcNombreMunicipio || '';
            if (r.colonias.length === 1) {
                el.cmbColonia.value = el.cmbColonia.options[1]?.value || '';
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  GUARDAR DATOS CLIENTE (auto-save on blur)
    // ═══════════════════════════════════════════════════════════════

    async function guardarDatosCliente() {
        if (!state.idNota) return;
        await apiPost('ActualizarNota', {
            idNota: state.idNota,
            nombreCliente: el.txtNombre.value.trim() || null,
            telefonos: el.txtTelefonos.value.trim() || null,
            rfc: el.txtRFC.value.trim() || null,
            calle: el.txtCalle.value.trim() || null,
            codigoPostal: el.txtCP.value.trim() || null,
            colonia: el.cmbColonia.value || null,
            ciudad: el.txtCiudad.value.trim() || null,
            estado: el.txtEstado.value.trim() || null,
            municipio: el.txtMunicipio.value.trim() || null,
            codigoBarrasCliente: el.txtCBCliente.value.trim() || null,
            factura: state.esFactura,
            idVendedor: state.idVendedor || state.idUsuario
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  REPETIDAS
    // ═══════════════════════════════════════════════════════════════

    async function cargarRepetidas() {
        const r = await apiGet('Repetidas');
        if (!r.ok) return;
        el.cmbRepetida.innerHTML = '<option value="">— Seleccionar repetida —</option>';
        (r.repetidas || []).forEach(rep => {
            const opt = document.createElement('option');
            opt.value = rep.codigoBarras;
            opt.textContent = `${rep.descripcion} ${rep.kilates || ''}`.trim();
            el.cmbRepetida.appendChild(opt);
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  EVENT HANDLERS
    // ═══════════════════════════════════════════════════════════════

    function initEventHandlers() {
        // Sesiones
        el.btnNuevaSesion.addEventListener('click', nuevaSesion);
        el.btnCancelar.addEventListener('click', cancelarSesion);
        el.btnCerrarNota.addEventListener('click', mostrarModalCerrar);
        el.btnConfirmarCerrar.addEventListener('click', cerrarNota);

        el.cmbSesion.addEventListener('change', () => {
            const id = parseInt(el.cmbSesion.value);
            if (id) seleccionarSesion(id);
        });

        // Código de barras
        el.txtCodigoBarras.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                const cb = el.txtCodigoBarras.value.trim();
                if (cb) agregarPieza(cb);
            }
        });

        // Repetida seleccionada
        el.cmbRepetida.addEventListener('change', () => {
            const cb = el.cmbRepetida.value;
            if (cb && state.sesionActiva) {
                el.txtCodigoBarras.value = cb;
                el.txtCodigoBarras.focus();
            }
        });

        // Repetida cantidad
        el.btnConfirmarRepetida.addEventListener('click', () => {
            if (state.pendingRepetida) {
                const cant = parseInt(el.txtCantidadRepetida.value) || 1;
                agregarPieza(state.pendingRepetida, cant);
                state.pendingRepetida = null;
                el.lblRepetidaCantidad.style.display = 'none';
            }
        });

        // Descuento / SobrePrecio recalc
        el.txtDescuento.addEventListener('blur', recalcularResumen);
        el.txtSobrePrecio.addEventListener('blur', recalcularResumen);

        // Factura checkbox
        el.chkFactura.addEventListener('change', () => {
            state.esFactura = el.chkFactura.checked;
            recalcularResumen();
        });

        // Pagos — delegación de eventos
        el.tbodyPiezas.addEventListener('click', (e) => {
            const btn = e.target.closest('.btn-del-pieza');
            if (btn) eliminarPieza(btn.dataset.cb);
        });
        el.tbodyPagos.addEventListener('click', (e) => {
            const btn = e.target.closest('.btn-del-pago');
            if (btn) eliminarPago(btn.dataset.op, btn.dataset.imp);
        });

        // Modal pago
        el.btnRegistrarPago.addEventListener('click', registrarPago);
        el.paymentGrid.addEventListener('click', (e) => {
            const btn = e.target.closest('.btn-opcion-pago');
            if (!btn) return;
            // Highlight seleccionada
            el.paymentGrid.querySelectorAll('.btn-opcion-pago').forEach(b => b.classList.remove('active', 'btn-primary'));
            btn.classList.add('active', 'btn-primary');
            btn.classList.remove('btn-outline-primary');

            el.pagoIdOpcionPago.value = btn.dataset.id;
            el.pagoIdMoneda.value = btn.dataset.moneda;
            el.pagoExtranjera.value = btn.dataset.ext;

            if (btn.dataset.ext === 'true' || btn.dataset.ext === 'True') {
                el.divMonedaExtranjera.style.display = '';
                el.pagoImporteOriginal.focus();
            } else {
                el.divMonedaExtranjera.style.display = 'none';
                el.pagoImporte.focus();
            }
        });

        // Tipo de cambio calcula equivalente
        el.pagoTipoCambio.addEventListener('input', () => {
            const orig = parseFloat(el.pagoImporteOriginal.value) || 0;
            const tc = parseFloat(el.pagoTipoCambio.value) || 0;
            el.pagoEquivalenteMXN.value = fmt(orig * tc);
            el.pagoImporte.value = (orig * tc).toFixed(2);
        });
        el.pagoImporteOriginal.addEventListener('input', () => {
            const orig = parseFloat(el.pagoImporteOriginal.value) || 0;
            const tc = parseFloat(el.pagoTipoCambio.value) || 0;
            el.pagoEquivalenteMXN.value = fmt(orig * tc);
            el.pagoImporte.value = (orig * tc).toFixed(2);
        });

        // CP → buscar colonias
        el.txtCP.addEventListener('blur', buscarColonias);

        // Auto-save datos cliente on blur
        [el.txtNombre, el.txtTelefonos, el.txtRFC, el.txtCalle, el.txtCiudad, el.txtEstado, el.txtMunicipio].forEach(input => {
            if (input) input.addEventListener('blur', guardarDatosCliente);
        });
        el.cmbColonia.addEventListener('change', guardarDatosCliente);

        // ─── Keyboard Shortcuts ────────────────────────────────
        document.addEventListener('keydown', (e) => {
            // Prevent defaults for function keys
            if (e.key.startsWith('F') && !e.key.startsWith('Fn')) {
                const fNum = parseInt(e.key.substring(1));
                if (fNum >= 1 && fNum <= 12) e.preventDefault();
            }

            switch (e.key) {
                case 'F1': nuevaSesion(); break;
                case 'F2': mostrarModalCerrar(); break;
                case 'F4':
                    if (state.sesionActiva) { el.txtCodigoBarras.disabled = false; el.txtCodigoBarras.focus(); }
                    break;
                case 'F5': abrirModalPago(); break;
                case 'F7': if (state.sesionActiva) el.txtNombre.focus(); break;
                case 'F8': el.cmbSesion.focus(); break;
                case 'Escape': cancelarSesion(); break;
            }

            // Ctrl combos
            if (e.ctrlKey) {
                switch (e.key.toLowerCase()) {
                    case 'd':
                        e.preventDefault();
                        if (state.sesionActiva) el.txtDescuento.focus();
                        break;
                    case 't':
                        e.preventDefault();
                        if (state.sesionActiva) el.txtTotal.focus();
                        break;
                    case 'r':
                        e.preventDefault();
                        if (state.sesionActiva) el.cmbRepetida.focus();
                        break;
                }
            }
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  INIT
    // ═══════════════════════════════════════════════════════════════

    document.addEventListener('DOMContentLoaded', () => {
        initEventHandlers();
        cargarSesiones();
        cargarRepetidas();
    });

})();
