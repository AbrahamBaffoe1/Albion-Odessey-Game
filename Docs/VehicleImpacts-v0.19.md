# Vehicle impacts and repairs — v0.19

Drive with WASD or the arrow keys. Space brakes. E enters or exits a nearby stopped car. Stop and press R to inspect or repair the car; R also works within five metres on foot. Escape closes the repair screen.

Cars can knock down the fictional, rigged campus students at speeds of at least 2.5 m/s. Eleven connected physics bodies drive the character skeleton, settle against ground and obstacles, then blend back to standing and resume campus activity. Recovery includes a short immunity window. This applies to local roaming and activity agents, not remote human players or the older primitive classroom roster.

Solid impacts above 3 m/s deform each car's body mesh at the contact point, across all three levels of detail. Damage is capped at 100 and retains the eight most recent dents. Cars stop at solid obstacles, including buildings and parked cars. Holding the accelerator against a wall does not repeatedly damage the car. A fully damaged car retains 60% of its top speed so an empty wallet does not lock exploration.

The damage card appears while driving. Repairs require a stopped, nearby car and an explicit payment choice. The full repair costs one acorn coin per ten damage points, rounded up, or one gem per 35 points, rounded up. Both methods restore all body meshes and driving performance. No real-money payments are involved.

Each of the twelve golden memories earns three acorn coins and one gem. Existing collected memories count toward the gem balance. Gems are finite collectibles per keeper, as are the existing memory rewards; this update does not introduce repeatable currency farming. Each keeper has a separate wallet; the three cars share their persistent damage across keepers on the device.

Save schema 3 upgrades older saves while preserving memories, buildings, school progress and coin balances. Repair spending is part of the validated economy ledger. Payment and damage removal are written together; a failed write restores both, and repair visuals change only after a successful save. Vehicle damage persists locally. This is not a server-authoritative multiplayer damage or economy system.

Collision, student impact and successful repair use distinct, consistent sound cues and obey the effects mute/volume preferences.

## Verification

The dedicated `-impactSmoke` player scenario uses an isolated save. It checks swept vehicle/student contact, skeletal physics and ground contact, recovery and resumed walking, a student protected behind a wall, building collision, deformation of all three LOD meshes, per-car isolation, repeat wall contact, saved damage, both repair currencies, duplicate payment and insufficient funds. It captures before/after and repair-screen evidence.

Pure C# checks exercise legacy migration, rejected/throwing persistence, both payment ledgers, bounded damage records, corrupt data and 20,000 randomized mixed collection/build/repair transactions, alongside the existing 110,000 school and economy transactions.

The get-up uses a procedural blend into the existing locomotion animation. Authored directional get-up clips, suspension, tire simulation, deformable buildings, passenger damage, and synchronized online player impacts remain separate work.

All seven Mac player suites passed: impact, vehicle, craft, combined campus, core campus, tour and shell. The Quest Android package compiled successfully; headset hardware was not tested in this pass.

[Repair screen](Verification-v0.19/05-repair-screen.png) · [Student knockdown](Verification-v0.19/02-student-knockdown.png) · [Recovered student](Verification-v0.19/03-student-recovered.png) · [Check results](Verification-v0.19/release-checks.json).
