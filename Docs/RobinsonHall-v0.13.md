# Robinson Hall authored asset

The 0.13 desktop build replaces the procedural Robinson shell with an authored Blender asset:

- `Art/RobinsonHall.blend` contains a four-storey brick academic building, limestone portico and arch, slate roof, dormers, windows, stair flights, seminar rooms, tables, chairs, teaching boards and lights.
- `Unity/Assets/Resources/CampusCraft/robinson-*.bytes` contains the exterior and four streamed interior sections. `robinson.json` records the material groups and explicit box-collider volumes.
- Unity keeps the existing map destination, entrance travel, `E` door interaction and `H` history/media flow. Robinson seminar doors and level signage are added after the authored sections load, so the existing classroom/activity system can use the same stations.

The facade is guided by Albion's public virtual campus tour, the official campus map and the Robinson history entry. The interior is a playable game-scale reconstruction; hidden room dimensions are not presented as a measured survey without permissioned floor plans or a supplied survey.

The packaged build passed the full smoke suite: four-floor Robinson registration and loading, 61 campus destinations, 31 walkable buildings, 33 activity spaces, student navigation, history, courses, snow, audio and save migration.
