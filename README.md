# Cosmic Squirrels: Albion Odyssey

Explore memories. Build your legacy. Together, anywhere.

An Unreal Engine C++ prototype and Unity C# implementation with an original Blender environment kit for a campus exploration and creative building game inspired by Albion College. Players collect memory echoes, turn starlit acorns into a personal campus, and contribute to a shared Constellation Beacon. Room play requires neither GPS nor a camera.

## Full-size 3D building — start here

**Open `Art/Architecture/LegacyHall_FullScale.blend` in Blender.** This is editable 3D architecture: an eight-storey, 32.8-meter tower with a real entrance, interior floors, hallways, study spaces, and seven stair connections. It is an original game building, not a measured replica of an Albion building.

The primary game map is now a **first-person walkthrough**. The Unity Mac build has been compiled, launched, and tested through all eight floors using its actual CharacterController. Both engine implementations include walking, mouse look, sprinting, jumping and memory collection. F2 connects exploration to each engine’s personal-campus builder.

- Twelve real FBX meshes: eight tower floors, a roof, site, classroom and history pavilion.
- 360,724 render triangles and 1,470 authored UCX collision hulls.
- Original brick color, normal, and roughness textures.
- A 1.80 m reference figure and a 1.65 m player eye height.
- 3,193 floor-support/headroom samples across a complete eight-floor route.
- All twelve FBXs round-tripped through Blender with geometry, UVs, and meter-scale collision verified.

The Blender model is built and verified. The Unreal C++ and import integration are prepared but **not yet compiled/run in Unreal**, because the engine is not installed. Renders in the asset folder are inspection views of the model; the `.blend` and `.fbx` files contain the actual building.

See [the step-by-step workflow](Docs/FullScaleArchitecture.md).

## New in 0.4 — Learning spaces and local classes

Two additional walkable Blender buildings join the tower: the Common Classroom and Albion History Pavilion. Press **M** for a map and travel, **H** inside a learning space for sourced history, and **K** to create a course for an owned Hall or Library. Enroll a local Keeper, assign simulated students, run a twelve-seat class and answer its lesson question. Course ownership, capacity and completion persist; old version-1 saves upgrade without losing discoveries or buildings.

Movement now supports **WASD and all four arrow keys**. **O** enables clickable direction/turn buttons, **E/F** picks up or interacts, and **Esc/F1** opens help with Save and quit. **P** throws short-lived paper balls inside the classroom. This is local classroom simulation, not online enrollment or multiplayer.

Start with [the play guide](Docs/PlayGuide.md). The [counted roadmap](Docs/Roadmap.md) has **5 completed prototype packages and 17 remaining major packages**.

## New in 0.3 — The Keeper’s Charter

Legacy Hall now has eight named, furnished floors with bookcases, chairs, lounges, plants, a maker workbench, gallery sculptures, an astronomy table and council seating. The plaza has trees, benches and **Pip**, an original 3D squirrel guide modelled in Blender. Aim at Pip and press **E** for your next objective.

Press **J** to open a twelve-entry journal of original game fiction. Six persistent charter milestones take you from your first discovery to a completed campus and an illuminated shared Beacon. Earned milestones survive reclaiming and redesigning your buildings. The builder now previews valid/blocked plots and supports mouse-wheel zoom.

These additions are implemented in Unity. Updated Blender/FBX architecture is shared with Unreal; the new journal and charter gameplay have not been ported to the Unreal implementation.

## Current status — 0.4.0 local learning prototype

**The Unity Mac prototype now builds and runs.** Verified with Unity 6000.6.0f1 on an Apple M4 Max Mac: twelve real Blender meshes render, the controller ascends and descends the tower, all twelve memory raycasts work, and the complete six-milestone charter—including build/refund and save/readback—passes. A packaged app is available in the development workspace outputs; binaries are kept out of Git. **The separate Unreal module and import scripts remain uncompiled and unrun**, because Unreal Engine is not installed.

This is a foundation for the larger game, not a finished release. The exploration building now uses full-size architectural proportions and brick/stone materials. The separate campus-builder kit retains its small tile-based models. Neither is an accurate reconstruction of Albion's grounds; the fantasy builder appearance adds purple masonry and celestial details.

| Capability | Implementation status |
| --- | --- |
| Full-size first-person tower exploration | Unity Mac runtime verified across all eight floors; Unreal verification pending |
| Personal campus building | Unity build/refund and rendered structure verified; Unreal verification pending |
| Two appearances per player | Eight generated Blender/FBX modules; source switches appearances |
| Four independent personal campuses | Local pass-and-play profiles; not four simultaneous online players |
| Shared building project | One local Beacon with tracked per-profile contributions |
| Memory collection, journal, guided objectives | Unity has twelve fictional entries and six persistent charter milestones; Unreal has a separate archive |
| Persistence | Unity validated JSON save round-trip verified; Unreal SaveGame verification pending |
| Real-world location collection | Tested distance/accuracy/permission policy only; no live GPS adapter yet |
| Courses and classroom enrollment | Local saved courses, twelve-seat capacity, simulated students and quizzes; online remains pending |
| Map, history and screen movement buttons | Implemented in Unity; sourced history and original concept architecture |
| AR, accounts, online multiplayer, chat | Not implemented |
| Realistic surveyed campus, full 360 coverage | Reference gathering started; capture and reconstruction remain |
| Adaptive AI | Not implemented; current objectives use deterministic progression rules |

## Open in Unity

The `Unity/` project uses the actual eight-storey Blender tower with first-person controls, memory collection, four local Keepers, a campus builder, and validated JSON saves. Unity 6000.6.0f1 is installed and licensed on the development Mac. Editor compilation, Mac packaging and an automated eight-floor physics walkthrough pass. See [Unity setup and controls](Docs/Unity.md).

