# Changelog

## Unreleased

### Added

- Separates other players' voices from game audio with a companion process, so
  each source can be recorded to a different track.
- Sends only remote-player voice playback to the companion process.
- Uses the current Windows multimedia default output; no virtual audio device
  is required.
- Includes `[General] Enabled` to turn voice separation on or off without
  uninstalling the package.
- Includes `[General] FallbackToGameOutput` to keep remote voice on the normal
  game output when the companion process is unavailable.
- Applies supported BepInEx configuration UI changes immediately, without
  restarting the game.
- Keeps the companion process available while Lethal Company is open, even
  after temporary connection or audio-device recovery.
- Silences remote voice by default if the companion process cannot accept it,
  preventing it from entering the game-audio recording track.

### Notes

- This will be the first public Thunderstore release and is a beta. Complete
  multiplayer voice-path, recording-track, endpoint-recovery, host-termination,
  and live-configuration coverage remains required before stable approval.
- Compatibility
    - Lethal Company v81 (2026-04-17 UTC)
        - Steam Manifest ID: `6423525044216269478`
        - Test environment
            - BepInExPack v5.4.2305 (2026-03-17 UTC)
    - Windows with .NET Framework 4.8
    - OBS Studio Application Audio Capture
- Install the mod only on clients that want separate remote-voice recording.
