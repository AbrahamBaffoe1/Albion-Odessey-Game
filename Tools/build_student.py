import bpy,bmesh,math,shutil,json
from pathlib import Path
from mathutils import Vector
root=Path(__file__).resolve().parents[1];refs=root/'Art/References';res=root/'Unity/Assets/Resources/CampusCraft';out=res/'Student';out.mkdir(exist_ok=True)
src=refs/'CharacterSource/Universal Base Characters[Standard]';base=src/'Base Characters/Godot - UE'
# The supplied glTF uses two texture aliases missing from the archive.
for a,b in [('T_Hair_1_Normal.png','T_Hair_1_Normal_png.png'),('T_Eye_Normal.png','T_Eye_Normal_png.png')]:shutil.copy2(base/a,base/b)
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.context.scene.render.fps=30
bpy.ops.import_scene.gltf(filepath=str(base/'Superhero_Male_FullBody.gltf'))
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE');body=bpy.data.objects['SuperHero_Male']
for o in list(bpy.context.scene.objects):
 if o.name=='Icosphere':bpy.data.objects.remove(o,do_unlink=True)
# Cloth retains the artist's skin weights, has its own surface and sewn thickness.
def cloth(name,planes,color,thickness):
 o=body.copy();o.data=body.data.copy();bpy.context.collection.objects.link(o);o.name=name
 bm=bmesh.new();bm.from_mesh(o.data)
 for co,no in planes:bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=co,plane_no=no,clear_outer=True,dist=.00001)
 bm.to_mesh(o.data);bm.free()
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;shader=m.node_tree.nodes['Principled BSDF'];shader.inputs['Base Color'].default_value=(*color,1);shader.inputs['Roughness'].default_value=.86
 # Fine woven bump in the Blender source. Runtime uses the imported cloth material.
 tex=m.node_tree.nodes.new('ShaderNodeTexNoise');tex.inputs['Scale'].default_value=180;bump=m.node_tree.nodes.new('ShaderNodeBump');bump.inputs['Strength'].default_value=.15;bump.inputs['Distance'].default_value=.002;m.node_tree.links.new(tex.outputs['Fac'],bump.inputs['Height']);m.node_tree.links.new(bump.outputs['Normal'],shader.inputs['Normal'])
 o.data.materials.clear();o.data.materials.append(m)
 for f in o.data.polygons:f.material_index=0;f.use_smooth=True
 mod=o.modifiers.new('Fabric thickness','SOLIDIFY');mod.thickness=thickness;mod.offset=1
 return o
shirt=cloth('Campus sweatshirt',[((0,0,.99),(0,0,-1)),((0,0,1.572),(0,0,1)),((.69,0,0),(1,0,0)),((-.69,0,0),(-1,0,0))],(.22,.12,.33),.023)
pants=cloth('Indigo denim',[((0,0,.16),(0,0,-1)),((0,0,1.04),(0,0,1))],(.09,.13,.19),.016)
# Remove skin feet where fully enclosed by modeled shoes.
bm=bmesh.new();bm.from_mesh(body.data);bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),plane_co=(0,0,.12),plane_no=(0,0,-1),clear_outer=True);bm.to_mesh(body.data);bm.free()
# Fit a short hairstyle from the same character kit, binding it to the head.
hairpath=next((src/'Hairstyles/Origin at 0').rglob('Hair_SimpleParted.gltf'))
before=set(bpy.context.scene.objects);bpy.ops.import_scene.gltf(filepath=str(hairpath))
for o in set(bpy.context.scene.objects)-before:
 if o.type!='MESH':continue
 o.name='Student hair';o.parent=rig;o.matrix_parent_inverse=rig.matrix_world.inverted()
 m=bpy.data.materials.new('Student hair dark');m.use_nodes=True;m.diffuse_color=(.045,.025,.016,1);m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=m.diffuse_color;m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.72;o.data.materials.clear();o.data.materials.append(m)
 vg=o.vertex_groups.new(name='Head');vg.add(list(range(len(o.data.vertices))),1,'REPLACE');mod=o.modifiers.new('Head skinning','ARMATURE');mod.object=rig
# Backpack with beveled corners, straps and pocket; bound to upper spine.
def packpart(name,loc,dim,color,bone='spine_03'):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=dim;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 m=bpy.data.materials.get(name+' fabric')
 if m is None:
  m=bpy.data.materials.new(name+' fabric');m.diffuse_color=(*color,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=m.diffuse_color;m.node_tree.nodes['Principled BSDF'].inputs['Roughness'].default_value=.8
 o.data.materials.append(m);mod=o.modifiers.new('Soft fabric edges','BEVEL');mod.width=.04;mod.segments=3
 bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=mod.name)
 # Bake local coordinates so armature modifier weights use the same rest space.
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True);o.parent=rig
 vg=o.vertex_groups.new(name=bone);vg.add(list(range(len(o.data.vertices))),1,'REPLACE');mod=o.modifiers.new('Backpack skinning','ARMATURE');mod.object=rig
