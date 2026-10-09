# Gameplay and online completion gates

Requested scope: complete all seven areas step by step, and verify before claiming completion.
This checklist supersedes the obsolete prototype counts in Docs/Roadmap.md for this work.
Existing visual work remains preserved in the working tree.

| Gate | Required evidence | Current status |
|---|---|---|
| 1. Adventure | Introduction, linked objectives, saved progression, meaningful rewards, ending; full human playthrough and save/resume | Implemented locally: chapter/rules/runtime/save-roundtrip checks pass. Meaningful reward expansion and normal player walkthrough still pending |
| 2. Campus life | Assignments, clubs, schedules, revisit loop; completion and repeat/reload tests | Three local clubs, daily field assignments, reflections, badges and existing course timetable linked. Rules and runtime checks pass; richer assignment variety, schedule integration and human usability remain |
| 3. Multiplayer | Two real players: chat, board, drive, exit, disconnect/reconnect, shared exploration | Not verified; automated sockets are not a substitute |
| 4. Persistence | Written ownership/lifetime policy; database restart/reconnect/concurrency tests | Not complete |
| 5. Driving ratings | Durable eligible-ride ratings, deduplication, visible driver profile | Not complete |
| 6. Safeguards | Mute/block/report UI, enforced filtering, moderation process, authoritative reward/vehicle protections and abuse tests | Not complete |
| 7. Release | Performance measurements, migration/backup recovery, accessibility review, clean install/update, Developer ID signing/notarization | Not complete |

## Persistence policy for implementation

- Local adventure, campus assignments, discoveries and customization remain per Keeper in the local save until explicit account synchronization is built. No local wallet or completion flag can mint online rewards.
- Online profile, moderation preferences and eligible driver ratings should be account-owned and durable.
- Room chat, car occupancy and active driving poses are session state. Reconnect must release stale seats; do not restore a player into an occupied or moving car.
- Durable shared discoveries must belong to an explicit persistent group/world identifier, never to a recycled room code. This identity and its permissions are not implemented yet.
- Shared reward claims must use server-issued event IDs and unique database constraints, with transactions and retry-safe grants.

## Adventure verification

Automated rules cover prerequisites, duplicate observations, failed-save rollback, one-time ending, Keeper isolation, legacy migration and unchanged currency invariants.
The runtime acceptance test uses an isolated save and moves the test character to exercise destination and UI integrations. It does not substitute for manually walking the route or a human usability review.

## Evidence from this development pass

- `dotnet run --project Tests/UnityRules`: adventure/activity rules pass alongside existing 100,000-transaction economy and other regression suites.
- `npm test` in Backend/Realtime: 31/31 existing backend tests pass. No backend changes were deployed in this pass.
- `bash Tools/unity_mac.sh build`: Mac build succeeds.
- `-adventureSmoke`: isolated-save acceptance covers introduction, objective gates, destination travel, explicit sky observations, habitat records, three club submissions, ending, saved-state reload and campus-lighting restoration. Test movement is automated, not a full walking playthrough.
- Two people and two Macs are available per user; real multiplayer test results have not yet been collected.
- User reports Apple Developer signing configured. Local identity inspection found Apple Development identities, but no Developer ID Application identity. Distribution signing remains unverified, not complete.
- The new build has not replaced the installed app. Existing downtown visual work remains uncommitted and outside this gameplay verification.

## Real-player test procedure (pending)

1. Use distinct accounts on two Macs and confirm both run the same candidate version. Record version and room ID, without passwords or tokens.
2. Create one private room on Mac A; join its code on Mac B. Confirm both avatars and names.
3. Exchange messages both ways, including punctuation and a longer message. Confirm room isolation from a third, separate room if available.
4. A enters a parked car as driver; B boards as passenger. Check both views show correct seats.
5. A drives; confirm B cannot steer or board another car and sees smooth movement.
6. Stop; B exits, rates once; check the driver profile once persistent ratings are implemented. Repeat submission must not count.
7. Interrupt B's connection while seated. Confirm its seat is released, then reconnect and rejoin without duplicate avatars.
8. Interrupt the driver's connection. Confirm occupants recover safely and the vehicle cannot remain remotely controlled.
9. Explore campus and nature together; compare discoveries after reconnect and service restart once durable shared progress is implemented.
10. Record pass/fail and a screenshot or short recording for each step. Any failure keeps the multiplayer gate open.

Visual evidence: [intro](adventure-intro.png), [ending](adventure-ending.png), [clubs](campus-activities.png). Screens were captured after the loading transition finished.
