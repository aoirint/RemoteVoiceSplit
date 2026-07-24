# Remote Voice Split

Remote Voice Split is a client-side BepInEx 5 Mono mod for Lethal Company on
Windows. It separates other players' voices from the rest of the game audio,
so each can be recorded on a different track. A small companion process
named `RemoteVoiceSplit.AudioHost.exe` provides the separate audio source.

Music, sound effects, and the local microphone remain in the normal Lethal
Company process. When the audio host cannot be started or connected safely,
remote voices are silent by default so they do not leak into the game-audio
recording track. An opt-out setting can keep them on the normal game output
instead.

## Beta status

The first Thunderstore release is planned as a public beta. The implementation
and build-time verification are complete, but the full supported-build runtime
matrix is not. Expect audio-routing or recovery issues that have not appeared
in deterministic tests, and include the mod version, game build, configuration,
and relevant BepInEx log entries when reporting them.

Thunderstore package versions use three numeric parts without a prerelease
suffix. A numeric package version therefore identifies the beta artifact; it
does not mean that the project has completed stable-release validation.

## Requirements

- Lethal Company v81 on Windows:
  Steam Build ID `22825947`, Manifest ID `6423525044216269478`.
- BepInEx 5.x Mono.
- OBS Studio with Application Audio Capture support.
- .NET Framework 4.8, which is included with supported Windows releases.

This mod is client-side. Other players and the lobby host do not need it.
Other game versions are not currently supported.

## Install

Copy these files from the package into the same plugin directory:

```text
Lethal Company/BepInEx/plugins/RemoteVoiceSplit/RemoteVoiceSplit.dll
Lethal Company/BepInEx/plugins/RemoteVoiceSplit/RemoteVoiceSplit.AudioHost.exe
Lethal Company/BepInEx/plugins/RemoteVoiceSplit/RemoteVoiceSplit.AudioHost.exe.config
```

Do not rename or separate these files. Do not copy build-time BepInEx,
Harmony, Unity, game, or .NET reference assemblies.

## Configure

BepInEx creates
`BepInEx/config/com.aoirint.remotevoicesplit.cfg` after the first launch.
The default is:

```ini
[General]
Enabled = true
FallbackToGameOutput = false
```

Set `Enabled` to `false` to disable voice separation and keep remote voices on
the normal game output. Keep `FallbackToGameOutput` at `false` for strict
recording-track separation while the mod is enabled. Remote voices are
inaudible whenever separate process output cannot accept them. Set it to
`true` to fall back to `Lethal Company.exe` during those failures, accepting
that remote voices can appear in the game-audio track.

Changes made through a BepInEx configuration UI apply immediately to the next
voice block. Disabling the mod does not unload its process-lifetime integration;
it keeps the routing infrastructure available so `Enabled` can be turned back
on without restarting the game. The mod does not watch external edits to the
generated configuration file.

## Configure OBS Studio

1. Start Lethal Company and wait for the audio-host window to appear.
2. In OBS, add an **Application Audio Capture (BETA)** source.
3. Select the window named **Lethal Company Remote Voice Split**.
4. Capture `Lethal Company.exe` separately for game audio.
5. In Advanced Audio Properties, assign the two sources to different recording
   tracks.

Disable global Desktop Audio or otherwise exclude duplicate monitoring when it
would record the same sounds a second time. The companion process renders to
the current Windows multimedia default output, so players still hear remote
voice normally. Its OBS-selectable window remains available across temporary
audio-device or connection recovery while Lethal Company keeps running.

## Build

The repository pins .NET SDK 10.0.201 and all NuGet versions.

```powershell
dotnet restore RemoteVoiceSplit.slnx --locked-mode
dotnet format RemoteVoiceSplit.slnx --no-restore --verify-no-changes
dotnet build RemoteVoiceSplit.slnx --no-restore -c Release -p:BepInExPluginVersion=0.0.0
dotnet run --project RemoteVoiceSplit.Tests --no-build -c Release -- RemoteVoiceSplit/bin/Release/netstandard2.1/RemoteVoiceSplit.dll RemoteVoiceSplit.AudioHost/bin/Release/net48/RemoteVoiceSplit.AudioHost.exe 0.1.0-alpha.4 0.1.0-alpha.4
```

Add `--live-audio` to the test command on a Windows machine with an active
default render endpoint to exercise same-PID reconnection, forced termination,
default-endpoint failure, and recovery. Use
`--live-audio-soak-seconds 60` instead to include a sixty-second connected
host-lifetime check.

CI owns creation of the validated Thunderstore-compatible ZIP. Version `0.0.0`
always produces an edge artifact, and SemVer prereleases publish only to
GitHub. The current workflow does not yet publish a numeric beta to GitHub or
Thunderstore; enabling that path is a separate reviewed change. See
[release operations](docs/operations/release.md).

## Troubleshooting

- No `Lethal Company Remote Voice Split` window: remote voices are silent with
  the default fallback setting. Confirm that both packaged files remain
  together and inspect `BepInEx/LogOutput.log` for
  `com.aoirint.remotevoicesplit`.
- OBS shows the window but receives no voice: join a lobby with another player,
  verify that OBS targets `RemoteVoiceSplit.AudioHost.exe`, and check that the
  source is not muted.
- Remote voice remains in the game source: confirm that
  `General.Enabled` is `true` and `General.FallbackToGameOutput` is `false`.
  With the mod disabled or fallback enabled, Unity deliberately keeps the
  applicable remote-voice blocks. The first warning in the BepInEx log
  identifies an unavailable-host transition.
- Duplicate remote voice: do not capture global Desktop Audio alongside both
  application sources, and remove other voice-routing mods while testing.

Developer documentation starts at [docs/README.md](docs/README.md).

## License

This project is released under the [MIT License](LICENSE).
