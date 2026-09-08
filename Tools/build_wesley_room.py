import bpy,math,json,struct
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];art=root/'Art';art.mkdir(exist_ok=True)
res=root/'Unity/Assets/Resources/CampusTour'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
materials={};colliders=[]
def mat(name,color):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(*color,1);m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.65;materials[name]=m;return m
cream=mat('Warm plaster',(.79,.76,.65));white=mat('Painted trim',(.91,.90,.83));wood=mat('Honey wood',(.43,.24,.095));navy=mat('Navy mattress',(.035,.065,.15));floor=mat('Ivory floor',(.67,.67,.60));green=mat('Green floor border',(.06,.22,.15));metal=mat('Radiator',(.60,.60,.50));glass=mat('Window daylight',(.56,.70,.77));black=mat('Outlet',(.05,.045,.04))
def box(name,loc,dim,material,bevel=0,collision=False):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material)
 if bevel:
  mod=o.modifiers.new('Soft edges','BEVEL');mod.width=bevel;mod.segments=2
  o.modifiers.new('Weighted normals','WEIGHTED_NORMAL')
 if collision:colliders.append({'name':name,'center':[loc[0],loc[2],loc[1]],'size':[dim[0],dim[2],dim[1]]})
 return o
# Provisional 4.8 × 6.4m room, with a real 1.4m doorway and small exit landing.
box('Floor',(0,-.35,-.10),(5,8.1,.2),floor,collision=True)
for x in [-2.4,2.4]:box('Side wall',(x,0,1.55),(.18,6.6,3.1),cream,collision=True)
box('Window wall left',(-1.65,3.2,1.55),(1.5,.18,3.1),cream,collision=True)
box('Window wall right',(1.65,3.2,1.55),(1.5,.18,3.1),cream,collision=True)
box('Under window',(0,3.2,.6),(1.8,.18,1.2),cream,collision=True)
box('Above window',(0,3.2,2.89),(1.8,.18,.42),cream,collision=True)
for x in [-1.59,1.59]:box('Door wall',(x,-3.2,1.55),(1.58,.18,3.1),cream,collision=True)
box('Door lintel',(0,-3.2,2.72),(1.6,.18,.76),cream,collision=True)
box('Ceiling',(0,0,3.17),(5,6.6,.14),white,collision=True)
for x in [-2.28,2.28]:
 box('Baseboard',(x,0,.1),(.06,6.3,.2),white,.01)
 box('Green border',(x,0,.007),(.24,6.4,.014),green)
box('Back green border',(0,2.97,.007),(4.6,.34,.014),green)
# Fine grout lines establish scale without photographed textures.
for y in range(-6,7):box('Floor seam',(0,y*.5,.004),(4.6,.008,.008),metal)
for x in range(-4,5):box('Floor seam',(x*.5,0,.004),(.008,6.3,.008),metal)
for x in [-.88,.88]:box('Window jamb',(x,3.08,1.93),(.09,.10,1.57),white,.01)
for z in [1.16,2.71]:box('Window horizontal trim',(0,3.08,z),(1.85,.11,.09),white,.01)
box('Window pane',(0,3.15,1.94),(1.67,.03,1.45),glass)
box('Window sill',(0,2.97,1.16),(1.98,.38,.075),white,.015)
for i in range(25):box('Blind slat',(0,3.015,1.25+i*.055),(1.63,.055,.024),white,.004)
box('Radiator body',(0,2.99,.43),(1.85,.28,.72),metal,.018,True)
for i in range(21):box('Radiator grille',(-.85+i*.085,2.836,.66),(.035,.018,.15),black,.005)
for x in [-1.54,1.54]:
 box('Bed mattress',(x,1.57,.66),(1.18,2.35,.19),navy,.065,True)
 for sx in [-.6,.6]:box('Bed side rail',(x+sx,1.57,.46),(.075,2.45,.22),wood,.02)
 for y in [.29,2.85]:
  box('Bed end panel',(x,y,.51),(1.31,.08,.46),wood,.018)
  for sx in [-.6,.6]:box('Bed leg',(x+sx,y,.35),(.09,.09,.7),wood,.015)
 box('Bed collision',(x,1.57,.36),(1.31,2.62,.72),wood,collision=True).hide_render=True
 for y in [.8,2.6]:box('Wall outlet',(x/abs(x)*2.296,y,.36),(.02,.09,.14),black,.005)
# Minimal provisional rear half; keep unseen furniture out of the factual reconstruction.
box('Ceiling fixture',(0,-.4,3.055),(.6,1.1,.09),white,.04)
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.world.color=(.38,.40,.45)
def area(name,loc,energy,size,color,target):
 bpy.ops.object.light_add(type='AREA',location=loc);l=bpy.context.object;l.name=name;l.data.energy=energy;l.data.shape='DISK';l.data.size=size;l.data.color=color;l.rotation_euler=(Vector(target)-l.location).to_track_quat('-Z','Y').to_euler()
area('Window daylight',(0,2.65,2.25),180,2.0,(.86,.92,1),(0,0,1));area('Warm ceiling fill',(0,-.8,2.95),120,3,(1,.94,.80),(0,1,0))
bpy.ops.object.camera_add(location=(0,-2.62,1.66));camera=bpy.context.object;camera.rotation_euler=(Vector((0,2.1,1.25))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=23;scene.camera=camera
scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.filepath=str(art/'Wesley-room-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(art/'WesleyRoom.blend'))
# Export evaluated meshes with explicit Blender Z-up to Unity Y-up conversion.
verts=[];groups=[[] for m in materials];names=list(materials);deps=bpy.context.evaluated_depsgraph_get()
for obj in scene.objects:
 if obj.type!='MESH' or obj.hide_render:continue
 evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles();normals=obj.matrix_world.to_3x3().inverted().transposed()
 for tri in mesh.loop_triangles:
  group=groups[names.index(mesh.materials[tri.material_index].name)];idx=[]
  for li in tri.loops:
   loop=mesh.loops[li];v=obj.matrix_world@mesh.vertices[loop.vertex_index].co;n=(normals@mesh.corner_normals[li].vector).normalized();idx.append(len(verts));verts.append((v.x,v.z,v.y,n.x,n.z,n.y))
  group.extend(reversed(idx))
 evaluated.to_mesh_clear()
with (res/'wesley-mesh.bytes').open('wb') as f:
 f.write(b'AOT1');f.write(struct.pack('<ii',len(verts),len(groups)))
 for v in verts:f.write(struct.pack('<6f',*v))
 for g in groups:f.write(struct.pack('<i',len(g)));f.write(struct.pack('<'+'i'*len(g),*g))
(res/'wesley-model.json').write_text(json.dumps({'materials':[{'name':m.name,'color':list(m.diffuse_color)} for m in materials.values()],'colliders':colliders,'note':'Reference-based room study; room dimensions and rear surfaces provisional.'},indent=2))
bpy.ops.object.select_all(action='DESELECT')
for o in scene.objects:
 if o.type=='MESH' and not o.hide_render:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(art/'WesleyRoom.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False)
print('WESLEY_MODEL_OK',len(verts),'vertices',len(colliders),'collision boxes')
bpy.ops.render.render(write_still=True)
