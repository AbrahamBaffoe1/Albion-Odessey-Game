# Campus fauna — version 0.7

Albion Odyssey now has a living campus population: 18 animated squirrels roam the map around a deterministic habitat graph derived from the campus trees. The population is capped and created once when the exterior campus loads, so a player can visit all 61 destinations without spawning an unbounded number of agents.

Each squirrel is a real runtime 3D model assembled from low-cost meshes: body, head, muzzle, ears, eyes, four legs and a segmented tail. Four coat variants (chestnut, russet, silver and golden) make the group readable at a distance; the golden variant has a small collar. Legs, heads and tails animate continuously with different gait speeds for forage, run, rest and climb activities.

The habitat graph places four points around every authored campus tree. Squirrels choose points from that graph, check ground and capsule clearance, steer around obstacles and remain near the tree network. Resting pauses, short runs and slower forage walks prevent the group from moving as a synchronized flock. No NavMesh query runs per frame. Agents use a bounded sphere cast and a ground ray only while moving.

The feature is cosmetic and safe to ignore: squirrels have no gameplay collider, do not block arrivals, do not alter course or collection rules, and do not affect saved progress. They are visible in the third-person campus view and continue their routine while a player opens the map, character editor or car.

## Verification

The packaged Unity walkthrough checks 18 active squirrels, 432 habitat points, a tree-adjacent spawn, more than two metres of movement in the test window, and continued proximity to the campus tree network. It also checks all 61 campus destinations, third-person character behavior, cars, discoveries, classroom progression, sound cues and save migration. The close-up review frame is `Playtest/20-squirrel-habitat.png` in the version 0.7 output.

The map buildings and tree placement are approximate game geometry derived from the reviewed campus map. The squirrels, behavior, colors and habitat logic are original game content.
