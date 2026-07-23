# Changelog

All notable developer-facing changes to this project are documented here.
Package-facing release notes are maintained in `assets/CHANGELOG.md`.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed

- Replaced `Shell.Application` COM activation, which is unsupported by the
  target Unity Mono runtime, with native Windows explicit-parent process
  creation while preserving fail-open behavior and OBS process-tree
  separation.
- Kept the OBS-facing audio-host process and window stable across recoverable
  pipe, renderer, and default-endpoint failures; only the verified connection
  session is now replaced.
- Kept the host available for the verified game-process lifetime instead of
  closing it when a Unity startup transition exceeds a reconnection timeout.

### Added

- A client-side BepInEx 5 plugin for Lethal Company v81 on Windows.
- A companion .NET Framework audio host with a stable OBS-selectable title.
- Per-player capture queues, mixed fixed-duration protocol frames, named-pipe
  transport, and event-driven shared-mode WASAPI rendering.
- A process-tree guard that permits routing only when the audio host is outside
  the Lethal Company process tree.
- Fail-open transitions that preserve Unity voice output until the host is
  connected, verified, and ready.
- Deterministic tests for buffering, mixing, protocol identity, process
  ancestry, lifecycle races, managed metadata, and the package contract.
- A host-neutral package containing the plugin DLL, companion EXE, and
  generated .NET Framework runtime configuration at its root.
- Pinned GitHub Actions, NuGet lockfiles, APM-managed Agent Skills, contributor
  governance, and release automation inherited from the related repository
  family.
- Domain, architecture, development, and release documentation for the
  supported game and OBS process-capture contract.

### Security

- Uses an unguessable per-game-session pipe name and validates protocol magic,
  version, channel count, sample rate, frame bounds, and both actual pipe-peer
  process IDs, including the server executable path.
- Rejects routing when the helper is the game process or a descendant, which
  prevents OBS process-tree capture from folding remote voice back into the
  game source.
- Bounds audio queues and package entry, expansion, and compression sizes.
- Pins direct and transitive NuGet dependencies with content hashes.

### Changed

- Reused repository-family contribution, CI, release, and documentation
  structures while replacing the prior output-device selection design with a
  separate-process recording design.
- Reserved project version `0.0.0` for non-release edge artifacts.
