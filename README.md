# Remote Voice Split

Remote Voice Split is a client-side BepInEx 5 Mono mod for Lethal Company on
Windows. It sends other players' voices to a companion audio process so remote
voice and the rest of the game can be recorded on different tracks.

**This project is currently in public beta. Real-world testing remains limited,
and issues such as reduced game-audio quality, latency, missing audio, or
incorrect audio routing may occur.**

## Features

- Separates remote player voices from music, sound effects, and other game
  audio.
- Exposes the companion process as an application audio source without
  requiring a virtual audio device.
- Sends only remote-player voice playback to the companion process.
- Mutes remote voice by default when separate output is unavailable, preventing
  it from entering the game-audio track.
- Recovers the companion process and audio endpoint while the game remains
  open.

## Compatibility

- Lethal Company v81 (2026-04-17 UTC)
    - Steam Manifest ID: `6423525044216269478`
    - Test environment
        - [BepInExPack][bepinexpack-package] v5.4.2305 (2026-03-17 UTC)
- Windows with .NET Framework 4.8
- OBS Studio Application Audio Capture

Only clients that want separate remote-voice recording need this mod. The
lobby host and other players do not need to install it. Other Lethal Company
versions are not currently supported.

## Installation

Install a release package through a compatible mod manager, or copy these files
from the package into the same plugin directory:

```text
Lethal Company/BepInEx/plugins/RemoteVoiceSplit/RemoteVoiceSplit.dll
Lethal Company/BepInEx/plugins/RemoteVoiceSplit/RemoteVoiceSplit.AudioHost.exe
Lethal Company/BepInEx/plugins/RemoteVoiceSplit/RemoteVoiceSplit.AudioHost.exe.config
```

Do not rename or separate these files. Do not copy build-time BepInEx,
Harmony, Unity, game, or .NET reference assemblies.

## OBS setup

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

## Configuration

BepInEx creates
`BepInEx/config/com.aoirint.remotevoicesplit.cfg` after the first launch.

| Setting | Default | Behavior |
| --- | --- | --- |
| `General.Enabled` | `true` | Separates remote voice. Set it to `false` to keep remote voice on the normal game output. |
| `General.FallbackToGameOutput` | `false` | Keeps remote voice silent when separate output is unavailable. Set it to `true` to use the normal game output during those failures. |

Either setting can place remote voice in the game-audio track. Changes made
through a BepInEx configuration UI apply to the next voice block. Disabling
the mod keeps its process-lifetime routing infrastructure available, so it can
be enabled again without restarting the game. The mod does not watch direct
edits to the generated configuration file.

## Troubleshooting

- **The audio-host window does not appear:** keep all three installed files
  together and inspect `BepInEx/LogOutput.log` for
  `com.aoirint.remotevoicesplit`. Remote voice is silent with the default
  fallback setting until the host is available.
- **OBS shows the window but receives no voice:** join a lobby with another
  player, confirm that the source targets `RemoteVoiceSplit.AudioHost.exe`, and
  check that the source is not muted.
- **Remote voice remains in the game source:** confirm that
  `General.Enabled` is `true` and `General.FallbackToGameOutput` is `false`.
- **Remote voice is duplicated:** avoid capturing global Desktop Audio
  alongside both application sources, and remove other voice-routing mods
  while testing.

Include the package version, Lethal Company build, configuration, and relevant
log entries when [reporting a problem](CONTRIBUTING.md#reporting-issues).

## Development

The repository pins .NET SDK 10.0.201 and all NuGet versions.
The build does not require a local Lethal Company installation.

```powershell
dotnet restore RemoteVoiceSplit.slnx --locked-mode
dotnet format RemoteVoiceSplit.slnx --no-restore --verify-no-changes
dotnet build RemoteVoiceSplit.slnx --no-restore -c Release -p:BepInExPluginVersion=0.0.0
$projectVersion = ([xml](Get-Content -Raw RemoteVoiceSplit/RemoteVoiceSplit.csproj)).Project.PropertyGroup.Version | Select-Object -First 1
dotnet run --project RemoteVoiceSplit.Tests --no-build -c Release -- RemoteVoiceSplit/bin/Release/netstandard2.1/RemoteVoiceSplit.dll RemoteVoiceSplit.AudioHost/bin/Release/net48/RemoteVoiceSplit.AudioHost.exe $projectVersion $projectVersion
```

For Debug builds, live-audio checks, dependency review, Markdown lint, and
runtime installation, see
[development operations](docs/operations/development.md).

## Release status

CI owns creation of the validated Thunderstore-compatible ZIP. Pushes to
`main` produce edge artifacts, SemVer prereleases publish only to GitHub, and
the selected numeric public beta publishes the same verified ZIP to GitHub and
Thunderstore.

See [release operations](docs/operations/release.md) for archive validation,
runtime gates, publication steps, and recovery.

## Documentation

- [Developer documentation](docs/README.md)
- [Audio-routing architecture](docs/architecture/audio-routing.md)
- [Development operations](docs/operations/development.md)
- [Release operations](docs/operations/release.md)

## Contributing

Bug reports, compatibility notes, documentation improvements, and focused code
changes are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening an
issue or pull request.

## License

This project is released under the [MIT License](LICENSE).

[bepinexpack-package]: https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/
