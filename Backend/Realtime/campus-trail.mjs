// Match CampusCatalog.Point and the walkable entrance approaches (game meters).
export const trailStops = Object.freeze([
 {id:'26',name:'Ferguson Hall',x:18,z:411.5},
 {id:'49',name:'Seaton Hall',x:156,z:498.5},
 {id:'50',name:'Wesley Hall',x:19.5,z:689.5},
]);
export function updateTrail(room,player) {
 room.trail ??= new Map();
 if(player.y < -1 || player.y > 3) return;
 for(const stop of trailStops) {
  if(!room.trail.has(stop.id) && Math.hypot(player.x-stop.x,player.z-stop.z)<=9)
   room.trail.set(stop.id,{id:stop.id,by:player.display});
 }
}
export function trailSnapshot(room) {
 return trailStops.map(stop=>({...stop,visited:room.trail?.has(stop.id)??false,by:room.trail?.get(stop.id)?.by??''}));
}
