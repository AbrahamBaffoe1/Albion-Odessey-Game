"""Export the actual evaluated Blender tower geometry into Unity mesh buffers.
Run in Blender with LegacyHall_FullScale.blend open. Explicit meter-scale axis
conversion makes mesh positions and authored colliders use the same coordinates.
"""
import bpy
import json
import shutil
import struct
from pathlib import Path

root=Path(__file__).resolve().parents[1]
source=root/"Art"/"Architecture"
dest=root/"Unity"/"Assets"/"Resources"/"Architecture"
dest.mkdir(parents=True,exist_ok=True)
manifest=json.loads((source/"architecture_manifest.json").read_text())
metadata=[]
deps=bpy.context.evaluated_depsgraph_get()
for entry in manifest["assets"]:
    obj=bpy.data.objects[entry["name"]]
    evaluated=obj.evaluated_get(deps)
    mesh=evaluated.to_mesh()
    mesh.calc_loop_triangles()
    matrix=obj.matrix_world
    normals=matrix.to_3x3().inverted().transposed()
    uv=mesh.uv_layers.active.data
    vertices=[]
    groups=[[] for _ in mesh.materials]
    for tri in mesh.loop_triangles:
        indices=[]
        for li in tri.loops:
            loop=mesh.loops[li]
            v=matrix@mesh.vertices[loop.vertex_index].co
            normal=(normals@mesh.corner_normals[li].vector).normalized()
            tex=uv[li].uv
            indices.append(len(vertices))
            vertices.append((v.x,v.z,v.y,normal.x,normal.z,normal.y,tex.x,tex.y))
        groups[tri.material_index].extend(reversed(indices))
    with (dest/(entry["name"]+".bytes")).open("wb") as f:
        f.write(b"AOM1")
        f.write(struct.pack("<ii",len(vertices),len(groups)))
        for v in vertices: f.write(struct.pack("<8f",*v))
        for group in groups:
            f.write(struct.pack("<i",len(group)))
            f.write(struct.pack("<"+"i"*len(group),*group))
    metadata.append({"name":entry["name"],"materials":[m.name for m in mesh.materials],"vertices":len(vertices)})
    evaluated.to_mesh_clear()
for filename in ["architecture_manifest.json","T_Architecture_Brick_BaseColor.png","T_Architecture_Brick_Normal.png","T_Architecture_Brick_Roughness.png"]:
    shutil.copy2(source/filename,dest/filename)
(dest/"mesh_catalog.json").write_text(json.dumps({"assets":metadata},indent=2))
print("UNITY_GEOMETRY_EXPORT_OK",len(metadata),sum(e["vertices"] for e in metadata))
