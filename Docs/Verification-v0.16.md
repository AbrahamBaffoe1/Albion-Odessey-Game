# Student accounts — 0.16 verification

Verified September 13, 2026 (America/Chicago), Unity 6000.6.0f1.

## Live backend

26 checks passed against the deployed Supabase service using actual emails
delivered to two owned QA inboxes, including:

- New account email delivery and verified sign-up.
- Invalid and reused email code rejection.
- Server-created profiles and persistent name changes.
- Anonymous profile access denied.
- Cross-account reads and writes denied by database row-level security.
- Identity changes and client-side profile insertion denied.
- Invalid display-name rejection.
- Session renewal and logout with refresh revocation.
- Returning-student sign-in retains the same identity and online profile.
- QA account cleanup.

Local report: `Verification/accounts-live-report.json`.

## Real player

The packaged Mac game passed the live account integration check after the final
UI fixes. It requested a real email, rejected an incorrect code, verified the
delivered code, loaded its profile, saved/reloaded a new name and signed out.
Opening the account menu disabled player movement; returning to play restored it.
The screenshots were reviewed for layout, displayed profile updates and selected
button readability.

Local report: `Verification/accounts-unity-final/result.json` and its two UI captures.

Both macOS and Android/Quest packages built successfully. The installed Mac app
reports version 0.16.0 and passes strict deep signature validation with an ad-hoc
signature. The previous installed app was retained in the 0.15 download folder.

## Scope

This is real online email authentication and private profile persistence. Local
Keeper progress and course data remain on this device. The LAN prototype has not
been converted into authenticated hosted multiplayer. A verified email is not
college enrollment or teacher authorization. Sessions are memory-only, so players
request another code after restarting the game.

Physical gamepad/headset account entry, headset email UX, sustained campus-scale
traffic, Developer ID signing and notarization have not been verified here. The
initial hosted email limit is 30/hour; expand and monitor capacity before a large
campus launch.
