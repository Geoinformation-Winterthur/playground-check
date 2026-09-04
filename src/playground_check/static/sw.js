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


self.addEventListener('push',event=>{
  let payload={};
  try{payload=event.data?event.data.json():{};}catch{payload={};}
  const notification=payload.notification||payload;
  const tasks=[];
  if(notification&&notification.title){tasks.push(self.registration.showNotification(notification.title,{body:notification.body||'',icon:notification.icon,badge:notification.badge,data:notification.data||{}}));}
  tasks.push(clients.matchAll({type:'window',includeUncontrolled:true}).then(all=>Promise.all(all.map(client=>client.postMessage({type:'push-message',message:payload})))));
  event.waitUntil(Promise.all(tasks));
});

self.addEventListener('notificationclick',event=>{
  event.notification.close();
  const data=event.notification.data||{};
  let url=data.url||'/defects';
  if(data.defectTid&&data.playdeviceFid)url=`/defect/${data.playdeviceFid}/${data.defectTid}`;
  event.waitUntil(clients.matchAll({type:'window',includeUncontrolled:true}).then(async all=>{
    const target=new URL(url,self.location.origin).href;
    if(all.length){const client=all[0];if('navigate' in client)await client.navigate(target);return client.focus();}
    return clients.openWindow?clients.openWindow(target):undefined;
  }));
});
