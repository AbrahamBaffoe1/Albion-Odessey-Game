import pg from 'pg';
import {createPortraits} from './portraits.mjs';
const EMAIL=/^[^\s<>@]+@[^\s<>@]+\.[^\s<>@]+$/;
export const profileColumns='id,display_name,avatar_skin,avatar_outfit,avatar_hair,avatar_backpack,avatar_ready';
export function validProfile(body){
 const allowed=new Set(['display_name','avatar_skin','avatar_outfit','avatar_hair','avatar_backpack','avatar_ready']);
 if(!body||Array.isArray(body)||Object.keys(body).length===0||Object.keys(body).some(k=>!allowed.has(k)))return false;
 if('display_name' in body&&(typeof body.display_name!=='string'||!body.display_name.trim()||body.display_name.trim().length>24||/[<>\x00-\x1f\x7f]/.test(body.display_name)))return false;
 for(const [k,max] of [['avatar_skin',4],['avatar_outfit',4],['avatar_hair',2]])if(k in body&&(!Number.isInteger(body[k])||body[k]<0||body[k]>max))return false;
 for(const k of ['avatar_backpack','avatar_ready'])if(k in body&&typeof body[k]!=='boolean')return false;
 return true;
}
export function createNeonAccounts(env=process.env,{pool,fetcher=fetch,logger=console}={}){
 const configured=Boolean(env.NEON_DATABASE_URL&&env.NEON_AUTH_URL);
 if(!configured)return null;
 const db=pool??new pg.Pool({connectionString:env.NEON_DATABASE_URL,max:5,idleTimeoutMillis:30000,connectionTimeoutMillis:10000});
 const portraits=createPortraits({db,env});
 const base=env.NEON_AUTH_URL.replace(/\/$/,'');const limits=new Map();
 async function call(path,body,token){
  const headers={'Content-Type':'application/json','Origin':env.PUBLIC_URL||'https://albion-odyssey-online.onrender.com'};
  if(token?.startsWith('neon-cookie:')){
   const value=token.slice(12);if(!/^[A-Za-z0-9%._~+\/=-]{20,4096}$/.test(value))throw Object.assign(Error('Invalid session'),{status:401});
   headers.Cookie='__Secure-neon-auth.session_token='+value;
  }else if(token)headers.Authorization='Bearer '+token;
  const response=await fetcher(base+path,{method:body===undefined?'GET':'POST',headers,body:body===undefined?undefined:JSON.stringify(body),redirect:'error',signal:AbortSignal.timeout(12000)});
  const data=await response.json().catch(()=>null);
  if(!response.ok){
   // Log only bounded provider error identifiers: never bodies, email, OTP or credentials.
   const code=typeof data?.code==='string'&&/^[A-Z_]{1,80}$/.test(data.code)?data.code:'UNKNOWN';
   logger.warn('ACCOUNT_PROVIDER_FAILURE',JSON.stringify({stage:path.split('?')[0],status:response.status,code}));
   throw Object.assign(new Error('Authentication request failed'),{status:response.status===429?429:response.status>=500?503:401});
  }
  // Managed Neon Auth uses its signed HttpOnly session cookie. Native clients
  // retain only this opaque credential in memory; never forward unrelated cookies.
  const cookies=response.headers.getSetCookie?.()||[response.headers.get('set-cookie')||''];
  const cookie=cookies.map(v=>v.match(/(?:^|,\s*)__Secure-neon-auth\.session_token=([^;]+)/)?.[1]).find(v=>v&&/^[A-Za-z0-9%._~+\/=-]{20,4096}$/.test(v));
  return {data,token:cookie?'neon-cookie:'+cookie:response.headers.get('set-auth-token')||data?.token};
 }
 async function profile(user){
  const result=await db.query(`INSERT INTO odyssey_profiles(auth_id) VALUES($1) ON CONFLICT(auth_id) DO UPDATE SET auth_id=EXCLUDED.auth_id RETURNING ${profileColumns}`,[user.id]);return result.rows[0];
 }
 async function session(token){
  if(typeof token!=='string'||token.length<20||token.length>8192||/[\r\n]/.test(token))throw Object.assign(Error('Sign in'),{status:401});
  const {data}=await call('/get-session?disableCookieCache=true',undefined,token);
  if(!data?.user?.id||data.user.emailVerified!==true||!data.session||!Number.isFinite(Date.parse(data.session.expiresAt))||Date.parse(data.session.expiresAt)<=Date.now()){
   logger.warn('ACCOUNT_SESSION_REJECTED',JSON.stringify({hasUser:Boolean(data?.user?.id),verified:data?.user?.emailVerified===true,hasSession:Boolean(data?.session)}));
   throw Object.assign(Error('Sign in'),{status:401});
  }
  return data;
 }
 async function envelope(token){const s=await session(token),p=await profile(s.user);return {access_token:token,refresh_token:token,expires_in:Math.min(900,Math.max(1,Math.floor((Date.parse(s.session.expiresAt)-Date.now())/1000))),user:{id:p.id,email:s.user.email,email_confirmed_at:s.user.updatedAt||new Date().toISOString()}};}
 function permit(key,max,window){const now=Date.now();for(const[k,v]of limits)if(v.until<=now)limits.delete(k);if(limits.size>10000)return false;const v=limits.get(key)||{n:0,until:now+window};v.n++;limits.set(key,v);return v.n<=max;}
 return {
  portraits,
  ready:()=>env.NEON_SMTP_READY==='true',
  async init(){await db.query(`CREATE TABLE IF NOT EXISTS odyssey_profiles (id uuid PRIMARY KEY DEFAULT gen_random_uuid(),auth_id text UNIQUE NOT NULL,display_name text NOT NULL DEFAULT 'Student' CHECK(length(display_name) BETWEEN 1 AND 24),avatar_skin integer NOT NULL DEFAULT 1 CHECK(avatar_skin BETWEEN 0 AND 4),avatar_outfit integer NOT NULL DEFAULT 0 CHECK(avatar_outfit BETWEEN 0 AND 4),avatar_hair integer NOT NULL DEFAULT 0 CHECK(avatar_hair BETWEEN 0 AND 2),avatar_backpack boolean NOT NULL DEFAULT true,avatar_ready boolean NOT NULL DEFAULT false,created_at timestamptz NOT NULL DEFAULT now())`);await portraits.init();},
  async identify(token){const s=await session(token),p=await profile(s.user);return {id:p.id,display:p.display_name,skin:p.avatar_skin,outfit:p.avatar_outfit,hair:p.avatar_hair,backpack:p.avatar_backpack};},
  async handle(req,body){
   const url=new URL(req.url,'http://localhost'),path=url.pathname;
   if(!path.startsWith('/auth/v1/')&&path!=='/rest/v1/student_profiles')return null;
   // Render's socket address is a shared proxy: use its final forwarded address only on Render.
   const ip=env.RENDER?String(req.headers['x-forwarded-for']||req.socket.remoteAddress).split(',').at(-1).trim():req.socket.remoteAddress;
   if(!permit('ip:'+ip,120,60000))return {status:429,body:{error:'Please wait before trying again.'}};
   try{
    if(path==='/auth/v1/otp'&&req.method==='POST'){
     const email=String(body.email||'').trim().toLowerCase();if(email.length>254||!EMAIL.test(email))return {status:400,body:{error:'Enter a valid email.'}};
     if(env.NEON_SMTP_READY!=='true')return {status:503,body:{error:'Hostinger email setup is not complete.'}};
     if(!permit('email:'+email,1,60000))return {status:429,body:{error:'Wait a minute before requesting another code.'}};
     await call('/email-otp/send-verification-otp',{email,type:'sign-in'});return {status:200,body:{}};
    }
    if(path==='/auth/v1/verify'&&req.method==='POST'){
     if(!EMAIL.test(body.email||'')||!/^\d{6,10}$/.test(body.token||''))return {status:400,body:{error:'Invalid code.'}};
     if(!permit('verify:'+String(body.email).toLowerCase(),12,60000))return {status:429,body:{error:'Too many attempts.'}};
     const result=await call('/sign-in/email-otp',{email:body.email.trim().toLowerCase(),otp:body.token,name:'Student'});
     try{return {status:200,body:await envelope(result.token)};}
     catch(error){
      logger.warn('ACCOUNT_SESSION_SETUP_FAILED',JSON.stringify({status:error.status||503,credentialTransport:result.token?.startsWith('neon-cookie:')?'cookie':result.token?'bearer':'missing'}));
      return {status:503,body:{code:'SESSION_SETUP_FAILED',error:'Your code was accepted, but sign-in could not finish. Please request a fresh code and try again shortly.'}};
     }
    }
    const token=String(req.headers.authorization||'').replace(/^Bearer /,'');
    if(path==='/auth/v1/token'&&req.method==='POST')return {status:200,body:await envelope(body.refresh_token)};
    if(path==='/auth/v1/logout'&&req.method==='POST'){await session(token);await call('/sign-out',{},token);return {status:200,body:{}};}
    if(path==='/auth/v1/user'&&req.method==='GET')return {status:200,body:(await envelope(token)).user};
    if(path==='/rest/v1/student_profiles'){
     const s=await session(token),p=await profile(s.user);
     if(url.searchParams.get('id')!=='eq.'+p.id)return {status:403,body:{error:'This profile is private.'}};
     if(req.method==='GET')return {status:200,body:[p]};
     if(req.method==='PATCH'){
      if(!validProfile(body))return {status:400,body:{error:'Invalid profile update.'}};
      const keys=Object.keys(body),values=keys.map(k=>k==='display_name'?body[k].trim():body[k]);values.push(s.user.id);
      await db.query(`UPDATE odyssey_profiles SET ${keys.map((k,i)=>k+'=$'+(i+1)).join(',')} WHERE auth_id=$${values.length}`,values);return {status:200,body:{}};
     }
    }
    return {status:404,body:{error:'Not found'}};
   }catch(error){return {status:error.status||503,body:{error:error.status===429?'Too many requests. Try again shortly.':error.status===401?'Sign in again or request a new code.':'Account service is temporarily unavailable.'}};}
  },async close(){await db.end();}
 };
}