packpart('Backpack',(0,.22,1.29),(.34,.19,.44),(.24,.28,.23))
packpart('Backpack pocket',(0,.33,1.2),(.26,.075,.20),(.24,.28,.23))
for x in [-.14,.14]:packpart('Backpack strap',(x,-.115,1.3),(.032,.028,.34),(.24,.28,.23))
def sneaker(x,side):
 vs=[];faces=[]
 for y,w,h in [(-.24,.04,.065),(-.20,.075,.10),(-.10,.083,.125),(0,.074,.165),(.10,.066,.16),(.14,.05,.12)]:
  for i in range(12):
   a=math.tau*i/12;vs.append((x+w*math.cos(a),y,.044+(h-.044)*(.5+.5*math.sin(a))))
 for ring in range(5):
  for i in range(12):faces.append((ring*12+i,ring*12+(i+1)%12,(ring+1)*12+(i+1)%12,(ring+1)*12+i))
 faces.extend([tuple(reversed(range(12))),tuple(range(60,72))])
 me=bpy.data.meshes.new('Sneaker stitched upper');me.from_pydata(vs,[],faces);me.update();o=bpy.data.objects.new('Canvas sneakers',me);bpy.context.collection.objects.link(o);o.parent=rig
 m=bpy.data.materials.new('Sneaker canvas');m.diffuse_color=(.19,.21,.23,1);m.use_nodes=True;m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=m.diffuse_color;me.materials.append(m)
 for f in me.polygons:f.use_smooth=True
 vg=o.vertex_groups.new(name='foot_'+side);vg.add(list(range(len(vs))),1,'REPLACE');mod=o.modifiers.new('Shoe skinning','ARMATURE');mod.object=rig
for x,side in [(.114,'l'),(-.114,'r')]:
 sneaker(x,side)
 packpart('Sneaker rubber sole',(x,-.045,.035),(.18,.39,.07),(.76,.75,.70),'foot_'+side)
for o in bpy.context.scene.objects:
 if o.name=='Eyebrows':
  m=bpy.data.materials.new('Student eyebrows');m.use_nodes=True;m.diffuse_color=(.045,.025,.016,1);m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=m.diffuse_color;o.data.materials.clear();o.data.materials.append(m)
# Import the CC0 authored animation library and bind its compatible actions to this rig.
before=set(bpy.context.scene.objects)
bpy.ops.import_scene.gltf(filepath=str(next((refs/'AnimationSource').rglob('UAL1_Standard.glb'))))
for o in list(set(bpy.context.scene.objects)-before):bpy.data.objects.remove(o,do_unlink=True)
keep=['Idle_Loop','Walk_Loop','Jog_Fwd_Loop','Sprint_Loop','Driving_Loop','Jump_Loop','Interact']
for a in list(bpy.data.actions):
 if a.name not in keep:bpy.data.actions.remove(a)
rig.animation_data_create();rig.animation_data.action=bpy.data.actions['Idle_Loop']
if len(rig.animation_data.action.slots):rig.animation_data.action_slot=rig.animation_data.action.slots[0]
for a in bpy.data.actions:a.use_fake_user=True
# Preserve texture files with exact names and point Blender at a portable directory.
for img in bpy.data.images:
 if img.source=='FILE' and img.size[0]>0:
  target=out/(img.name if img.name.lower().endswith(('.png','.jpg')) else img.name+'.png')
  try:img.save(filepath=str(target));img.filepath=str(target)
  except Exception:pass
scene=bpy.context.scene;scene.frame_set(0)
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects:
 if o.type in {'MESH','ARMATURE'}:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(out/'Student.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.2,path_mode='STRIP')
# Source file and a lit portrait for checking proportions and clothing.
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.world.color=(.3,.34,.4)
for pos,power,size in [((2,-4,4),350,4),((-3,-1,2),160,3),((0,3,3),300,2)]:
 bpy.ops.object.light_add(type='AREA',location=pos);l=bpy.context.object;l.data.energy=power;l.data.size=size;l.rotation_euler=(Vector((0,0,1))-l.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(2.5,-4.5,2));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=70;scene.camera=cam
scene.render.resolution_x=850;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.filepath=str(root/'Art/Student-review.png')
bpy.ops.file.pack_all();bpy.ops.wm.save_as_mainfile(filepath=str(root/'Art/StudentRig.blend'));bpy.ops.render.render(write_still=True)
print('STUDENT_MODEL_OK',len(rig.data.bones),'bones',len(bpy.data.actions),'authored clips')
