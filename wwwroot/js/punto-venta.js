// ═══════════════════════════════════════════════════════════════
// Punto de Venta Web — JavaScript
// Migración de frmPuntodeVenta.frm (VB6)
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
        pendingRepetida: null  // pieza repetida esperando cantidad
    };

    // ─── Elementos DOM ─────────────────────────────────────────
    const $ = id => document.getElementById(id);
    const el = {
        txtUsuario: $('txtUsuario'),
        lblUsuario: $('lblUsuario'),
        txtVendedor: $('txtVendedor'),
        cmbSesion: $('cmbSesion'),
        dtFechaBaja: $('dtFechaBaja'),
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
        txtNombre: $('txtNombre'),
        txtTelefonos: $('txtTelefonos'),
        txtComentarios: $('txtComentarios'),
        txtFormaPago: $('txtFormaPago'),
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
    async function api(handler, method, body) {
        const url = `/PuntoVenta?handler=${handler}`;
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
            opt.textContent = `${s.nombreUsuario} (Nota: ${s.idNota})`;
            opt.dataset.userId = s.idUsuario;
            el.cmbSesion.appendChild(opt);
        });
        if (r.sesiones.length === 1) {
            el.cmbSesion.value = r.sesiones[0].idNota;
            seleccionarSesion(r.sesiones[0].idNota);
        }
    }

    async function seleccionarSesion(idNota) {
        const r = await apiGet('Sesion', { idNota });
        if (!r.ok) { showError(r.error); return; }
        state.idNota = r.sesion.idNota;
        state.idUsuario = r.sesion.idUsuario;
        state.idVendedor = r.sesion.idVendedor;
        state.esFactura = r.sesion.factura;
        state.descuentoPct = r.sesion.descuento || 0;
        state.sesionActiva = true;

        el.txtUsuario.value = r.sesion.idUsuario;
        el.lblUsuario.textContent = r.sesion.nombreUsuario;
        el.txtVendedor.value = r.sesion.idVendedor;
        el.txtNombre.value = r.sesion.nombreCliente || '';
        el.txtTelefonos.value = r.sesion.telefonos || '';
        el.txtComentarios.value = r.sesion.comentarios || '';
        el.chkFactura.checked = r.sesion.factura;
        if (r.sesion.fechaBaja) {
            el.dtFechaBaja.value = r.sesion.fechaBaja.substring(0, 10);
        }

        renderPiezas(r.piezas);
        renderPagos(r.pagos);
        updateResumen(r.resumen);
        habilitarCampos(true);
        el.txtCodigoBarras.focus();
    }

    async function nuevaSesion() {
        const userId = parseInt(el.txtUsuario.value);
        if (!userId || isNaN(userId)) {
            showError('Ingrese un ID de usuario válido.');
            el.txtUsuario.focus();
            return;
        }
        const fechaBaja = el.dtFechaBaja.value || null;
        const r = await apiPost('CrearSesion', {
            idUsuario: userId,
            fechaBaja: fechaBaja ? fechaBaja + 'T00:00:00' : null
        });
        if (!r.ok) { showError(r.error); return; }
        state.idNota = r.sesion.idNota;
        state.idUsuario = r.sesion.idUsuario;
        state.idVendedor = r.sesion.idVendedor;
        state.sesionActiva = true;

        el.lblUsuario.textContent = r.sesion.nombreUsuario;
        el.txtVendedor.value = r.sesion.idVendedor;
        await cargarSesiones();
        el.cmbSesion.value = r.sesion.idNota;
        habilitarCampos(true);
        limpiarGrids();
        updateResumenVacio();
        el.txtCodigoBarras.focus();
    }

    async function cancelarSesion() {
        if (!state.idNota) return;
        if (!confirm('¿Cancelar la sesión actual? Se borrarán las piezas y pagos capturados.')) return;
        await apiPost('CancelarSesion', { idNota: state.idNota });
        resetEstado();
        await cargarSesiones();
    }

    function resetEstado() {
        state.idNota = null;
        state.idUsuario = null;
        state.idVendedor = null;
        state.esFactura = false;
        state.descuentoPct = 0;
        state.sobrePrecio = 0;
        state.sesionActiva = false;
        state.pendingRepetida = null;
        habilitarCampos(false);
        limpiarGrids();
        updateResumenVacio();
        el.txtNombre.value = '';
        el.txtTelefonos.value = '';
        el.txtComentarios.value = '';
        el.txtFormaPago.value = '';
        el.txtUsuario.value = '';
        el.lblUsuario.textContent = '';
        el.txtVendedor.value = '';
        el.chkFactura.checked = false;
        el.divFactura.style.display = 'none';
        el.lblRepetidaCantidad.style.display = 'none';
    }

    function habilitarCampos(activo) {
        el.txtCodigoBarras.disabled = !activo;
        el.txtDescuento.disabled = !activo;
        el.txtSobrePrecio.disabled = !activo;
        el.txtNombre.disabled = !activo;
        el.txtTelefonos.disabled = !activo;
        el.txtComentarios.disabled = !activo;
        el.chkFactura.disabled = !activo;
        el.btnCerrarNota.disabled = !activo;
        el.btnCancelar.disabled = !activo;
        el.txtUsuario.disabled = activo;
    }

    // ═══════════════════════════════════════════════════════════════
    //  PIEZAS
    // ═══════════════════════════════════════════════════════════════

    async function agregarPieza(codigoBarras, cantidad) {
        if (!state.idNota || !codigoBarras) return;

        // Primero buscar para ver si es repetida
        const lookup = await apiGet('BuscarPieza', { cb: codigoBarras });
        if (!lookup.ok) { showError(lookup.error); el.txtCodigoBarras.value = ''; el.txtCodigoBarras.focus(); return; }

        if (lookup.pieza.tipoPieza === 'Repetida' && !cantidad) {
            // Pedir cantidad
            state.pendingRepetida = codigoBarras;
            el.lblRepetidaCantidad.style.display = 'block';
            el.txtCantidadRepetida.value = '1';
            el.txtCantidadRepetida.focus();
            el.txtCantidadRepetida.select();
            return;
        }

        const r = await apiPost('AgregarPieza', {
            idNota: state.idNota,
            codigoBarras: codigoBarras,
            cantidad: cantidad || null,
            esFactura: state.esFactura
        });
        if (!r.ok) { showError(r.error); el.txtCodigoBarras.value = ''; el.txtCodigoBarras.focus(); return; }

        renderPiezas(r.piezas);
        updateResumen(r.resumen);
        el.txtCodigoBarras.value = '';
        el.txtCodigoBarras.focus();
        el.lblRepetidaCantidad.style.display = 'none';
        state.pendingRepetida = null;
    }

    async function eliminarPieza(codigoBarras) {
        if (!state.idNota || !codigoBarras) return;
        const r = await apiPost('EliminarPieza', {
            idNota: state.idNota,
            codigoBarras: codigoBarras,
            descuentoPct: state.descuentoPct,
            sobrePrecio: state.sobrePrecio,
            esFactura: state.esFactura
        });
        if (r.ok) {
            renderPiezas(r.piezas);
            updateResumen(r.resumen);
        }
    }

    function renderPiezas(piezas) {
        if (!piezas || piezas.length === 0) {
            el.tbodyPiezas.innerHTML = '<tr id="trPiezasVacio"><td colspan="6" class="text-center text-muted py-4"><i class="fa-solid fa-barcode me-1"></i>Escanee un código de barras para comenzar</td></tr>';
            return;
        }
        el.tbodyPiezas.innerHTML = piezas.map(p => `
            <tr data-cb="${p.codigoBarras}" class="pieza-row">
                <td><code>${p.codigoBarras}</code></td>
                <td class="small">${escapeHtml(p.descripcion)}</td>
                <td class="text-center">${p.cantidad}</td>
                <td class="text-end">${fmt(p.subTotal)}</td>
                <td class="text-end fw-bold">${fmt(p.total)}</td>
                <td><button class="btn btn-sm btn-outline-danger btn-eliminar-pieza" data-cb="${p.codigoBarras}" title="Cancelar pieza"><i class="fa-solid fa-xmark"></i></button></td>
            </tr>
        `).join('');

        // Event listeners para eliminar
        el.tbodyPiezas.querySelectorAll('.btn-eliminar-pieza').forEach(btn => {
            btn.addEventListener('click', () => eliminarPieza(btn.dataset.cb));
        });
    }

    // ═══════════════════════════════════════════════════════════════
    //  PAGOS
    // ═══════════════════════════════════════════════════════════════

    let opcionesPago = [];

    async function cargarOpcionesPago() {
        const r = await apiGet('OpcionesPago');
        if (!r.ok) return;
        opcionesPago = r.opciones;
        renderPaymentGrid();
    }

    function renderPaymentGrid() {
        el.paymentGrid.innerHTML = opcionesPago.map(op => `
            <button type="button" class="btn btn-outline-success btn-pago-opcion"
                    data-id="${op.idOpcionPago}" data-moneda="${op.idMoneda}" data-extranjera="${op.extranjera}">
                ${escapeHtml(op.opcionPago)}
            </button>
        `).join('');

        el.paymentGrid.querySelectorAll('.btn-pago-opcion').forEach(btn => {
            btn.addEventListener('click', () => {
                // Deseleccionar otros
                el.paymentGrid.querySelectorAll('.btn-pago-opcion').forEach(b => b.classList.remove('active', 'btn-success'));
                btn.classList.add('active', 'btn-success');
                btn.classList.remove('btn-outline-success');

                el.pagoIdOpcionPago.value = btn.dataset.id;
                el.pagoIdMoneda.value = btn.dataset.moneda;
                el.pagoExtranjera.value = btn.dataset.extranjera;

                const extranjera = btn.dataset.extranjera === 'true' || btn.dataset.extranjera === 'True';
                el.divMonedaExtranjera.style.display = extranjera ? 'block' : 'none';

                // Default: lo que resta
                const cambio = parseFloat(el.txtCambio.value.replace(/,/g, '')) || 0;
                el.pagoImporte.value = Math.abs(cambio).toFixed(2);
                el.pagoImporteOriginal.value = '';
                el.pagoTipoCambio.value = '';
                el.pagoEquivalenteMXN.value = '';

                if (extranjera) {
                    el.pagoImporteOriginal.focus();
                } else {
                    el.pagoImporte.focus();
                    el.pagoImporte.select();
                }
            });
        });
    }

    function abrirModalPago() {
        if (!state.idNota) return;
        // Reset
        el.paymentGrid.querySelectorAll('.btn-pago-opcion').forEach(b => {
            b.classList.remove('active', 'btn-success');
            b.classList.add('btn-outline-success');
        });
        el.pagoIdOpcionPago.value = '';
        el.pagoImporte.value = '';
        el.pagoImporteOriginal.value = '';
        el.pagoTipoCambio.value = '';
        el.pagoEquivalenteMXN.value = '';
        el.divMonedaExtranjera.style.display = 'none';

        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('modalPago'));
        modal.show();
    }

    async function registrarPago() {
        const idOp = parseInt(el.pagoIdOpcionPago.value);
        if (!idOp) { showError('Seleccione una forma de pago.'); return; }

        const extranjera = el.pagoExtranjera.value === 'true' || el.pagoExtranjera.value === 'True';
        let importe, tipoCambio = 0, importeOriginal = 0;

        if (extranjera) {
            importeOriginal = parseFloat(el.pagoImporteOriginal.value) || 0;
            tipoCambio = parseFloat(el.pagoTipoCambio.value) || 0;
            importe = importeOriginal * tipoCambio;
            if (importe <= 0) { showError('Ingrese importe y tipo de cambio.'); return; }
        } else {
            importe = parseFloat(el.pagoImporte.value) || 0;
            if (importe <= 0) { showError('Ingrese un importe.'); return; }
        }

        const r = await apiPost('RegistrarPago', {
            idNota: state.idNota,
            idOpcionPago: idOp,
            importe: importe,
            tipoCambio: tipoCambio,
            importeOriginal: importeOriginal
        });

        if (!r.ok) { showError(r.error); return; }

        renderPagos(r.pagos);
        await recalcularResumen();
        bootstrap.Modal.getInstance(document.getElementById('modalPago')).hide();

        // Verificar si está cubierto → ofrecer cerrar
        revisarSiCerrar();
    }

    async function eliminarPago(idOpcionPago, importe) {
        if (!state.idNota) return;
        const r = await apiPost('EliminarPago', {
            idNota: state.idNota,
            idOpcionPago: idOpcionPago,
            importe: importe
        });
        if (r.ok) {
            renderPagos(r.pagos);
            await recalcularResumen();
        }
    }

    function renderPagos(pagos) {
        if (!pagos || pagos.length === 0) {
            el.tbodyPagos.innerHTML = '<tr id="trPagosVacio"><td colspan="5" class="text-center text-muted py-3">Sin pagos registrados</td></tr>';
            el.txtFormaPago.value = '';
            return;
        }
        el.tbodyPagos.innerHTML = pagos.map(p => `
            <tr class="pago-row" data-idop="${p.idOpcionPago}" data-importe="${p.importe}">
                <td>${escapeHtml(p.opcionPago)}</td>
                <td class="text-end">${fmt(p.importe)}</td>
                <td class="text-end">${p.tipoCambio > 0 ? fmt(p.tipoCambio) : '-'}</td>
                <td class="text-end">${p.importeOriginal > 0 ? fmt(p.importeOriginal) : '-'}</td>
                <td><button class="btn btn-sm btn-outline-danger btn-eliminar-pago" data-idop="${p.idOpcionPago}" data-importe="${p.importe}" title="Cancelar pago"><i class="fa-solid fa-xmark"></i></button></td>
            </tr>
        `).join('');

        el.tbodyPagos.querySelectorAll('.btn-eliminar-pago').forEach(btn => {
            btn.addEventListener('click', () => eliminarPago(parseInt(btn.dataset.idop), parseFloat(btn.dataset.importe)));
        });

        // Concatenar formas de pago
        const formas = [...new Set(pagos.filter(p => p.importe > 0).map(p => p.opcionPago))];
        el.txtFormaPago.value = formas.join(' / ');
    }

    // ═══════════════════════════════════════════════════════════════
    //  RESUMEN / TOTALES
    // ═══════════════════════════════════════════════════════════════

    async function recalcularResumen() {
        if (!state.idNota) return;
        const r = await apiGet('Resumen', {
            idNota: state.idNota,
            descuento: state.descuentoPct,
            sobrePrecio: state.sobrePrecio,
            factura: state.esFactura
        });
        if (r.ok) updateResumen(r.resumen);
    }

    function updateResumen(res) {
        if (!res) return;
        el.txtSubTotal.value = fmt(res.subTotal);
        el.txtTotal.value = fmt(res.total);
        el.txtTotalPagado.value = fmt(res.totalPagado);
        el.txtCambio.value = fmt(res.cambio);
        el.txtFormaPago.value = res.formasPago || el.txtFormaPago.value;

        if (res.esFactura) {
            el.divFactura.style.display = 'block';
            el.txtSubTotalFactura.value = fmt(res.subTotal);
            el.txtTotalFactura.value = fmt(res.totalFactura);
        } else {
            el.divFactura.style.display = 'none';
        }
    }

    function updateResumenVacio() {
        ['txtSubTotal', 'txtTotal', 'txtTotalPagado', 'txtCambio', 'txtSubTotalFactura', 'txtTotalFactura'].forEach(id => {
            document.getElementById(id).value = '';
        });
    }

    function limpiarGrids() {
        el.tbodyPiezas.innerHTML = '<tr id="trPiezasVacio"><td colspan="6" class="text-center text-muted py-4"><i class="fa-solid fa-barcode me-1"></i>Escanee un código de barras para comenzar</td></tr>';
        el.tbodyPagos.innerHTML = '<tr id="trPagosVacio"><td colspan="5" class="text-center text-muted py-3">Sin pagos registrados</td></tr>';
    }

    // ═══════════════════════════════════════════════════════════════
    //  CERRAR NOTA
    // ═══════════════════════════════════════════════════════════════

    function revisarSiCerrar() {
        const totalField = state.esFactura ? el.txtTotalFactura : el.txtTotal;
        const total = parseFloat(totalField.value.replace(/,/g, '')) || 0;
        const pagado = parseFloat(el.txtTotalPagado.value.replace(/,/g, '')) || 0;

        if (pagado >= total && total > 0 && el.txtNombre.value.trim()) {
            if (confirm('¿Desea Cerrar e Imprimir la Nota?')) {
                cerrarNota();
            }
        }
    }

    function mostrarModalCerrar() {
        if (!state.idNota) return;
        document.getElementById('modalTotal').textContent = '$' + el.txtTotal.value;
        document.getElementById('modalPagado').textContent = '$' + el.txtTotalPagado.value;
        document.getElementById('modalCambio').textContent = '$' + el.txtCambio.value;
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('modalCerrarNota'));
        modal.show();
    }

    async function cerrarNota() {
        if (!state.idNota) return;

        // Validaciones
        if (!el.dtFechaBaja.value) { showError('Falta la fecha de baja (fecha del corte).'); el.dtFechaBaja.focus(); return; }
        if (!el.txtNombre.value.trim()) { showError('Falta el nombre del cliente.'); el.txtNombre.focus(); return; }

        const totalField = state.esFactura ? el.txtTotalFactura : el.txtTotal;
        const total = parseFloat(totalField.value.replace(/,/g, '')) || 0;
        const pagado = parseFloat(el.txtTotalPagado.value.replace(/,/g, '')) || 0;
        if (pagado < total) { showError('Todavía no ha sido cubierto el total de la nota.'); return; }

        // Cerrar modal si está abierto
        const modalEl = document.getElementById('modalCerrarNota');
        const modal = bootstrap.Modal.getInstance(modalEl);
        if (modal) modal.hide();

        const r = await apiPost('CerrarNota', {
            idNota: state.idNota,
            nombreCliente: el.txtNombre.value.trim(),
            telefonos: el.txtTelefonos.value.trim(),
            comentarios: el.txtComentarios.value.trim(),
            factura: state.esFactura,
            fechaBaja: el.dtFechaBaja.value + 'T00:00:00',
            descuento: state.descuentoPct,
            sobrePrecio: state.sobrePrecio,
            bruto: parseFloat(el.txtSubTotal.value.replace(/,/g, '')) || 0,
            neto: parseFloat(el.txtTotal.value.replace(/,/g, '')) || 0,
            total: total,
            formaPago: el.txtFormaPago.value,
            idVendedor: parseInt(el.txtVendedor.value) || state.idVendedor
        });

        if (!r.ok) { showError(r.error); return; }

        // Imprimir
        await imprimirNota(r.idNota);

        // Reset y recargar sesiones
        resetEstado();
        await cargarSesiones();
        showSuccess('Nota cerrada exitosamente.');
    }

    async function imprimirNota(idNota) {
        const r = await apiGet('NotaCerrada', { idNota });
        if (!r.ok) return;
        const nota = r.nota;

        const piezasHtml = nota.piezas.map(p => `
            <tr>
                <td>${escapeHtml(p.codigoBarras)}</td>
                <td>${escapeHtml(p.descripcion)}</td>
                <td class="text-center">${p.cantidad}</td>
                <td class="text-end">$${fmt(p.total)}</td>
            </tr>
        `).join('');

        const pagosHtml = nota.pagos.map(p => `
            <tr><td>${escapeHtml(p.opcionPago)}</td><td class="text-end">$${fmt(p.importe)}</td></tr>
        `).join('');

        const tipo = nota.factura ? 'FACTURA' : 'CERTIFICADO DE GARANTÍA';
        const fecha = nota.fechaBaja ? new Date(nota.fechaBaja).toLocaleDateString('es-MX') : '';

        el.printArea.innerHTML = `
            <div class="print-nota">
                <div class="text-center mb-3">
                    <h3>DIAMONDS</h3>
                    <h5>${tipo}</h5>
                    <p>Nota: ${nota.idNota} | Fecha: ${fecha}</p>
                </div>
                <p><strong>Cliente:</strong> ${escapeHtml(nota.nombreCliente)}</p>
                ${nota.telefonos ? `<p><strong>Tel:</strong> ${escapeHtml(nota.telefonos)}</p>` : ''}
                <p><strong>Vendedor:</strong> ${escapeHtml(nota.nombreVendedor || '')}</p>
                <table class="table table-sm">
                    <thead><tr><th>Código</th><th>Descripción</th><th>Cant</th><th class="text-end">Precio</th></tr></thead>
                    <tbody>${piezasHtml}</tbody>
                </table>
                <hr/>
                <table class="table table-sm">
                    <thead><tr><th>Forma de Pago</th><th class="text-end">Importe</th></tr></thead>
                    <tbody>${pagosHtml}</tbody>
                </table>
                <hr/>
                <div class="text-end">
                    <p><strong>SubTotal:</strong> $${fmt(nota.bruto)}</p>
                    ${nota.descuento > 0 ? `<p><strong>Descuento:</strong> ${nota.descuento}%</p>` : ''}
                    <p class="fs-5"><strong>TOTAL: $${fmt(nota.total)}</strong></p>
                </div>
                ${nota.comentarios ? `<p class="mt-3"><em>${escapeHtml(nota.comentarios)}</em></p>` : ''}
                <div class="text-center mt-4 small text-muted">
                    <p>Gracias por su compra</p>
                </div>
            </div>
        `;

        // Print
        const printWin = window.open('', '_blank', 'width=600,height=800');
        printWin.document.write(`
            <!DOCTYPE html><html><head><title>${tipo} - ${nota.idNota}</title>
            <style>
                body { font-family: Arial, sans-serif; margin: 20px; font-size: 12px; }
                h3, h5 { margin: 5px 0; }
                table { width: 100%; border-collapse: collapse; }
                th, td { padding: 4px 8px; border-bottom: 1px solid #ddd; }
                .text-center { text-align: center; }
                .text-end { text-align: right; }
                .fs-5 { font-size: 16px; }
                hr { border: 0; border-top: 1px dashed #999; margin: 10px 0; }
                @media print { body { margin: 0; } }
            </style></head><body>
            ${el.printArea.innerHTML}
            </body></html>
        `);
        printWin.document.close();
        printWin.focus();
        setTimeout(() => { printWin.print(); }, 500);
    }

    // ═══════════════════════════════════════════════════════════════
    //  DESCUENTO
    // ═══════════════════════════════════════════════════════════════

    async function aplicarDescuento() {
        state.descuentoPct = parseFloat(el.txtDescuento.value) || 0;
        state.sobrePrecio = parseFloat(el.txtSobrePrecio.value) || 0;

        if (state.descuentoPct > 20) {
            showError('Imposible realizar descuentos de esta magnitud. Favor de rectificar.');
            state.descuentoPct = 20;
            el.txtDescuento.value = '20';
        }

        await recalcularResumen();

        // También actualizar en BD
        if (state.idNota) {
            await apiPost('ActualizarNota', {
                idNota: state.idNota,
                factura: null,
                fechaBaja: null,
                idVendedor: null,
                nombreCliente: null,
                telefonos: null,
                comentarios: null
            });
        }
    }

    // ═══════════════════════════════════════════════════════════════
    //  TECLAS DE FUNCIÓN
    // ═══════════════════════════════════════════════════════════════

    document.addEventListener('keydown', function (e) {
        // Ignorar si está en un modal
        if (document.querySelector('.modal.show')) return;

        switch (e.key) {
            case 'F1':
                e.preventDefault();
                resetEstado();
                el.txtUsuario.disabled = false;
                el.txtUsuario.focus();
                break;
            case 'F2':
                e.preventDefault();
                if (state.sesionActiva) mostrarModalCerrar();
                break;
            case 'F4':
                e.preventDefault();
                if (state.sesionActiva) { el.txtCodigoBarras.disabled = false; el.txtCodigoBarras.focus(); }
                break;
            case 'F5':
                e.preventDefault();
                if (state.sesionActiva) abrirModalPago();
                break;
            case 'F6':
                e.preventDefault();
                // Focus primera pieza
                const firstPieza = el.tbodyPiezas.querySelector('.pieza-row');
                if (firstPieza) firstPieza.focus();
                break;
            case 'F7':
                e.preventDefault();
                if (state.sesionActiva) el.txtNombre.focus();
                break;
            case 'F8':
                e.preventDefault();
                el.cmbSesion.focus();
                break;
            case 'F9':
                e.preventDefault();
                const firstPago = el.tbodyPagos.querySelector('.pago-row');
                if (firstPago) firstPago.focus();
                break;
            case 'F11':
                e.preventDefault();
                el.txtUsuario.disabled = false;
                el.txtUsuario.focus();
                break;
            case 'Escape':
                e.preventDefault();
                if (state.pendingRepetida) {
                    state.pendingRepetida = null;
                    el.lblRepetidaCantidad.style.display = 'none';
                    el.txtCodigoBarras.value = '';
                    el.txtCodigoBarras.focus();
                } else if (state.sesionActiva) {
                    cancelarSesion();
                }
                break;
        }

        // Ctrl shortcuts
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
                case 'f':
                    e.preventDefault();
                    el.dtFechaBaja.focus();
                    break;
            }
        }
    });

    // ═══════════════════════════════════════════════════════════════
    //  EVENT LISTENERS
    // ═══════════════════════════════════════════════════════════════

    // Nueva sesión
    el.btnNuevaSesion.addEventListener('click', nuevaSesion);

    // Enter en usuario → crear sesión
    el.txtUsuario.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            nuevaSesion();
        }
    });

    // Seleccionar sesión
    el.cmbSesion.addEventListener('change', function () {
        const idNota = parseInt(this.value);
        if (idNota) seleccionarSesion(idNota);
    });

    // Código de barras → Enter = agregar pieza
    el.txtCodigoBarras.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            const cb = this.value.trim();
            if (cb) agregarPieza(cb);
        }
    });
    // También al perder foco (como el legacy)
    el.txtCodigoBarras.addEventListener('blur', function () {
        const cb = this.value.trim();
        if (cb && state.sesionActiva) agregarPieza(cb);
    });

    // Confirmar cantidad repetida
    el.btnConfirmarRepetida.addEventListener('click', function () {
        if (state.pendingRepetida) {
            const cant = parseInt(el.txtCantidadRepetida.value) || 1;
            agregarPieza(state.pendingRepetida, cant);
        }
    });
    el.txtCantidadRepetida.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            el.btnConfirmarRepetida.click();
        }
    });

    // Descuento / SobrePrecio → recalcular al cambiar
    el.txtDescuento.addEventListener('change', aplicarDescuento);
    el.txtSobrePrecio.addEventListener('change', aplicarDescuento);

    // Total editable → proceso inverso (como el legacy)
    el.txtTotal.addEventListener('focus', function () {
        this.readOnly = false;
    });
    el.txtTotal.addEventListener('blur', async function () {
        this.readOnly = true;
        const totalDeseado = parseFloat(this.value.replace(/,/g, '')) || 0;
        const subTotal = parseFloat(el.txtSubTotal.value.replace(/,/g, '')) || 0;
        if (subTotal === 0) return;

        if (totalDeseado > subTotal) {
            // SobrePrecio
            state.sobrePrecio = totalDeseado - subTotal;
            state.descuentoPct = 0;
            el.txtSobrePrecio.value = state.sobrePrecio.toFixed(2);
            el.txtDescuento.value = '0';
        } else {
            state.sobrePrecio = 0;
            state.descuentoPct = parseFloat(((1 - totalDeseado / subTotal) * 100).toFixed(2));
            el.txtSobrePrecio.value = '0';
            el.txtDescuento.value = state.descuentoPct.toFixed(2);
        }

        if (state.descuentoPct > 20) {
            showError('Imposible realizar descuentos de esta magnitud. Favor de rectificar.');
        }

        await recalcularResumen();
    });

    // Factura checkbox
    el.chkFactura.addEventListener('change', async function () {
        state.esFactura = this.checked;
        el.divFactura.style.display = this.checked ? 'block' : 'none';
        if (state.idNota) {
            await apiPost('ActualizarNota', { idNota: state.idNota, factura: this.checked });
            await recalcularResumen();
        }
    });

    // Cliente campos → guardar al perder foco
    ['txtNombre', 'txtTelefonos', 'txtComentarios'].forEach(id => {
        document.getElementById(id).addEventListener('blur', function () {
            if (!state.idNota) return;
            const field = id === 'txtNombre' ? 'nombreCliente' : id === 'txtTelefonos' ? 'telefonos' : 'comentarios';
            apiPost('ActualizarNota', { idNota: state.idNota, [field]: this.value });
        });
    });

    // Fecha baja → guardar
    el.dtFechaBaja.addEventListener('change', function () {
        if (!state.idNota || !this.value) return;
        apiPost('ActualizarNota', { idNota: state.idNota, fechaBaja: this.value + 'T00:00:00' });
    });

    // Vendedor → guardar
    el.txtVendedor.addEventListener('blur', function () {
        if (!state.idNota) return;
        const v = parseInt(this.value);
        if (v) apiPost('ActualizarNota', { idNota: state.idNota, idVendedor: v });
    });

    // Cancelar sesión
    el.btnCancelar.addEventListener('click', cancelarSesion);

    // Cerrar nota
    el.btnCerrarNota.addEventListener('click', mostrarModalCerrar);
    el.btnConfirmarCerrar.addEventListener('click', cerrarNota);

    // Registrar pago
    el.btnRegistrarPago.addEventListener('click', registrarPago);

    // Moneda extranjera → calcular equivalente MXN
    el.pagoImporteOriginal.addEventListener('input', calcularEquivalenteMXN);
    el.pagoTipoCambio.addEventListener('input', calcularEquivalenteMXN);
    function calcularEquivalenteMXN() {
        const imp = parseFloat(el.pagoImporteOriginal.value) || 0;
        const tc = parseFloat(el.pagoTipoCambio.value) || 0;
        const equiv = imp * tc;
        el.pagoEquivalenteMXN.value = fmt(equiv);
        el.pagoImporte.value = equiv.toFixed(2);
    }

    // Enter en campos de pago → registrar
    [el.pagoImporte, el.pagoTipoCambio].forEach(input => {
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); registrarPago(); }
        });
    });

    // Catálogo repetidas → setear código de barras
    el.cmbRepetida.addEventListener('change', function () {
        if (this.value && state.sesionActiva) {
            el.txtCodigoBarras.value = this.value;
            el.txtCodigoBarras.focus();
        }
    });

    // ═══════════════════════════════════════════════════════════════
    //  UTILIDADES
    // ═══════════════════════════════════════════════════════════════

    function escapeHtml(str) {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    function showError(msg) {
        const div = document.createElement('div');
        div.className = 'alert alert-danger alert-dismissible fade show pos-alert';
        div.innerHTML = `<i class="fa-solid fa-exclamation-circle me-1"></i>${escapeHtml(msg)}<button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
        document.querySelector('.pos-toolbar').before(div);
        setTimeout(() => div.remove(), 5000);
    }

    function showSuccess(msg) {
        const div = document.createElement('div');
        div.className = 'alert alert-success alert-dismissible fade show pos-alert';
        div.innerHTML = `<i class="fa-solid fa-check-circle me-1"></i>${escapeHtml(msg)}<button type="button" class="btn-close" data-bs-dismiss="alert"></button>`;
        document.querySelector('.pos-toolbar').before(div);
        setTimeout(() => div.remove(), 5000);
    }

    // ═══════════════════════════════════════════════════════════════
    //  INICIALIZACIÓN
    // ═══════════════════════════════════════════════════════════════

    async function init() {
        // Fecha de hoy por default
        el.dtFechaBaja.value = new Date().toISOString().substring(0, 10);

        await Promise.all([
            cargarSesiones(),
            cargarOpcionesPago()
        ]);
    }

    init();
})();
