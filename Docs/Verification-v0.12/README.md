# Desktop release candidate verification

This verification folder describes the macOS desktop release candidate. The player includes the Ferguson, Robinson, Bonta and Science Complex vertical slice, local courses and building mode, save-backup recovery, accessibility controls, LAN session foundations, optional XR support, runtime profiling and crash-session reporting.

Release gates:

- Unity player builds successfully for macOS.
- Core rules, geometry, audio, catalog and building-editor tests pass.
- The five isolated player integration suites pass.
- The focused walkable-building smoke test passes for Robinson, Bonta and Science Complex.
- The bundle has a verified code signature and a recorded version manifest.
- Developer ID notarization is a separate gate requiring Apple distribution credentials; an Apple Development or ad-hoc signature is suitable for local testing only.

The app writes a `session.active` marker while running. If the next launch finds that marker, the player reports an interrupted session and continues using the atomic save backup. Error reports are written to the app's `crash-reports` directory without overwriting player progress.

The tested local bundle is signed with the available Apple Development identity. No Developer ID Application identity or notarization profile is configured on this Mac, so distribution signing remains the only external release gate.
