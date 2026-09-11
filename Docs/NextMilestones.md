# Next development milestones

The 0.8 update includes a playable three-level Ferguson reconstruction, a rigged student, reusable doors/interior loading, campus textures and trees, and a consolidated interface. The 0.9 work adds reference-driven, walkable Robinson Hall and Bonta Admission Center shells. The 0.10 work starts the connected Science Complex group. The 0.11 work replaces that placeholder with four connected wings, adds campus life and accessibility systems, and lays down LAN, XR, save recovery and profiling foundations. The 0.12 desktop release candidate adds crash-session reporting, release signature checks and a macOS acceptance gate. This remains a prototype milestone, not completion of a production MMO or a surveyed digital twin.

1. **Ferguson usability polish.** Keep refining its reference-based exterior and interior furniture, signage and room collision as the next art review.
2. **Robinson and Bonta polish.** Robinson now has a dedicated Blender exterior/interior asset and authored collision in the 0.13 build; continue visual review alongside the Bonta pass.
3. **Science Complex art review.** Tune the four-wing proportions, lab dressing, collection displays, accessibility signs and lighting against the public references.
4. **Campus placement and landscape.** Continue replacing generic map exteriors with individually authored halls and improve roads, planting and lighting.
5. **Student movement and campus life.** Connect more natural animation transitions and extend NPC schedules beyond the six deterministic routes.
6. **Interface and accessibility.** Per-action keyboard rebinding, building audio-description controls and per-media caption cards are shipped in 0.15. Add controller glyph assets and import complete source transcripts where they are available.
7. **Building and learning gameplay.** Expand the schedule into prerequisites, assignments, clubs and saved student activity history.
8. **Online service hardening.** The LAN session now binds UDP 40777, carries presence/movement snapshots, shows online nameplates, keeps a capped sanitized chat feed and supports host moderation. The production phase still needs accounts, an authoritative hosted server, encrypted transport, cloud persistence and abuse reporting.
9. **VR and release readiness.** OpenXR loaders, headset-specific comfort settings, performance guards and recovery hooks are shipped in 0.15. Complete physical Quest/SteamVR comfort QA, notarization and distribution signing.

Completion gates for each new hall: an identifiable referenced exterior, a usable entrance, walkable rooms and stairs where applicable, collision and camera checks, history/media interactions, safe return to campus, and measured performance. Full-campus and VR completion must be assessed separately from the count of map pins.
