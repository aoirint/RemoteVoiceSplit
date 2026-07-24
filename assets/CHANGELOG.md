# Changelog

## v0.1.0 - 2026-07-25

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

- This is the first public Thunderstore release and is a beta. See the package
  beta notice for its current quality and validation scope.
- Compatibility: Lethal Company v81.
    - Steam Build ID: `22825947`
    - Steam Manifest ID: `6423525044216269478`
- Install the mod only on clients that want separate remote-voice recording.
