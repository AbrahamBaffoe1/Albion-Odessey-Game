// Server-owned runner simulation. Clients send intentions, never distances or rewards.
import {randomInt} from 'node:crypto';
export const COURSE_LENGTH=1200;
export function makeCourse(seed){
 let n=seed>>>0;const rand=()=>{n=(Math.imul(n,1664525)+1013904223)>>>0;return n/4294967296;};const events=[];
 for(let z=35;z<COURSE_LENGTH-20;z+=18){const lane=Math.floor(rand()*3)-1;const kind=['log','branch','rock','seed','treasure'][Math.floor(rand()*5)];events.push({id:events.length,z,lane,kind});if(kind==='rock')events.push({id:events.length,z:z+7,lane:lane===1?-1:lane+1,kind:'seed'});}
 return events;
}
export function forestCommand(room,player,m,now=Date.now()){
 if(m.action==='join'){
  if(!room.forest||room.forest.phase==='finished'){
   const seed=randomInt(1,2147483647);room.forest={seed,phase:'countdown',startsAt:now+5000,lastTick:now,players:new Map(),events:makeCourse(seed),seeds:0,treasures:0,rescues:0,used:new Set()};
  }
  const f=room.forest;if(f.players.has(player.id))return;
  // Join ongoing runs at the current group distance with a short grace period.
  const distance=Math.max(0,...[...f.players.values()].filter(p=>!p.caught).map(p=>p.distance));
  f.players.set(player.id,{id:player.id,display:player.display,lane:0,distance,gap:20,caught:false,finished:false,pose:'run',poseUntil:0,nextAction:0,graceUntil:now+2500,treasures:0});return;
 }
 const f=room.forest,p=f?.players.get(player.id);if(!p)return;
 if(m.action==='leave'){f.players.delete(player.id);if(!f.players.size)room.forest=null;return;}
 if(f.phase!=='running'||p.caught||p.finished||now<p.nextAction)return;
 if(m.action==='left'||m.action==='right'){p.lane=Math.max(-1,Math.min(1,p.lane+(m.action==='left'?-1:1)));p.nextAction=now+140;}
 else if((m.action==='jump'||m.action==='slide')&&now>=p.poseUntil){p.pose=m.action;p.poseUntil=now+950;p.nextAction=now+140;}
 else if(m.action==='rescue'&&f.rescues>0){const other=[...f.players.values()].find(q=>q.caught);if(other){f.rescues--;other.caught=false;other.gap=18;other.distance=p.distance;other.graceUntil=now+2500;p.nextAction=now+1000;}}
}
export function forestLeave(room,id){if(!room?.forest)return;room.forest.players.delete(id);if(!room.forest.players.size)room.forest=null;}
export function tickForest(room,now=Date.now()){
 const f=room.forest;if(!f||f.phase==='finished')return;
 if(now<f.startsAt){f.lastTick=now;return;}f.phase='running';const dt=Math.max(0,Math.min(.25,(now-Math.max(f.lastTick,f.startsAt))/1000));f.lastTick=now;
 for(const p of f.players.values()){
  if(p.caught||p.finished)continue;if(now>=p.poseUntil)p.pose='run';
  const before=p.distance;p.distance=Math.min(COURSE_LENGTH,p.distance+dt*(10+Math.min(6,p.distance/160)));
  p.gap=Math.min(24,p.gap+dt*(f.seeds>=9?.65:.22));
  for(const e of f.events){if(e.z<=before||e.z>p.distance||e.lane!==p.lane)continue;
   if(e.kind==='treasure'||e.kind==='seed'){
    if(f.used.has(e.id))continue;f.used.add(e.id);
    if(e.kind==='treasure'){f.treasures++;p.treasures++;}else{f.seeds++;if(f.seeds%3===0)f.rescues++;}
   }else if(now>=p.graceUntil&&!((e.kind==='log'&&p.pose==='jump')||(e.kind==='branch'&&p.pose==='slide'))){p.gap-=8;if(p.gap<=0){p.caught=true;p.pose='caught';break;}}
  }
  if(p.distance>=COURSE_LENGTH)p.finished=true;
 }
 if([...f.players.values()].every(p=>p.caught||p.finished))f.phase='finished';
}
export function forestSnapshot(room,now=Date.now()){
 const f=room.forest;if(!f)return null;
 return {seed:f.seed,phase:f.phase,countdown:Math.max(0,(f.startsAt-now)/1000),length:COURSE_LENGTH,seeds:f.seeds,treasures:f.treasures,rescues:f.rescues,restored:f.seeds>=9,players:[...f.players.values()].map(({nextAction,poseUntil,graceUntil,...p})=>p),events:f.events.filter(e=>!f.used.has(e.id))};
}
