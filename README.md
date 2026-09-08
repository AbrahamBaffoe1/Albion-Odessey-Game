# Albion Odyssey 0.9 — explorable Robinson and Bonta update

Open **Albion Odyssey.app**. On the launch screen, choose **Explore Ferguson Hall** on the right. Approach the entrance, press **E** to open the door and walk inside. Press **E** near room doors. Stairs at the east end connect all three floors. **H** opens Ferguson’s history and official media; **G** opens all building stories.

## Controls and interface

WASD or arrow keys move; mouse looks; Shift runs; Space jumps; V changes camera. O enables the four on-screen movement buttons. M opens the map, B opens the Building Studio, K opens courses, J opens the journal, and Esc opens the main menu. No function keys are needed for these actions.

During exploration the interface has one compact location header, Map/Menu buttons and one contextual action prompt. Movement buttons appear only when enabled. Achievements use one notification area. Detailed stats, courses, character choices and media remain in their dedicated panels.

Choose **Finish session** to save and see the summary, then **Quit to desktop**. All existing campus, Legacy Hall, classroom, courses, collections, media and building tools are in the same app. Internet is needed only for the official streamed media.

## What changed

- Ferguson’s solid exterior placeholder is replaced with actual Blender meshes, photographed brick textures, a recessed arched entrance, three levels, stairs, furniture and 19 interactive doors.
- The student is now a skinned character based on Quaternius’s CC0 model, with project-made campus clothing, footwear and backpack. A 65-bone rig plays authored idle, walking, jogging, sprinting and seated animations through a Unity Animator blend tree. This is a stylized human character, not a photorealistic person.
- Campus lawns and paving have photographed textures. Blender oak trees have branches and individual leaf geometry. Lighting and shadows are coordinated across the scene. The campus still uses the approximate centers derived from the college’s published map.
- Generic mesh-section loading and reusable door components support the next buildings. Interiors are created on approach, hidden when distant, and reused on return. History and travel are available through the existing directory and map.
- Robinson Hall now has a reference-driven four-level atrium reconstruction with nine reusable doors, stairs, offices, balcony rails and a history panel. Bonta Admission Center now has a reference-driven walkable lobby, admissions offices, three doors and its own history panel. Both buildings use measured reconstruction dimensions recorded in `Art/BuildingReferences-v0.9.md`.
- Duplicate HUD panels are removed. World labels use depth testing so they do not show through walls.

## Accuracy and remaining work

Ferguson’s visible facade follows reference photographs. Robinson’s four-story atrium is supported by a published renovation report, and Bonta’s visitor-facing layout follows the official tour and public descriptions. Their hidden room dimensions and all three buildings’ complete floor plans remain provisional reconstructions, not measured digital twins. Gameplay room labels do not assert current official office assignments. Other campus buildings still need individual facade/interior reconstruction; adding all 61 map destinations was not the same as completing 61 explorable buildings. Wesley remains a separate reference-based room study accessible from its directory entry.

Headset VR, online multiplayer and online enrollment are not implemented. Courses and four Keeper profiles are local. Custom Building Studio designs can be edited and walked through; campus course buildings use the separate plot-building mode inside this same app.

## Source and resources

The consolidated source archive includes the Unity project and editable Blender sources: **FergusonHall.blend**, **StudentRig.blend** and **CampusOaks.blend**. Open Unity with 6000.6.0f1. Authoring tools are under Tools; generated editor caches are omitted. The canonical development checkout is `~/Development/Albion-Odessey-Game`. Unity is the current playable game; `Source/`, `Config/` and the `.uproject` preserve the earlier Unreal foundation.

See **Art/ASSET-CREDITS.md** and **Art/AssetProvenance.json** for reference links, CC0 materials, character/animation credits and hashes. College photos/videos remain linked to their providers rather than copied into the app as geometry.

## Verification

The release checks exercise physical entrance collision, door opening, walking both stair flights, rooms, history, real skinned-mesh deformation, proximity loading, camera and audio ownership, studio save/return, all 61 destinations, cars, courses, media, menus and save-failure recovery. The new walkable-building check verifies Robinson and Bonta registration, dimensions, doors, arrival, interior access and history mapping. Results are recorded in [Docs/Verification-v0.9](Docs/Verification-v0.9). New local test output goes to the ignored `Verification/` directory.

## Build and check from this repository

Large binary models, textures and audio use Git LFS. Install Git LFS and run `git lfs pull` after cloning so Unity and Blender receive the actual assets.

1. In Unity Hub, add the `Unity` folder and open it with Unity **6000.6.0f1**. Use **Odyssey → Prepare playable scene**, then Play. The checked-in runtime meshes, textures and rig are ready to import; Blender is needed only to edit/regenerate assets.
2. Build the Mac app with `bash Tools/unity_mac.sh build`. The default output is `Unity/Builds/Albion Odyssey.app`; `UNITY_EDITOR` and `ODYSSEY_BUILD_PATH` can override the editor and output paths.
3. Run `python3 Tools/check_craft_release.py` against that app, or pass `--app '/path/to/Albion Odyssey.app'`. This launches five isolated test sessions and writes results under `Verification/`.
4. Run `bash Tools/test.sh`, then `dotnet run --project Tests/UnityRules/Rules.csproj`, `dotnet run --project Tests/BuildingDesigner/Rules.csproj`, and `dotnet run --project Tests/Catalog/Catalog.csproj` for rules, design and catalog checks without launching the game. These require a C++ compiler, Python 3 and .NET 9.

Editable Blender files are in `Art/`; source-generation tools and asset credits are included. The old `Tools/clean_campus_ui.py` is a historical one-time migration and must not be rerun on this version.

See [next development milestones](Docs/NextMilestones.md) for the current work order. Older versioned documents describe their respective releases.
