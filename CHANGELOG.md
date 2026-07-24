# Changelog

All notable developer-facing changes to this project are documented here.
Package-facing release notes are maintained in `assets/CHANGELOG.md`.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Refocused the user-facing introduction on OBS recording-track separation.
- Defined the first Thunderstore publication as a clearly labeled public beta
  whose numeric package version does not imply stable-release approval.
- Removed the product-specific recording application name from package-list
  and introductory descriptions while retaining it in detailed setup guidance.
- Replaced GitHub-only alpha entries in the package-facing changelog with one
  consolidated draft for the first public Thunderstore release.
- Updated the APM-managed `release-note-workflow` so destination-specific
  histories retain SemVer prereleases where published without presenting
  another destination's headings as public history.

## [0.1.0-alpha.4] - 2026-07-23

### Changed

- Replaced the package artwork with the repository-family text icon style.

### Notes

- This GitHub prerelease changes package artwork only and retains the alpha
  runtime-validation scope. Complete two-player OBS track separation,
  host/client and voice-path coverage, endpoint recovery, host termination,
  and live configuration switching remain required before a stable release.
- Compatibility: Lethal Company v81, Steam Build `22825947`, Steam Manifest
  `6423525044216269478`, Windows, and BepInEx 5.

## [0.1.0-alpha.3] - 2026-07-23

### Added

- Added `General.Enabled`, defaulting to `true`, so users can disable voice
  separation and preserve normal game output without uninstalling the mod.

### Changed

- Moved `FallbackToGameOutput` from `Audio` to `General` because it controls
  routing failure policy rather than an audio-format or device parameter.
- Applies both supported settings immediately when changed through a BepInEx
  configuration API.

### Notes

- This GitHub prerelease retains the alpha runtime-validation scope. Complete
  two-player OBS track separation, host/client and voice-path coverage,
  endpoint recovery, host termination, and live configuration switching remain
  required before a stable release.
- Upgrading from alpha.2 does not migrate
  `Audio.FallbackToGameOutput`; configure
  `General.FallbackToGameOutput` instead.
- Compatibility: Lethal Company v81, Steam Build `22825947`, Steam Manifest
  `6423525044216269478`, Windows, and BepInEx 5.

## [0.1.0-alpha.2] - 2026-07-23

### Added

- Added `Audio.FallbackToGameOutput` to let users retain remote voice on the
  normal game output when separate process output cannot accept it.

### Changed

- Silences remote voice by default whenever separate process output cannot
  accept it, preventing it from leaking into the game-audio recording track.
- Applies `Audio.FallbackToGameOutput` changes made through BepInEx
  configuration APIs immediately without restarting the game.

### Notes

- This GitHub prerelease retains the alpha runtime-validation scope. Complete
  two-player OBS track separation, host/client and voice-path coverage,
  endpoint recovery, host termination, and live configuration switching remain
  required before a stable release.
- Compatibility: Lethal Company v81, Steam Build `22825947`, Steam Manifest
  `6423525044216269478`, Windows, and BepInEx 5.

## [0.1.0-alpha.1] - 2026-07-23

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
- Kept routing and Harmony integration alive when Unity destroys the BepInEx
  plugin component during startup; cleanup now follows the application-quit
  event instead of `OnDestroy`.

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
- Reserved project version `0.0.0` for non-release edge artifacts and
  enabled immutable GitHub prereleases for SemVer prerelease versions.
- Kept prerelease BepInEx metadata and the Thunderstore manifest at `0.0.0`;
  prerelease identity remains in assembly metadata, the ZIP name, tag, and
  GitHub Release.

[Unreleased]: https://github.com/aoirint/RemoteVoiceSplit/compare/v0.1.0-alpha.4...HEAD
[0.1.0-alpha.4]: https://github.com/aoirint/RemoteVoiceSplit/releases/tag/v0.1.0-alpha.4
[0.1.0-alpha.3]: https://github.com/aoirint/RemoteVoiceSplit/releases/tag/v0.1.0-alpha.3
[0.1.0-alpha.2]: https://github.com/aoirint/RemoteVoiceSplit/releases/tag/v0.1.0-alpha.2
[0.1.0-alpha.1]: https://github.com/aoirint/RemoteVoiceSplit/releases/tag/v0.1.0-alpha.1
