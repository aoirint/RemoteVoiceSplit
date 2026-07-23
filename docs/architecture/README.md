# Architecture Documentation

Architecture documents explain the mod's own design choices and invariants.

- [Source layout](source-layout.md) covers project ownership and dependency
  direction.
- [Audio routing](audio-routing.md) covers the data path, process boundary,
  concurrency, endpoint lifecycle, and fallback behavior.
- [Game integration](game-integration.md) covers reflection, Harmony callbacks,
  player selection, and callback containment.
- [Implementation evidence](implementation-evidence.md) records the repository
  baseline delta, evidence ledger, decisions, and blocked release branches.
- [Release pipeline](release-pipeline.md) covers version classification,
  artifact handoff, and publication boundaries.
