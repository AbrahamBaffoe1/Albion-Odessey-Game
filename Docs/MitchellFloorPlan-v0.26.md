# Mitchell Towers reconstruction

The previous five-story single block is replaced by two four-story residential towers. The first-floor lobby joins them; upper floors remain separate. The model preserves 121 numbered bedrooms across the four diagrams (25 on first, 32 on each upper floor), plus the staff apartments, offices, studies, lounges and elevator locations.

## References and what they establish

- [Official floor plans](https://www.albion.edu/wp-content/uploads/2022/02/Mitchell-Towers-Floor-Plan.pdf), all four pages visually inspected: tower arrangement, room sequence, four tower stair cores, first-floor connecting lobby and special first-floor rooms. All pages say **not drawn to scale**. There is no room-dimension table.
- [Official residence description](https://www.albion.edu/offices/community-living/living-on-campus/our-communities/mitchell-towers/): suite-style accommodation with two double rooms connected by a shared bathroom, central recreation/study amenities, and 38 × 80 inch twin beds.
- [Official exterior image](https://www.albion.edu/wp-content/uploads/2021/03/Mitchell-pg-300x206.jpg): brick volumes, pale panels around window bays, a dark upper roof zone and a lower connecting structure. Its small resolution does not support measuring the facade. No photograph is included as a game texture.
- [August 2025 campus map](https://www.albion.edu/wp-content/uploads/2025/10/ac_campus_map_8-25.pdf): towers run north–south along Mingo Street. The source plan is rotated 90 degrees for the game so Mingo is east and the shared lobby faces campus to the west.

## Accuracy limits

The 57.2 × 29.6 m local envelope, 3.2 m floors, room proportions, door locations, stair dimensions and all window bays are provisional. The dark upper exterior is a qualitative material treatment; the exact mansard slopes and openings are not modeled. The diagrams do not show the shared suite-bathroom partitions; those bathrooms and their connecting doors still need reference drawings. The provisional double-room furnishings include two beds, desks with shelves, chairs and three-drawer dressers. Mattress dimensions follow the published 38 × 80 inch specification; other furniture dimensions, placement and special-room occupancy remain estimates. Finishes are still basic.

The additional stair labeled in the first-floor link does not appear on the upper-floor plans. Its rise, destination and relationship to the lobby remain unresolved. Its location is preserved in the plan data as `unresolvedStair`; a flat circulation area in the game must not be mistaken for a reconstructed stair. Four tower stair cores are modeled and separately tested. Secondary tower entrances, elevator cars, kitchen/recreation fittings and exact facade details remain unfinished.

The two staff apartments use joined rectangular regions so their irregular outlines remain enclosed without inventing walls across their living areas. Their internal rooms and furnishings are not complete.

## Verification

`-mitchellPlanSmoke` checks the west lobby entrance, the four stair cores (24 flight traversals), bedroom totals from the source, a physical walk from one tower to the other through the first-floor lobby, and absence of an invented upper-floor connection. Images and results are written to `MITCHELL_OUTPUT`. These checks establish playability, not architectural accuracy.

The macOS build and Mitchell runtime walkthrough passed: 24 stair flights, lobby entry, tower connection, furnished room entry and correct floor lookup. Winter checks confirmed that snow excludes a first-floor bedroom while remaining in the open space between towers. Snow masking now follows each walkable building footprint, including rotated plan-based halls and their courtyard openings. Room lighting is a provisional playability treatment.

The campus collision regression also passed after the expanded towers and furniture were added: long-step wall prevention, starting-overlap detection, obstacle routing and moving-student clearance.
