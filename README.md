# Albion Odyssey

Albion Odyssey is a living campus adventure about learning, belonging and making a place your own.

The current Unity campus build includes a production wildlife layer: 18 animated squirrels use a 408-point habitat graph around the authored campus trees. They forage, sprint, rest and climb, with obstacle-aware movement and four coat variants. See [the fauna notes](Docs/CampusFauna-v0.7.md) and [verification record](Docs/Verification-v0.7.md).

The game begins at Legacy Hall, where a new Keeper discovers the stories held in its rooms. From there, the campus opens into a walkable world: historic buildings, study spaces, paths, gardens, student activity and the people who give a college its rhythm. Every building is an invitation to look closer. A doorway leads to a room, a room leads to a story, and a story gives the player a reason to return.

## The vision

Albion Odyssey brings together two kinds of play. One is grounded in the real character of Albion College: recognizable landmarks, public history, campus traditions and spaces designed for learning. The other is imaginative: a Keeper can shape a personal campus, choose a visual style, create a building and turn it into a classroom, library, gathering place or new chapter of the story.

The result should feel like a place rather than a menu. Students can tour, explore and learn at their own pace. Players can collect memories, meet campus characters, build spaces, create courses and invite other people into a shared world. The campus is both the setting and the game system.

## What players do

- Explore Legacy Hall, Ferguson Hall, Robinson Hall, Bonta Admission Center and the growing Science Complex through walkable entrances, rooms, stairs and history interactions.
- Read the story of each place, open linked college media and discover how architecture, people and campus traditions connect.
- Move across the campus on foot or by car, follow paths through landscaped grounds and encounter student NPCs travelling between destinations.
- Create a personal campus in either a campus-inspired or fantasy style, then furnish its plots and make space for learning.
- Create courses with schedules, enrol Keepers, assign simulated students, teach a session and complete lessons.
- Shape a Keeper with saved appearance, collection progress, sound feedback and an accessible control scheme.
- Host or join a small shared LAN campus, with presence, movement snapshots, chat filtering and host moderation.

## A campus built to grow

The building system is deliberately reusable. Doors, stairs, floors, rooms, signs, furniture, history panels and proximity loading are shared components, so each new hall can receive its own architecture without losing the consistent feel of the world. The Science Complex is the next architectural pattern: four connected wings around an atrium, with teaching laboratories, research spaces, collections and public circulation.

The world is authored in Blender and played in Unity. Materials, collision and character animation are part of the same pipeline. Public campus references guide placement and visible character; game-scale reconstruction keeps the world playable while individual halls are refined.

## The experience we are making

Albion Odyssey is meant to be calm enough for a self-guided tour, playful enough for discovery and deep enough to support a long-running shared campus. History is encountered through movement. Learning is expressed through rooms and schedules. Building is a form of authorship. Multiplayer is a way to turn a campus into a community.

The current development path is to finish the major academic halls one by one, improve the daily life of the campus, expand courses and activities, harden shared-world persistence and moderation, then bring the experience to tested VR hardware and signed releases.

## Project resources

- [Player guide](Docs/PlayGuide.md) — controls and the first ten minutes.
- [Game design](Docs/GameDesign.md) — the world model, learning loop and Keeper progression.
- [Next milestones](Docs/NextMilestones.md) — the current build order.
- [Public campus research](Art/PublicCampusResearch-2026-09.md) — sources and evidence used for reconstruction.
- [Asset credits](Art/ASSET-CREDITS.md) and [asset provenance](Art/AssetProvenance.json) — Blender, texture, character and audio attribution.
- [Developer guide](Docs/DeveloperGuide.md) — Unity setup, builds and verification for contributors.

Albion Odyssey is an original game project inspired by the experience of learning and living on a college campus. The real college references inform the setting; the Keeper, personal campus, fantasy layer and game systems are original work.
