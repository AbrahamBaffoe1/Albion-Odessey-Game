"""Reference-based Ferguson exterior and explicitly provisional playable interior.
Coordinates: Blender X east, Y depth, Z up. Export swaps Y/Z and winding.
"""
import bpy,math,json,struct,random
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];res=root/'Unity/Assets/Resources/CampusCraft';art=root/'Art'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials={};parts={};colliders={};active='exterior'
def material(name,color,texture=None,scale=1):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True
 b=m.node_tree.nodes.get('Principled BSDF');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Roughness'].default_value=.7
 if texture:
  t=m.node_tree.nodes.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(res/(texture+'_Color.jpg')));m.node_tree.links.new(t.outputs['Color'],b.inputs['Base Color'])
 materials[name]={'material':m,'color':[*color,1],'texture':texture or '', 'scale':scale};return m
brick=material('Ferguson brick',(.62,.32,.22),'red_brick_03');stone=material('Pale cut limestone',(.78,.76,.68));trim=material('Window frames',(.8,.81,.77));glass=material('Reflective blue glass',(.12,.22,.26));glass.node_tree.nodes.get('Principled BSDF').inputs['Metallic'].default_value=.45
roof=material('Standing seam roof',(.13,.16,.17));copper=material('Aged copper cupola',(.22,.38,.34));plaster=material('Warm interior plaster',(.84,.82,.75));floorMat=material('Lobby stone floor',(.7,.7,.67),'concrete_pavement');wood=material('Oak millwork',(.4,.23,.1));fabric=material('Upholstery',(.23,.25,.29));metal=material('Dark bronze',(.08,.09,.09));purple=material('Albion purple',(.21,.105,.31));leaf=material('Oak foliage',(.23,.36,.12));bark=material('Tree bark',(.23,.18,.12));lightMat=material('Warm lamps',(.98,.88,.65))
def record(o,solid=False):
 parts.setdefault(active,[]).append(o)
 if solid:colliders.setdefault(active,[]).append({'name':o.name,'center':[o.location.x,o.location.z,o.location.y],'size':[o.dimensions.x,o.dimensions.z,o.dimensions.y]})
 return o

def box(name,loc,size,mat,solid=False,bevel=.015):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat)
 if bevel:
  m=o.modifiers.new('Crafted edge bevel','BEVEL');m.width=bevel;m.segments=2;o.modifiers.new('Surface normals','WEIGHTED_NORMAL')
 return record(o,solid)
def cylinder(name,loc,radius,depth,mat,vertices=16):
 bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=loc);o=bpy.context.object;o.name=name;o.data.materials.append(mat);return record(o)
def mesh(name,vs,faces,mat):
 m=bpy.data.meshes.new(name);m.from_pydata(vs,[],faces);m.update();o=bpy.data.objects.new(name,m);bpy.context.collection.objects.link(o);o.data.materials.append(mat);return record(o)
def arch(cx,y,base,r,thick,depth):
 vs=[];fs=[]
 for i in range(25):
  a=i*math.pi/24
  for yy in [y-depth/2,y+depth/2]:
   for rr in [r,r+thick]:vs.append((cx+rr*math.cos(a),yy,base+rr*math.sin(a)))
 for i in range(24):
  j=i*4;k=j+4
  fs.extend([(j,j+1,k+1,k),(j+2,k+2,k+3,j+3),(j,k,k+2,j+2),(j+1,j+3,k+3,k+1)])
 fs.extend([(0,2,3,1),(96,97,99,98)]);mesh('Cut stone arch',vs,fs,stone)
def window(x,y,z,width=1.25):
 box('Inset glazing',(x,y,z),(width,.075,1.8),glass,False,0)
 for sx in [-1,1]:box('Window jamb',(x+sx*(width/2+.045),y-.045,z),(.08,.1,1.94),trim,False,.008)
 for dz in [-.94,.94]:box('Stone window lintel and sill',(x,y-.04,z+dz),(width+.28,.18,.12),stone)
 box('Window meeting rail',(x,y-.07,z-.12),(width,.075,.055),trim,False,.003)
 box('Window mullion',(x,y-.07,z),(.045,.075,1.8),trim,False,.003)
