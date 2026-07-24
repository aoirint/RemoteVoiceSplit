# Remote Voice Split

Remote Voice Split separates other players' voices from the rest of the game
audio, so each can be recorded on a different track.

**This package is currently in public beta. Real-world testing remains limited,
and issues such as reduced game-audio quality, latency, missing audio, or
incorrect audio routing may occur.**

## Compatibility

- Lethal Company v81
    - Steam Build ID: `22825947`
    - Steam Manifest ID: `6423525044216269478`
- BepInEx 5 through BepInExPack 5.4.2305
- Windows with .NET Framework 4.8
- OBS Studio Application Audio Capture

## What it does

Remote Voice Split moves voices from other players into a companion audio
process. Music, sound effects, and the local microphone remain in the normal
Lethal Company process.

The companion process uses the current Windows multimedia default output.
When separate routing is unavailable, remote voice is muted by default so it
does not enter the game-audio track. Its OBS-selectable window remains
available across temporary audio-device or connection recovery while the game
runs.

## Setup

1. Install the package and start Lethal Company.
2. Wait for the **Lethal Company Remote Voice Split** window to appear.
3. Add that window as an OBS Application Audio Capture source.
4. Capture Lethal Company separately and assign the sources to different
   recording tracks.

## Configuration

BepInEx creates
`BepInEx/config/com.aoirint.remotevoicesplit.cfg` after the first launch.
Set `[General] Enabled = false` to disable voice separation and keep remote
voice on the normal game output. Set
`[General] FallbackToGameOutput = true` to keep remote voice audible through
the normal game output only when separate process output cannot accept it.
Changes made through a BepInEx configuration UI apply immediately. Either
choice can place remote voice in the game-audio track.

## Who needs to install

Install the mod only on clients that want to record remote player voices
separately.

## AI Disclosure

Some parts of this project were developed with AI tools based on large
language models, including agent-based tools. The project maintainer reviews
the code. This disclosure is made in compliance with Thunderstore and
community policies.
