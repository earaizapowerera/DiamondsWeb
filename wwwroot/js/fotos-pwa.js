// ============================================================
// fotos-pwa.js — Diamonds Fotos PWA
// Captura de fotos de piezas via camara en vivo o galeria.
// ============================================================

(function () {
    'use strict';

    // ==================== Estado ====================
    var videoStream = null;
    var facingMode = 'environment';

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

        // iOS detection
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
        function update() { banner.classList.toggle('active', !navigator.onLine); }
        window.addEventListener('online', update);
        window.addEventListener('offline', update);
        update();
    }

    // ==================== Live Camera ====================

    function setupLiveCamera() {
        var btnCamera = document.getElementById('btn-camera');
        var btnSnap = document.getElementById('btn-snap');
        var btnSwitch = document.getElementById('btn-switch');
        var btnCloseCam = document.getElementById('btn-close-cam');
        var btnGallery = document.getElementById('btn-gallery');
        var cameraInput = document.getElementById('camera-input');

        if (btnCamera) {
            btnCamera.addEventListener('click', function () {
                // Si el navegador soporta getUserMedia, usar camara en vivo
                if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
                    openLiveCamera();
                } else {
                    // Fallback: input file con captura
                    if (cameraInput) {
                        cameraInput.setAttribute('capture', 'environment');
                        cameraInput.click();
                    }
                }
            });
        }

        if (btnGallery && cameraInput) {
            btnGallery.addEventListener('click', function () {
                cameraInput.removeAttribute('capture');
                cameraInput.click();
            });
        }

        if (btnSnap) btnSnap.addEventListener('click', captureFromVideo);
        if (btnSwitch) btnSwitch.addEventListener('click', switchCamera);
        if (btnCloseCam) btnCloseCam.addEventListener('click', closeLiveCamera);

        // File input change
        if (cameraInput) {
            cameraInput.addEventListener('change', function () {
                var files = cameraInput.files;
                if (!files || files.length === 0) return;
                for (var i = 0; i < files.length; i++) {
                    processAndUpload(files[i]);
                }
                cameraInput.value = '';
            });
        }
    }

    function openLiveCamera() {
        var cameraLive = document.getElementById('camera-live');
        var cameraStart = document.getElementById('camera-start');
        var video = document.getElementById('video-preview');
        if (!cameraLive || !video) return;

        var constraints = {
            video: { facingMode: facingMode, width: { ideal: 1920 }, height: { ideal: 1440 } },
            audio: false
        };

        navigator.mediaDevices.getUserMedia(constraints)
            .then(function (stream) {
                videoStream = stream;
                video.srcObject = stream;
                if (cameraStart) cameraStart.classList.add('d-none');
                cameraLive.classList.remove('d-none');
            })
            .catch(function (err) {
                if (err.name === 'NotAllowedError') {
                    showToast('Permiso de camara denegado. Revisa los permisos del navegador.', 'error');
                } else {
                    showToast('No se pudo abrir la camara: ' + err.message, 'error');
                    // Fallback a input file
                    var input = document.getElementById('camera-input');
                    if (input) { input.setAttribute('capture', 'environment'); input.click(); }
                }
            });
    }

    function closeLiveCamera() {
        var cameraLive = document.getElementById('camera-live');
        var cameraStart = document.getElementById('camera-start');
        var video = document.getElementById('video-preview');

        if (videoStream) {
            videoStream.getTracks().forEach(function (t) { t.stop(); });
            videoStream = null;
        }
        if (video) video.srcObject = null;
        if (cameraLive) cameraLive.classList.add('d-none');
        if (cameraStart) cameraStart.classList.remove('d-none');
    }

    function switchCamera() {
        facingMode = facingMode === 'environment' ? 'user' : 'environment';
        if (videoStream) {
            videoStream.getTracks().forEach(function (t) { t.stop(); });
        }
        openLiveCamera();
    }

    function captureFromVideo() {
        var video = document.getElementById('video-preview');
        var canvas = document.getElementById('capture-canvas');
        if (!video || !canvas || !videoStream) return;

        var track = videoStream.getVideoTracks()[0];
        var settings = track.getSettings();
        var w = settings.width || video.videoWidth || 1280;
        var h = settings.height || video.videoHeight || 960;

        canvas.width = w;
        canvas.height = h;
        var ctx = canvas.getContext('2d');
        ctx.drawImage(video, 0, 0, w, h);

        // Flash visual
        var cameraLive = document.getElementById('camera-live');
        if (cameraLive) {
            cameraLive.classList.add('flash');
            setTimeout(function () { cameraLive.classList.remove('flash'); }, 200);
        }

        canvas.toBlob(function (blob) {
            if (blob) {
                var ts = new Date();
                var name = 'captura_' + ts.getFullYear()
                    + pad(ts.getMonth() + 1) + pad(ts.getDate())
                    + '_' + pad(ts.getHours()) + pad(ts.getMinutes()) + pad(ts.getSeconds())
                    + '.jpg';
                uploadFile(blob, name);
            }
        }, 'image/jpeg', 0.85);
    }

    // ==================== Process & Upload ====================

    function processAndUpload(file) {
        if (!file.type.startsWith('image/')) {
            showToast('Solo se permiten imagenes', 'error');
            return;
        }

        // Comprimir si > 2MB
        if (file.size > 2 * 1024 * 1024) {
            compressImage(file, function (compressed) { uploadFile(compressed, compressed.name); });
        } else {
            uploadFile(file, file.name);
        }
    }

    function compressImage(file, callback) {
        var reader = new FileReader();
        reader.onload = function (e) {
            var img = new Image();
            img.onload = function () {
                var canvas = document.createElement('canvas');
                var maxDim = 1920;
                var w = img.width, h = img.height;
                if (w > maxDim || h > maxDim) {
                    if (w > h) { h = Math.round(h * maxDim / w); w = maxDim; }
                    else { w = Math.round(w * maxDim / h); h = maxDim; }
                }
                canvas.width = w;
                canvas.height = h;
                canvas.getContext('2d').drawImage(img, 0, 0, w, h);
                canvas.toBlob(function (blob) {
                    callback(new File([blob], file.name, { type: 'image/jpeg', lastModified: Date.now() }));
                }, 'image/jpeg', 0.85);
            };
            img.src = e.target.result;
        };
        reader.readAsDataURL(file);
    }

    function uploadFile(fileOrBlob, fileName) {
        var progress = document.getElementById('upload-progress');
        if (progress) progress.classList.add('active');

        var formData = new FormData();
        formData.append('file', fileOrBlob, fileName || 'foto.jpg');
        formData.append('source', 'mobile');

        // Incluir userId si disponible (para cuando el endpoint lo requiera)
        var hidUserId = document.getElementById('hid-user-id');
        if (hidUserId && hidUserId.value && parseInt(hidUserId.value) > 0) {
            formData.append('userId', hidUserId.value);
        }

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

        // Incluir userId en la consulta
        var params = 'count=20&source=mobile';
        var hidUserId = document.getElementById('hid-user-id');
        if (hidUserId && hidUserId.value && parseInt(hidUserId.value) > 0) {
            params += '&userId=' + hidUserId.value;
        }

        fetch('/api/fotos/recientes?' + params, { credentials: 'same-origin' })
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
                        '<img src="' + foto.url + '" alt="' + foto.fileName + '" loading="lazy"'
                        + ' onclick="viewPhoto(\'' + foto.url + '\')" />' +
                        '<div class="foto-info">' +
                            '<span class="badge badge-source bg-' + (foto.source === 'mobile' ? 'primary' : 'secondary') + '">' + foto.source + '</span> ' +
                            foto.uploadedAt +
                        '</div>' +
                        '<button class="btn-delete" data-id="' + foto.id + '" title="Eliminar">' +
                            '<i class="fa fa-times"></i>' +
                        '</button>';
                    grid.appendChild(card);
                });

                grid.querySelectorAll('.btn-delete').forEach(function (btn) {
                    btn.addEventListener('click', function (e) {
                        e.stopPropagation();
                        var fotoId = btn.getAttribute('data-id');
                        if (confirm('Eliminar esta foto?')) deleteFoto(fotoId);
                    });
                });
            })
            .catch(function (err) { console.error('Error loading photos:', err); });
    }

    function deleteFoto(fotoId) {
        fetch('/api/fotos/' + fotoId, { method: 'DELETE', credentials: 'same-origin' })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success) { showToast('Foto eliminada'); refreshPhotos(); }
                else showToast('Error al eliminar', 'error');
            })
            .catch(function () { showToast('Error al eliminar', 'error'); });
    }

    // ==================== Photo Viewer ====================

    window.viewPhoto = function (url) {
        var viewer = document.getElementById('photo-viewer');
        var img = document.getElementById('viewer-img');
        if (viewer && img) {
            img.src = url;
            viewer.classList.remove('d-none');
        }
    };

    window.closeViewer = function () {
        var viewer = document.getElementById('photo-viewer');
        if (viewer) viewer.classList.add('d-none');
    };

    // ==================== Toast ====================

    function showToast(message, type) {
        var existing = document.querySelector('.foto-toast');
        if (existing) existing.remove();

        var toast = document.createElement('div');
        toast.className = 'foto-toast';
        if (type === 'error') toast.style.background = '#dc3545';
        toast.textContent = message;
        document.body.appendChild(toast);

        requestAnimationFrame(function () { toast.classList.add('active'); });

        setTimeout(function () {
            toast.classList.remove('active');
            setTimeout(function () { toast.remove(); }, 300);
        }, 2500);
    }

    // ==================== Helpers ====================

    function pad(n) { return n < 10 ? '0' + n : '' + n; }

    // ==================== Init ====================

    function init() {
        registerServiceWorker();
        setupInstallPrompt();
        setupOfflineDetection();
        setupLiveCamera();
        refreshPhotos();

        // Refresh button
        var btnRefresh = document.getElementById('btn-refresh');
        if (btnRefresh) btnRefresh.addEventListener('click', refreshPhotos);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.initFotosPwa = init;
})();
