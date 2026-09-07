"""Run in background Blender to verify that the actual FBXs carry mesh/collision data."""
import bpy
import json
from pathlib import Path
from mathutils import Vector

folder=Path(__file__).resolve().parents[1]/"Art"/"Architecture"
manifest=json.loads((folder/"architecture_manifest.json").read_text())
bpy.context.scene.unit_settings.system="METRIC"
bpy.context.scene.unit_settings.scale_length=1.0
for asset in manifest["assets"]:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=str(folder/asset["file"]),use_anim=False)
    render=[o for o in bpy.context.scene.objects if o.type=="MESH" and not o.name.startswith("UCX_")]
    hulls=[o for o in bpy.context.scene.objects if o.name.startswith("UCX_")]
    assert len(render)==1 and render[0].name==asset["name"],f"Unexpected render meshes: {asset['name']}"
    assert len(hulls)==asset["collision_hulls"],f"Collision hulls missing: {asset['name']}"
    for i,record in enumerate(asset["collision_boxes"]):
        h=bpy.data.objects[f"UCX_{asset['name']}_{i:03d}"]
        points=[h.matrix_world@Vector(v) for v in h.bound_box]
        lo=Vector([min(v[k] for v in points) for k in range(3)])
        hi=Vector([max(v[k] for v in points) for k in range(3)])
        assert ((lo+hi)/2-Vector(record["center"])).length<.002,f"Unit/origin mismatch in {h.name}"
        assert ((hi-lo)-Vector(record["size"])).length<.002,f"Scale mismatch in {h.name}"
    assert render[0].data.uv_layers, "Missing texture coordinates"
    print("FBX_ROUNDTRIP_OK",asset["name"],len(hulls),flush=True)
print(f"All {len(manifest['assets'])} exported geometry/collision assets round-tripped at meter scale.")
