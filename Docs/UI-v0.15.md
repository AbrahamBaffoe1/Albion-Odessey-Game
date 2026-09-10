# Albion Odyssey UI v0.15

The gameplay interface now has a consistent Albion visual language: ink-blue panels, Albion gold actions, purple utility controls and cyan navigation accents. Cards preserve the view of campus instead of covering it with a debug wall.

The live HUD includes:

- Current building or campus location and Keeper status.
- A compact compass and objective card with memory and Beacon progress.
- Energy feedback while sprinting and speed feedback while driving.
- Contextual world tags for memories, Pip and classroom activities.
- One readable interaction prompt with the current action.
- Animated achievement and notice cards with the existing collection sounds.
- Responsive scaling for 16:9, ultrawide and headset preview resolutions.
- Safe-area insets for notches, camera cutouts and headset compositor margins; HUD cards, prompts and touch controls stay inside the readable viewport.

Keyboard, pointer buttons, controller input and the XR action layer use the same interaction methods. The HUD pauses behind history, course, builder, journal and launch panels, which now share the same card treatment without duplicating gameplay state.

The social layer now uses the same visual language: seated learners and lunch groups show short conversation bubbles while they talk, club players show a playful prompt while paused, and all bubbles face the active camera and disappear outside a readable distance. The VR walkthrough and shared-campus panels expose their focused action with a gold-arrow cue and show the same stick/trigger/menu footer used by the in-world controls.

## Typography and input pass

Headings use a cached display face (`Avenir Next Condensed` when available) and body/control copy uses a readable system face (`Helvetica Neue` when available). Quest and other devices fall back to Unity's bundled Arial font without changing layout. The same font roles are applied to the HUD, history, courses, builder, journal, accessibility, VR and shared-session panels.
