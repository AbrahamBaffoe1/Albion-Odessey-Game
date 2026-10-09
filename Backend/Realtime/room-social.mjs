// Room-owned, bounded state. Identity is supplied by the authenticated socket, never by a command.
// Spawn coordinates match CampusExpansion and AlbionCity; clients cannot invent a car location.
const spawns=[[13.5,413.5,0],[180,533.5,0],[298.5,346,0],[-926.4,630,0],[-933.6,692,180],[-1050,348.6,90],[-810,343.4,270]];
export function socialState(room){return room.social??= {messages:[],next:1,cars:new Map(spawns.map(([x,z,yaw],car)=>[car,{car,driver:'',x,y:.08,z,yaw,speed:0,seats:[]}])),rides:new Map(),ratings:new Map()};}
export function socialCommand(room,p,m,now){
 const s=socialState(room);
 if(m.type==='chat'){
  if(typeof m.text!=='string'||now-(p.lastChat??0)<800)return false;
  const text=m.text.replace(/[<>\x00-\x1f\x7f]/g,'').trim().slice(0,280);if(!text)return false;
  p.lastChat=now;s.messages.push({seq:s.next++,id:p.id,display:p.display,text});if(s.messages.length>40)s.messages.shift();return true;
 }
 if(m.type!=='ride')return false;
 const car=s.cars.get(m.car);
 if(m.action==='drive'){
  if(!Number.isInteger(m.car)||m.car<0||m.car>6||!Number.isFinite(m.x)||!Number.isFinite(m.z)||Math.abs(m.x)>2000||Math.abs(m.z)>2000||Math.hypot(p.x-m.x,p.z-m.z)>6||s.rides.has(p.id)||[...s.cars.values()].some(c=>c.driver===p.id&&c.car!==m.car)||(car&&car.driver&&car.driver!==p.id))return false;
  if(!car||Math.hypot(car.x-p.x,car.z-p.z)>6)return false;
  s.cars.set(m.car,{car:m.car,driver:p.id,x:car?.x??m.x,y:0,z:car?.z??m.z,yaw:car?.yaw??0,speed:0,seats:car?.seats??[]});return true;
 }
 if(m.action==='pose'){
  if(!car||car.driver!==p.id||![m.x,m.y,m.z,m.yaw,m.speed].every(Number.isFinite)||Math.abs(m.x)>2000||Math.abs(m.z)>2000||m.y< -10||m.y>30||Math.abs(m.speed)>20)return false;
  const dt=Math.min(1,Math.max(.05,(now-(car.at??now))/1000));if(Math.hypot(m.x-car.x,m.z-car.z)>23*dt+1)return false;
  Object.assign(car,{x:m.x,y:m.y,z:m.z,yaw:m.yaw,speed:m.speed,at:now});return true;
 }
 if(m.action==='board'){
  if(!car||!car.driver||car.driver===p.id||Math.abs(car.speed)>.5||car.seats.length>=3||s.rides.has(p.id)||[...s.cars.values()].some(c=>c.driver===p.id)||Math.hypot(p.x-car.x,p.z-car.z)>6)return false;
  const seat=[1,2,3].find(n=>!car.seats.some(r=>r.seat===n));const ride={id:p.id,driver:car.driver,car:m.car,seat,start:now};car.seats.push(ride);s.rides.set(p.id,ride);return true;
 }
 if(m.action==='exit'){
  const ride=s.rides.get(p.id);const current=ride?s.cars.get(ride.car):[...s.cars.values()].find(c=>c.driver===p.id);
  if(!current||Math.abs(current.speed)>.5)return false;
  if(ride){current.seats=current.seats.filter(r=>r.id!==p.id);s.rides.delete(p.id);p.rateRide=now-ride.start>=5000?{driver:ride.driver,expires:now+120000}:null;}
  else{if(current.seats.length)return false;current.driver='';}return true;
 }
 if(m.action==='rate'){
  const eligible=p.rateRide;if(!eligible||eligible.expires<now||eligible.driver===p.id||!Number.isInteger(m.stars)||m.stars<1||m.stars>5)return false;
  const rating=s.ratings.get(eligible.driver)??{id:eligible.driver,total:0,count:0};rating.total+=m.stars;rating.count++;s.ratings.set(eligible.driver,rating);p.rateRide=null;return true;
 }
 return false;
}
export function socialLeave(room,id){if(!room?.social)return;const s=room.social;s.rides.delete(id);for(const c of s.cars.values()){c.seats=c.seats.filter(r=>r.id!==id);if(c.driver===id){for(const r of c.seats)s.rides.delete(r.id);c.seats=[];c.driver='';c.speed=0;}}}
export function socialSnapshot(room){const s=socialState(room);return {messages:s.messages,cars:[...s.cars.values()],ratings:[...s.ratings.values()]};}
