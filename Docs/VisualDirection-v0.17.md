# Albion Odyssey 0.17 — UI and presentation

This update establishes a shared presentation system for the Unity game. It is an implemented, playable interface pass; it is not a claim that the campus or character art matches Fortnite production quality.

## Reference direction

- [Epic: Custom lobbies in Fortnite](https://www.fortnite.com/news/introducing-custom-lobbies-for-games-in-fortnite), published October 10, 2023: persistent navigation, a visible player character, and a clear route into play.
- [Epic: Designing for mobile](https://dev.epicgames.com/documentation/fortnite/designing-for-mobile-in-fortnite): one primary focus, readable controls, clear feedback, reduced visual clutter and fast loading.
- [Celia Hodent, former Epic UX director, discusses Fortnite](https://arstechnica.com/video/watch/10-things-you-might-not-have-noticed-in-fortnite/): relevant controls and feedback reduce what the player must remember.

These are references for hierarchy and interaction. No Fortnite logos, character models, screenshots or other game assets are shipped in Albion Odyssey.

## Implemented

- A character-led lobby with Explore, Build, Learn, Your student and Account navigation, one gold Enter campus action, and routes into Ferguson Hall, films and session saving.
- An actual rigged student rendered into the lobby and appearance screen, matching the saved outfit, skin, hair and backpack. Its idle animation and gentle rotation respect reduced-motion preferences.
- A backdrop rendered from the existing 3D campus, rather than a promotional landscape illustration.
- Six actual startup milestones: save reading, architecture, player preparation, campus construction, campus life and the live showcase. Progress counts completed stages; it does not pretend to measure bytes or remaining seconds. Existing synchronous construction still pauses animation within its longest individual stage.
- A rendered loading frame before the building studio and student screen transition. Account requests and media loading have busy indicators. Startup failures offer a quit route without writing a partially initialized save.
- Redesigned Escape menu, student appearance and session summary. Shared rounded controls, hover/focus feedback, menu selection sound, and high-contrast primary actions extend into account, course and history screens.
- A quieter gameplay HUD: location and next discovery, compact map, contextual interaction and a small energy/collection panel. The four on-screen movement buttons remain available with O.
- Existing normal maps enabled on character skin and eye materials, restrained material gloss, warm daylight, cooler snow lighting and atmospheric haze.

The palette is deep navy, white, mint feedback and gold primary actions. It uses the game's existing fonts and assets. Secondary legacy panels retain some of their earlier layouts; they are not all fully redesigned yet.

## Verification

The new `-presentationSmoke` player check observes monotonic real startup stages, checks the rigged portrait and campus render textures, captures lobby/student/HUD/menu/account/summary views, verifies transition completion and input ownership, and captures the lobby at 1024 × 768.

The five existing player integration suites cover building craft, combined studio/game operation, campus gameplay, history/media navigation, and shell/save/VR menu routing. All five suites passed on the packaged Mac player, as did the new presentation check. See [recorded results and screenshots](Verification-v0.17/presentation-checks.json). The Quest APK compiled successfully; it has not been tested on a physical headset.

## Art work still required

The current student is the existing rigged campus asset, not a newly sculpted production character. Diverse body and clothing meshes, refined anatomy, hair cards, facial expression and conversation clips need authored assets and review. The campus still needs terrain and road blending, foliage variation, improved building details, surface wear, lighting benchmarks and measured LOD budgets. These are separate from the UI improvement. Physical headset testing is also still required; a successful Quest compilation is not a headset playtest.
