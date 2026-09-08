# Campus weather · v0.8

Albion Odyssey now has a seasonal snowfall pass for the exterior campus. The clear state is the default. After a few minutes the weather can transition automatically, and the player can press **Y** at any time to start or clear snow while exploring.

Snowfall is made from three visual layers:

- A soft blanket covers the campus ground and paths.
- Falling flakes follow the Keeper through a bounded particle volume.
- Low snow caps settle on authored building roofs.

The weather layer is visual-only. The ground blanket, roof caps and flakes have no colliders, so the existing walkable buildings, doors, stairs, cars, squirrel habitats and safe-arrival checks continue to use the same collision map. Snow is shown while the Keeper is on the Albion College exterior campus and clears indoors or in Legacy Hall.

The smoke walkthrough forces a snow state, verifies the blanket and active flakes, and captures `21-campus-snow.png`. The release package includes that screenshot as a quick visual check.
