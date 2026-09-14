import test from 'node:test';import assert from 'node:assert/strict';import {once} from 'node:events';import {WebSocket} from 'ws';import {Webhook} from 'standardwebhooks';import {createServer,createMailHandler,validPosition} from './server.mjs';
const pause=ms=>new Promise(r=>setTimeout(r,ms));
async function waitFor(ws,type){const deadline=Date.now()+4000;while(Date.now()<deadline){const index=ws.messages.findIndex(m=>m.type===type);if(index>=0)return ws.messages.splice(index,1)[0];await pause(20);}throw Error('Missing '+type);}
async function client(url,token,room='QUAD'){const ws=new WebSocket(url);ws.messages=[];ws.on('message',raw=>ws.messages.push(JSON.parse(raw)));await once(ws,'open');ws.send(JSON.stringify({type:'authenticate',token,room}));return ws;}
async function fixture(maxRoom=16){const app=createServer({maxRoom,authTimeout:200,env:{},authenticate:async token=>{if(!['a','b','c'].includes(token))throw Error('invalid');return {id:token,display:'Student '+token,skin:1,outfit:2,hair:1,backpack:true};}});app.server.listen(0,'127.0.0.1');await once(app.server,'listening');return {...app,url:`ws://127.0.0.1:${app.server.address().port}/campus`};}
test('authenticated rooms synchronize true peers, validated appearance and emotes without exposing tokens',async()=>{const app=await fixture();try{
 const a=await client(app.url,'a'),b=await client(app.url,'b');await waitFor(a,'welcome');await waitFor(b,'welcome');
 a.send(JSON.stringify({type:'move',id:'spoof',display:'spoof',x:12,y:.08,z:430,yaw:45,speed:3}));a.send(JSON.stringify({type:'emote',emote:'wave'}));await pause(150);
 const snap=(await waitFor(b,'snapshot')).players;const actual=snap.find(p=>p.id==='a');assert.equal(actual.display,'Student a');assert.equal(actual.skin,1);assert.equal(actual.x,12);assert.equal(actual.emote,'wave');assert.equal(actual.token,undefined);
 const c=await client(app.url,'c','OTHER');await waitFor(c,'welcome');const separate=await waitFor(c,'snapshot');assert.equal(separate.players.length,1);
 a.close();await once(a,'close');await pause(150);assert.equal(app.peers.size,2);
 }finally{await app.close();}});
test('invalid credentials, room capacity, missing handshake and malformed packets are rejected',async()=>{const app=await fixture(1);try{
 const bad=await client(app.url,'invalid');assert.equal((await once(bad,'close'))[0],4401);
 const a=await client(app.url,'a');await waitFor(a,'welcome');const b=await client(app.url,'b');assert.equal((await once(b,'close'))[0],4403);
 a.send(JSON.stringify({type:'move',x:1e9,y:0,z:0,yaw:0,speed:0}));assert.equal((await once(a,'close'))[0],4400);
 const idle=new WebSocket(app.url);await once(idle,'open');assert.equal((await once(idle,'close'))[0],4401);
 const malformed=new WebSocket(app.url);await once(malformed,'open');malformed.send('null');assert.equal((await once(malformed,'close'))[0],4400);
 }finally{await app.close();}});
test('a replacement connection for the same account remains in the room',async()=>{const app=await fixture();try{const a=await client(app.url,'a');await waitFor(a,'welcome');const next=await client(app.url,'a');await waitFor(next,'welcome');const snap=await waitFor(next,'snapshot');assert.equal(snap.players.length,1);assert.equal(app.rooms.size,1);}finally{await app.close();}});
test('movement rejects non-finite and invalid floor values',()=>{assert(!validPosition({x:NaN,y:0,z:0,yaw:0}));assert(!validPosition({x:0,y:-500,z:0,yaw:0}));});
test('mail stays unavailable without a password and rejects unsigned requests',async()=>{assert.equal((await createMailHandler({}).handle('{}',{})).status,503);const handler=createMailHandler({SMTP_HOST:'mail.example.test',SMTP_USER:'a@example.test',SMTP_PASSWORD:'not-real',SEND_EMAIL_HOOK_SECRET:'whsec_'+Buffer.from('a'.repeat(32)).toString('base64')});assert.equal((await handler.handle('{}',{})).status,401);});
test('signed OTP mail sends once on retry; failed delivery can retry and never reports success',async()=>{
 const secret='whsec_'+Buffer.from('b'.repeat(32)).toString('base64');const env={SMTP_HOST:'mail.example.test',SMTP_USER:'a@example.test',SMTP_PASSWORD:'not-real',SEND_EMAIL_HOOK_SECRET:secret};let deliveries=0;
 const handler=createMailHandler(env,{sendMail:async mail=>{deliveries++;assert.equal(mail.from.address,'abraham@nexoralab.net');assert.equal(mail.to,'owned@example.test');assert(mail.text.includes('123456'));}});
 const raw=JSON.stringify({user:{email:'owned@example.test'},email_data:{email_action_type:'magiclink',token:'123456'}}),id='test-message',time=new Date();const headers={'webhook-id':id,'webhook-timestamp':String(Math.floor(time.getTime()/1000)),'webhook-signature':new Webhook(secret).sign(id,time,raw)};
 assert.equal((await handler.handle(raw,headers)).status,200);assert.equal((await handler.handle(raw,headers)).status,200);assert.equal(deliveries,1);
 const fail=createMailHandler(env,{sendMail:async()=>{throw Error('smtp failure');}});assert.equal((await fail.handle(raw,headers)).status,502);
});
