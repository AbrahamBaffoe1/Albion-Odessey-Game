# Unity 0.4 verification

Verified September 7, 2026 on the development M4 Max Mac with Unity 6000.6.0f1 and Blender 5.2.1 LTS.

- Native Mac development build: compiled and packaged successfully.
- Packaged runtime: eight tower floors ascended, seven stair connections descended, twelve memories collected, full charter completed, build/refund and Beacon saved and read back.
- Learning route: actual CharacterController walked from the tower through the teaching hall doorway and center aisle, then into the pavilion.
- Classroom: course creation, enrollment, twelve-seat capacity, simulated student render objects, class session and correct lesson completion passed. Course data survived Unity JSON serialization and validation.
- Save migration: a version-1 JSON fixture preserved its currency and discovery after upgrading; empty and occupied course slots survived serialization. Normal version-2 saves use a separate file and preserve the original version-1 file.
- Interaction: contextual history and modal movement lock verified; the directional input used by screen buttons moved the actual player controller. Arrow keys and E/F bindings were inspected in source; no automated OS keyboard event test is claimed.
- Paper play: spawned successfully in the classroom; runtime objects are capped and expire after six seconds.
- Geometry: twelve Blender meshes, 360,724 triangles and 1,470 authored architecture collision boxes. All twelve FBXs imported back into Blender with scale, collision and UV checks passing. Unity buffers passed finite-coordinate, normal, UV, count and index checks.
- Walking-route validation: 3,193 supported headroom samples, including both learning buildings.
- Portable rules: 100,047 C++ checks; 100,000 randomized Unity economy transactions; 10,000 randomized classroom transactions plus targeted ownership/capacity/migration/quiz cases.
- Visual inspection: classroom, course panel, history display and expanded campus captured from the packaged Unity player. Classroom mesh separation fixed interior lighting; course menu buttons use a flat style.

The complete runtime result and eleven screenshots are in the version 0.4 output's Playtest folder. The runtime pass took about fourteen seconds in an accelerated automated walkthrough; that is not a performance benchmark. Broad human playtesting, other platforms, Unreal runtime and online networking remain unverified.
