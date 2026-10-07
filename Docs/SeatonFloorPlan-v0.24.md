# Seaton Hall reconstruction

Seaton's former 30 × 11 m rectangular shell is replaced by a four-level L-shaped
plan. The Cass Street frontage faces north; the return wing lies on the west,
extending south toward Baldwin. The public plan supplies room sequence,
bathrooms, storage, lounges, laundry, staff spaces and three stair cores.

## Evidence

- [Official floor plans and dimensions](https://www.albion.edu/wp-content/uploads/2022/02/Seaton-Floor-Plan-and-Room-Dimensions.pdf), pages 1–5.
- [August 2025 campus map](https://www.albion.edu/wp-content/uploads/2025/10/ac_campus_map_8-25.pdf), for orientation. The older plan labels the west road Hannah Street; the current map labels this segment Ditzler Way.
- [Archived exterior and entrance photographs](https://isaackremer.com/albion/buildings/ac_seaton/), attributed on that page to Albion College Archives. Images inspected in-browser: `seaton_hall_1.jpg`, `seaton_hall.jpg` and `seaton_hall_2.jpg`. They show three exposed main storeys, a light cornice, four entrance supports and a short flight of entrance steps. Combined with the four-level floor plan, this supports a lower ground level mostly below grade. These are historical references, not a current photo survey. No source photograph is bundled as a game texture.

- [Official Seaton room photograph](https://www.albion.edu/wp-content/uploads/2023/01/Seaton-Room-scaled.jpg), used to inform cream interior walls and wood furniture finishes. The furnishings remain simplified placeholders, not replicas of the photographed room.

## Accuracy limits

This is **not an exact architectural replica**. The source diagrams state
“Not drawn to scale.” The 60.6 × 29.4 m envelope, 3.2 m storeys, -2.4 m ground
floor elevation, window bays, stair dimensions, door placement and furniture
positions remain provisional. The 1.5 m westward placement correction prevents
the enlarged provisional shell touching a neighboring placeholder building;
site positions still require a geographic survey.

The entry now rises 0.8 m above surrounding ground, instead of the previous
3.2 m test staircase. That reflects the photographs qualitatively; neither the
0.8 m elevation nor five risers are measured facts. Exterior detailing and
current conditions remain unfinished. The Baldwin connection is not yet built.

`Art/Architecture/Seaton-room-dimensions.json` preserves all 120 table entries,
including a storage entry without measurements. Eight entries have ambiguous
unit marks (rooms 11, 15, 16, 39, 40, 41, 105 and 138); those values are left
unconverted. The published measurements have not yet been reconciled into the
runtime walls. The third-floor drawing's unlabelled area above room 347 remains
explicitly unlabelled in the model.

## Playability

The terrain and snow blanket exclude the actual L-shaped footprint so the
lower ground floor remains accessible. The open side of the L retains terrain.
Room doors beside a stair open onto its landing, not into the middle of a
flight. The same plan renderer serves Wesley, with each building's orientation,
entrance and stair directions retained in its data.

`-seatonPlanSmoke` checks four levels, three stair cores, 18 flight traversals,
Cass Street entry, room access, a walk from a stair landing into room 43, and
the outdoor side of the L. It writes images and a result to `SEATON_OUTPUT`.
Functional checks do not establish architectural accuracy.

## Verification

The macOS Unity build succeeded. The final Seaton runtime check passed all 18 flight traversals, entry and end-room access, terrain clearance, and snow clearance. A Wesley regression passed all 34 flight traversals after the shared stair and door changes. Interior review confirmed that the external brick no longer appears on Seaton room walls and the bed and desk placeholders have supports.

The campus collision regression also passed after the footprint change: long-step wall prevention, starting-overlap detection, walking around an obstacle, and moving students remaining outside solid geometry.

## Furnishing correction — 4 October 2026

The official Seaton amenities list specifies a 38 × 80 inch XL twin bed, desk with shelf, chair and three-drawer dresser for each resident. Standard unmarked rooms now receive two resident furniture sets; source-labelled S rooms receive one. RA/H room occupancy is not established by this source, so their existing single-set treatment remains provisional. Furniture arrangement, frame/desk/dresser dimensions, closet construction and finishes remain unverified. This change does not resolve the room-size discrepancies identified in the separate measurement audit.

Source: https://www.albion.edu/offices/community-living/living-on-campus/our-communities/seaton-hall/

### Source conflict found during metric fitting

Page 5 visually prints room 232 length as 23′9″, whereas the PDF embedded text extracts 13′9″. The transcription now retains both readings, leaves its numeric length unset, and excludes that room from dimension comparisons pending confirmation. Do not resize the room from either value automatically. This is separate from the eight entries with ambiguous unit marks and the Storage entry. There are now 110 unambiguous numeric room pairs available for comparison. No room geometry was changed by this source correction.
