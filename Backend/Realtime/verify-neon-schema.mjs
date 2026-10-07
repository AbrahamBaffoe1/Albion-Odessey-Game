// Explicit admin validation; credentials stay in subprocess memory and are not logged.
import {execFileSync} from 'node:child_process';import {createNeonAccounts} from './neon-accounts.mjs';
const uri=execFileSync('neon',['connection-string','production','--project-id','winter-pine-85132555','--database-name','albion_odyssey','--pooled','--ssl','verify-full'],{encoding:'utf8'}).trim();
const accounts=createNeonAccounts({NEON_DATABASE_URL:uri,NEON_AUTH_URL:'https://ep-cool-morning-b5r28ljb.neonauth.c-7.us-east-2.aws.neon.tech/albion_odyssey/auth'});
try{await accounts.init();console.log('Neon profile schema ready');try{await accounts.identify('invalid-session-token-for-auth-check');throw Error('Invalid session accepted');}catch(e){if(e.status!==401)throw e;console.log('Live Neon rejects invalid sessions');}}finally{await accounts.close();}
