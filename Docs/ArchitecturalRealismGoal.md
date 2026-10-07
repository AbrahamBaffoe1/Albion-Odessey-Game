# Architectural realism delivery goal

Status: in progress. This continues the existing campus-replica goal; it does not certify the current 61 destinations as complete replicas.

## Reference building and evidence

Ferguson is the first rendering reference because it already has a Blender source, three playable levels and automated entrance/room/stair checks. Its 31.5 × 12 metre envelope, heights and interior layout remain reconstruction values, not surveyed dimensions. See Art/BuildingReferences-v0.9.md. The 2015 entrance photograph and 2024 Pleiad renovation report are visual references; the latter places the Career and Internship Center on the third floor. They do not supply a complete measured plan or all current elevations.

## Delivery gates

- Geometry: complete visible exterior and usable interior; validate reference-supported details separately from inferred ones. Verify openings, floor levels, stair access, wall/glass collision and facade orientation.
- Materials: physical texture scale, normal maps and correctly converted roughness; preserve source licensing.
- Preparation: persisted Unity meshes/materials/prefabs with secondary UVs; validate geometry and collider parity against Blender exports. Secondary UVs alone do not mean lighting has been baked.
- Lighting: compare day, dusk, night and interiors in the actual player; avoid a daytime reflection capture that remains at night. Baked indirect lighting requires a placed scene and tested day/night strategy before enabling it.
- Performance: lower-detail exterior meshes at distance, streamed interiors, bounded reflection updates, representative Mac frame measurements.
- Acceptance: screenshot review plus actual controller traversal; review and merge only passing work; install one latest verified game.
- Campus rollout: use Docs/CampusReplicaCoverage.txt as the 61-destination inventory. Each destination requires its own geometry/evidence acceptance; a shared material upgrade does not make generic buildings accurate.

## Current implementation work

Prepared asset generation and packed roughness are implemented for Ferguson and Robinson. Ferguson receives side-window reveals, corrected rear trim orientation, window collision and three exterior detail levels. One nearby, time-sliced reflection probe follows campus lighting. Interior arrangement and external dimensions remain provisional. Full-campus geometry, measured fidelity, baked indirect lighting and performance acceptance are still outstanding.

## Version 0.31 rendering milestone

- Ferguson exterior detail levels: 73,194 / 31,018 / 9,930 triangles (collisions remain independent of visible LOD).
- Textured brick and lobby floor no longer receive a second darkening tint in Unity; the arch uses 48 segments.
- Removed the duplicate CampusArtDirection lighting writer. CampusEnvironment now owns sun, ambient, fog and weather response, respects nature-scene ownership, and supports fixed-time verification.
- Architecture player checks verify prepared meshes, secondary UV presence, collision parity, LOD reduction, roughness binding and actual night-light reduction. Captures cover front/rear/entry, dusk/night and interior.
- First stationary exterior sample: Apple M4 Max, 1440 × 900, 300 frames after warmup, vsync on, target 72 FPS: median 27.07 ms, p95 43.35 ms, maximum 148.55 ms. This does not meet a stable 60 FPS target. Campus-wide profiling and frame-time improvements remain open; LOD counts alone are not performance acceptance.
- No new measured building dimensions or verified room plans were obtained in this milestone. Photo-based estimates remain explicitly labeled.
