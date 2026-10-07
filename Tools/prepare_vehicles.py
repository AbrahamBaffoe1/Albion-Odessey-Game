"""Adapt the CC-BY Car Concept in Blender into three runtime LODs.
Source attribution: Art/Vehicles/Source/LICENSE.md and Art/Vehicles/CREDITS.md.
Run with Blender --background --python Tools/prepare_vehicles.py.
"""
import bpy, math, json, shutil
from pathlib import Path
from mathutils import Vector, Matrix
root=Path(__file__).resolve().parents[1]
out=root/'Unity/Assets/Resources/Vehicles';out.mkdir(parents=True,exist_ok=True)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.gltf(filepath=str(root/'Art/Vehicles/Source/glTF/CarConcept.gltf'))
bpy.context.view_layer.update()
# Apply the imported nested transforms before creating clean gameplay pivots.
objects=[o for o in bpy.data.objects if o.type=='MESH']
for o in objects:
 w=o.matrix_world.copy();o.parent=None;o.matrix_world=w
bpy.context.view_layer.update()
for o in objects:
 o.data=o.data.copy();o.data.transform(o.matrix_world);o.matrix_world=Matrix.Identity(4)
# Source forward is -Y in Blender. Retain that forward in the FBX export.
# Fit the campus's existing 4.4m collision footprint; mirrors stay within 2.2m.
points=[v.co for o in objects for v in o.data.vertices]
lo=Vector(tuple(min(v[i] for v in points) for i in range(3)));hi=Vector(tuple(max(v[i] for v in points) for i in range(3)))
scale=Vector((2.10/(hi.x-lo.x),4.28/(hi.y-lo.y),1.40/(hi.z-lo.z)))
for o in objects:
 for v in o.data.vertices:v.co=Vector(((v.co.x-(lo.x+hi.x)/2)*scale.x,(v.co.y-(lo.y+hi.y)/2)*scale.y,(v.co.z-lo.z)*scale.z))
# Remove third-party marks in the distributed adaptation; keep source credits.
for o in list(objects):
 if o.name=='License Plate' or 'Emblem' in o.name:
  objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
colors={'Paint':(.30,.012,.016,1),'Accent':(.05,.065,.08,1),'Trim':(.025,.028,.032,1),'Rubber':(.012,.014,.016,1),'Alloy':(.45,.48,.52,1),'Glass':(.12,.20,.25,.34),'Seat':(.045,.035,.03,1),'Headlamp':(.85,.94,1,1),'Taillamp':(.65,.006,.003,1),'Brake':(.42,.025,.008,1),'Screen':(.02,.11,.13,1)}
mats={}
for name,color in colors.items():
 m=bpy.data.materials.new('Vehicle '+name);m.diffuse_color=color;m.use_nodes=True;p=m.node_tree.nodes.get('Principled BSDF');p.inputs['Base Color'].default_value=color;p.inputs['Roughness'].default_value=.22 if name in ('Paint','Alloy','Glass') else .65
 p.inputs['Metallic'].default_value=.7 if name in ('Paint','Alloy','Accent') else 0
 if name=='Glass':p.inputs['Transmission Weight'].default_value=.4;p.inputs['Alpha'].default_value=.45
 if name in ('Headlamp','Taillamp'):p.inputs['Emission Color'].default_value=color;p.inputs['Emission Strength'].default_value=.3
 mats[name]=m

def kind(name):
 if name.startswith('Paint 1'):return 'Paint'
 if name.startswith('Paint 2'):return 'Accent'
 if name in ('Glass',):return 'Glass'
 if name.startswith('Tire'):return 'Rubber'
 if name.startswith(('Rim','Disc')) or name in ('Hardware','Mirror'):return 'Alloy'
 if name=='Brake':return 'Brake'
 if name=='Headlight':return 'Headlamp'
 if name in ('Brakelight','Signallight'):return 'Taillamp'
 if name.startswith('Interior'):return 'Seat'
 if name=='Dashboard':return 'Screen'
 return 'Trim'
for o in objects:
 for i,m in enumerate(o.data.materials):o.data.materials[i]=mats[kind(m.name)]

# Identify wheel groups from actual tire geometry, including mesh nodes with generic names.
centers={}
for token in ('FrontL','FrontR','RearL','RearR'):
 rim=next(o for o in objects if o.name=='Wheel'+token+'Rim')
 coords=[v.co for v in rim.data.vertices];centers[token]=Vector(tuple((min(v[i] for v in coords)+max(v[i] for v in coords))/2 for i in range(3)))
groups={token:[] for token in centers};groups['Body']=[]
for o in objects:
 token=next((t for t in centers if o.name.startswith('Wheel'+t)),None)
 if token is None and all(m.name=='Vehicle Rubber' for m in o.data.materials):
  c=sum((v.co for v in o.data.vertices),Vector())/len(o.data.vertices);token=min(centers,key=lambda t:(c-centers[t]).length)
 groups[token or 'Body'].append(o)
