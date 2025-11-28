const CACHE_NAME = 'spk-cache-v1';
const ASSETS_TO_CACHE = [
    '/',
    '/static/style.css',
    '/static/roboto.css',
    '/static/assets/win_logo.svg',
    '/static/fonts/material-icons.woff2',
    '/static/fonts/roboto-300.ttf',
    '/static/fonts/roboto-400.ttf',
    '/static/fonts/roboto-500.ttf'
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => {
            return cache.addAll(ASSETS_TO_CACHE);
        })
    );
});

self.addEventListener('fetch', (event) => {
    event.respondWith(
        caches.match(event.request).then((response) => {
            return response || fetch(event.request);
        })
    );
});
