"""Execute inside Unreal Editor after building the native module.
Imports full-scale Blender meshes with authored UCX collision, reconstructs PBR
materials, and builds a first-person map with a real-size player start.
Only actors tagged OdysseyArchitectureGenerated are replaced on subsequent runs.
"""
import json
from pathlib import Path
import unreal

root=Path(unreal.Paths.project_dir())
source=root/"Art"/"Architecture"
manifest=json.loads((source/"architecture_manifest.json").read_text())
destination="/Game/Architecture"
tag="OdysseyArchitectureGenerated"
unreal.EditorAssetLibrary.make_directory(destination)
tools=unreal.AssetToolsHelpers.get_asset_tools()
editor=unreal.get_editor_subsystem(unreal.EditorActorSubsystem)
levels=unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
library=unreal.MaterialEditingLibrary

def import_file(path,options=None):
    task=unreal.AssetImportTask()
    task.filename=str(path); task.destination_path=destination; task.destination_name=path.stem
    task.automated=True; task.replace_existing=True; task.save=True
    if options:
        task.options=options
        task.factory=unreal.FbxFactory()
    tools.import_asset_tasks([task])
    if not task.imported_object_paths: raise RuntimeError(f"Import returned no assets: {path}")
    return unreal.EditorAssetLibrary.load_asset(destination+"/"+path.stem)

textures={}
for suffix in ["BaseColor","Normal","Roughness"]:
    texture=import_file(source/f"T_Architecture_Brick_{suffix}.png")
    if suffix=="Normal":
        texture.set_editor_property("compression_settings",unreal.TextureCompressionSettings.TC_NORMALMAP)
        texture.set_editor_property("srgb",False)
        texture.set_editor_property("flip_green_channel",True)
    elif suffix=="Roughness": texture.set_editor_property("srgb",False)
    unreal.EditorAssetLibrary.save_loaded_asset(texture)
    textures[suffix]=texture

palette={
    "Brick":((.36,.115,.055),.9,0), "Stone":((.62,.61,.55),.8,0),
    "Floor":((.30,.32,.33),.78,0), "Plaster":((.78,.77,.70),.88,0),
    "Metal":((.035,.046,.055),.3,.7), "Glass":((.10,.22,.27),.16,.25),
    "Wood":((.27,.12,.045),.55,0), "Grass":((.16,.21,.095),.95,0),
    "Asphalt":((.045,.05,.055),.94,0), "Accent":((.30,.14,.44),.5,0),
    "Memory":((1,.55,.07),.2,.25)
}
materials={}
for kind,(color,rough,metallic) in palette.items():
    name="M_Architecture_"+kind
    material=unreal.EditorAssetLibrary.load_asset(destination+"/"+name)
    if not material: material=tools.create_asset(name,destination,unreal.Material,unreal.MaterialFactoryNew())
    library.delete_all_material_expressions(material)
    tint=library.create_material_expression(material,unreal.MaterialExpressionConstant3Vector,-400,0)
    tint.set_editor_property("constant",unreal.LinearColor(*color,1))
    library.connect_material_property(tint,"",unreal.MaterialProperty.MP_BASE_COLOR)
    for value,prop,y in [(rough,unreal.MaterialProperty.MP_ROUGHNESS,150),(metallic,unreal.MaterialProperty.MP_METALLIC,250)]:
        node=library.create_material_expression(material,unreal.MaterialExpressionConstant,-400,y)
        node.set_editor_property("r",value)
        library.connect_material_property(node,"",prop)
    if kind=="Brick":
        for i,(suffix,prop) in enumerate([("BaseColor",unreal.MaterialProperty.MP_BASE_COLOR),("Normal",unreal.MaterialProperty.MP_NORMAL),("Roughness",unreal.MaterialProperty.MP_ROUGHNESS)]):
            node=library.create_material_expression(material,unreal.MaterialExpressionTextureSample,-650,i*220)
            node.set_editor_property("texture",textures[suffix])
            if suffix=="Normal": node.set_editor_property("sampler_type",unreal.MaterialSamplerType.SAMPLERTYPE_NORMAL)
            elif suffix=="Roughness": node.set_editor_property("sampler_type",unreal.MaterialSamplerType.SAMPLERTYPE_LINEAR_COLOR)
            library.connect_material_property(node,"RGB",prop)
    if kind=="Glass":
        material.set_editor_property("blend_mode",unreal.BlendMode.BLEND_TRANSLUCENT)
        material.set_editor_property("two_sided",True)
        opacity=library.create_material_expression(material,unreal.MaterialExpressionConstant,-400,350)
        opacity.set_editor_property("r",.30)
        library.connect_material_property(opacity,"",unreal.MaterialProperty.MP_OPACITY)
    if kind=="Memory": library.connect_material_property(tint,"",unreal.MaterialProperty.MP_EMISSIVE_COLOR)
    library.recompile_material(material)
    unreal.EditorAssetLibrary.save_loaded_asset(material)
    materials[name]=material

