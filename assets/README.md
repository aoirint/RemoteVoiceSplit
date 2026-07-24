# Remote Voice Split

A Lethal Company mod that separates other players' voices from the rest of the
game audio so each can be recorded on a different track.

**This package is currently in public beta. Real-world testing remains limited,
and issues such as reduced game-audio quality, latency, missing audio, or
incorrect audio routing may occur.**

## Compatibility

- Lethal Company v81 (Manifest ID: `6423525044216269478`)
    - Test environment
        - [BepInExPack][bepinexpack-package] v5.4.2305
- Windows with .NET Framework 4.8
- OBS Studio Application Audio Capture

## What it does

- Moves voices from other players into a companion audio process.
- Keeps music, sound effects, and the local microphone in the normal Lethal
  Company process.
- Uses the current Windows multimedia default output for the companion process.
- Mutes remote voice by default when separate routing is unavailable so it does
  not enter the game-audio track.
- Keeps the OBS-selectable window available during temporary audio-device or
  connection recovery while the game runs.

## Setup

1. Install the package and start Lethal Company.
2. Wait for the **Lethal Company Remote Voice Split** window to appear.
3. Add that window as an OBS Application Audio Capture source.
4. Capture Lethal Company separately and assign the sources to different
   recording tracks.

## Configuration

BepInEx creates
`BepInEx/config/com.aoirint.remotevoicesplit.cfg` after the first launch.

| Name | Type | Default | Description |
| :--- | :--- | :------ | :---------- |
| `Enabled` | bool | true | Set to false to disable voice separation and keep remote voice on the normal game output. |
| `FallbackToGameOutput` | bool | false | Set to true to use the normal game output when separate output is unavailable. |

Either setting can place remote voice in the game-audio track. Changes made
through a BepInEx configuration UI apply immediately.

## Who needs to install

Client-side only. The host and other players do not need to install this mod.

Install it on each client that wants to record remote player voices separately.

## AI Disclosure

Some parts of this project were developed with AI tools based on large language
models (LLMs), including agent-based tools. The project maintainer reviews the
code. This disclosure is made in compliance with Thunderstore and community
policies.

[bepinexpack-package]: https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/
