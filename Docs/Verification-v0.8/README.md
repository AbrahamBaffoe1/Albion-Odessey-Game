# Verified 0.8 Mac release

`release-checks.json` records five successful player check groups on the delivered macOS build. `ferguson-checks.json` records three floors, 19 doors, skin deformation, history and proximity loading. `Ferguson-playable.png` was captured from the release player.

The suites exercised physical entrance collision, both stair flights, room doors, history, the skinned character, interior hiding/reactivation, combined building tools, all 61 map destinations, courses, campus vehicles, media and session saving. They do not establish architectural accuracy, online multiplayer support or headset VR compatibility.

Run `python3 Tools/check_craft_release.py --app '/path/to/Albion Odyssey.app'` to repeat the player checks. Results from a new run are written under the ignored `Verification/` folder. Moving the source into the development checkout did not alter game runtime code or assets; build/test runner paths and documentation were made portable.
