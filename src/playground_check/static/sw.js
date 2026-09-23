const CACHE='spielplatzkontrolle-2026.11';
const BASE_PATH=new URL(self.registration.scope).pathname.replace(/\/$/,'');
const scoped=path=>`${BASE_PATH}${path}`||'/';
const APP_SHELL=[
  scoped('/'),
  scoped('/static/assets/favicon.ico'),
  scoped('/static/manifest.webmanifest'),
  scoped('/static/css/app.css'),
  scoped('/static/js/app.js')
];

self.addEventListener('install',event=>event.waitUntil(
  caches.open(CACHE).then(cache=>cache.addAll(APP_SHELL))
));

self.addEventListener('activate',event=>event.waitUntil(
  caches.keys().then(keys=>Promise.all(keys.filter(key=>key!==CACHE).map(key=>caches.delete(key))))
));

self.addEventListener('fetch',event=>{
  if(event.request.method!=='GET')return;
  const url=new URL(event.request.url);
  if(url.origin!==location.origin)return;

  // Angular's application group is prefetched and served cache-first.
  if(APP_SHELL.includes(url.pathname)){
    event.respondWith(caches.match(event.request).then(hit=>hit||fetch(event.request)));
    return;
  }

  // The original "assets" group is lazy: cache an asset only after first use.
  if(url.pathname.startsWith(scoped('/static/assets/'))){
    event.respondWith(caches.match(event.request).then(hit=>hit||fetch(event.request).then(response=>{
      if(response.ok){const copy=response.clone();caches.open(CACHE).then(cache=>cache.put(event.request,copy));}
      return response;
    })));
    return;
  }

  // API and arbitrary application requests are intentionally not cached here.
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
  let url=data.url||scoped('/defects');
  if(data.url&&data.url.startsWith('/'))url=scoped(data.url);
  if(data.defectTid&&data.playdeviceFid)url=scoped(`/defect/${data.playdeviceFid}/${data.defectTid}`);
  event.waitUntil(clients.matchAll({type:'window',includeUncontrolled:true}).then(async all=>{
    const target=new URL(url,self.location.origin).href;
    if(all.length){const client=all[0];if('navigate' in client)await client.navigate(target);return client.focus();}
    return clients.openWindow?clients.openWindow(target):undefined;
  }));
});
