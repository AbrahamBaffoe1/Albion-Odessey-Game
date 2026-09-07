# Full-size building workflow

The active direction is architecture that a player experiences at ground level. Legacy Hall is a complete, original eight-storey game building, 28 m wide, 20 m deep, and 32.8 m high including its rooftop plant enclosure. It is not an exact model of Mitchell Towers or another real campus building.

## Step 1 — inspect the actual Blender model

Open `Art/Architecture/LegacyHall_FullScale.blend` in Blender. The building is ordinary editable mesh geometry, with materials and texture images packed into the file. No animation or billboard substitutes for the structure.

The Outliner separates the site, each of the eight floors, the roof, a 1.80 m human reference, and hidden export/collision collections. Hide upper-floor collections to inspect the interior. Leave the hidden export and collision collections hidden during ordinary modelling, because they duplicate the visible geometry for game export.

Features include a real open entrance, entrance canopy, repeated window bays, a central hall with side-room openings, desks and benches, and a staircase linking the floors. Each storey is 3.6 m high. The two stair flights have 1.8 m usable width, 18 cm risers, 30 cm treads, and a half landing. Thin stair treads preserve headroom under the flights above.

Two saved cameras provide a street view and a lobby view at 1.65 m eye height. Those views are for checking the 3D model; the model remains fully editable and can be viewed from any direction.

## Step 2 — verify the exported geometry

`Art/Architecture` contains eight floor FBXs, one roof FBX, and one site FBX. Each building part retains a common origin, so Unreal can assemble the building by placing them together at world origin. The floor slabs have genuine stairwell openings.

Each export includes named UCX convex collision geometry. This matters because one convex hull around the whole building would make the interior inaccessible. The import script disables automatic replacement collision and checks the imported hull count against the manifest.

`Tools/verify_fbx_roundtrip.py` imports the actual FBXs back into Blender and checks their render meshes, UVs, collision hull counts, origins, and meter-scale dimensions. `Tools/validate_architecture.py` checks a route through the lobby and all eight floors for supporting surfaces and 1.92 m player headroom. These checks found and led to a fix for obstructive solid stair supports.

## Step 3 — import into Unreal

Unreal Engine must be installed and the C++ module must compile first. This has not yet been accomplished on the development Mac. The Blender deliverable is complete for this step; the Unreal integration remains unverified.

Once the editor opens the project, execute `Tools/setup_all.py` through **Tools → Execute Python Script**. It creates both game maps and leaves `CampusWalkthrough` open. The full-size import reconstructs materials from the original brick textures, imports UCX collision, and adds sunlight, hallway lights, and a player start outside the entrance.

The script uses the explicit FBX factory to avoid silently ignoring legacy FBX settings. If a material slot or collision count does not match, it stops with an error. Subsequent runs replace only actors tagged `OdysseyArchitectureGenerated`; hand-authored actors are preserved. Geometry, textures, and materials under `/Game/Architecture` are generated assets and may be replaced when the script runs.

## Step 4 — walk and test in Unreal

Press Play. Use WASD and mouse look, Shift to run, and Space to jump. Walk through the front entrance. Find a memory in the central corridor, aim at it within 3.5 m, and press E to collect it. Take the staircase to the next floor and repeat.

Press F2 to enter the personal-campus builder. The acorns collected in the tower use the same inventory, journal IDs, and save as the builder. Duplicate collection across maps cannot pay twice. F2 from the builder returns to the tower; Tab switches among the four local Keepers.

The full-size tower currently uses one architectural appearance. The builder still supports Campus/Fantasy appearances. A separate fantasy tower facade is future work.

## Native acceptance criteria — still pending

- Compile the editor target with a compatible Unreal/Xcode version.
- Confirm the import yields ten meshes at centimeter scale and exactly 1,127 convex collision hulls in total.
- Verify the player faces and reaches the entrance after FBX coordinate conversion.
- Walk up and down all seven stair connections without clipping, getting stuck, or falling through a floor.
- Confirm brick tiling, normal-map orientation, transparent glass, lighting, and frame time in the actual engine.
- Collect a memory in the tower, switch to the builder, spend its reward, save, restart, and verify persistence.
- Confirm previously collected memories do not reappear or pay rewards again when switching maps/profiles.
- Package and launch outside the editor before calling this a playable release.

## References

[Albion's official Mitchell Towers page](https://www.albion.edu/offices/community-living/living-on-campus/our-communities/mitchell-towers/) and its [published four-floor plan](https://www.albion.edu/wp-content/uploads/2022/02/Mitchell-Towers-Floor-Plan.pdf) were checked for context. That plan states it is not to scale. Legacy Hall's eight floors and dimensions are original design choices, not claims about Mitchell Towers.

The export workflow follows [Epic's FBX static-mesh and UCX collision conventions](https://dev.epicgames.com/documentation/en-us/unreal-engine/fbx-static-mesh-pipeline-in-unreal-engine). The import validation uses [Unreal's StaticMeshEditorSubsystem](https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/StaticMeshEditorSubsystem?application_version=5.5).
