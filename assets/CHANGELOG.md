# Changelog

## Unreleased

### Fixed

- Fixed audio-host startup under the Unity Mono runtime while keeping the host
  outside the Lethal Company process tree for OBS capture.
- Keeps the OBS-selectable audio-host window running while its audio connection
  recovers from a temporary failure.

### Added

- Routes other players' voices through a separate OBS-capturable process.
- Keeps game audio and the local microphone in the Lethal Company process.
- Uses the current Windows multimedia default output without a virtual audio
  device or in-game configuration.
- Keeps remote voices on the normal game output when separate routing fails.

### Notes

- Compatibility: Lethal Company v81.
    - Steam Build ID: `22825947`
    - Steam Manifest ID: `6423525044216269478`
- Install the mod only on clients that want separate remote-voice recording.
