"""Static project/asset validation; this does not replace an Unreal build."""
import ast
import json
import struct
from pathlib import Path

root=Path(__file__).resolve().parents[1]
project=json.loads((root/"AlbionOdyssey.uproject").read_text())
assert project["Modules"][0]["Name"]=="AlbionOdyssey"
for path in (root/"Tools").glob("*.py"):
    ast.parse(path.read_text(),filename=str(path))
for style in ["Campus","Fantasy"]:
    for kind in ["Garden","Library","Observatory","Hall"]:
        path=root/"Art"/f"SM_{kind}_{style}.fbx"
        assert path.stat().st_size>1000, f"Empty asset: {path}"
        assert path.read_bytes().startswith(b"Kaydara FBX Binary"), f"Not FBX: {path}"
for name in ["CampusConcept","EchoFantasy"]:
    data=(root/"Art"/f"{name}.png").read_bytes()
    assert data[:8]==b"\x89PNG\r\n\x1a\n"
    assert struct.unpack(">II",data[16:24])==(1600,1200)
assert (root/"Art"/"AlbionConceptKit.blend").stat().st_size>100000
for path in root.rglob("*"):
    if path.is_file() and ".git" not in path.parts:
        assert path.stat().st_size<50*1024*1024, f"Large asset needs Git LFS: {path}"
print("PASS: project metadata, Python syntax, eight FBX exports, Blender source, and two 1600x1200 previews")
