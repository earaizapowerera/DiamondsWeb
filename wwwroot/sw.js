// Service Worker para Diamonds Fotos PWA
var CACHE_NAME = 'diamonds-fotos-v2';
var SHELL_ASSETS = [
    '/Fotos/Captura',
    '/css/fotos-pwa.css',
    '/js/fotos-pwa.js',
    '/icons/icon-192.png',
    '/manifest.json'
];

// Install: pre-cache shell
self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(CACHE_NAME).then(function (cache) {
            return cache.addAll(SHELL_ASSETS).catch(function () {
                console.warn('Some shell assets failed to cache');
            });
        })
    );
    self.skipWaiting();
});

// Activate: limpiar caches viejos
self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(
                keys.filter(function (k) { return k !== CACHE_NAME; })
                    .map(function (k) { return caches.delete(k); })
            );
        })
    );
    self.clients.claim();
});

// Fetch: network-first para API y paginas, cache-first para assets
self.addEventListener('fetch', function (event) {
    var url = new URL(event.request.url);

    // No interceptar otros dominios ni POST
    if (url.origin !== location.origin) return;
    if (event.request.method !== 'GET') return;

    // API: siempre network
    if (url.pathname.startsWith('/api/')) return;

    // Assets estaticos: cache-first
    if (url.pathname.match(/\.(css|js|png|jpg|jpeg|webp|svg|woff2?)$/)) {
        event.respondWith(
            caches.match(event.request).then(function (cached) {
                return cached || fetch(event.request).then(function (response) {
                    var clone = response.clone();
                    caches.open(CACHE_NAME).then(function (cache) { cache.put(event.request, clone); });
                    return response;
                });
            })
        );
        return;
    }

    // Paginas: network-first, fallback a cache
    event.respondWith(
        fetch(event.request).then(function (response) {
            var clone = response.clone();
            caches.open(CACHE_NAME).then(function (cache) { cache.put(event.request, clone); });
            return response;
        }).catch(function () {
            return caches.match(event.request);
        })
    );
});
