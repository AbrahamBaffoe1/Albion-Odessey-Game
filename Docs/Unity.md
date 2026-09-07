# Unity implementation

The Unity project lives in `Unity/`, alongside the Unreal project. Both consume the same original, full-size Blender tower. This is an eight-storey, 32.8 m game building, not a surveyed Albion College replica.

## Setup and play

1. Install Unity **6000.6.0f1 (Apple silicon)** through Unity Hub and activate your appropriate Unity license.
2. Add the repository's `Unity` folder as a project in Hub and open it.
3. Choose **Odyssey → Prepare playable scene**. This saves `Assets/Scenes/LegacyHall.unity`, configures mouse input, and prepares the built-in Standard materials.
4. Press **Play**. The game constructs the tower from the evaluated Blender meshes and starts you outside the entrance.
5. For a standalone Mac game, choose **Odyssey → Build macOS game**. The default output is `Unity/Builds/Albion Odyssey.app`.

The command-line equivalent is `bash Tools/unity_mac.sh build`. Set `UNITY_EDITOR` if your editor is installed elsewhere, or `ODYSSEY_BUILD_PATH` to change the app output path. Close the editor before using the batch helper on the same project.

## Controls

- WASD walk, mouse look, Shift run, Space jump, E collect a nearby memory or talk to Pip.
- J opens the memory journal and charter checklist. Arrow keys browse entries; Escape or the Close button returns to play.
- The mouse wheel zooms the builder; green and red footprints show available or blocked placements.
- Escape releases the pointer; click the game to capture it again.
- F2 switches between first-person tower exploration and personal campus building.
- In the builder, 1–4 select Garden / Library / Observatory / Hall, click builds, right-click reclaims with a full refund, and T switches the campus/fantasy palette.
- Tab changes between four local Keepers. C contributes two acorns to the shared Beacon.

Each Keeper starts with six acorns. Twelve memories give three acorns each, once per Keeper. Eight memories are in the tower and four are outside. Keeper progress is saved in a validated local JSON file under Unity's application data folder. The two engines use the same economy, but their save-file formats are separate.

The Unity personal-campus structures are procedural prototype geometry. The main exploration building is the actual detailed Blender architecture. Online co-op, AR/GPS integration, surveyed campus reconstruction are not implemented. Unity has its own journal and six persistent charter milestones. All journal entries are clearly labelled original game fiction.

## Asset workflow

Run:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background Art/Architecture/LegacyHall_FullScale.blend --python Tools/export_unity_geometry.py
python3 Tools/validate_unity_geometry.py
```

This exports Blender's evaluated geometry, normals, UVs, material assignments and the existing authored collision manifest. Coordinates are explicitly converted from Blender Z-up meters to Unity Y-up meters. FBX remains the exchange format for Unreal; Unity uses the same geometry in an auditable `AOM1` buffer to keep visual and collider coordinates identical.

`AOM1`: four-byte signature; little-endian int32 vertex and submesh counts; eight float32 values per vertex (position xyz, normal xyz, UV xy); then, for each submesh, an int32 index count followed by int32 indices. Each triangle corner has its own vertex, preserving hard edges and UV seams. These are rendered meshes, not image planes.

## Verification

`dotnet run --project Tests/UnityRules/Rules.csproj` checks the Unity economy with 100,000 randomized transactions, duplicate collection, costs/refunds, profile isolation and corrupt-state rejection. `Tools/validate_unity_geometry.py` verifies 320,632 triangles across ten buffers, complete triangle indices, unit normals, finite UVs, and the collision manifest's match to the Blender source.

The opt-in `-odysseySmoke` player argument runs an actual CharacterController route through the entrance, up eight floors and down all seven stair connections, checks Pip and all twelve memories, and exercises construction, refunds, all six charter milestones, Beacon completion, journal modal controls and save readback. It writes a separate test save, screenshots and `result.json` under the folder in `ODYSSEY_SMOKE_PATH` (or the application's `Smoke` data subfolder). This is an automated runtime test, not evidence of a play test until its result has actually passed.

## Current verification status

Version 0.3 compiled and packaged with Unity 6000.6.0f1 (Apple silicon), without C# warnings or errors. The Mac game passed the expanded automated route: eight floors ascended, seven stair connections descended, Pip targetable, all twelve memories collected, all four building types placed, refunds, all six charter milestones, full Beacon completion, journal controls and saved-state readback. Runtime screenshots were inspected. These tests use direct controller movement; longer human input and usability testing remains. See `Docs/Verification.md` for measured results.

## Version 0.4 learning expansion

See [the complete play guide](PlayGuide.md) for arrow-key and screen-button controls, the map, sourced history and local course creation. The runtime now imports twelve Blender mesh modules. Version-2 saves include building-linked courses, ownership, twelve-seat enrollment, simulated students and per-Keeper lesson completion. First launch can import a valid version-1 save without modifying it. Multiplayer and real online students are not implemented.
