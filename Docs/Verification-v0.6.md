# Albion Odyssey v0.6 verification

Tested September 7, 2026 on this Mac using Unity 6000.6.0f1.

- C# player build: passed, no C# compiler errors or warnings.
- Existing logic suite: passed 100,000 economy transactions and 10,000 classroom transactions, including isolation, save migration, ownership, capacity, and sound feedback.
- Actual Mac player walkthrough: passed in 22.41 seconds.
- All 61 named campus models exist, and every travel arrival has clear standing space and ground support.
- Visible third-person avatar, character appearance save/readback, and Keeper isolation passed.
- Third-person interaction can target Pip without hitting the player's own collider; camera pulls inward before a test wall.
- Car entry, acceleration, reverse, braking, stopped exit, collision barrier, and menu pause passed.
- Seven discovery cards collect and persist.
- Existing eight-floor ascent, seven stair descents, classroom access, student roster, course completion, journal, charter, builder and all 20 sound assets passed.
- Screenshots of campus, character settings, walking, driving, discovery journal and map were visually reviewed.

These are automated runtime checks and a visual review, not a prolonged human usability test. New campus exteriors are approximate models. Real building interiors, the off-map boathouse, multiplayer and high-detail character art remain outside this release.
