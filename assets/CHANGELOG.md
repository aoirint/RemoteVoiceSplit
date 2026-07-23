# Changelog

## Unreleased

## v0.1.0-alpha.2 - 2026-07-23

### Added

- Added `Audio.FallbackToGameOutput`. Set it to `true` to keep remote voice on
  the normal game output when separate process output cannot accept it.

### Changed

- Remote voice is now silent by default while the separate audio host is
  unavailable or recovering, keeping it out of the game-audio recording track.
- Changes made through a BepInEx configuration UI now apply immediately
  without restarting the game.

### Notes

- This is an alpha GitHub release. Complete two-player OBS track separation,
  host/client and voice-path coverage, endpoint recovery, host termination,
  and live configuration switching remain required before a stable release.
- Compatibility: Lethal Company v81.
    - Steam Build ID: `22825947`
    - Steam Manifest ID: `6423525044216269478`
- Install the mod only on clients that want separate remote-voice recording.

## v0.1.0-alpha.1 - 2026-07-23

### Fixed

- Fixed audio-host startup under the Unity Mono runtime while keeping the host
  outside the Lethal Company process tree for OBS capture.
- Keeps the OBS-selectable audio-host window running while its audio connection
  recovers from a temporary failure.
- Keeps waiting for recovery during slow game startup and scene transitions.
- Keeps voice routing active when the BepInEx plugin component is destroyed
  during game startup.

### Added

- Routes other players' voices through a separate OBS-capturable process.
- Keeps game audio and the local microphone in the Lethal Company process.
- Uses the current Windows multimedia default output without a virtual audio
  device or in-game configuration.
- Keeps remote voices on the normal game output when separate routing fails.

### Notes

- This is an alpha GitHub release. The companion window has been observed
  through game startup, but complete two-player OBS track-separation coverage
  remains required before a stable release.
- Compatibility: Lethal Company v81.
    - Steam Build ID: `22825947`
    - Steam Manifest ID: `6423525044216269478`
- Install the mod only on clients that want separate remote-voice recording.