meshes=[]
mesh_editor=unreal.get_editor_subsystem(unreal.StaticMeshEditorSubsystem)
for entry in manifest["assets"]:
    options=unreal.FbxImportUI()
    options.import_mesh=True; options.import_as_skeletal=False; options.import_materials=False; options.import_textures=False
    options.automated_import_should_detect_type=False
    options.mesh_type_to_import=unreal.FBXImportType.FBXIT_STATIC_MESH
    options.static_mesh_import_data.combine_meshes=True
    options.static_mesh_import_data.auto_generate_collision=False
    options.static_mesh_import_data.one_convex_hull_per_ucx=True
    options.static_mesh_import_data.convert_scene=True
    options.static_mesh_import_data.convert_scene_unit=True
    options.static_mesh_import_data.generate_lightmap_u_vs=False
    mesh=import_file(source/entry["file"],options)
    if not isinstance(mesh,unreal.StaticMesh): raise RuntimeError(f"Not a static mesh: {entry['name']}")
    for i,slot in enumerate(mesh.get_editor_property("static_materials")):
        slot_name=str(slot.get_editor_property("imported_material_slot_name"))
        if slot_name in materials: mesh.set_material(i,materials[slot_name])
        else: raise RuntimeError(f"Unmapped material slot {slot_name} in {entry['name']}")
    # Check a missing UCX import now, instead of accepting a tower-shaped solid hull.
    actual_hulls=mesh_editor.get_convex_collision_count(mesh)
    if actual_hulls!=entry["collision_hulls"]:
        raise RuntimeError(f"Collision count mismatch for {entry['name']}: expected {entry['collision_hulls']}, got {actual_hulls}")
    unreal.EditorAssetLibrary.save_loaded_asset(mesh)
    meshes.append(mesh)

path="/Game/Maps/CampusWalkthrough"
if unreal.EditorAssetLibrary.does_asset_exist(path): levels.load_level(path)
else:
    unreal.EditorAssetLibrary.make_directory("/Game/Maps")
    if not levels.new_level(path): raise RuntimeError("Cannot create CampusWalkthrough")
for actor in editor.get_all_level_actors():
    if tag in [str(t) for t in actor.tags]: editor.destroy_actor(actor)

def spawn(cls,position,label,rotation=None):
    actor=editor.spawn_actor_from_class(cls,unreal.Vector(*position),rotation or unreal.Rotator())
    actor.set_actor_label(label); actor.tags=[unreal.Name(tag)]
    return actor

for mesh in meshes:
    actor=spawn(unreal.StaticMeshActor,(0,0,0),mesh.get_name())
    component=actor.static_mesh_component
    component.set_static_mesh(mesh)
    component.set_collision_profile_name("BlockAll")

sun=spawn(unreal.DirectionalLight,(0,-2500,6000),"Campus sun",unreal.Rotator(-40,-35,0))
sun.light_component.set_editor_property("intensity",4.0)
sun.light_component.set_editor_property("mobility",unreal.ComponentMobility.MOVABLE)
sky=spawn(unreal.SkyLight,(0,0,4000),"Campus skylight")
sky.light_component.set_editor_property("mobility",unreal.ComponentMobility.MOVABLE)
sky.light_component.set_editor_property("intensity",1.0)
spawn(unreal.SkyAtmosphere,(0,0,0),"Campus sky")
for floor in range(8):
    for y in [-500,300]:
        lamp=spawn(unreal.PointLight,(0,y,floor*360+270),f"Floor {floor+1} hall light")
        lamp.light_component.set_editor_property("mobility",unreal.ComponentMobility.MOVABLE)
        lamp.light_component.set_editor_property("intensity",3500.0)
        lamp.light_component.set_editor_property("attenuation_radius",1200.0)
        lamp.light_component.set_editor_property("cast_shadows",False)
spawn(unreal.PlayerStart,(0,-2200,100),"Player start - approach the entrance",unreal.Rotator(0,90,0))
world=unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()
world.get_world_settings().set_editor_property("default_game_mode",unreal.load_class(None,"/Script/AlbionOdyssey.OdysseyWalkMode"))
if not levels.save_current_level(): raise RuntimeError("Could not save walkthrough map")
unreal.log("Full-size Legacy Hall imported. Press Play: WASD/mouse to walk, E collect, F2 builder. Verify entrance/stair collision in Play before declaring the map complete.")
