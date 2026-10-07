# Whitehouse Hall reconstruction

The generic rectangular hall is replaced by a stepped four-level plan with a west return, an offset east wing, three stair cores, and floor-specific residential and service spaces. The Porter Street entrance opens onto the ground level. The third-floor grey block above room 312 is kept as an unlabelled closed area, not invented as a bedroom.

## Evidence

- [Official Whitehouse floor plans and room dimensions](https://www.albion.edu/wp-content/uploads/2022/02/Whitehouse-Floor-Plan-and-Room-Dimensions.pdf), all five pages visually inspected. Pages 1–4 explicitly say **not drawn to scale**.
- [Campus map, August 2025](https://www.albion.edu/wp-content/uploads/2025/10/ac_campus_map_8-25.pdf): west return toward Baldwin, Porter Street to the south, former Hannah Street/current Ditzler Way to the west.
- [Historical exterior photos](https://isaackremer.com/albion/buildings/ac_whitehouse/): the southwest view shows brick walls, a pale central frontage, pitched roof and central pediment. The covered-walkway photo shows tall square masonry supports and steps at the Baldwin connection. The page attributes those views to Isaac Kremer, 1999–2001; they do not establish current conditions. No source photograph is bundled as a texture.

The raster dimensions table was manually transcribed into `Art/Architecture/Whitehouse-room-dimensions.json`. All 106 entries are retained, including the source's different apostrophe/asterisk separators. The diagram contains room 226 but the table omits it; its runtime dimensions remain explicitly estimated. The 106 published length/width pairs now drive the room wall positions. They are interpreted as clear rectangular dimensions; the table does not describe its measurement method, closets or wall construction. This fit is not an as-built survey.

## Unresolved accuracy

Room rows now use each published length and width instead of the former repeated grid. A provisional 0.18 m wall allowance is added between clear room rectangles; corridors, common spaces, structural infill, floor heights, window bays, roof pitch and building placement remain estimates. Variable room lengths create corridor recesses while exterior wall lines stay aligned. These inferred recesses require architectural plans to verify. The east and middle stair diagrams shift between pages. The model registers the three shafts vertically, preserving room order while normalizing those schematic offsets; that registration requires architectural drawings to confirm. The fitted envelope is approximately 61.301 × 33.556 m, but remains a consequence of these assumptions, not a measurement of the hall. Roof spans and exposed lower roofs follow that fitted plan.

The current room furniture and finishes are simplified. Elevator machinery/cab, side exits, detailed bathrooms and kitchen fittings, precise pediment and facade dimensions, and the covered Baldwin connection remain unfinished. Roof junctions and changes in footprint need further exterior evidence. Room door positions are inferred from corridor adjacency, not measured door schedules.

## Verification

The pre-fit nominal rectangles differed from the table by up to 2.2004 m on one axis, with mean absolute axis error 0.13938 m across the 106 rooms. `Art/Architecture/Whitehouse-before-fit-errors.json` records the comparison.

`-whitehousePlanSmoke` casts rays against physical wall and finish colliders to check both axes of all 106 measured rooms (3 mm tolerance), then exercises the Porter Street entry, all three stair cores through four levels, room-access adjacency, and the distinction between the second-floor single and third-floor closed area. It writes review images to `WHITEHOUSE_OUTPUT`. Functional checks do not prove an exact replica.

The macOS build passed. The entry and all 18 stair flights passed runtime checks. The campus collision regression passed long-step wall prevention, overlap detection, obstacle routing and moving-student clearance after the expanded hall was added. The model contains the 107 bedrooms shown on the four floor diagrams; 106 have entries in the published dimension table.

The fitted build passed all 106 physical-wall dimension checks; maximum numerical deviation from the interpreted table values was 0.00081 m. This is implementation precision, not evidence that the source measurements or full building are accurate to a millimetre. The entrance and 18 stair flights also passed after the resizing.

The campus collision regression passed on the fitted build: long-step wall prevention, overlap detection, obstacle routing, and moving students staying outside solid geometry.