# Main walls are separate panels around real window and door openings.
for side in [-1,1]:
 y=side*6
 for f in range(3):
  z=f*3.5
  box('Masonry spandrel',(0,y,z+.65),(31.5,.36,1.3),brick,True)
  box('Masonry lintel band',(0,y,z+3.23),(31.5,.36,.54),brick,True)
  xs=[-14,-11,-8,-5,-2,2,5,8,11,14]
  last=-15.75
  for x in xs:
   a=x-.65;b=x+.65
   if a>last:box('Window bay pier',((last+a)/2,y,z+2.13),(a-last,.36,1.66),brick,True)
   window(x,y-side*.09,z+2.12)
   last=b
  box('Corner masonry',((last+15.75)/2,y,z+2.13),(15.75-last,.36,1.66),brick,True)
# Remove center front ground spandrel: rebuild as two halves so doorway is actually open.
for o in list(parts['exterior']):
 if o.name.startswith('Masonry spandrel') and abs(o.location.y+6)<.01 and o.location.z<1:
  parts['exterior'].remove(o);colliders['exterior']=[b for b in colliders['exterior'] if b['name']!=o.name];bpy.data.objects.remove(o,do_unlink=True)
for x in [-8.825,8.825]:box('Entrance opening masonry',(x,-6,.65),(13.85,.36,1.3),brick,True)
# Center vertical front pier would block door; remove it on ground floor and lintel to 2.8m.
for o in list(parts['exterior']):
 if o.name.startswith('Window bay pier') and abs(o.location.x)<.1 and o.location.y<-5.9 and o.location.z<3:
  parts['exterior'].remove(o);colliders['exterior']=[b for b in colliders['exterior'] if b['name']!=o.name];bpy.data.objects.remove(o,do_unlink=True)
for side in [-1,1]:
 box('Side masonry',(side*15.75,0,5.25),(.36,12,10.5),brick,True)
 for f in range(3):
  for y in [-3,0,3]:
   o=box('Side glazing',(side*15.95,y,2.12+f*3.5),(.06,1.3,1.8),glass,False,0)
box('Foundation left',(-8.825,-6,.12),(13.85,.65,.24),stone,True)
box('Foundation right',(8.825,-6,.12),(13.85,.65,.24),stone,True)
for z in [3.4,10.55]:box('Stone cornice',(0,0,z),(32.1,12.55,.22),stone)
# Three-story projecting entry bay with monumental portico.
for x in [-2.8,2.8]:
 box('Portico stone pier',(x,-7,1.4),(.75,1.1,2.8),stone,True,.035)
 box('Pier base',(x,-7,.15),(1,1.35,.3),stone,True)
 box('Pier capital',(x,-7,2.75),(1,1.4,.24),stone)
 box('Entry bay vertical',(x,-6.4,7.2),(.65,1.0,6.0),brick)
arch(0,-7,2.65,2.42,.38,1.4)
box('Portico entablature',(0,-7,5.48),(6.6,1.55,.32),stone)
box('Entry bay crown',(0,-6.6,10.75),(6.6,1.6,.3),stone)
for z in [6.8,9]:
 for x in [-1.5,0,1.5]:window(x,-6.95,z,1.32)
box('Name plaque',(0,-7.12,5.84),(4.85,.15,.44),stone)
# Hipped roof, ridgeline and visible seams.
mesh('Hipped slate roof',[(-16.15,-6.5,10.8),(16.15,-6.5,10.8),(16.15,6.5,10.8),(-16.15,6.5,10.8),(-11,0,13),(11,0,13)],[(0,1,5,4),(1,2,5),(2,3,4,5),(3,0,4)],roof)
for x in range(-11,12):
 for side in [-1,1]:
  o=box('Roof standing seam',(x,side*3.2,11.89),(.022,6.85,.025),metal,False,0);o.rotation_euler.x=side*math.atan2(-2.2,6.5)
box('Cupola plinth',(0,0,13.1),(4.5,4.5,.35),stone)
for x in [-1.55,1.55]:
 for y in [-1.55,1.55]:box('Cupola columns',(x,y,14.6),(.32,.32,2.8),trim)
