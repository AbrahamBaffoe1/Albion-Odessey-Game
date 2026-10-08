# Albion Odyssey 0.33 — portraits and discovery destinations

## Playable changes

- Store → Portraits (P): choose a bounded PNG/JPEG using the Mac file picker, review it locally, consent to processing, preview the generated result, then explicitly equip it. Signing out clears local portrait textures. Only the approved portrait appears on the title screen.
- Observatory (campus map destination 5): walk through the portico, take the west staircase to the upper gallery and interact with the refractor. Drag or use arrows to aim, scroll to zoom, and select the Moon, Saturn or Orion. The sky is illustrative, not an ephemeris.
- Forest run results → Continue to Whitehouse: an arrival camera introduces a discovery pavilion. Enter, speak to the student guide, and record three habitat notes in the keeper's saved journal. Return via the existing Nature Trails menu.
- The first launch reveal includes an original bronze acorn medallion dropping, thunder, lightning and a splitting transition. Enter/Space/Escape skips it. Reduced motion skips the sequence; sound follows the effects volume/mute settings.

## Portrait activation — pending

OpenRouter was provisioned through Stripe Projects with the user's explicit terms/privacy approval. The user authorized **at most $5 per month across the portrait service**. No paid generation has been run for this release.

Generation is deliberately disabled until server-only `PORTRAIT_OPENROUTER_API_KEY`, `PORTRAIT_MODEL` and `PORTRAIT_BUDGET_CONFIRMED=true` are configured. Before activation:

1. The supplied key was preserved and verified with a $5 monthly reset on October 7, 2026. It is stored in Stripe Projects as `portrait-api-key`, bound to `PORTRAIT_OPENROUTER_API_KEY`. No key is in Unity or source control.
2. Selected `bytedance-seed/seedream-5-0-flash`: its OpenRouter image endpoint reports $0.018 per output image and $0 per input image, with 1K square output and image references supported (verified October 7, 2026). This is below the conservative 25-cent reservation. Recheck pricing before activation.
3. Confirm that billing fees, existing usage and any automatic credit purchases cannot exceed the user's authorized total. Do not buy a credit package above the remaining allowance.
4. Deploy the backend, then test an authenticated consent → generate → preview → equip → reload → remove cycle and two-account isolation using consenting test users.

The backend reserves 25 cents in a PostgreSQL transaction under an advisory lock before each attempt; the shared limit is 500 cents per UTC calendar month, with three attempts per user per day. Failed/ambiguous provider requests retain their reservation. Every paid request also checks `/api/v1/key`: an unlimited key, a limit above $5, an unsuitable reset period or insufficient remaining budget blocks generation. A $5 lifetime key is accepted as stricter than the monthly allowance. The environment flag alone cannot bypass this check.

Uploads are authenticated, bounded to 6 MiB / 16 megapixels, decoded and re-encoded with metadata removed. The original is not stored in the game database. Generated candidates and approved portraits are private to their owner. The upstream processor receives the normalized photo only after consent. This feature makes a profile portrait, not a scan or a reconstructed 3D face.

## Source and fidelity notes

- Albion observatory history: https://www.albion.edu/departments/physics/observatory-history/
- Public observing: https://www.albion.edu/departments/physics/public-observing/
- Campus facilities: https://www.albion.edu/departments/physics/campus-facilities/
- Moon texture: NASA's Scientific Visualization Studio, CGI Moon Kit, Ernie Wright / LROC WAC data. https://svs.gsfc.nasa.gov/4720/ — `lroc_color_2k.jpg`, saved unchanged as `Presentation/MoonLROC.jpg`. This is an aesthetic visualization texture, not scientific measurement data.
- `Presentation/AcornMedallion.png` is original AI-generated artwork made for this game. Thunder is an original synthesized sound.
- The supplied Final Fantasy XVI and Elite Dangerous images/videos guided spacing, serif titles, panel hierarchy and transitions. Their characters, logos, footage and store art are not shipped in the game.

Observatory massing and the eight-inch Alvan Clark refractor follow public references. Interior dimensions, the dome geometry and nature-center pavilion remain interpretive reconstructions; this release is not a measured architectural replica. Planet sizes/bearings and constellation placement are educational approximations.

## Verification

- Backend: `npm test --prefix Backend/Realtime` (26 passing tests, including spending safeguards, consent, image bounds, preview ownership and failure behavior).
- Unity build: `bash Tools/unity_mac.sh build` (also rebuilds the arm64 Mac file-picker plugin from source).
- `-journeySmoke`: isolated save, observatory doorway/wall collision, telescope control handoff, arrival handoff, journal/studio screens, and medallion sequence.
- `-storeSmoke`: existing purchase, equip, save/reload and rollback checks.
- Paid end-to-end portrait generation is **not yet verified**. Sign-in and the key spending limit are verified; the account has $0 credits and requires private billing-address entry before funding. No purchase was made. Generation remains disabled pending funding and deployment verification.

## 0.33.1 title update

The title underline is removed. The menu now renders a live GPU nebula, three parallax star layers and occasional meteors. Original AI-generated squirrel cutouts flank the title, with independent UV-deformation animation for breathing, head movement and tail sway. These are animated 2D characters, not rigged 3D animals. Reduced motion freezes the scene. Rendering is capped at 30 updates per second and 1600 pixels wide, and runs only while the title is drawn.

The console UI smoke capture includes two title frames to inspect motion and continues to verify menu routing and invalid-code feedback.
