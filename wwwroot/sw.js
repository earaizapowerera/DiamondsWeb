// Service Worker para Diamonds Fotos PWA
const CACHE_NAME = 'diamonds-fotos-v1';
const SHELL_ASSETS = [
    '/Fotos/Captura',
    '/css/fotos-pwa.css',
    '/js/fotos-pwa.js',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/manifest.json'
];

// Install: pre-cache shell
self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            return cache.addAll(SHELL_ASSETS).catch(() => {
                // Si falla alguno, no bloquear install
                console.warn('Some shell assets failed to cache');
            });
        })
    );
    self.skipWaiting();
});

// Activate: limpiar caches viejos
self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(keys
                .filter((k) => k !== CACHE_NAME)
                .map((k) => caches.delete(k))
            )
        )
    );
    self.clients.claim();
});

// Fetch: network-first para API y paginas, cache-first para assets estaticos
self.addEventListener('fetch', (event) => {
    const url = new URL(event.request.url);

    // No interceptar requests a otros dominios
    if (url.origin !== location.origin) return;

    // API calls: siempre network
    if (url.pathname.startsWith('/api/')) return;

    // Assets estaticos: cache-first
    if (url.pathname.match(/\.(css|js|png|jpg|jpeg|webp|svg|woff2?)$/)) {
        event.respondWith(
            caches.match(event.request).then((cached) => {
                return cached || fetch(event.request).then((response) => {
                    const clone = response.clone();
                    caches.open(CACHE_NAME).then((cache) => cache.put(event.request, clone));
                    return response;
                });
            })
        );
        return;
    }

    // Paginas: network-first, fallback a cache
    event.respondWith(
        fetch(event.request)
            .then((response) => {
                const clone = response.clone();
                caches.open(CACHE_NAME).then((cache) => cache.put(event.request, clone));
                return response;
            })
            .catch(() => caches.match(event.request))
    );
});
