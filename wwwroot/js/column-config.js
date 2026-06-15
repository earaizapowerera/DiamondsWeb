/**
 * column-config.js — Módulo reutilizable para columnas configurables en grids.
 * Migración de la funcionalidad VB6 "Ver Columnas / Ver Todas".
 *
 * Uso:
 *   initColumnConfig({
 *     tableId: 'tblPiezas',
 *     vista: 'vPiezas',
 *     storageKey: 'colcfg-piezas',
 *     toolbar: document.getElementById('toolbar-piezas')
 *   });
 *
 * Cada <th> del table DEBE tener un atributo data-col="NombreColumna".
 * Las <td> correspondientes heredan la visibilidad por índice de columna.
 */

(function () {
    'use strict';

    /**
     * Inicializa la configuración de columnas para una tabla.
     * @param {Object} opts
     * @param {string} opts.tableId - ID del <table>
     * @param {string} opts.vista - Nombre de la vista para la BD (ej: 'vPiezas')
     * @param {string} opts.storageKey - Clave para localStorage
     * @param {HTMLElement} opts.toolbar - Elemento donde insertar los controles
     */
    function initColumnConfig(opts) {
        var table = document.getElementById(opts.tableId);
        if (!table) return;

        var headers = table.querySelectorAll('thead th[data-col]');
        if (headers.length === 0) return;

        // Construir lista de columnas desde los headers
        var columns = [];
        headers.forEach(function (th) {
            columns.push({
                name: th.getAttribute('data-col'),
                label: th.textContent.trim(),
                index: th.cellIndex
            });
        });

        // Crear UI
        var ui = buildUI(opts, columns);
        if (opts.toolbar) {
            opts.toolbar.appendChild(ui.button);
        }
        document.body.appendChild(ui.offcanvas);

        // Cargar estado guardado de localStorage
        var saved = loadFromStorage(opts.storageKey);
        if (saved) {
            applyVisibility(table, columns, saved);
            updateCheckboxes(ui.checkboxes, saved);
        }

        // Cargar configuraciones guardadas en BD
        loadSavedConfigs(opts.vista, ui.selectConfig);

        // Event listeners
        bindEvents(table, columns, opts, ui);
    }

    /**
     * Construye el botón de toggle y el offcanvas con checkboxes.
     */
    function buildUI(opts, columns) {
        // Botón que abre el offcanvas
        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-outline-secondary btn-sm';
        btn.setAttribute('data-bs-toggle', 'offcanvas');
        btn.setAttribute('data-bs-target', '#offcanvasColCfg-' + opts.tableId);
        btn.innerHTML = '<i class="fa-solid fa-table-columns me-1"></i>Columnas';
        btn.title = 'Configurar columnas visibles';

        // Offcanvas (Bootstrap 5)
        var offcanvas = document.createElement('div');
        offcanvas.className = 'offcanvas offcanvas-end colcfg-offcanvas';
        offcanvas.id = 'offcanvasColCfg-' + opts.tableId;
        offcanvas.setAttribute('tabindex', '-1');
        offcanvas.innerHTML =
            '<div class="offcanvas-header">' +
                '<h6 class="offcanvas-title"><i class="fa-solid fa-table-columns me-2"></i>Columnas</h6>' +
                '<button type="button" class="btn-close" data-bs-dismiss="offcanvas"></button>' +
            '</div>' +
            '<div class="offcanvas-body p-0">' +
                '<div class="colcfg-section px-3 pt-2 pb-2 border-bottom">' +
                    '<label class="form-label form-label-sm mb-1 text-muted">Configuración guardada</label>' +
                    '<div class="input-group input-group-sm">' +
                        '<select class="form-select form-select-sm colcfg-select">' +
                            '<option value="">-- Ninguna --</option>' +
                        '</select>' +
                        '<button type="button" class="btn btn-outline-danger btn-sm colcfg-btn-delete" title="Eliminar configuración">' +
                            '<i class="fa-solid fa-trash"></i>' +
                        '</button>' +
                    '</div>' +
                '</div>' +
                '<div class="colcfg-section px-3 pt-2 pb-2 border-bottom">' +
                    '<div class="d-flex gap-2">' +
                        '<button type="button" class="btn btn-outline-primary btn-sm flex-fill colcfg-btn-all">' +
                            '<i class="fa-solid fa-eye me-1"></i>Ver Todas' +
                        '</button>' +
                        '<button type="button" class="btn btn-outline-success btn-sm flex-fill colcfg-btn-save">' +
                            '<i class="fa-solid fa-floppy-disk me-1"></i>Guardar' +
                        '</button>' +
                    '</div>' +
                '</div>' +
                '<div class="colcfg-checklist px-3 py-2"></div>' +
            '</div>';

        // Generar checkboxes
        var checklist = offcanvas.querySelector('.colcfg-checklist');
        var checkboxes = {};

        columns.forEach(function (col) {
            var wrapper = document.createElement('div');
            wrapper.className = 'form-check colcfg-check-item';

            var input = document.createElement('input');
            input.type = 'checkbox';
            input.className = 'form-check-input';
            input.id = 'colcfg-chk-' + opts.tableId + '-' + col.name;
            input.checked = true;
            input.setAttribute('data-col', col.name);

            var label = document.createElement('label');
            label.className = 'form-check-label';
            label.htmlFor = input.id;
            label.textContent = col.label;

            wrapper.appendChild(input);
            wrapper.appendChild(label);
            checklist.appendChild(wrapper);

            checkboxes[col.name] = input;
        });

        var selectConfig = offcanvas.querySelector('.colcfg-select');

        return {
            button: btn,
            offcanvas: offcanvas,
            checkboxes: checkboxes,
            selectConfig: selectConfig,
            btnAll: offcanvas.querySelector('.colcfg-btn-all'),
            btnSave: offcanvas.querySelector('.colcfg-btn-save'),
            btnDelete: offcanvas.querySelector('.colcfg-btn-delete')
        };
    }

    /**
     * Enlaza todos los eventos de la UI.
     */
    function bindEvents(table, columns, opts, ui) {
        // Toggle individual de checkbox
        columns.forEach(function (col) {
            var chk = ui.checkboxes[col.name];
            if (chk) {
                chk.addEventListener('change', function () {
                    var state = getCurrentState(ui.checkboxes);
                    applyVisibility(table, columns, state);
                    saveToStorage(opts.storageKey, state);
                    ui.selectConfig.value = '';
                });
            }
        });

        // Ver Todas
        ui.btnAll.addEventListener('click', function () {
            Object.keys(ui.checkboxes).forEach(function (key) {
                ui.checkboxes[key].checked = true;
            });
            var state = getCurrentState(ui.checkboxes);
            applyVisibility(table, columns, state);
            saveToStorage(opts.storageKey, state);
            ui.selectConfig.value = '';
        });

        // Guardar configuración
        ui.btnSave.addEventListener('click', function () {
            var nombre = prompt('Nombre de la configuración:');
            if (!nombre || !nombre.trim()) return;

            var state = getCurrentState(ui.checkboxes);
            var body = {
                Descripcion: nombre.trim(),
                Vista: opts.vista,
                Columnas: Object.keys(state).map(function (key) {
                    return { Columna: key, Visible: state[key] };
                })
            };

            fetch('/api/column-config', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(body)
            })
            .then(function (r) {
                if (!r.ok) throw new Error('Error al guardar');
                return r.json();
            })
            .then(function (data) {
                loadSavedConfigs(opts.vista, ui.selectConfig);
                showToast('Configuración "' + nombre.trim() + '" guardada', 'success');
            })
            .catch(function (err) {
                showToast('Error al guardar configuración', 'danger');
            });
        });

        // Cargar configuración seleccionada
        ui.selectConfig.addEventListener('change', function () {
            var id = parseInt(this.value);
            if (!id) return;

            fetch('/api/column-config/' + id)
            .then(function (r) {
                if (!r.ok) throw new Error('Error al cargar');
                return r.json();
            })
            .then(function (data) {
                var state = {};
                data.columnas.forEach(function (c) {
                    state[c.columna] = c.visible;
                });
                // Las columnas que no están en la config guardada se muestran
                columns.forEach(function (col) {
                    if (!(col.name in state)) {
                        state[col.name] = true;
                    }
                });
                applyVisibility(table, columns, state);
                updateCheckboxes(ui.checkboxes, state);
                saveToStorage(opts.storageKey, state);
            })
            .catch(function (err) {
                showToast('Error al cargar configuración', 'danger');
            });
        });

        // Eliminar configuración
        ui.btnDelete.addEventListener('click', function () {
            var id = parseInt(ui.selectConfig.value);
            if (!id) {
                showToast('Seleccione una configuración para eliminar', 'warning');
                return;
            }
            var nombre = ui.selectConfig.options[ui.selectConfig.selectedIndex].text;
            if (!confirm('¿Eliminar la configuración "' + nombre + '"?')) return;

            fetch('/api/column-config/' + id, { method: 'DELETE' })
            .then(function (r) {
                if (!r.ok) throw new Error('Error al eliminar');
                loadSavedConfigs(opts.vista, ui.selectConfig);
                showToast('Configuración eliminada', 'success');
            })
            .catch(function (err) {
                showToast('Error al eliminar configuración', 'danger');
            });
        });
    }

    /**
     * Aplica la visibilidad a todas las filas de la tabla.
     */
    function applyVisibility(table, columns, state) {
        columns.forEach(function (col) {
            var visible = state[col.name] !== false;
            var display = visible ? '' : 'none';

            // Header
            var th = table.querySelector('thead th[data-col="' + col.name + '"]');
            if (th) th.style.display = display;

            // Body cells por índice de columna
            var rows = table.querySelectorAll('tbody tr');
            rows.forEach(function (row) {
                // Saltar filas de "no resultados" (colspan)
                if (row.cells.length <= 1) return;
                var td = row.cells[col.index];
                if (td) td.style.display = display;
            });
        });

        // Actualizar colspan de la fila vacía si existe
        var emptyRow = table.querySelector('tbody tr td[colspan]');
        if (emptyRow) {
            var visibleCount = columns.filter(function (c) {
                return state[c.name] !== false;
            }).length;
            // +1 para columnas sin data-col (ej: Acciones)
            var totalVisible = visibleCount;
            var allHeaders = table.querySelectorAll('thead th');
            allHeaders.forEach(function (th) {
                if (!th.hasAttribute('data-col') && th.style.display !== 'none') {
                    totalVisible++;
                }
            });
            emptyRow.setAttribute('colspan', totalVisible);
        }
    }

    /**
     * Lee el estado actual de los checkboxes.
     */
    function getCurrentState(checkboxes) {
        var state = {};
        Object.keys(checkboxes).forEach(function (key) {
            state[key] = checkboxes[key].checked;
        });
        return state;
    }

    /**
     * Actualiza los checkboxes según un estado dado.
     */
    function updateCheckboxes(checkboxes, state) {
        Object.keys(checkboxes).forEach(function (key) {
            checkboxes[key].checked = state[key] !== false;
        });
    }

    /**
     * Guarda estado en localStorage.
     */
    function saveToStorage(key, state) {
        try {
            localStorage.setItem(key, JSON.stringify(state));
        } catch (e) { /* localStorage no disponible */ }
    }

    /**
     * Carga estado desde localStorage.
     */
    function loadFromStorage(key) {
        try {
            var data = localStorage.getItem(key);
            return data ? JSON.parse(data) : null;
        } catch (e) {
            return null;
        }
    }

    /**
     * Carga las configuraciones guardadas en BD para una vista.
     */
    function loadSavedConfigs(vista, selectEl) {
        fetch('/api/column-config?vista=' + encodeURIComponent(vista))
        .then(function (r) { return r.json(); })
        .then(function (configs) {
            // Limpiar options excepto la primera
            while (selectEl.options.length > 1) {
                selectEl.remove(1);
            }
            configs.forEach(function (cfg) {
                var opt = document.createElement('option');
                opt.value = cfg.idTablaColumnas;
                opt.textContent = cfg.descripcion;
                selectEl.appendChild(opt);
            });
        })
        .catch(function () { /* silenciar si no hay conexión */ });
    }

    /**
     * Muestra un toast temporal de Bootstrap.
     */
    function showToast(message, type) {
        var container = document.getElementById('colcfg-toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'colcfg-toast-container';
            container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
            container.style.zIndex = '1090';
            document.body.appendChild(container);
        }

        var toastEl = document.createElement('div');
        toastEl.className = 'toast align-items-center text-bg-' + (type || 'info') + ' border-0';
        toastEl.setAttribute('role', 'alert');
        toastEl.innerHTML =
            '<div class="d-flex">' +
                '<div class="toast-body">' + message + '</div>' +
                '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
            '</div>';

        container.appendChild(toastEl);

        var toast = new bootstrap.Toast(toastEl, { delay: 3000 });
        toast.show();

        toastEl.addEventListener('hidden.bs.toast', function () {
            toastEl.remove();
        });
    }

    // Exponer globalmente
    window.initColumnConfig = initColumnConfig;

})();
