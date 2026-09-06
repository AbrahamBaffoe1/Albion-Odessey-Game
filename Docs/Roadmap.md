# From prototype to the full game

## 1. Verify the native slice

Install an Unreal version compatible with the development Mac/Xcode combination. Compile the editor target, run the import script, and exercise the native checklist. Fix any API/compiler issues discovered. Produce a signed or locally launchable Mac package and record the exact engine version. Replace the initial text HUD with responsive UMG controls and add controller/touch input.

Exit: a new user can install, launch, collect, build, switch appearance, switch local profile, save, quit, and resume without the editor.

## 2. Build Albion's exploration map

Use the reference manifest and approved capture process to reconstruct a verified campus route. Add accessible virtual traversal, landmark entry points, sourced history, and authored quests. Keep the freely designed Legacy Campus as a distinct map.

Exit: the first five landmarks match reviewed references, with useful views from every side and no unsourced historical assertions.

## 3. Deliver mobile campus mode

Implement a location provider against the tested policy, with permission, stale-fix, and accuracy UI. Add Unreal AR session support on supported iOS/Android devices, anchors, and touch interaction. Never spoof GPS to provide room play: room play is its own first-class route to the same content. Add packaged device tests and confirm no precise location enters ordinary save files or logs.

Exit: both a remote student and an on-campus student can complete the same discovery objective, with real GPS and camera behavior verified on hardware.

## 4. Online co-op and shared construction

Add authenticated accounts and a server-authoritative inventory/transaction service. Persist personal islands by stable user identity and community contributions in a transaction ledger. Include idempotency, reconnect handling, rate limits, server-side eligibility checks, and conflict resolution. Replicate world events; client save files are never authoritative online. Add invitations and a friends list before expanding social tools.

Exit: two physical devices can independently build their islands, contribute concurrently, reconnect, and agree with the server after duplicate/retried commands.

## 5. Creative and educational expansion

Introduce blueprint postcards, era windows, a squirrel guide, and reviewed student memory trails. Start adaptive hints with transparent rules and measure whether they help. Add machine learning only when a real learning need, suitable consent, and evaluation data justify it. Build moderation/reporting and content provenance before enabling public user-generated stories or chat.

Exit: students can create and share a bounded design safely, with published history verified against sources and accessible alternatives available.
