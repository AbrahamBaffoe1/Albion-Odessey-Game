# Verification record

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
