# Leucistic squirrel reference

`LeucisticSquirrelPortrait.blend` is a purpose-built wildlife portrait scene based on the supplied right-hand reference image. It contains one side-profile squirrel, one diagonal snow-covered branch, blurred autumn branches and leaves, a level 120 mm camera and eye-focused depth of field. It deliberately contains no campus buildings, roads, grass fields or landscape props.

Run `Tools/build_squirrel_portrait.py` with Blender 5.2+ to rebuild the `.blend`, `.png` and posed `Squirrel_Leucistic.fbx` outputs. The FBX is copied into Unity `Resources/Wildlife` and loaded by `CampusFauna`; the runtime keeps its procedural fallback for import failures.

The squirrel is original game geometry shaped from the supplied visual reference. The reference photograph itself is not embedded in the game asset.
