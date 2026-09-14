import http from 'node:http';
import {readFileSync} from 'node:fs';
import {pathToFileURL} from 'node:url';
import {WebSocketServer, WebSocket} from 'ws';
import nodemailer from 'nodemailer';
import {Webhook} from 'standardwebhooks';

const publicConfig=JSON.parse(readFileSync(new URL('./account-config.json',import.meta.url)));
export function cleanName(value){return String(value??'Student').replace(/[<>\x00-\x1f\x7f]/g,'').trim().slice(0,24)||'Student';}
export function validPosition(p){return [p.x,p.y,p.z,p.yaw].every(Number.isFinite)&&Math.abs(p.x)<=2000&&Math.abs(p.z)<=2000&&p.y>=-20&&p.y<=150;}
export async function identify(token,env=process.env){
 if(typeof token!=='string'||token.length<40||token.length>8192)throw Error('sign_in');
 const base=env.SUPABASE_URL||publicConfig.url,key=env.SUPABASE_PUBLISHABLE_KEY||publicConfig.publishableKey;
 const headers={apikey:key,Authorization:`Bearer ${token}`};
 const userResponse=await fetch(`${base}/auth/v1/user`,{headers,redirect:'error',signal:AbortSignal.timeout(12000)});
 if(!userResponse.ok)throw Error('sign_in');const user=await userResponse.json();
 if(!user.id||!user.email_confirmed_at||user.is_anonymous)throw Error('sign_in');
 const profileResponse=await fetch(`${base}/rest/v1/student_profiles?select=id,display_name,avatar_skin,avatar_outfit,avatar_hair,avatar_backpack&id=eq.${encodeURIComponent(user.id)}`,{headers,redirect:'error',signal:AbortSignal.timeout(12000)});
 if(!profileResponse.ok)throw Error('profile');const rows=await profileResponse.json();const p=rows[0];
 if(rows.length!==1||p.id!==user.id)throw Error('profile');
 return {id:user.id,display:cleanName(p.display_name),skin:p.avatar_skin,outfit:p.avatar_outfit,hair:p.avatar_hair,backpack:p.avatar_backpack};
}
export function createMailHandler(env=process.env,transport){
 const ready=()=>Boolean(env.SMTP_HOST&&env.SMTP_USER&&env.SMTP_PASSWORD&&env.SEND_EMAIL_HOOK_SECRET);
 const deliveries=new Map();
 return {ready,async handle(raw,headers){
  if(!ready())return {status:503,body:{error:'Email sender is awaiting configuration.'}};
  let payload;
  try{payload=new Webhook(env.SEND_EMAIL_HOOK_SECRET.replace(/^v1,/, '')).verify(raw,headers);}catch{return {status:401,body:{error:'Invalid webhook signature.'}};}
  const data=payload.email_data,recipient=payload.user?.email;
  if(!['signup','magiclink','recovery','reauthentication'].includes(data?.email_action_type)||!/^\d{6,10}$/.test(data?.token)||typeof recipient!=='string'||recipient.length>254||!/^([^\s<>@]+)@([^\s<>@]+\.[^\s<>@]+)$/.test(recipient))return {status:400,body:{error:'Unsupported authentication email.'}};
  const id=headers['webhook-id'];const now=Date.now();for(const [key,item] of deliveries)if(now-item.time>600000)deliveries.delete(key);
  if(!deliveries.has(id)){
   if(deliveries.size>=1000)return {status:429,body:{error:'Sender is busy. Retry later.'}};
   const smtp=transport??nodemailer.createTransport({host:env.SMTP_HOST,port:Number(env.SMTP_PORT||587),secure:env.SMTP_SECURE==='true',requireTLS:env.SMTP_SECURE!=='true',auth:{user:env.SMTP_USER,pass:env.SMTP_PASSWORD},connectionTimeout:5000,greetingTimeout:5000,socketTimeout:5000,disableFileAccess:true,disableUrlAccess:true});
   const code=data.token;
   const pending=smtp.sendMail({from:{name:'Albion Odyssey',address:env.SMTP_FROM||'abraham@nexoralab.net'},to:recipient,subject:'Your Albion Odyssey sign-in code',text:`Your Albion Odyssey code is ${code}.\n\nEnter it in the game to continue. If you did not request this code, ignore this email.`,html:`<div style="background:#101c2e;padding:40px;font-family:Arial;color:#fff"><p style="color:#b6efcb;letter-spacing:3px">ALBION ODYSSEY</p><h1>Your campus is waiting.</h1><p>Enter this code in the game:</p><p style="font-size:36px;letter-spacing:8px">${code}</p><p>If you did not request this code, ignore this email.</p></div>`});
   deliveries.set(id,{time:now,pending});
  }
  try{await deliveries.get(id).pending;return {status:200,body:{}};}catch{deliveries.delete(id);return {status:502,body:{error:'Email delivery unavailable. Please retry.'}};}
 }};
}
export function createServer({authenticate=identify,env=process.env,maxRoom=16,maxClients=128,authTimeout=15000}={}){
 const mail=createMailHandler(env),rooms=new Map(),peers=new Map(),attempts=new Map();
 function json(res,status,body){res.writeHead(status,{'Content-Type':'application/json','Cache-Control':'no-store','X-Content-Type-Options':'nosniff'});res.end(JSON.stringify(body));}
 const server=http.createServer(async(req,res)=>{
  if(req.url==='/health'&&req.method==='GET')return json(res,200,{ok:true,version:'0.20.0',service:'Albion Odyssey online rooms',mailConfigured:mail.ready(),roomCapacity:maxRoom});
  if(req.url!=='/auth/send-email'||req.method!=='POST')return json(res,404,{error:'Not found'});
  let raw='',bytes=0;
  try{for await(const chunk of req){bytes+=chunk.length;if(bytes>65536){json(res,413,{error:'Payload too large'});req.destroy();return;}raw+=chunk.toString();}const result=await mail.handle(raw,req.headers);json(res,result.status,result.body);}catch{if(!res.headersSent)json(res,400,{error:'Invalid request'});}
 });
 server.requestTimeout=15000;server.headersTimeout=10000;
 const wss=new WebSocketServer({noServer:true,maxPayload:12288,perMessageDeflate:false});
 function send(ws,message){if(ws.readyState===WebSocket.OPEN){if(ws.bufferedAmount>128000){ws.close(1013,'Connection too slow');return;}ws.send(JSON.stringify(message));}}
 function leave(ws){const p=peers.get(ws);if(!p)return;const room=rooms.get(p.room);room?.delete(ws);if(room?.size===0)rooms.delete(p.room);peers.delete(ws);}
 server.on('upgrade',(req,socket,head)=>{
  if(req.url!=='/campus'||wss.clients.size>=maxClients){socket.end('HTTP/1.1 503 Service Unavailable\r\n\r\n');return;}
  const ip=req.socket.remoteAddress,now=Date.now();let record=attempts.get(ip);if(!record||now-record.since>60000)record={since:now,count:0};record.count++;attempts.set(ip,record);
  // Counts connections, not game packets. Cap anonymous upgrade pressure on a single instance.
  if(record.count>120){socket.end('HTTP/1.1 429 Too Many Requests\r\n\r\n');return;}
  wss.handleUpgrade(req,socket,head,ws=>wss.emit('connection',ws,req));
 });
 wss.on('connection',ws=>{
  ws.alive=true;let authenticating=false,packets=0,windowAt=Date.now(),lastProfile=0;
  const timeout=setTimeout(()=>{if(!peers.has(ws))ws.close(4401,'Sign in required');},authTimeout);
  ws.on('pong',()=>{ws.alive=true;});ws.on('error',()=>{});ws.on('close',()=>{clearTimeout(timeout);leave(ws);});
  ws.on('message',async(raw,binary)=>{
   const now=Date.now();if(now-windowAt>=1000){windowAt=now;packets=0;}if(++packets>30){ws.close(4429,'Too many updates');return;}
   let m;try{if(binary)throw Error();m=JSON.parse(raw.toString());if(!m||Array.isArray(m)||typeof m.type!=='string')throw Error();}catch{ws.close(4400,'Invalid message');return;}
   const p=peers.get(ws);
   if(!p){
    if(authenticating)return;if(m.type!=='authenticate'||!/^[-A-Z0-9]{3,18}$/.test(m.room??'')){ws.close(4401,'Sign in and choose a room');return;}
    authenticating=true;
    try{
     const identity=await authenticate(m.token,env);if(ws.readyState!==WebSocket.OPEN)return;
     let room=rooms.get(m.room);if(!room){room=new Set();rooms.set(m.room,room);}
     for(const old of room)if(peers.get(old)?.id===identity.id){leave(old);old.close(4409,'Account joined from another session');}
     if(room.size>=maxRoom){ws.close(4403,'Room is full');return;}
     const player={...identity,room:m.room,x:6,y:.08,z:418,yaw:0,speed:0,emote:'',emoteUntil:0,token:m.token,verifiedAt:now,revalidating:false};
     rooms.set(m.room,room);peers.set(ws,player);room.add(ws);clearTimeout(timeout);send(ws,{type:'welcome',id:identity.id,room:m.room,capacity:maxRoom});
    }catch{ws.close(4401,'Sign in or reload your profile');}return;
   }
   if(m.type==='move'){
    if(!validPosition(m)||!Number.isFinite(m.speed)||m.speed<0||m.speed>30){ws.close(4400,'Invalid movement');return;}
    p.x=m.x;p.y=m.y;p.z=m.z;p.yaw=m.yaw%360;p.speed=m.speed;
   }else if(m.type==='emote'){
    if(['wave','cheer','dance'].includes(m.emote)&&now>p.emoteUntil){p.emote=m.emote;p.emoteUntil=now+3000;}
   }else if(m.type==='profile'&&now-lastProfile>5000){
    lastProfile=now;try{const identity=await authenticate(p.token,env);if(peers.has(ws))Object.assign(p,identity);}catch{ws.close(4401,'Sign in again');}
   }
  });
 });
 const snapshots=setInterval(()=>{
  const now=Date.now();for(const room of rooms.values()){
   const players=[...room].map(ws=>{const p=peers.get(ws);return {id:p.id,display:p.display,x:p.x,y:p.y,z:p.z,yaw:p.yaw,speed:p.speed,skin:p.skin,outfit:p.outfit,hair:p.hair,backpack:p.backpack,emote:now<p.emoteUntil?p.emote:''};});
   for(const ws of room)send(ws,{type:'snapshot',players});
  }
 },100);
 const heartbeat=setInterval(()=>{
  const now=Date.now();for(const [ip,r] of attempts)if(now-r.since>60000)attempts.delete(ip);
  for(const ws of wss.clients){if(!ws.alive){ws.terminate();continue;}ws.alive=false;ws.ping();const p=peers.get(ws);
   if(p&&!p.revalidating&&now-p.verifiedAt>600000){p.revalidating=true;authenticate(p.token,env).then(identity=>{if(peers.has(ws)){Object.assign(p,identity);p.verifiedAt=Date.now();p.revalidating=false;}}).catch(()=>ws.close(4401,'Session expired; sign in again'));}
  }
 },15000);
 async function close(){clearInterval(snapshots);clearInterval(heartbeat);for(const ws of wss.clients)ws.terminate();await new Promise(resolve=>wss.close(resolve));await new Promise(resolve=>server.close(resolve));}
 return {server,close,rooms,peers};
}
if(process.argv[1]&&import.meta.url===pathToFileURL(process.argv[1]).href){
 const app=createServer();app.server.listen(Number(process.env.PORT||10000),'0.0.0.0',()=>console.log('Albion Odyssey online rooms listening'));
 process.on('SIGTERM',()=>app.close().then(()=>process.exit(0)));process.on('SIGINT',()=>app.close().then(()=>process.exit(0)));
}
