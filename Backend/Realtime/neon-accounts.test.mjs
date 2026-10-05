import test from 'node:test';import assert from 'node:assert/strict';import {createNeonAccounts,validProfile} from './neon-accounts.mjs';
const env={NEON_DATABASE_URL:'test',NEON_AUTH_URL:'https://auth.example.test',NEON_SMTP_READY:'true'};
function setup(){const calls=[];const pool={query:async(sql,values)=>{calls.push({sql,values});return {rows:[{id:'11111111-1111-4111-8111-111111111111',display_name:'Student',avatar_skin:1,avatar_outfit:0,avatar_hair:0,avatar_backpack:true,avatar_ready:false}]};},end:async()=>{}};let verified=true;const fetcher=async(url,options)=>{if(url.includes('get-session'))return Response.json({session:{expiresAt:new Date(Date.now()+3600000).toISOString()},user:{id:'neon-user',email:'qa@example.test',emailVerified:verified}});return Response.json({success:true,token:'a'.repeat(40)});};return {accounts:createNeonAccounts(env,{pool,fetcher}),calls,setVerified:v=>verified=v};}
function req(url,method='GET'){return {url,method,headers:{authorization:'Bearer '+'a'.repeat(40)},socket:{remoteAddress:'127.0.0.1'}};}
test('Neon profiles reject identity and unexpected column mutations',async()=>{assert(!validProfile({id:'spoof'}));assert(!validProfile({avatar_skin:9}));assert(!validProfile({display_name:'<admin>'}));const {accounts,calls}=setup();const r=await accounts.handle(req('/rest/v1/student_profiles?id=eq.someone-else','PATCH'),{display_name:'Other'});assert.equal(r.status,403);assert(!calls.some(c=>c.sql.startsWith('UPDATE')));});
test('only a verified Neon session may read or save the owning profile',async()=>{const x=setup(),path='/rest/v1/student_profiles?id=eq.11111111-1111-4111-8111-111111111111';assert.equal((await x.accounts.handle(req(path),{})).status,200);assert.equal((await x.accounts.handle(req(path,'PATCH'),{display_name:' Ada '})).status,200);assert.equal(x.calls.at(-1).values[0],'Ada');x.setVerified(false);assert.equal((await x.accounts.handle(req(path),{})).status,401);});
test('email resend is limited and Hostinger configuration fails closed',async()=>{const x=setup();const request=req('/auth/v1/otp','POST');assert.equal((await x.accounts.handle(request,{email:'qa@example.test'})).status,200);assert.equal((await x.accounts.handle(request,{email:'qa@example.test'})).status,429);const blocked=createNeonAccounts({...env,NEON_SMTP_READY:'false'},{pool:{end:async()=>{}},fetcher:async()=>{throw Error('Must not send');}});assert.equal((await blocked.handle(request,{email:'qa@example.test'})).status,503);});


test('email code sign-in carries the signed Neon session cookie into verified session lookup',async()=>{
 const credential='signed-neon-session-token.signature%2Bvalue';let verified=false;
 const pool={query:async()=>({rows:[{id:'11111111-1111-4111-8111-111111111111'}]}),end:async()=>{}};
 const fetcher=async(url,options)=>{
  if(url.endsWith('/sign-in/email-otp'))return new Response(JSON.stringify({token:'raw-token-not-sufficient'}),{headers:{'Content-Type':'application/json','Set-Cookie':'__Secure-neonauth.session_token='+credential+'; Path=/; Secure; HttpOnly'}});
  assert.equal(options.headers.Cookie,'__Secure-neonauth.session_token='+credential);assert.equal(options.headers.Authorization,undefined);verified=true;
  return Response.json({user:{id:'neon-user',email:'qa@example.test',emailVerified:true},session:{expiresAt:new Date(Date.now()+3600000).toISOString()}});
 };
 const accounts=createNeonAccounts(env,{pool,fetcher});
 const result=await accounts.handle(req('/auth/v1/verify','POST'),{email:'qa@example.test',token:'123456'});
 assert.equal(result.status,200);assert.equal(verified,true);assert.equal(result.body.access_token,'neon-cookie:'+credential);assert.equal(result.body.user.id,'11111111-1111-4111-8111-111111111111');
});

test('cookie session rejects header injection before contacting Neon',async()=>{
 let called=false;const accounts=createNeonAccounts(env,{pool:{end:async()=>{}},fetcher:async()=>{called=true;throw Error('unexpected');}});
 const request=req('/auth/v1/user');request.headers.authorization='Bearer neon-cookie:aaaaaaaaaaaaaaaaaaaa; other=bad';
 assert.equal((await accounts.handle(request,{})).status,401);assert.equal(called,false);
});
