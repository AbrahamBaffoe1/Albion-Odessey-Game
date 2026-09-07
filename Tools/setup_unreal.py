"""Run once in Unreal Editor: Tools > Execute Python Script.
Generates the material, imports the Blender kit, and creates the playable map.
"""
from pathlib import Path
import unreal

root=Path(unreal.Paths.project_dir())
destination="/Game/Generated"
unreal.EditorAssetLibrary.make_directory(destination)
tools=unreal.AssetToolsHelpers.get_asset_tools()
mat_path=destination+"/M_Odyssey"
mat=unreal.EditorAssetLibrary.load_asset(mat_path)
if not mat:
    mat=tools.create_asset("M_Odyssey",destination,unreal.Material,unreal.MaterialFactoryNew())
    node=unreal.MaterialEditingLibrary.create_material_expression(mat,unreal.MaterialExpressionVectorParameter,-300,0)
    node.set_editor_property("parameter_name","Tint")
    node.set_editor_property("default_value",unreal.LinearColor(.5,.3,.7,1))
    unreal.MaterialEditingLibrary.connect_material_property(node,"",unreal.MaterialProperty.MP_BASE_COLOR)
    unreal.MaterialEditingLibrary.recompile_material(mat)
    unreal.EditorAssetLibrary.save_loaded_asset(mat)

for fbx in sorted((root/"Art").glob("SM_*.fbx")):
    task=unreal.AssetImportTask()
    task.filename=str(fbx)
    task.destination_path=destination
    task.destination_name=fbx.stem
    task.automated=True
    task.replace_existing=True
    task.save=True
    options=unreal.FbxImportUI()
    options.import_mesh=True
    options.import_as_skeletal=False
    options.import_materials=True
    options.import_textures=False
    options.mesh_type_to_import=unreal.FBXImportType.FBXIT_STATIC_MESH
    options.static_mesh_import_data.combine_meshes=True
    options.static_mesh_import_data.auto_generate_collision=True
    task.options=options
    tools.import_asset_tasks([task])
    if not task.imported_object_paths:
        raise RuntimeError(f"Import failed: {fbx.name}")

levels=unreal.get_editor_subsystem(unreal.LevelEditorSubsystem)
map_path="/Game/Maps/LegacyCampus"
if not unreal.EditorAssetLibrary.does_asset_exist(map_path):
    unreal.EditorAssetLibrary.make_directory("/Game/Maps")
    if not levels.new_level(map_path):
        raise RuntimeError("Could not create LegacyCampus map")
else:
    levels.load_level(map_path)
world=unreal.get_editor_subsystem(unreal.UnrealEditorSubsystem).get_editor_world()
world.get_world_settings().set_editor_property("default_game_mode",unreal.load_class(None,"/Script/AlbionOdyssey.OdysseyGameMode"))
if not levels.save_current_level():
    raise RuntimeError("Could not save LegacyCampus map")
unreal.log("Albion Odyssey is ready for Play. See README for controls.")