## Open in Unreal

1. Install Unreal Engine through Epic Games Launcher. The project association is set to **5.5** as a baseline; choose a version compatible with your installed macOS and Xcode. See [Epic's Mac requirements](https://dev.epicgames.com/documentation/en-us/unreal-engine/macos-development-requirements-for-unreal-engine). No engine-version combination is claimed tested yet.
2. Open `AlbionOdyssey.uproject`. Let Unreal build the C++ module. If using a different engine version, switch the project association first.
3. In the editor, run **Tools → Execute Python Script → Tools/setup_all.py**. This imports the full-size architecture and builder assets, creates materials and collision, and saves `/Game/Maps/CampusWalkthrough` and `/Game/Maps/LegacyCampus`. A missing-map warning on first launch is expected before this step.
4. Press **Play**. You start outside Legacy Hall in first person. Walk inside, climb the stairs, and press E to collect a memory. F2 switches to the personal-campus builder.

On macOS the optional helper builds the editor target and opens Unreal with the setup script:

```sh
UE_ROOT='/path/to/UE_5.5' bash Tools/build_mac.sh
```

The setup script is safe to rerun for asset updates and keeps an existing map. It replaces generated imports under `/Game/Generated`, so put hand-authored content elsewhere.

## First-person controls

In Unity, WASD or arrow keys move, the mouse looks around, Shift runs, Space jumps, E/F interacts, Tab switches local Keeper, and F2 opens the builder. M opens travel, H reads nearby history, K manages courses and O toggles screen controls. The Unreal source still uses its earlier controls. The stairs are toward the right-hand rear of the building in Blender coordinates. Eight tower memories share IDs and rewards with the existing archive, so switching maps cannot duplicate a reward.

## Play the Unreal campus builder

Start with six acorns. Click the glowing memories around the island to collect three more per memory. Select a building, click an empty tile, and begin designing. Every profile can collect all twelve memories. Collection does not depend on leaving your room.

| Control | Action |
| --- | --- |
| Left click | Collect a memory or build on a tile |
| 1 / 2 / 3 / 4 | Garden (2), Library (4), Observatory (6), Hall (3) |
| Right click | Reclaim a building for a full refund |
| T | Switch your campus / fantasy appearance |
| Tab | Switch to the next of four local Keepers |
| C | Contribute two acorns to the shared Beacon (24 total) |
| J | Open/close the memory journal |
| [ / ] | Browse collected memories while the journal is open |
| E | Toggle room exploration / construction |
| W A S D | Move the camera focus |
| Q / R | Orbit the camera |
| Mouse wheel | Zoom |
| Home | Reset the camera |
| F2 | Return to the full-size first-person tower |

Changes save after successful transactions, appearance switches, and profile switches. Saves are local to the installation and use `AlbionOdyssey_Local_v1`. A local profile is a convenience for shared-device play, not an authenticated identity.

The first objectives guide collection, building three structures, and completing the Beacon. All four Keepers can contribute. There is no paid currency and no penalty for redesigning: buildings return their full construction cost.

## Earlier campus-builder kit

Open `Art/AlbionConceptKit.blend` in Blender. The scene selector contains **Campus Concept** and **Echo Fantasy**. Each has its own camera and lighting. The initial scene contains the reusable source meshes, hidden from rendering.

![Campus architectural concept, rendered in Blender](Art/CampusConcept.png)

The library, hall, observatory, and garden each have Campus and Fantasy versions. The source meshes use meters and FBX unit metadata; Unreal imports them in centimeters. Each module fits one 420 cm game tile. Assets are original procedural geometry, not downloaded college photographs or logos.

To regenerate with Blender (tested with Blender 5.2.1 LTS):

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/generate_assets.py
```

## Regenerate the full-size architecture

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/build_architecture.py
/Applications/Blender.app/Contents/MacOS/Blender --background --python Tools/verify_fbx_roundtrip.py
```

## Verify changes

```sh
bash Tools/test.sh
```

This compiles the same engine-independent rules included by the Unreal game using address/undefined-behavior sanitizers, then checks project metadata, scripts, exported assets, and the full-size tower headroom/support route. It covers collection, duplicates, costs, refunds, profile isolation, shared contributions, invalid state, location-policy edge cases, and 100,000 randomized transactions. GitHub Actions runs this check on pushes and pull requests. **These checks do not establish that the Unreal module compiles or renders correctly.** See [the native verification checklist](Docs/Verification.md).

## Project guide

- `Unity`: C# game implementation, actual exported Blender geometry, editor setup, and an opt-in physics walkthrough test.
- `Source/AlbionOdyssey/Core`: portable rules and optional future location eligibility policy.
- `Source/AlbionOdyssey/OdysseyWalkthrough.*`: full-size first-person movement, collection, and map switching.
- `Source/AlbionOdyssey/OdysseyPersistence.*`: shared validated saves for both maps.
- `Source/AlbionOdyssey/OdysseyGame.*`: Unreal world, input, HUD, memory archive, and local saves.
- `Tools`: Blender generation, Unreal import, macOS build helper, tests, and validation.
- `Art/Architecture`: the full-size editable tower and learning spaces, twelve FBX modules, PBR textures, and collision manifest.
- `Art`: the earlier campus-builder kit and inspection previews.
- [Campus references](Docs/CampusReferences.md): official map/tour and reconstruction plan.
- [Game direction](Docs/GameDesign.md): dual-mode vision and planned creative features.
- [Next implementation milestones](Docs/Roadmap.md): concrete work needed for the full game.

No student ID, credentials, personal location records, or third-party photographs are included.