for y in [-1.6,1.6]:box('Cupola louver',(0,y,14.4),(2.8,.12,1.8),copper)
for x in [-1.6,1.6]:box('Cupola louver',(x,0,14.4),(.12,2.8,1.8),copper)
box('Cupola cornice',(0,0,16.05),(4,4,.22),stone)
vs=[(0,0,18.5)]+[(2.2*math.cos(i*math.pi/4),2.2*math.sin(i*math.pi/4),16.15) for i in range(8)]
mesh('Copper cupola roof',vs,[(0,1+i,1+(i+1)%8) for i in range(8)],copper)
cylinder('Cupola finial',(0,0,18.75),.07,.6,metal)
# Entrance terrace, brick border and seating. Floor remains flush for access.
box('Entrance terrace',(0,-9,-.02),(11,6,.12),floorMat,True)
for side in [-1,1]:
 for k in range(5):box('Bench oak slat',(side*5,-8+k*.1,.52),(2.5,.075,.055),wood)
 for x in [-.9,.9]:box('Bench foot',(side*5+x,-7.8,.25),(.08,.45,.5),metal,True)
# Interior floor plates have a real stair opening. Public room arrangement is a reconstruction.
for f in range(3):
 active='floor'+str(f);z=f*3.5
 if f==0:box('Ground floor slab',(0,0,-.08),(31.2,11.7,.16),floorMat,True)
 else:
  box('Floor main plate',(-3,0,z-.1),(25.2,11.7,.2),floorMat,True)
  box('Stair outer passage',(14.6,0,z-.1),(1.9,11.7,.2),floorMat,True)
  box('Stair upper landing',(11.7,5.22,z-.1),(4,1.26,.2),floorMat,True)
  box('Stair lower landing',(11.7,-5.2,z-.1),(4,1.3,.2),floorMat,True)
 # corridor runs along x, rooms both sides. Gaps at -10,-4,4 provide access.
 for y in [-1.6,1.6]:
  edges=[-15.55,-10.7,-9.3,-4.7,-3.3,3.3,4.7,8.4]
  for a,b in zip(edges[::2],edges[1::2]):
   if f==0 and y<0 and a<0<b:
    for l,r in [(a,-2.2),(2.2,b)]:box('Lobby opening wall',((l+r)/2,y,z+1.6),(r-l,.15,3.2),plaster,True)
   else:box('Corridor wall',((a+b)/2,y,z+1.6),(b-a,.15,3.2),plaster,True)
  for x in [-10,-4,4]:box('Room door lintel',(x,y,z+2.85),(1.4,.15,.7),plaster,True)
 for x in [-7,-.5]:
  for y in [-3.8,3.8]:
   if not(f==0 and x==-.5 and y<0):box('Room division',(x,y,z+1.6),(.15,4.2,3.2),plaster,True)
 for x in [-11,-4,4]:
  for y in [-4.25,4.25]:
   box('Oak work table',(x,y,z+.76),(2.4,1.05,.1),wood,True,.04)
   for dx in [-1,1]:
    for dy in [-.37,.37]:box('Desk legs',(x+dx,y+dy,z+.36),(.06,.06,.72),metal)
   box('Desk monitor',(x,y+.3,z+1.05),(.63,.08,.4),metal,False,.02)
   box('Desk keyboard',(x,y-.13,z+.825),(.5,.2,.018),metal)
   box('Chair cushion',(x,y-1,z+.48),(.52,.52,.12),fabric,True,.055)
   box('Chair back',(x,y-1.25,z+.82),(.52,.1,.63),fabric,False,.06)
   cylinder('Chair pedestal',(x,y-1,z+.22),.055,.4,metal)
 for x in [-12,-6,0,6]:box('Ceiling light',(x,0,z+3.28),(.38,1.15,.045),lightMat)
 # straight stair flight with 20 genuinely climbable treads, 17.5cm rise.
 if f<2:
  for step in range(20):
   yy=-4.4+(step+.5)*.45;hh=(step+1)*.175
   box('Stair tread',(11.7,yy,z+hh-.0875),(2.55,.45,.175),stone,True,.008)
  for side in [-1,1]:
   for step in range(0,21,2):
    yy=-4.4+step*.45;hh=z+step*.175
    box('Stair baluster',(11.7+side*1.38,yy,hh+.52),(.045,.045,1.04),metal,False,.004)
   o=box('Continuous stair rail',(11.7+side*1.38,.1,z+2.75),(.07,9.66,.07),wood,False,.018);o.rotation_euler.x=math.atan2(3.5,9)
 # no flat ceiling across stairwell, the next floor provides ceiling.
