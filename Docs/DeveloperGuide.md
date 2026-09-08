# Developer guide

This document keeps build, controls and verification details out of the project story.

## Open and play

Open the `Unity` folder with Unity **6000.6.0f1**. Use **Odyssey → Prepare playable scene**, then press Play. The checked-in runtime meshes, materials and rig are ready to import; Blender is only needed when editing or regenerating source assets.

The macOS player is built with `bash Tools/unity_mac.sh build` and writes to `Unity/Builds/Albion Odyssey.app`. The reproducible packaging script is `Tools/package_mac_release.sh`; it creates a signed local archive and SHA-256 manifest. Set `APPLE_SIGNING_IDENTITY` to choose an identity, or set `REQUIRE_DISTRIBUTION_SIGNING=1` to refuse anything except a Developer ID Application identity. Check the result with `python3 Tools/check_mac_release.py`. Developer ID signing and notarization require the matching Apple credentials and profile.

## Controls

WASD or arrow keys move, the mouse looks, Shift runs, Space jumps and V changes camera. E interacts with doors, objects, vehicles and characters. M opens the campus map, H opens nearby history, G opens building stories, K opens courses, J opens the journal and F2 opens personal building mode.

F4 opens accessibility settings: captions, large text, high contrast, reduced motion and alternate movement keys. F5 opens the LAN shared-campus panel. F8 opens optional XR mode. F9 shows the runtime profiler. O enables the four on-screen movement buttons for mouse-only play.

## Verification

Run `bash Tools/test.sh` for core rules and asset checks. Run the Unity rules, geometry, audio, designer and catalog checks described in the repository workflow for the full non-player validation. A built player can run the five isolated integration sessions with `python3 Tools/check_craft_release.py --app '/path/to/Albion Odyssey.app'`.

The latest results are recorded in [Docs/Verification-v0.12](Verification-v0.12). The focused walkable-building check covers Robinson, Bonta and the four-wing Science Complex. Local runtime reports and crash reports are written to the ignored `Verification/` directory and the app's `crash-reports` folder.

## Source layout

- `Unity/Assets/Scripts` contains gameplay systems and shared building components.
- `Unity/Assets/Resources` contains runtime meshes, materials, audio and authored data.
- `Art/` contains Blender sources, exported architecture and provenance records.
- `Tools/` contains asset generation, Unity setup, packaging and validation scripts.
- `Tests/` contains rules and randomized model checks.

Large binary assets use Git LFS. Run `git lfs pull` after cloning before opening the Unity project.
