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

Keyboard, pointer buttons, controller input and the XR action layer continue to use the same interaction methods. The HUD pauses behind history, course, builder, journal and launch panels, so those screens can be given the same card treatment in the next pass without duplicating gameplay state.

## Next UI slice

The next pass should apply this theme helper to `CampusLife`, `CampusTour` and `BlueprintStudio`: shared typography, selected states, animated open/close transitions, and larger controller focus targets. The information architecture is already kept separate from the HUD so those panel changes remain safe to iterate.
