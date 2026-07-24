# Changelog

## Unreleased

### Added

- Separates other players' voices from game audio through a dedicated companion
  process, allowing each source to be recorded on a different track.
- Keeps game audio and the local microphone in the normal Lethal Company
  process.
- Uses the current Windows multimedia default output without requiring a
  virtual audio device.
- Provides `[General] Enabled` to turn separation on or off without
  uninstalling the package.
- Provides `[General] FallbackToGameOutput` to keep remote voice on the normal
  game output when the companion process is unavailable.
- Keeps the companion process available across temporary connection and audio
  device recovery while Lethal Company remains open.

### Changed

- Remote voice is silent by default when the companion process cannot accept
  it, preventing it from entering the game-audio recording track.
- Changes made through a BepInEx configuration UI apply
  immediately without restarting the game.
- Marks the upcoming Thunderstore publication as a public beta.
- Keeps recording-application details in the setup instructions instead of the
  package summary and introduction.

### Notes

- This will be the first public Thunderstore release and is a beta. Complete
  multiplayer voice-path, recording-track, endpoint-recovery, host-termination,
  and live-configuration coverage remains required before stable approval.
- Compatibility: Lethal Company v81.
    - Steam Build ID: `22825947`
    - Steam Manifest ID: `6423525044216269478`
- Install the mod only on clients that want separate remote-voice recording.
