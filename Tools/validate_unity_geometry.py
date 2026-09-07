"""Validate the actual Blender-to-Unity buffers without requiring a Unity license."""
import json
import math
import struct
from pathlib import Path

root = Path(__file__).resolve().parents[1]
folder = root / "Unity/Assets/Resources/Architecture"
catalog = json.loads((folder / "mesh_catalog.json").read_text())["assets"]
manifest = json.loads((folder / "architecture_manifest.json").read_text())
source = json.loads((root / "Art/Architecture/architecture_manifest.json").read_text())
assert manifest == source, "Unity collision export is stale"
assert len(catalog) == len(manifest["assets"]) == 10
triangles = 0
for entry, original in zip(catalog, manifest["assets"]):
    assert entry["name"] == original["name"]
    data = (folder / (entry["name"] + ".bytes")).read_bytes()
    assert data[:4] == b"AOM1"
    count, submeshes = struct.unpack_from("<ii", data, 4)
    assert count == entry["vertices"] and submeshes == len(entry["materials"])
    vertices = list(struct.iter_unpack("<8f", data[12:12 + count * 32]))
    assert len(vertices) == count
    for vertex in vertices:
        assert all(math.isfinite(v) for v in vertex)
        assert abs(sum(v*v for v in vertex[3:6])-1) < .002, "Non-unit normal"
    offset = 12 + count * 32
    indices = []
    for _ in range(submeshes):
        n, = struct.unpack_from("<i", data, offset)
        offset += 4
        assert n >= 0 and n % 3 == 0
        group = struct.unpack_from("<" + "i" * n, data, offset)
        offset += n * 4
        assert all(0 <= i < count for i in group)
        indices.extend(group)
    assert offset == len(data), "Trailing mesh data"
    assert sorted(indices) == list(range(count)), "Missing or duplicated corners"
    assert len(indices)//3 == original["triangles"]
    triangles += len(indices)//3
assert triangles == sum(asset["triangles"] for asset in manifest["assets"])
assert triangles > 100000
print(f"UNITY_GEOMETRY_OK: {len(catalog)} Blender meshes, {triangles} triangles, finite UVs/unit normals, complete indices, matching colliders")
