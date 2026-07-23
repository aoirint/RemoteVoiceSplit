# Remote Voice Split

Remote Voice Split moves voices from other Lethal Company players into a
separate audio process. Capture **Lethal Company Remote Voice Split** with OBS
Studio's Application Audio Capture and assign it to a different recording
track from `Lethal Company.exe`. No virtual audio device is required.

## Compatibility

- Lethal Company v81
    - Steam Build ID: `22825947`
    - Steam Manifest ID: `6423525044216269478`
- BepInEx 5 through BepInExPack 5.4.2305
- Windows with .NET Framework 4.8
- OBS Studio Application Audio Capture

## Setup

1. Install the package and start Lethal Company.
2. Wait for the **Lethal Company Remote Voice Split** window to appear.
3. Add that window as an OBS Application Audio Capture source.
4. Capture Lethal Company separately and assign the sources to different
   recording tracks.

The companion process uses the current Windows multimedia default output.
When separate routing is unavailable, remote voice remains on the normal game
output instead of being muted. Its OBS-selectable window remains available
across temporary audio-device or connection recovery while the game runs.

Only clients that want separate remote-voice recording need this mod.

## AI Disclosure

Some parts of this project were developed with AI tools based on large
language models, including agent-based tools. The project maintainer reviews
the code. This disclosure is made in compliance with Thunderstore and
community policies.