steering_parts=[o for o in groups['Body'] if 'InteriorSteeringWheel' in o.name]
for o in steering_parts:groups['Body'].remove(o)
groups['CabinSteering']=steering_parts
coords=[v.co for o in steering_parts for v in o.data.vertices]
centers['CabinSteering']=Vector(tuple((min(v[i] for v in coords)+max(v[i] for v in coords))/2 for i in range(3)))
# Strip import containers after the mesh transforms have been baked.
for o in list(bpy.data.objects):
 if o.type!='MESH':bpy.data.objects.remove(o,do_unlink=True)
exports=[];stats=[]
for level,ratio in enumerate((.48,.13,.028)):
 lod=bpy.data.objects.new('LOD'+str(level),None);bpy.context.collection.objects.link(lod);exports.append(lod)
 count=0
 for group,items in groups.items():
  anchor=Vector() if group=='Body' else centers[group]
  steer=bpy.data.objects.new(('Body' if group=='Body' else 'CabinSteering' if group=='CabinSteering' else 'Steer'+group)+'_L'+str(level),None);bpy.context.collection.objects.link(steer);steer.parent=lod;steer.location=anchor;exports.append(steer)
  spin=steer
  if group not in ('Body','CabinSteering'):
   spin=bpy.data.objects.new('Wheel'+group+'_L'+str(level),None);bpy.context.collection.objects.link(spin);spin.parent=steer;exports.append(spin)
  # Combine by material/group so all 24 parked cars do not each issue 100 draws.
  byparent={}
  for source in items:
   copy=source.copy();copy.data=source.data.copy();bpy.context.collection.objects.link(copy)
   bpy.context.view_layer.objects.active=copy
   for selected in bpy.context.selected_objects:selected.select_set(False)
   copy.select_set(True)
   effective=ratio
   if 'Wipers' in source.name:effective=min(ratio,.045)
   if len(copy.data.polygons)>160:
    dec=copy.modifiers.new('Runtime polygon budget','DECIMATE');dec.ratio=effective;bpy.ops.object.modifier_apply(modifier=dec.name)
   parent=steer if 'BrakePad' in source.name else spin
   byparent.setdefault(parent,[]).append(copy)
  for parent,copies in byparent.items():
   bpy.ops.object.select_all(action='DESELECT')
   for c in copies:c.select_set(True)
   bpy.context.view_layer.objects.active=copies[0];bpy.ops.object.join();joined=copies[0]
   joined.name=group+('_Caliper' if parent==steer and group!='Body' else '_Geometry')+'_L'+str(level)
   joined.data.transform(Matrix.Translation(-anchor));joined.parent=parent;joined.location=Vector();exports.append(joined)
   joined.data.calc_loop_triangles();count+=len(joined.data.loop_triangles)
 stats.append(count)
for o in objects:bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.object.select_all(action='DESELECT')
for o in exports:o.select_set(True)
bpy.context.view_layer.objects.active=exports[0]
bpy.ops.export_scene.fbx(filepath=str(out/'CampusCoupe.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',bake_space_transform=False,add_leaf_bones=False,bake_anim=False,path_mode='AUTO')
# Canonical wheel metadata is in Unity coordinates (+Z forward).
manifest={'triangles':stats,'size':[2.1,1.4,4.28],'wheels':[{'name':t,'position':[c.x,c.z,-c.y]} for t,c in centers.items() if t!='CabinSteering']}
(out/'vehicle-metadata.json').write_text(json.dumps(manifest,indent=2)+'\n')
# The editable .blend includes all LODs, with lower ones hidden from rendering.
for o in exports:
 if o.name.startswith('LOD') and o.name!='LOD0':
  for child in o.children_recursive:child.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=str(root/'Art/Vehicles/CampusCoupe.blend'))
# A neutral contact sheet for checking the silhouette before game integration.
bpy.ops.mesh.primitive_plane_add(size=200);ground=bpy.context.object;ground.name='Review ground';ground.data.materials.append(mats['Trim'])
for pos,power,size in [((3,-4,6),1100,5),((-4,-1,3),900,4),((0,4,5),1400,4)]:
 data=bpy.data.lights.new('Vehicle review softbox','AREA');data.energy=power;data.shape='DISK';data.size=size;o=bpy.data.objects.new(data.name,data);bpy.context.collection.objects.link(o);o.location=pos;o.rotation_euler=(Vector((0,0,.6))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(5,-7,3.1));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,.7))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=55;bpy.context.scene.camera=cam
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True;scene.render.resolution_x=1200;scene.render.resolution_y=760;scene.render.resolution_percentage=100;scene.world.color=(.18,.18,.18);scene.render.filepath=str(root/'Art/Vehicles/Coupe-review.png');bpy.ops.render.render(write_still=True)
print('VEHICLE_EXPORT_OK',json.dumps(manifest))
