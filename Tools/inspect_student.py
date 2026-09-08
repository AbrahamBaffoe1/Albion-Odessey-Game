import bpy
from pathlib import Path
p=Path(__file__).resolve().parents[1]/'Art/References/CharacterSource/Universal Base Characters[Standard]/Base Characters/Godot - UE/Superhero_Male_FullBody.gltf'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False);bpy.ops.import_scene.gltf(filepath=str(p))
for o in bpy.context.scene.objects:
 print('OBJECT',o.name,o.type,tuple(o.dimensions))
 if o.type=='ARMATURE':print('BONES',[(b.name,tuple(b.head_local)) for b in o.data.bones])
 if o.type=='MESH':print('MATS',[(m.name) for m in o.data.materials])
