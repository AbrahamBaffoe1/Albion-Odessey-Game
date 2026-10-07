# Ingham reconstruction

The former generic sealed map box now has a dedicated exterior implementation in `InghamExterior.cs`. Its front faces west toward Ingham Street. The raised porch, pale round columns, bracketed cornices, grouped front windows and central three-window gabled dormer follow the official photograph at https://www.albion.edu/wp-content/uploads/2021/03/ingham.jpg. The roof geometry contains an opening for the dormer, preventing roof planes from covering its glazing.

This is an incomplete reconstruction. The catalog's estimated footprint is retained. Column count/spacing, roof pitch, elevations, rear/side windows and measured façade geometry remain unverified. A walkable interior is not implemented; the house still has a sealed collision shell. It must not be described as an exact replica or as having modeled rooms.

`extract_ingham_dimensions.py` produces eight source measurements from the official housing page. The two supplied diagram floors have eight bedrooms and four bathrooms in total. The stairs include turning/winder geometry and continuations beyond the supplied levels. The webpage occupancy total and diagram occupancy labels conflict. Do not resolve that conflict by silently deleting or reassigning a room.

The exterior smoke check verifies west-facing orientation, presence of the dormer and approach steps, and captures the actual runtime rendering. It does not verify architectural dimensions, interior circulation or measured stair geometry.

## Staged room fit

`build_ingham_plan.py` now generates `Art/Architecture/Ingham-staged-plan.json`: two floors containing all eight measured room rectangles, the living room, four provisional bathroom rectangles and a reserved central stair area. It checks dimension pairs, footprint containment and pairwise overlaps. The model preserves the broad published room arrangement, but room-axis orientation, entry projection, alcoves/notches, common-space sizes and all circulation geometry require further work. The eight published room sizes do not uniquely determine these details.

The staged plan deliberately is not a runtime Resource and has `runtimeReady: false`. A future integration must replace the sealed shell, model the actual turning/winder stairs, provide source-consistent doors and verify player traversal. The current 10.5 × 9 m exterior estimate differs from this room-fit envelope; do not place the interior inside that shell without reconciling the envelope first.

The staged plan now stores explicit doorway sides for all eight bedrooms and four bathrooms. Estimated offsets/widths are marked as such. A 0.05 m grid connectivity check with a 0.30 m clearance radius reaches all 12 doorway approaches through common circulation; this excludes stair traversal and is not a Unity controller test. The reserved stair area moved slightly to retain the first-floor bathroom approach. Its location and dimensions remain provisional.

Official living-room and bedroom images were inspected and recorded in `Ingham-interior-evidence.json`. Neither reveals the full staircase. The bedroom image annotates 12′8″ × 15′2″ but supplies no room number; it must not silently override a numbered room's published table entry.

## Isolated 3D verification

`InghamInteriorSmoke` constructs the staged plan away from the campus only when launched with `-inghamInteriorSmoke` and an explicit `INGHAM_PLAN` path. Shared plan rendering now accepts explicit door positions and derives the top floor/roof from the actual floor count, rather than assuming four levels. Existing halls retain their default door-selection behavior.

The first controller run rejected the estimated 0.80 m doors. The actual player capsule radius is 0.42 m. No player or published room dimensions were changed. Provisional doors are now 0.95 m wide and the reserved stair area was shifted to retain a clear bathroom approach. These opening widths and offsets are not published measurements and need confirmation. The 2D clearance check now includes a 0.42 m radius plus 0.09 m half-wall allowance, superseding the earlier 0.30 m check.

Latest isolated runtime result: eight room clear-dimension pairs raycast against built physical walls, maximum numeric discrepancy 0.00083 m; twelve closed-door blocking checks and twelve open-door controller traversals passed; roof closes the second level. This is a rectangle-fit validation, not a building survey. The stage uses provisional 3.2 m floor heights, unfinished materials, no furniture or stair flights, and has not replaced the live sealed exterior. Actual room alcoves, the entrance projection, stairs, concealed levels and exterior reconciliation remain incomplete.

## Living-room visual reconstruction

The isolated stage now includes the documented green fireplace surround and hearth, pale mantel trim, round brass-colored mirror frame, terracotta paint, base/crown molding, carpet, sofa, two armchairs and two pendant fixtures. A three-window group replaces the blank front living-room wall. Paint panels follow actual wall spans so they do not cover the window openings. Dimensions, placement, material response and furniture shapes remain approximations; the mirror is a tinted surface, not a reflection capture. This stage is not installed in the live campus and does not establish photorealism or surveyed accuracy.
