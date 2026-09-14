# Escape menu repair — 0.16.1

The closed VR panel previously interpreted the shared Cancel input (including
Escape) as an instruction to open itself, before the campus menu could handle it.
It now handles input only when explicitly opened with F8 or through the menu.

Escape and the on-screen MENU button open a dedicated campus menu. VR settings
use the same modal ownership as the other panels, hiding the gameplay HUD and
restoring the previous panel, cursor and movement state on return. Both screens
have explicit backgrounds and text colors; VR no longer renders pale labels on
white buttons or shows headset-only controller instructions on desktop.

The Mac player's isolated `-shellSmoke` check passed on September 14, 2026:

- A closed VR panel does not consume Back/Escape.
- Opening the campus menu stops player movement and releases the cursor.
- Explicit VR navigation opens the settings panel; Back restores the campus menu
  without resuming player movement behind it.
- F8 toggles VR settings and returns correctly to gameplay.
- Resume, accounts and main-menu routes work.
- Existing launch, story/video, summary, failed-save recovery and resume checks pass.

Rendered screenshots `Pause-menu.png` and `VR-comfort-menu.png` were visually
reviewed at 1280×800. Physical controller/headset checks were not performed.
The session test uses isolated saves and does not modify the player's campus.
