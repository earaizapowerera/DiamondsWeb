// ============================================================
// piezas-fotos.js — Funciones de foto + mejora de descripcion con IA
// Para la pantalla Alta de Piezas Sencillas
// ============================================================

// ==================== FOTOS ====================

/**
 * Sube una foto seleccionada desde el input file al servidor.
 * Muestra preview al completar.
 */
function subirFoto(input) {
    if (!input.files || !input.files[0]) return;

    var file = input.files[0];
    var maxSize = 10 * 1024 * 1024; // 10 MB

    if (file.size > maxSize) {
        alert('El archivo es muy grande. Maximo 10 MB.');
        input.value = '';
        return;
    }

    var formData = new FormData();
    formData.append('foto', file);

    // Obtener token antiforgery
    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) formData.append('__RequestVerificationToken', token.value);

    // Mostrar spinner
    document.getElementById('fotoUploadArea').classList.add('d-none');
    document.getElementById('fotoLoading').classList.remove('d-none');

    fetch('?handler=SubirFoto', {
        method: 'POST',
        body: formData
    })
    .then(function(r) { return r.json(); })
    .then(function(data) {
        document.getElementById('fotoLoading').classList.add('d-none');

        if (data.success) {
            mostrarFotoPreview(data.url, data.storedFileName);
        } else {
            document.getElementById('fotoUploadArea').classList.remove('d-none');
            alert('Error al subir foto: ' + (data.error || 'desconocido'));
        }
    })
    .catch(function(err) {
        document.getElementById('fotoLoading').classList.add('d-none');
        document.getElementById('fotoUploadArea').classList.remove('d-none');
        alert('Error de conexion al subir foto');
        console.error('Error subir foto:', err);
    });

    // Limpiar input para poder subir el mismo archivo de nuevo
    input.value = '';
}

/**
 * Selecciona una foto del carrusel de fotos moviles.
 */
function seleccionarFotoMovil(storedFileName, url) {
    mostrarFotoPreview(url, storedFileName);
}

/**
 * Muestra la preview de la foto y guarda el storedFileName en el hidden.
 */
function mostrarFotoPreview(url, storedFileName) {
    document.getElementById('imgPreview').src = url;
    document.getElementById('hidArchivoFoto').value = storedFileName;
    document.getElementById('fotoPreview').classList.remove('d-none');
    document.getElementById('fotoUploadArea').classList.add('d-none');

    // Ocultar galeria de movil
    var fotosMovil = document.getElementById('fotosMovil');
    if (fotosMovil) fotosMovil.classList.add('d-none');
}

/**
 * Quita la foto seleccionada (no la elimina del servidor).
 */
function quitarFoto() {
    document.getElementById('imgPreview').src = '';
    document.getElementById('hidArchivoFoto').value = '';
    document.getElementById('fotoPreview').classList.add('d-none');
    document.getElementById('fotoUploadArea').classList.remove('d-none');

    // Mostrar galeria de movil
    var fotosMovil = document.getElementById('fotosMovil');
    if (fotosMovil) fotosMovil.classList.remove('d-none');
}

// ==================== DRAG & DROP ====================

document.addEventListener('DOMContentLoaded', function() {
    var dropzone = document.getElementById('fotoDropzone');
    if (!dropzone) return;

    dropzone.addEventListener('dragover', function(e) {
        e.preventDefault();
        dropzone.classList.add('foto-dropzone-active');
    });

    dropzone.addEventListener('dragleave', function() {
        dropzone.classList.remove('foto-dropzone-active');
    });

    dropzone.addEventListener('drop', function(e) {
        e.preventDefault();
        dropzone.classList.remove('foto-dropzone-active');

        if (e.dataTransfer.files && e.dataTransfer.files[0]) {
            var inputFoto = document.getElementById('inputFoto');
            // Crear un DataTransfer para asignar al input
            var dt = new DataTransfer();
            dt.items.add(e.dataTransfer.files[0]);
            inputFoto.files = dt.files;
            subirFoto(inputFoto);
        }
    });
});

// ==================== MEJORAR DESCRIPCION CON IA ====================

var _descripcionOriginalBackup = '';

/**
 * Llama al LLM para mejorar la descripcion.
 * Si hay foto, se envia tambien para generar una descripcion basada en la imagen.
 */
function mejorarDescripcion() {
    var inputDesc = document.getElementById('descripcionPieza');
    var archivoFoto = document.getElementById('hidArchivoFoto').value;
    var descripcion = inputDesc ? inputDesc.value.trim() : '';

    if (!descripcion && !archivoFoto) {
        alert('Escribe una descripcion o adjunta una foto para mejorar con IA.');
        return;
    }

    // Obtener grupo seleccionado (texto visible)
    var grupo = '';
    var selGrupo = document.getElementById('selGrupo');
    if (selGrupo && selGrupo.selectedIndex >= 0) {
        grupo = selGrupo.options[selGrupo.selectedIndex].text;
    }

    // Mostrar spinner, deshabilitar boton
    var btn = document.getElementById('btnMejorarDesc');
    var icon = document.getElementById('iconMejorar');
    var spinner = document.getElementById('spinnerMejorar');
    btn.disabled = true;
    icon.classList.add('d-none');
    spinner.classList.remove('d-none');

    // Ocultar sugerencia previa
    document.getElementById('llmSugerencia').classList.add('d-none');

    // Preparar request
    var formData = new FormData();
    formData.append('descripcion', descripcion);
    if (archivoFoto) formData.append('archivoFoto', archivoFoto);
    if (grupo) formData.append('grupo', grupo);

    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) formData.append('__RequestVerificationToken', token.value);

    fetch('?handler=MejorarDescripcion', {
        method: 'POST',
        body: formData
    })
    .then(function(r) { return r.json(); })
    .then(function(data) {
        btn.disabled = false;
        icon.classList.remove('d-none');
        spinner.classList.add('d-none');

        if (data.success && data.descripcionMejorada) {
            _descripcionOriginalBackup = descripcion;
            document.getElementById('textoSugerencia').textContent = data.descripcionMejorada;
            document.getElementById('llmSugerencia').classList.remove('d-none');
        } else {
            alert('No se pudo mejorar la descripcion: ' + (data.error || 'sin respuesta'));
        }
    })
    .catch(function(err) {
        btn.disabled = false;
        icon.classList.remove('d-none');
        spinner.classList.add('d-none');
        alert('Error de conexion al mejorar descripcion');
        console.error('Error LLM:', err);
    });
}

/**
 * Acepta la sugerencia del LLM y la pone en el campo de descripcion.
 */
function aceptarSugerencia() {
    var sugerencia = document.getElementById('textoSugerencia').textContent;
    var inputDesc = document.getElementById('descripcionPieza');
    if (inputDesc && sugerencia) {
        inputDesc.value = sugerencia;
    }
    document.getElementById('llmSugerencia').classList.add('d-none');
}

/**
 * Rechaza la sugerencia del LLM, mantiene la descripcion original.
 */
function rechazarSugerencia() {
    document.getElementById('llmSugerencia').classList.add('d-none');
}
