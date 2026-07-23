# Developer Documentation

This documentation separates game and platform knowledge, mod-owned design,
and repeatable maintainer procedures.

## Documentation boundaries

- `domain/` owns facts about the game, OBS process capture, Windows audio,
  NuGet dependencies, and GitHub automation that can change independently of
  the mod.
- `architecture/` owns the mod's boundaries, invariants, lifecycle, and
  failure policy.
- `operations/` owns repeatable development, verification, packaging, and
  release procedures.

## Domain

- [Lethal Company voice playback](domain/lethal-company-voice-playback.md)
  records voice assignment, playback assets, and runtime boundaries for the
  supported build.
- [OBS process audio capture](domain/obs-process-audio-capture.md) records
  process-loopback and capture-window requirements.
- [Windows Core Audio](domain/windows-core-audio.md) records endpoint identity,
  format, COM, and WASAPI facts used by the implementation.
- [Windows process creation](domain/windows-process-creation.md) records
  desktop-shell identity and explicit-parent process creation.
- [Dependency baseline](domain/dependencies.md) records package provenance, pinning, scope, and security review.
- [GitHub automation dependencies](domain/github-automation.md) records pinned Actions and downloaded tool review.

## Architecture

- [Source layout](architecture/source-layout.md) owns project boundaries,
  dependency direction, and CI packaging ownership.
- [Audio routing](architecture/audio-routing.md) owns the data path,
  concurrency model, lifecycle, and unavailable-host policy.
- [Game integration](architecture/game-integration.md) owns reflection targets,
  Harmony callbacks, player-role rules, and callback containment.
- [Implementation evidence](architecture/implementation-evidence.md) records
  the baseline delta, decisions, and unresolved runtime gates.
- [Release pipeline](architecture/release-pipeline.md) owns version
  classification, artifact handoff, and publication invariants.

## Operations

- [Development](operations/development.md) defines restore, format, build, test, metadata, and dependency checks.
- [Release](operations/release.md) defines packaging, archive validation, CI, GitHub Release, and recovery procedures.
- [Icon authoring](icon-authoring.md) defines the editable package-icon source,
  rendering, and verification contract.

The root [README](../README.md) is the user-facing installation and
configuration entry point.
