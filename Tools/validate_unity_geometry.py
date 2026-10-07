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
assert len(catalog) == len(manifest["assets"]) == 12
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

# The runner's authored stretches must join without a notch in either trail verge.
forest = root / "Unity/Assets/Resources/CampusCraft"
materials = json.loads((forest / "forest.json").read_text())["materials"]
trail_boundaries = []
for name in ("forest_stretch_a", "forest_stretch_b"):
    data = (forest / (name + ".bytes")).read_bytes()
    assert data[:4] == b"AOM1"
    count, groups = struct.unpack_from("<ii", data, 4)
    assert 3 <= count <= 2000000 and groups == len(materials)
    vertices = list(struct.iter_unpack("<8f", data[12:12 + count * 32]))
    assert all(all(math.isfinite(v) for v in vertex) for vertex in vertices)
    offset = 12 + count * 32
    boundaries = [set(), set()]
    for material in materials:
        n, = struct.unpack_from("<i", data, offset)
        offset += 4
        assert 0 <= n <= count * 3 and n % 3 == 0
        indices = struct.unpack_from("<" + "i" * n, data, offset)
        offset += n * 4
        assert all(0 <= i < count for i in indices)
        if material["name"] in ("Forest trail earth", "Forest trail edge", "Forest river glass water"):
            for i in range(0, n, 3):
                a, b, c = [vertices[j] for j in indices[i:i + 3]]
                up = (b[2] - a[2]) * (c[0] - a[0]) - (b[0] - a[0]) * (c[2] - a[2])
                assert up > 0, (name, material["name"], "downward face")
        if material["name"] in ("Forest trail earth", "Forest trail edge"):
            for i in indices:
                x, y, z = vertices[i][:3]
                for edge, endpoint in enumerate((0, 210)):
                    if abs(z - endpoint) < .0001:
                        boundaries[edge].add((material["name"], round(x, 4), round(y, 4)))
    assert offset == len(data)
    assert boundaries[0] and boundaries[0] == boundaries[1], (name, "trail seam")
    trail_boundaries.append(boundaries)
assert trail_boundaries[0] == trail_boundaries[1], "A/B trail edges do not meet"
print("FOREST_GEOMETRY_OK: two meshes, valid buffers, upward trail/water faces, matching A/B trail seams")
