# Verification record

## Version 0.3 — furnished exploration and complete charter

- Regenerated the actual Blender building with eight themed floors, bookcases, chairs, lounges, plants, studio/gallery/astronomy furniture, courtyard trees and Pip the squirrel guide.
- Ten exported modules contain **320,632 render triangles and 1,336 authored architecture collision hulls**. All ten FBXs round-tripped through Blender; 2,361 supported headroom samples pass.
- Unity 6000.6.0f1 compiled and packaged the updated Mac app without C# errors or warnings.
- The packaged game passed the expanded automated test: eight floors ascended, seven stair connections descended, Pip targetable, all twelve memory raycasts collected, all four structure types built, a refund processed, all six charter milestones earned, and Beacon completed at 24.
- Completed state was saved and read back with all twelve memories and all six milestones intact. Normal player saves are separate from the test slot. Existing version-1 saves gain an initially empty milestone field; resource balances and plots retain their original format.
- Journal opening suspends movement; closing restores the appropriate play mode. The game screenshot shows all six completed objectives. Runtime images of furnished interiors, Pip, the journal and completed campus were inspected.
- Runtime test finished in 11.63 seconds with accelerated incremental controller moves. It reports 1,386 active BoxCollider components: architecture, 49 plots and the guide target. This is not a human-play speed or framerate measurement.
- Standalone C# tests also verify persistent milestones survive reclaiming a building, plus 100,000 randomized transactions with story updates and valid resource accounting.
- Evidence: [v0.3 runtime result](UnityPlaytest-v03/result.json), [journal](UnityPlaytest-v03/05-journal.png), [Pip](UnityPlaytest-v03/06-pip-guide.png).
- Remaining limits: longer human input/playtesting, animation/audio, menus/accessibility, full campus reconstruction, network play and mobile/AR. The Unreal runtime is still unverified.

## Verified Unity Mac prototype — September 7, 2026

- Installed Unity Hub 3.21.1 and Unity 6000.6.0f1 (f7f8ed4d1e24), with an active license.
- Compiled and packaged the Unity project without C# warnings or errors; launched the Mac app on an Apple M4 Max running macOS 15.7.7.
- Exported all ten evaluated Blender meshes into Unity meter-scale buffers: 132,660 triangles with normals, UVs and materials.
- Geometry validation confirms complete indices, finite values, unit normals, and matching authored colliders.
- Unity C# rules pass 100,000 randomized transactions plus explicit spending, refund, duplicate-collection, player-isolation and tamper cases under .NET 9.
- Added first-person controls, building, appearance switches, local player profiles, a shared Beacon and validated JSON saves.
- Automated runtime walkthrough **passed**: the actual CharacterController traversed the entrance and seven stair connections, reached all eight floor heights, and targeted eight memory colliders.
- Build/refund/contribution transactions, library scene creation, and reading back the saved JSON state passed. Smoke tests use a separate save file from normal play.
- Runtime reports 132,660 tower triangles and 1,176 BoxColliders (1,127 tower/site hulls plus 49 campus tiles).
- Inspected actual engine images of the exterior, eighth-floor corridor and built library. Fixed a focus-related test pause, builder test refresh and harsh point-light highlights.
- Automated test completed in 7.21 seconds using accelerated incremental controller moves. This is not a measurement of normal gameplay speed or framerate.
- Unity’s first interactive launch crashed in its UI text renderer. A normal restart succeeded; LegacyHall was prepared through the Odyssey menu and Play mode visibly rendered the tower and campus builder with Keeper progress.
- Longer human testing of mouse/keyboard feel, descending every staircase, jumping, screen-size coverage and restart persistence through the UI remains. Interiors and campus surroundings need substantial further art work.

## Full-size architecture update — September 7, 2026

- Built an editable eight-storey, 32.8 m original campus tower in Blender, with ten exported architectural modules.
- Generated 132,660 render triangles and 1,127 authored UCX collision hulls.
- Verified 2,361 supported player-headroom samples along a route spanning all eight floors. Corrected stair supports that initially obstructed headroom.
- Imported all ten FBXs back into Blender and verified mesh identities, UVs, collision counts, origins, and meter-scale dimensions.
- Added first-person Unreal source, shared persistence with the builder, tower collectibles, and two-way map switching.
- Unreal compilation, import, collision behavior, material appearance, and packaged gameplay remain unverified because Unreal Engine is not installed.

See [FullScaleArchitecture.md](FullScaleArchitecture.md) for the revised native acceptance criteria.

## Completed locally

- Blender 5.2.1 LTS executed `Tools/generate_assets.py` successfully.
- Eight nonempty binary FBX files and `AlbionConceptKit.blend` were generated.
- Both 1600×1200 renders were visually inspected for composition and missing geometry.
- C++ rules compiled with Clang, strict warnings, AddressSanitizer, and UndefinedBehaviorSanitizer.
- 100,047 checks passed, including 100,000 randomized transactions preserving currency and contribution invariants.
- Static validation checks the project descriptor, Python syntax, FBX headers, preview dimensions, Blender file presence, and Git-friendly file sizes.

## Not verified

- Unreal C++ compilation / UnrealHeaderTool processing.
- `Tools/setup_unreal.py` against an installed engine.
- Play-in-Editor, input hit testing, imported materials/scale, journal layout, local SaveGame round trips.
- Packaging/cooking and launching outside the editor.
- iOS/Android, real AR/GPS, controllers, touch, online co-op, and authentication (not implemented).

The Mac has Epic Games Launcher, Blender, and Xcode. Epic's installed-engine manifest is empty, and filesystem checks did not find UnrealEditor. Launcher engine-page navigation was unsuccessful in the available UI session. This is the current blocker for native verification; a generated project is not evidence of a successfully running Unreal game.

## Native acceptance checklist

1. Build the editor target cleanly and run asset setup. Confirm eight imports, the `Tint` material parameter, and the saved LegacyCampus map.
2. Start Play. Confirm a colored 7×7 island, twelve collection points, camera controls, readable HUD, and a visible shared Beacon.
3. Click a memory. Balance goes from 6 to 9; that memory disappears. Reopening the journal shows the entry; duplicate collection cannot pay twice.
4. Build an observatory for 6. Try an unaffordable library and an occupied plot; neither changes state. Reclaim the observatory and confirm a full refund.
5. Switch appearances. Geometry/materials change, while currency, collection, and layout remain the same.
6. Switch Keepers. Confirm a separate campus and collection. Return to the first Keeper and confirm its state is intact.
7. Contribute across all four Keepers. Confirm the shared Beacon caps at 24 and contributions match deductions.
8. Quit Play, restart, and confirm all profiles and the Beacon persist. Test a malformed save in a disposable save slot and verify rejection without a crash.
9. Collect all memories and browse the journal. Test small and large window sizes. Click on HUD panels and confirm no world action occurs underneath.
10. Package a Mac development build, launch it, and repeat the collection/build/save flow. Confirm primitive meshes and generated models are included by the cooker.

Record engine version, Xcode version, outcome, and screenshots before upgrading the README status to a verified playable build.
