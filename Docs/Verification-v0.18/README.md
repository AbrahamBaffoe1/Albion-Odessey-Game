# Vehicle release verification — 0.18.0

The dedicated vehicle check and all five player integration suites passed on the macOS player. The installed app passed strict signature verification (ad-hoc). The Quest APK compiled successfully; physical headset testing has not been performed.

Actual Unity captures show the front, rear, populated parking, and occupied vehicle. The Blender review is separately stored in Art/Vehicles. The asset was checked in Unity after correcting an FBX transform issue; a scale assertion now guards both lower and upper dimensions.

See results.json for fleet counts and LOD triangle counts, and regression-checks.json for the five existing suites.