active='exterior';box('Roof ceiling',(0,0,10.45),(31.2,11.7,.12),plaster,True)
# Assign the same physical UVs to the authoring meshes used by the review render.
for objects in parts.values():
 for o in objects:
  if o.type!='MESH':continue
  uv=o.data.uv_layers.active or o.data.uv_layers.new(name='Physical meters')
  for face in o.data.polygons:
   n=o.matrix_world.to_3x3()@face.normal
   for li in face.loop_indices:
    v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co
    uv.data[li].uv=(v.x/2,v.y/2) if abs(n.z)>.7 else (v.y/2,v.z/2) if abs(n.x)>.7 else (v.x/2,v.z/2)
# Export grouped UV-mapped meshes. Planar UVs in physical meters keep brick scale consistent.
names=list(materials)
def export(group,objects):
 verts=[];indices=[[] for _ in names];deps=bpy.context.evaluated_depsgraph_get()
 for obj in objects:
  if obj.type!='MESH':continue
  ev=obj.evaluated_get(deps);me=ev.to_mesh();me.calc_loop_triangles();norm=obj.matrix_world.to_3x3().inverted().transposed()
  for tri in me.loop_triangles:
   ids=[];midx=names.index(me.materials[tri.material_index].name)
   for li in tri.loops:
    l=me.loops[li];v=obj.matrix_world@me.vertices[l.vertex_index].co;n=(norm@me.corner_normals[li].vector).normalized()
    if abs(n.z)>.7:uv=(v.x/2,v.y/2)
    elif abs(n.x)>.7:uv=(v.y/2,v.z/2)
    else:uv=(v.x/2,v.z/2)
    ids.append(len(verts));verts.append((v.x,v.z,v.y,n.x,n.z,n.y,*uv))
   indices[midx].extend(reversed(ids))
  ev.to_mesh_clear()
 with (res/('ferguson-'+group+'.bytes')).open('wb') as out:
  out.write(b'AOM1');out.write(struct.pack('<ii',len(verts),len(names)))
  for v in verts:out.write(struct.pack('<8f',*v))
  for ids in indices:out.write(struct.pack('<i',len(ids)));out.write(struct.pack('<'+'i'*len(ids),*ids))
 print(group,len(verts),'vertices',len(colliders.get(group,[])),'colliders')
for group,objs in parts.items():export(group,objs)
model={'materials':[{'name':n,'color':materials[n]['color'],'texture':materials[n]['texture']} for n in names],'sections':[{'name':n,'colliders':colliders.get(n,[])} for n in parts],'note':'Exterior based on photographs. Scale, floor plan and public room contents are provisional reconstructions; no official floor plan supplied.'}
(res/'ferguson.json').write_text(json.dumps(model,indent=2))
# Save authoring scene and a review render.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=16
scene.world.color=(.35,.4,.5)
bpy.ops.object.light_add(type='SUN',location=(10,-15,25));bpy.context.object.rotation_euler=(.5,-.4,-.4);bpy.context.object.data.energy=2
bpy.ops.object.light_add(type='AREA',location=(0,-18,15));bpy.context.object.data.energy=1700;bpy.context.object.data.size=18;bpy.context.object.rotation_euler=(Vector((0,0,5))-bpy.context.object.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(31,-39,15));cam=bpy.context.object;cam.rotation_euler=(Vector((0,-1,6))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=42;scene.camera=cam
scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100;scene.render.filepath=str(art/'Ferguson-review.png')
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(art/'FergusonHall.blend'))
bpy.ops.render.render(write_still=True)
print('FERGUSON_MODEL_OK')
