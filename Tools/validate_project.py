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
catalog=json.loads((root/"Unity/Assets/Resources/CampusTour/catalog.json").read_text())
assert any(place["name"]=="Munger Annex (E-House)" and "placeholder" not in place["summary"].lower() for place in catalog["places"])
assert all(media.get("caption","").strip() for place in catalog["places"] for media in place.get("media",[]))
tour_ui=(root/"Unity/Assets/Scripts/CampusTour/CampusTour.cs").read_text()
assert "hear the voice" not in tour_ui.lower()
assert "not yet been linked" not in tour_ui.lower()
assert "pending source verification" in tour_ui.lower()
world_label_ui=(root/"Unity/Assets/Scripts/CampusWorldLabel.cs").read_text()
for marker in ["World label backing", "Physics.Raycast", "LookRotation(targetCamera.transform.position"]:
    assert marker in world_label_ui, f"World-label readability hook missing: {marker}"
geometry_ui=(root/"Unity/Assets/Scripts/CampusGeometry.cs").read_text()
walkable_ui=(root/"Unity/Assets/Scripts/CampusCraft/WalkableCampusBuilding.cs").read_text()
assert "BuildingNameplate" in geometry_ui and "CampusWorldLabel" in geometry_ui, "Building nameplate hook missing from campus geometry"
assert "nameplate.Configure" in walkable_ui, "Walkable building nameplate hook missing"
controls_ui=(root/"Unity/Assets/Scripts/AlbionControls.cs").read_text()
for marker in ["MenuFooter", "GameplayFooter", "A / CROSS", "TRIGGER"]:
    assert marker in controls_ui, f"Controller legend hook missing: {marker}"
online_ui=(root/"Unity/Assets/Scripts/CampusOnlineSession.cs").read_text()
for marker in ["chatLog", "AddChat", "chatLog.Count > 8"]:
    assert marker in online_ui, f"Shared chat feed hook missing: {marker}"
campus_life_ui=(root/"Unity/Assets/Scripts/CampusWorldSystems.cs").read_text()+"\n"+(root/"Unity/Assets/Scripts/CampusActivitySystem.cs").read_text()
for marker in ["pauseVariant", "void Idle()", "void Work()"]:
    assert marker in campus_life_ui, f"Campus-life animation hook missing: {marker}"
course_ui=(root/"Unity/Assets/Scripts/CampusLife.cs").read_text()
assert "RosterLabel" in course_ui and "ROSTER" in course_ui, "Course roster presentation hook missing"
builder_ui=(root/"Unity/Assets/Scripts/BuildingDesigner/BlueprintStudio.cs").read_text()
for marker in ["controllerGrid", "UpdateControllerPreview", "PlaceAtControllerCursor", "Grid cursor"]:
    assert marker in builder_ui, f"Controller blueprint placement hook missing: {marker}"
for path in root.rglob("*"):
    if path.is_file() and ".git" not in path.parts:
        if any(part in path.parts for part in ("Builds", "Releases", "Verification", "Library", "Temp", "Logs", "Obj", "UserSettings")):
            continue
        assert path.stat().st_size<50*1024*1024, f"Large asset needs Git LFS: {path}"
print("PASS: project metadata, Python syntax, eight FBX exports, Blender source, and two 1600x1200 previews")
