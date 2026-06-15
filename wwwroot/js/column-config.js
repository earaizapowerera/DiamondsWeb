/**
 * column-config.js — Columnas configurables para tablas de piezas.
 * Replica la funcionalidad VB6 de ComboColumnas/TablasColumnas.
 *
 * Uso: en la pagina, inicializar con:
 *   initColumnConfig('vPiezasWeb', '#piezasTable');
 *
 * Requiere: data-col="NombreColumna" en cada <th> y <td> de la tabla.
 */
(function () {
    'use strict';

    let _vista = '';
    let _tableSelector = '';
    let _config = null;

    /**
     * Inicializa el modulo de columnas configurables.
     * @param {string} vista - Nombre de la vista (e.g. 'vPiezasWeb')
     * @param {string} tableSelector - Selector CSS de la tabla
     */
    window.initColumnConfig = async function (vista, tableSelector) {
        _vista = vista;
        _tableSelector = tableSelector;

        try {
            const resp = await fetch(`/api/columnas/${vista}`, {
                headers: { 'Accept': 'application/json' }
            });
            if (!resp.ok) throw new Error(`HTTP ${resp.status}`);
            _config = await resp.json();

            renderColumnSelector();
            applyColumnVisibility();
        } catch (err) {
            console.error('Error cargando configuracion de columnas:', err);
        }
    };

    /**
     * Renderiza el dropdown de checkboxes para seleccionar columnas.
     */
    function renderColumnSelector() {
        const container = document.getElementById('columnSelectorContainer');
        if (!container || !_config) return;

        const visibles = new Set(_config.columnasVisibles || []);
        const todas = _config.todasLasColumnas || [];

        let html = '<div class="dropdown">';
        html += '<button class="btn btn-outline-secondary btn-sm dropdown-toggle" type="button" data-bs-toggle="dropdown" data-bs-auto-close="outside" title="Configurar columnas visibles">';
        html += '<i class="fa-solid fa-table-columns me-1"></i>Columnas';
        html += '</button>';
        html += '<div class="dropdown-menu shadow-sm p-3" style="min-width:220px; max-height:400px; overflow-y:auto;">';
        html += '<div class="d-flex justify-content-between align-items-center mb-2">';
        html += '<strong class="small">Columnas visibles</strong>';
        html += '<button type="button" class="btn btn-link btn-sm p-0 text-decoration-none" id="btnVerTodas" title="Mostrar todas las columnas">';
        html += '<i class="fa-solid fa-eye me-1"></i>Ver Todas';
        html += '</button>';
        html += '</div>';
        html += '<hr class="my-1">';

        for (const col of todas) {
            const checked = visibles.has(col.key) ? 'checked' : '';
            html += '<div class="form-check">';
            html += `<input class="form-check-input col-toggle" type="checkbox" value="${col.key}" id="col_${col.key}" ${checked}>`;
            html += `<label class="form-check-label small" for="col_${col.key}">${col.label}</label>`;
            html += '</div>';
        }

        html += '<hr class="my-2">';
        html += '<div class="d-flex gap-1">';
        html += '<button type="button" class="btn btn-primary btn-sm flex-grow-1" id="btnGuardarColumnas">';
        html += '<i class="fa-solid fa-floppy-disk me-1"></i>Guardar';
        html += '</button>';
        html += '<button type="button" class="btn btn-outline-secondary btn-sm" id="btnResetColumnas" title="Restablecer a valores predeterminados">';
        html += '<i class="fa-solid fa-rotate-right"></i>';
        html += '</button>';
        html += '</div>';
        html += '</div></div>';

        container.innerHTML = html;

        // Event listeners
        document.getElementById('btnVerTodas')?.addEventListener('click', verTodas);
        document.getElementById('btnGuardarColumnas')?.addEventListener('click', guardarColumnas);
        document.getElementById('btnResetColumnas')?.addEventListener('click', resetColumnas);

        // Toggle inmediato al hacer check/uncheck
        container.querySelectorAll('.col-toggle').forEach(cb => {
            cb.addEventListener('change', () => {
                applyColumnVisibilityFromCheckboxes();
            });
        });
    }

    /**
     * Aplica visibilidad de columnas basandose en la config cargada.
     */
    function applyColumnVisibility() {
        if (!_config) return;
        const visibles = new Set(_config.columnasVisibles || []);
        const todas = _config.todasLasColumnas || [];

        for (const col of todas) {
            toggleColumn(col.key, visibles.has(col.key));
        }
        updateColspan();
    }

    /**
     * Aplica visibilidad basandose en los checkboxes actuales (sin guardar).
     */
    function applyColumnVisibilityFromCheckboxes() {
        const container = document.getElementById('columnSelectorContainer');
        if (!container) return;

        container.querySelectorAll('.col-toggle').forEach(cb => {
            toggleColumn(cb.value, cb.checked);
        });
        updateColspan();
    }

    /**
     * Muestra/oculta una columna por su nombre.
     */
    function toggleColumn(colName, visible) {
        const table = document.querySelector(_tableSelector);
        if (!table) return;

        table.querySelectorAll(`[data-col="${colName}"]`).forEach(el => {
            el.style.display = visible ? '' : 'none';
        });
    }

    /**
     * Actualiza el colspan del mensaje "No se encontraron piezas".
     */
    function updateColspan() {
        const table = document.querySelector(_tableSelector);
        if (!table) return;

        const visibleHeaders = table.querySelectorAll('thead th:not([style*="display: none"])');
        const emptyRow = table.querySelector('td[colspan]');
        if (emptyRow) {
            emptyRow.setAttribute('colspan', visibleHeaders.length.toString());
        }
    }

    /**
     * Marca todas las columnas como visibles.
     */
    function verTodas() {
        const container = document.getElementById('columnSelectorContainer');
        if (!container) return;

        container.querySelectorAll('.col-toggle').forEach(cb => {
            cb.checked = true;
        });
        applyColumnVisibilityFromCheckboxes();
    }

    /**
     * Guarda la configuracion actual via AJAX.
     */
    async function guardarColumnas() {
        const container = document.getElementById('columnSelectorContainer');
        const btn = document.getElementById('btnGuardarColumnas');
        if (!container || !btn) return;

        const columnasVisibles = [];
        container.querySelectorAll('.col-toggle:checked').forEach(cb => {
            columnasVisibles.push(cb.value);
        });

        if (columnasVisibles.length === 0) {
            showToast('Debe seleccionar al menos una columna', 'warning');
            return;
        }

        btn.disabled = true;
        btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin me-1"></i>Guardando...';

        try {
            // Obtener antiforgery token
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            const headers = {
                'Content-Type': 'application/json',
                'Accept': 'application/json'
            };
            if (tokenInput) {
                headers['RequestVerificationToken'] = tokenInput.value;
            }

            const resp = await fetch('/api/columnas/guardar', {
                method: 'POST',
                headers: headers,
                body: JSON.stringify({
                    vista: _vista,
                    descripcion: 'Mi configuracion',
                    columnasVisibles: columnasVisibles
                })
            });

            if (!resp.ok) throw new Error(`HTTP ${resp.status}`);

            showToast('Columnas guardadas correctamente', 'success');
        } catch (err) {
            console.error('Error guardando columnas:', err);
            showToast('Error al guardar: ' + err.message, 'danger');
        } finally {
            btn.disabled = false;
            btn.innerHTML = '<i class="fa-solid fa-floppy-disk me-1"></i>Guardar';
        }
    }

    /**
     * Restaura la configuracion a valores predeterminados.
     */
    async function resetColumnas() {
        try {
            await fetch(`/api/columnas/${_vista}`, { method: 'DELETE' });

            // Recargar config
            const resp = await fetch(`/api/columnas/${_vista}`, {
                headers: { 'Accept': 'application/json' }
            });
            if (resp.ok) {
                _config = await resp.json();
                renderColumnSelector();
                applyColumnVisibility();
                showToast('Columnas restablecidas a valores predeterminados', 'info');
            }
        } catch (err) {
            console.error('Error reseteando columnas:', err);
            showToast('Error al restablecer: ' + err.message, 'danger');
        }
    }

    /**
     * Muestra un toast de Bootstrap 5 para feedback.
     */
    function showToast(message, type) {
        // Crear toast container si no existe
        let toastContainer = document.getElementById('colConfigToasts');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'colConfigToasts';
            toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            toastContainer.style.zIndex = '1100';
            document.body.appendChild(toastContainer);
        }

        const toastId = 'toast_' + Date.now();
        const iconMap = {
            success: 'fa-check-circle',
            danger: 'fa-exclamation-circle',
            warning: 'fa-exclamation-triangle',
            info: 'fa-info-circle'
        };
        const icon = iconMap[type] || 'fa-info-circle';

        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-bg-${type} border-0" role="alert">
                <div class="d-flex">
                    <div class="toast-body">
                        <i class="fa-solid ${icon} me-1"></i>${message}
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>`;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastEl = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
        toast.show();

        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    }
})();
