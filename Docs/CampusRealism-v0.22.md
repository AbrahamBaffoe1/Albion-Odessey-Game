# Campus realism correction build

This is a defect-fix and reference-gathering build, not an exact architectural replica.

## Changes

- Campus batching now preserves every submesh/material. Previously only submesh zero survived, which removed the oak foliage while leaving the trunks and branches.
- Photographed CC0 daylight sky and ground textures replace the simple sky and previous rocky ground. Grass is tinted for a greener lawn. These are generic environment assets, not photographs taken at Albion.
- Roads now follow bounded segments traced from the official August 2025 campus map. The previous implementation extended each road across the whole map, including places with no road. Added sidewalks. Map scale remains schematic.
- Exterior student destinations move from building centres to approach points. Unity's navigation module builds traversable paths around authored colliders. A live capsule sweep, initial-overlap check and destination-overlap check guard each step, including changes after the navigation mesh was built. Unreachable routes stop or choose another destination rather than crossing solid walls.
- Clothing meshes are welded and relaxed with cuffs/boundaries preserved; covered skin faces are removed to reduce skin poking through clothes. The runtime character has a narrower silhouette. These remain stylized characters from the existing CC0 Quaternius kit, not photoreal humans or a diverse new character roster.

## Evidence and limits

Official campus map: https://www.albion.edu/wp-content/uploads/2025/10/ac_campus_map_8-25.pdf

Official Wesley floor plans and room dimensions: https://www.albion.edu/wp-content/uploads/2022/02/Wesley-Floor-Plan-and-Room-Dimensions.pdf

The public floor plans show the room relationships and published room measurements but explicitly state "Not Drawn to Scale". The current simplified Wesley shell does not match the full complex's topology. It needs replacement using dimensioned elevations and a full plan reconstruction; it is not now an exact replica. Most other halls remain approximate. Unseen elevations, actual terrain, parking details, planting species and positions, full interiors, and all-building dimensions are not established by the available references. A photo cannot establish geometry hidden behind its visible surface.

Environment provenance is recorded in Art/RealismAssetProvenance.json. Relevant asset pages:
- https://polyhaven.com/a/kloofendal_48d_partly_cloudy_puresky
- https://polyhaven.com/a/grass_ground

## Verification

Run Tools/unity_mac.sh build, then the built player with -realismSmoke and REALISM_OUTPUT set to a writable output directory. The test checks retained leaves, sky material, large-step and initial-overlap wall rejection, actual routing around a test wall, and live exterior student clearance over 120 frames, with in-game campus and student captures. The existing craft and housing tests cover door traversal, stairs and character animation.

The movement fix covers simulated campus NPCs. Online peer interpolation and multiplayer server authority remain separate systems; this build does not claim that arbitrary remote clients cannot cheat or teleport.

Verified locally: Unity Mac build, realism/navigation smoke, main building craft smoke, and Wesley housing smoke all passed. No claim of full architectural accuracy or photoreal student replacement is made.
