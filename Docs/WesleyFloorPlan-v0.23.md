# Wesley floor-plan reconstruction

The former two-storey rectangular frontage has been replaced by a four-level,
connected residential complex. Room identifiers, wing relationships, commons,
recreation lounge, bathrooms and six stair-core locations are transcribed from
Albion College's public floor plan. This is a substantial topology correction,
**not a completed exact replica**.

## Source and accuracy

- [Official floor plans and room dimensions](https://www.albion.edu/wp-content/uploads/2022/02/Wesley-Floor-Plan-and-Room-Dimensions.pdf), pages 1–5.
- [Official Wesley photographs and furnishings](https://www.albion.edu/offices/community-living/living-on-campus/our-communities/wesley-hall/).
- The diagrams explicitly state **not drawn to scale**. The runtime's 63.6 × 71.4 m envelope, 3.2 m storey spacing, walls, window bays, grade relationship and stairs are provisional. They must not be used as survey dimensions.
- All 250 entries in the room-dimension table are preserved separately in `Art/Architecture/Wesley-room-dimensions.json`. These measured room dimensions have **not yet been reconciled into the runtime wall layout**.
- The source does not show door swings. Four recessed corner vestibules on each upper level are provisional access geometry, not verified room boundaries.
- Suite bedrooms/common areas are still represented as combined suite spaces. Elevator motion, detailed bathrooms, kitchens, roofs, dormers, interiors, terrain and landscaping remain unfinished.
- Ground/first-floor entrance grades need a site survey. The current exterior stair is a playable connection, not a verified replica of the real entrance elevation.

## Implementation

`Tools/Architecture/build_wesley_plan.py` generates the auditable room/space source
in `Unity/Assets/Resources/CampusCraft/wesley-plan.json`. The runtime derives wall
boundaries and room openings from that plan, preserves stair voids in upper
slabs, constructs switchback flights, and distinguishes open courts from rooms.
Windows occupy real openings in the wall, with solid glazing collision. Static
meshes are batched while moving door leaves and physical barriers stay separate.
The full campus survey and other buildings remain outstanding.

`Tools/Architecture/extract_wesley_dimensions.py` decodes the source PDF's custom
numeric font into metric measurements without substituting diagram pixel sizes.
It requires the original PDF at `work/realism/wesley-plans.pdf` and `pdftotext`.

Run the application with `-wesleyPlanSmoke` and `WESLEY_OUTPUT` set to an output
folder to exercise the front entry, both flights of all published stair connections
(34 flight traversals, including the west-front core starting on the first floor), and the courtyard classification. The test emits
three review images and a result file. It checks playability, not architectural
accuracy.

## Verification result

The macOS build succeeded. The running application passed the entrance, room-access, courtyard and 34 stair-flight checks. The campus realism regression passed wall sweeps, navigation around obstacles, foliage/sky availability and student clearance. These are functional checks; architectural accuracy remains incomplete.
