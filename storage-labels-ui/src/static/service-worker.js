// This service worker doesn't actually do anything!
// self.addEventListener('install', e => {
//     e.waitUntil(
//         // after the service worker is installed,
//         // open a new cache
//         caches.open('food-pwa-cache').then(cache => {
//             // add all URLs of resources we want to cache
//             return cache.addAll([
//                 '/',
//                 '/api',
//             ]);
//         })
//     );
// });

// self.addEventListener('fetch', function(event) {
//     console.log(event.request.url);
//     event.respondWith(
//         caches.match(event.request).then(function(response) {
//             return response || fetch(event.request);
//         })
//     );
// });

self.addEventListener('fetch', event => {
    // Let the browser do its default thing
    // for non-GET requests.
    if (event.request.method != 'GET') return;
  
    // Prevent the default, and handle the request ourselves.
    event.respondWith(async function() {
      // Try to get the response from a cache.
      const cache = await caches.open('storage-labels-pwa-cache');
      const cachedResponse = await cache.match(event.request);
  
      if (cachedResponse) {
        // If we found a match in the cache, return it, but also
        // update the entry in the cache in the background.
        event.waitUntil(cache.add(event.request));
        return cachedResponse;
      }
  
      // If we didn't find a match in the cache, use the network.
      return fetch(event.request);
    }());
  });