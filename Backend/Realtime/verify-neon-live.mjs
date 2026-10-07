// Explicit opt-in QA only. Credentials, OTPs and message contents never enter logs.
import {readFileSync} from 'node:fs';
import assert from 'node:assert/strict';
if(!process.argv.includes('--run-live'))throw Error('Requires --run-live');
const inboxes=JSON.parse(readFileSync('Verification/account-inboxes.json'));
const email=inboxes['qa-a'];
const base=JSON.parse(readFileSync('Unity/Assets/Resources/AccountConfig.json')).url;
const start=new Date();
const mailBase='https://api.agentmail.to/v0/inboxes/'+encodeURIComponent(email)+'/messages';
async function request(path,body,token){const r=await fetch(base+path,{method:body?'POST':'GET',headers:{'Content-Type':'application/json',...(token?{Authorization:'Bearer '+token}:{})},body:body?JSON.stringify(body):undefined,signal:AbortSignal.timeout(90000)});return {status:r.status,data:await r.json().catch(()=>null)};}
async function mail(url){const r=await fetch(url,{headers:{Authorization:'Bearer '+process.env.AGENTMAIL_AGENTMAIL_API_KEY}});if(!r.ok)throw Error('QA mailbox HTTP '+r.status);return r.json();}
try{
 const send=await request('/auth/v1/otp',{email});console.log('Request code HTTP',send.status);if(send.status!==200)process.exit(1);
 let code;
 for(let n=0;n<30&&!code;n++){
  const list=await mail(mailBase+'?after='+encodeURIComponent(start.toISOString())+'&limit=10');
  for(const item of list.messages||[]){const m=await mail(mailBase+'/'+encodeURIComponent(item.message_id));if(!String(m.from).includes('abraham@nexoralab.net'))continue;code=((m.text||'')+' '+(m.html||'').replace(/<[^>]*>/g,' ')).match(/(?<!\w)\d{6,10}(?!\w)/)?.[0];if(code)break;}
  if(!code)await new Promise(r=>setTimeout(r,2000));
 }
 if(!code)throw Error('QA verification email not delivered');console.log('Fresh Hostinger email received');
 const verified=await request('/auth/v1/verify',{email,token:code,type:'email'});console.log('Verify code HTTP',verified.status);
 if(verified.status!==200){console.log('Response category',verified.data?.code||'AUTH_FAILED');process.exit(1);}
 const token=verified.data.access_token;const uid=verified.data.user.id;
 const p=await request('/rest/v1/student_profiles?id=eq.'+uid,undefined,token);console.log('Profile HTTP',p.status);assert.equal(p.status,200);assert.equal(p.data[0].id,uid);
 const refresh=await request('/auth/v1/token',{refresh_token:token});console.log('Refresh HTTP',refresh.status);assert.equal(refresh.status,200);assert.equal(refresh.data.user.id,uid);
 const reused=await request('/auth/v1/verify',{email,token:code});assert([400,401].includes(reused.status));console.log('Reused code rejected: PASS');
 const out=await request('/auth/v1/logout',{},token);console.log('Logout HTTP',out.status);assert.equal(out.status,200);
 const revoked=await request('/auth/v1/user',undefined,token);assert.equal(revoked.status,401);console.log('Revoked session rejected: PASS');console.log('LIVE_NEON_AUTH_PASS');
}catch(e){console.error(e.message?.startsWith('QA')?e.message:'Live authentication test could not finish');process.exitCode=1;}
