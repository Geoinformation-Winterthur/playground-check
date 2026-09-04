const CACHE='spielplatzkontrolle-2026.7.1';
const SHELL=['/','/static/css/app.css','/static/js/app.js','/static/manifest.webmanifest','/static/assets/win_logo.svg'];
self.addEventListener('install',event=>event.waitUntil(caches.open(CACHE).then(cache=>cache.addAll(SHELL))));
self.addEventListener('activate',event=>event.waitUntil(caches.keys().then(keys=>Promise.all(keys.filter(key=>key!==CACHE).map(key=>caches.delete(key))))));
self.addEventListener('fetch',event=>{
  if(event.request.method!=='GET'||new URL(event.request.url).origin!==location.origin)return;
  const path=new URL(event.request.url).pathname;
  if(path.startsWith('/Account/')||path.startsWith('/Inspection/')||path.startsWith('/Playground/')||path.startsWith('/Playdevice/')||path.startsWith('/Defect/')||path.startsWith('/Document/')||path.startsWith('/PushSubscription/')||path.startsWith('/Collections/'))return;
  event.respondWith(caches.match(event.request).then(hit=>hit||fetch(event.request).then(response=>{const copy=response.clone();caches.open(CACHE).then(cache=>cache.put(event.request,copy));return response;})));
});
