// Diamonds Fotos PWA - Camera capture and upload logic

(function () {
    'use strict';

    // ==================== Service Worker ====================

    function registerServiceWorker() {
        if ('serviceWorker' in navigator) {
            navigator.serviceWorker.register('/sw.js')
                .then(function (reg) { console.log('SW registrado:', reg.scope); })
                .catch(function (err) { console.warn('SW fallo:', err); });
        }
    }

    // ==================== PWA Install ====================

    var deferredPrompt = null;

    function setupInstallPrompt() {
        var banner = document.getElementById('install-banner');
        var btnInstall = document.getElementById('btn-install');
        var btnDismiss = document.getElementById('btn-dismiss');

        window.addEventListener('beforeinstallprompt', function (e) {
            e.preventDefault();
            deferredPrompt = e;
            if (banner) banner.classList.add('active');
        });

        if (btnInstall) {
            btnInstall.addEventListener('click', function () {
                if (deferredPrompt) {
                    deferredPrompt.prompt();
                    deferredPrompt.userChoice.then(function () { deferredPrompt = null; });
                }
                if (banner) banner.classList.remove('active');
            });
        }

        if (btnDismiss) {
            btnDismiss.addEventListener('click', function () {
                if (banner) banner.classList.remove('active');
            });
        }

        // iOS detection (no beforeinstallprompt)
        var isIos = /iphone|ipad|ipod/.test(navigator.userAgent.toLowerCase());
        var isStandalone = window.matchMedia('(display-mode: standalone)').matches
            || navigator.standalone === true;
        var iosInstructions = document.getElementById('ios-instructions');
        if (isIos && !isStandalone && iosInstructions) {
            iosInstructions.classList.add('active');
        }
    }

    // ==================== Offline Detection ====================

    function setupOfflineDetection() {
        var banner = document.getElementById('offline-banner');
        if (!banner) return;

        function update() {
            banner.classList.toggle('active', !navigator.onLine);
        }
        window.addEventListener('online', update);
        window.addEventListener('offline', update);
        update();
    }

    // ==================== Camera / Upload ====================

    function setupCamera() {
        var input = document.getElementById('camera-input');
        var btnCamera = document.getElementById('btn-camera');
        if (!input || !btnCamera) return;

        btnCamera.addEventListener('click', function () {
            input.click();
        });

        input.addEventListener('change', function () {
            var files = input.files;
            if (!files || files.length === 0) return;

            for (var i = 0; i < files.length; i++) {
                processAndUpload(files[i]);
            }
            // Reset para permitir re-seleccionar el mismo archivo
            input.value = '';
        });
    }

    function processAndUpload(file) {
        // Validar tipo
        if (!file.type.startsWith('image/')) {
            showToast('Solo se permiten imagenes', 'error');
            return;
        }

        // Comprimir si es mayor a 2MB
        var maxSize = 2 * 1024 * 1024;
        if (file.size > maxSize) {
            compressImage(file, function (compressed) {
                uploadFile(compressed);
            });
        } else {
            uploadFile(file);
        }
    }

    function compressImage(file, callback) {
        var reader = new FileReader();
        reader.onload = function (e) {
            var img = new Image();
            img.onload = function () {
                var canvas = document.createElement('canvas');
                var maxDim = 1920;
                var w = img.width;
                var h = img.height;

                if (w > maxDim || h > maxDim) {
                    if (w > h) {
                        h = Math.round(h * maxDim / w);
                        w = maxDim;
                    } else {
                        w = Math.round(w * maxDim / h);
                        h = maxDim;
                    }
                }

                canvas.width = w;
                canvas.height = h;
                var ctx = canvas.getContext('2d');
                ctx.drawImage(img, 0, 0, w, h);

                canvas.toBlob(function (blob) {
                    var compressed = new File([blob], file.name, {
                        type: 'image/jpeg',
                        lastModified: Date.now()
                    });
                    callback(compressed);
                }, 'image/jpeg', 0.85);
            };
            img.src = e.target.result;
        };
        reader.readAsDataURL(file);
    }

    function uploadFile(file) {
        var progress = document.getElementById('upload-progress');
        if (progress) progress.classList.add('active');

        var formData = new FormData();
        formData.append('file', file);
        formData.append('source', 'mobile');

        fetch('/api/fotos/upload', {
            method: 'POST',
            body: formData,
            credentials: 'same-origin'
        })
        .then(function (response) {
            if (response.status === 401 || response.status === 302) {
                showToast('Sesion expirada. Inicia sesion de nuevo.', 'error');
                setTimeout(function () { window.location.href = '/Security/Auth/Login'; }, 1500);
                throw new Error('Unauthorized');
            }
            return response.json();
        })
        .then(function (data) {
            if (data.error) {
                showToast('Error: ' + data.error, 'error');
            } else {
                showToast('Foto subida correctamente');
                refreshPhotos();
            }
        })
        .catch(function (err) {
            if (err.message !== 'Unauthorized') {
                showToast('Error al subir foto. Verifica tu conexion.', 'error');
                console.error('Upload error:', err);
            }
        })
        .finally(function () {
            if (progress) progress.classList.remove('active');
        });
    }

    // ==================== Photo Grid ====================

    function refreshPhotos() {
        var grid = document.getElementById('fotos-grid');
        var empty = document.getElementById('empty-state');
        var counter = document.getElementById('foto-count');
        if (!grid) return;

        fetch('/api/fotos/recientes?count=20', { credentials: 'same-origin' })
            .then(function (r) { return r.json(); })
            .then(function (fotos) {
                grid.innerHTML = '';
                if (!fotos || fotos.length === 0) {
                    if (empty) empty.style.display = 'block';
                    if (counter) counter.textContent = '0';
                    return;
                }
                if (empty) empty.style.display = 'none';
                if (counter) counter.textContent = fotos.length;

                fotos.forEach(function (foto) {
                    var card = document.createElement('div');
                    card.className = 'foto-card';
                    card.innerHTML =
                        '<img src="' + foto.url + '" alt="' + foto.fileName + '" loading="lazy" />' +
                        '<div class="foto-info">' +
                            '<span class="badge badge-source bg-' + (foto.source === 'mobile' ? 'primary' : 'secondary') + '">' + foto.source + '</span> ' +
                            foto.uploadedAt +
                        '</div>' +
                        '<button class="btn-delete" data-id="' + foto.id + '" title="Eliminar">' +
                            '<i class="fa fa-times"></i>' +
                        '</button>';
                    grid.appendChild(card);
                });

                // Attach delete handlers
                grid.querySelectorAll('.btn-delete').forEach(function (btn) {
                    btn.addEventListener('click', function (e) {
                        e.stopPropagation();
                        var fotoId = btn.getAttribute('data-id');
                        if (confirm('Eliminar esta foto?')) {
                            deleteFoto(fotoId);
                        }
                    });
                });
            })
            .catch(function (err) {
                console.error('Error loading photos:', err);
            });
    }

    function deleteFoto(fotoId) {
        fetch('/api/fotos/' + fotoId, {
            method: 'DELETE',
            credentials: 'same-origin'
        })
        .then(function (r) { return r.json(); })
        .then(function (data) {
            if (data.success) {
                showToast('Foto eliminada');
                refreshPhotos();
            } else {
                showToast('Error al eliminar', 'error');
            }
        })
        .catch(function () { showToast('Error al eliminar', 'error'); });
    }

    // ==================== Toast ====================

    function showToast(message, type) {
        var existing = document.querySelector('.foto-toast');
        if (existing) existing.remove();

        var toast = document.createElement('div');
        toast.className = 'foto-toast';
        if (type === 'error') toast.style.background = '#dc3545';
        toast.textContent = message;
        document.body.appendChild(toast);

        requestAnimationFrame(function () {
            toast.classList.add('active');
        });

        setTimeout(function () {
            toast.classList.remove('active');
            setTimeout(function () { toast.remove(); }, 300);
        }, 2500);
    }

    // ==================== Init ====================

    function init() {
        registerServiceWorker();
        setupInstallPrompt();
        setupOfflineDetection();
        setupCamera();
        refreshPhotos();
    }

    // Support both full page load and SPA navigation
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose for SPA re-init
    window.initFotosPwa = init;
})();
