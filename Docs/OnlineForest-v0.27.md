# Online accounts and Briton Treasure Run

The account destination is Neon project `winter-pine-85132555`, database `albion_odyssey`, production branch `br-royal-fog-b5az2ver`. It was created through the Neon web console using the owner's existing GitHub sign-in. The project belongs to the existing Scale organization: compute is capped at 0.5 CU with a 300-second idle suspension. No new subscription was purchased.

Neon Managed Better Auth handles OTP generation, verification, sessions and revocation. Render exposes a compatibility API for the Unity email-code flow and stores game profiles in Neon PostgreSQL. The DB URI is a Render secret; it never ships in Unity. Every profile operation revalidates the Neon session and restricts reads and updates to the session owner. Unverified emails cannot join multiplayer. Signup and sign-in both use Neon's passwordless OTP flow; the Create account choice is retained for familiarity, but Neon creates a missing account only after OTP verification. Existing Supabase data has not been deleted or migrated. Returning players will initially have a new Neon profile.

Hostinger sender: `Albion Odyssey <abraham@nexoralab.net>`, `smtp.hostinger.com`, SSL port 465. The mailbox password must be saved privately in Neon Settings → Auth → Email provider. Neon then sends account messages directly through Hostinger; Render's free-tier SMTP block does not affect this path. `NEON_SMTP_READY=false` deliberately prevents new code requests until that setup is finished. Do not interpret a healthy multiplayer process as verified email delivery.

## Forest game

Sign in with F7, join the same room with F5, then choose Forest Treasure Run or press F6. A 5-second shared countdown begins a 1,200-meter course. Arrow keys/A/D change lanes, Space jumps logs, Down/S slides under branches, and R spends a shared rescue charge to restore a caught teammate. Rocks must be dodged. Green restoration seeds grant one rescue charge per three pickups; collecting nine increases recovery distance from the mascot. Gold treasures are shared once across the group. Late joiners catch up to the group with temporary collision grace. Scores reset between runs and when the room empties; they are not a persistent economy.

The server owns distance, obstacle encounters, treasure, captures, rescues and race completion. This is separate from casual campus exploration, whose position is still reported by clients. It is not a fully authoritative campus physics server. Door states, construction and vehicles are not synchronized.

The course is original game fiction inspired by Whitehouse Nature Center's woodland, marsh and river trails, not a geographic replica. The mascot is an original stylized Briton interpretation. Existing authored oak meshes and photographic ground/sky assets are reused. The course scenery was redrawn in v0.30 with a shared nature look (see [NatureLook-v0.30.md](NatureLook-v0.30.md)); the mascot still needs art refinement, and no photorealism claim is made.

Sources: https://www.albion.edu/about/our-campus/whitehouse-nature-center/ and https://www.albion.edu/wp-content/uploads/2021/09/whitehouse-nature-center-trail-map-1.pdf

## Verification

`npm test --prefix Backend/Realtime` tests the room protocol, the authoritative runner, profile authorization, invalid/unverified sessions, OTP resend limits and email configuration failure. Two local WebSocket clients verify shared race state and disconnect cleanup. These use injected test authentication and do not prove live email delivery.

`node Backend/Realtime/verify-neon-schema.mjs` initializes the real Neon profile schema and checks that the live Neon auth service rejects an invalid session; it never prints credentials.

`bash Tools/unity_mac.sh build` builds the Mac client. `-forestSmoke` with `FOREST_OUTPUT` captures a rendered fixture and checks that campus controls are locked during the run and restored on exit. This is explicitly a visual test, not live multiplayer or email verification.

Live delivered-email signup, returning sign-in and two real account multiplayer still require the Hostinger configuration and end-to-end checks.
