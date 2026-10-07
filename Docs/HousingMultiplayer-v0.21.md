# Campus trail and housing reconstruction — local development build

The online room client now displays a cooperative campus trail. Anyone in the room can visit the entrance approaches at Ferguson, Seaton and Wesley; the server records those visits and sends the same progress to every participant, including late arrivals. Progress lasts while the room remains occupied and resets when the last participant leaves. This is a casual exploration activity, not a competitive or cheat-resistant game: movement remains client-reported. It does not synchronize construction, vehicles, door states or inventories.

Open F7 to sign in and F5 to join the same room code as friends (up to 16 players). The existing room system provides remote avatars, movement and gestures. The room roster now scrolls to show all participants. Authentication failures stop automatic retries. Shift+F5 retains the existing local-network mode; the new trail belongs to the online room protocol.

## Housing and rendering

Wesley's historic frontage now has six white portico columns, an entablature with dentils, pitched roofing, four dormers, chimneys, sash-window divisions, an arched entrance surround, hedge beds and mature trees. The visual reconstruction is based on the official exterior photograph, viewed on 2026-10-04:

- https://www.albion.edu/wp-content/uploads/2021/03/wesley.jpg
- https://www.albion.edu/offices/community-living/living-on-campus/our-communities/wesley-hall/
- Campus placement reference: https://www.albion.edu/wp-content/uploads/2025/10/ac_campus_map_8-25.pdf

The model depicts the two main facade levels and dormered roof visible in that photo. It does **not** represent the complete four-story residential complex or its later wings. The footprint is 36 × 24 game meters; those dimensions, orientation, rear elevations, landscaping placement and interior layout are approximations, not surveyed measurements. Other halls retain their existing approximate masses. This is a recognizable architectural reconstruction, not photogrammetry or a claim of photorealistic accuracy.

Shared window construction is corrected throughout the walkable building system: panes are vertical, frames surround rather than cover the glass, and muntins define the sashes. Textured boxes use consistent meter-based UVs rather than stretching a single texture across each wall. Static decorative surfaces are combined by material to reduce draw calls; animated doors remain separate.

## Verification and release state

- Backend tests: seven passing, including shared visits, late join, room isolation, invalid elevated visits, empty-room reset, authentication, capacity and malformed movement.
- Unity macOS build completed successfully.
- `-housingSmoke` verifies six columns, vertical glazing and actual controller movement through the open Wesley entrance and up to its second floor, then saves an in-game render.
- Existing main-building gameplay integration passed: three floors, 19 doors, avatar skin deformation, interior streaming and history.
- The standalone .NET compile harness currently lacks the existing XR Hands references; the full Unity build resolves those packages and compiles successfully.
- Existing hosted `/health` responds successfully with capacity 16. Its mail sender reports `mailConfigured: false`. No real two-account internet gameplay test was performed.
- Backend changes in this checkout have **not** been published. The cooperative trail requires deployment of `server.mjs` together with `campus-trail.mjs`. The client tolerates older snapshots without trail data.
- Stripe Projects successfully refreshed the Render provider link. Its catalog does not support adopting the existing hosted service; no duplicate service was provisioned.

## Reproduce

Run `npm test --prefix Backend/Realtime`, then `bash Tools/unity_mac.sh build`.
Run the Mac player with `-housingSmoke` and set `HOUSING_OUTPUT` to a destination directory to capture the housing check and render. Run `-craftSmoke` with `CRAFT_OUTPUT` for the existing main-building integration check.
