import bpy,math,random,struct,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];res=root/'Unity/Assets/Resources/CampusCraft'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
mats=[]
for name,c in [('Oak bark',(.25,.21,.16)),('Sunlit leaves',(.36,.46,.18)),('Middle leaves',(.23,.36,.12)),('Shade leaves',(.13,.25,.10))]:
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);mats.append(m)
for seed in range(3):
 rng=random.Random(1835+seed);vs=[];fs=[];mi=[]
 def branch(a,b,r1,r2):
  axis=(b-a).normalized();u=axis.cross(Vector((0,1,0))).normalized();v=axis.cross(u);start=len(vs)
  for p,r in [(a,r1),(b,r2)]:
   for i in range(7):vs.append(p+(u*math.cos(i*math.tau/7)+v*math.sin(i*math.tau/7))*r)
  for i in range(7):fs.append((start+i,start+(i+1)%7,start+(i+1)%7+7,start+i+7));mi.append(0)
 branch(Vector((0,0,0)),Vector((.14,0,6)),.29,.085)
 for j in range(20):
  a=j*2.399;h=2.3+j*.16;tip=Vector((math.cos(a)*rng.uniform(1.8,3.0),math.sin(a)*rng.uniform(1.8,3.0),h+rng.uniform(1.4,2.4)));origin=Vector((.1,0,h));branch(origin,tip,.07,.008)
  for k in range(5):
   center=origin.lerp(tip,.45+k*.12)+Vector((rng.uniform(-.8,.8),rng.uniform(-.8,.8),rng.uniform(.1,.8)));branch(origin.lerp(tip,.35+k*.1),center,.021,.003)
   for l in range(15):
    p=center+Vector((rng.uniform(-.72,.72),rng.uniform(-.72,.72),rng.uniform(-.48,.55)));az=rng.uniform(0,math.tau);u=Vector((math.cos(az),math.sin(az),rng.uniform(-.6,.6)))*rng.uniform(.13,.23);v=Vector((-math.sin(az),math.cos(az),rng.uniform(-.4,.4)))*rng.uniform(.06,.11);n=len(vs)
    vs.extend([p-u,p+v,p+u,p-v]);fs.extend([(n,n+1,n+2,n+3),(n+3,n+2,n+1,n)]);c=rng.randrange(1,4);mi.extend([c,c])
 mesh=bpy.data.meshes.new('Oak crown '+str(seed));mesh.from_pydata(vs,[],fs);mesh.update();o=bpy.data.objects.new('Campus oak '+str(seed),mesh);bpy.context.collection.objects.link(o)
 for m in mats:mesh.materials.append(m)
 for i,f in enumerate(mesh.polygons):f.material_index=mi[i]
 mesh.calc_loop_triangles();vertices=[];ids=[[] for _ in mats]
 for tri in mesh.loop_triangles:
  ix=[]
  for vi in tri.vertices:
   v=mesh.vertices[vi].co;n=tri.normal;ix.append(len(vertices));vertices.append((v.x,v.z,v.y,n.x,n.z,n.y,v.x,v.z))
  ids[tri.material_index].extend(reversed(ix))
 with (res/('oak'+str(seed)+'.bytes')).open('wb') as f:
  f.write(b'AOM1');f.write(struct.pack('<ii',len(vertices),len(mats)))
  for v in vertices:f.write(struct.pack('<8f',*v))
  for a in ids:f.write(struct.pack('<i',len(a)));f.write(struct.pack('<'+'i'*len(a),*a))
 o.location.x=seed*9
(res/'oak.json').write_text(json.dumps({'materials':[{'name':m.name,'color':list(m.diffuse_color),'texture':''} for m in mats],'sections':[{'name':'oak','colliders':[{'name':'Trunk','center':[0,2,0],'size':[.55,4,.55]}]}]}))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Art/CampusOaks.blend'));print('CAMPUS_TREES_OK')
