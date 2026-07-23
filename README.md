# Remote Voice Split

Remote Voice Split is a client-side BepInEx 5 Mono mod for Lethal Company on
Windows. It plays voices from other players through a small companion process
named `RemoteVoiceSplit.AudioHost.exe`. OBS Studio can capture that process as
an Application Audio Capture source, so remote voice and the rest of the game
can be assigned to different recording tracks without a virtual audio device.

Music, sound effects, and the local microphone remain in the normal Lethal
Company process. When the audio host cannot be started or connected safely,
remote voices stay on the normal game output instead of becoming silent.

The implementation and build-time verification are complete. A two-player
runtime validation on the supported build is still required before a stable
release.

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
dotnet run --project RemoteVoiceSplit.Tests --no-build -c Release -- RemoteVoiceSplit/bin/Release/netstandard2.1/RemoteVoiceSplit.dll RemoteVoiceSplit.AudioHost/bin/Release/net48/RemoteVoiceSplit.AudioHost.exe 0.1.0-alpha.1 0.1.0-alpha.1
```

Add `--live-audio` to the test command on a Windows machine with an active
default render endpoint to exercise same-PID reconnection, forced termination,
default-endpoint failure, and recovery. Use
`--live-audio-soak-seconds 60` instead to include a sixty-second connected
host-lifetime check.

CI owns creation of the validated Thunderstore-compatible ZIP. Version `0.0.0`
always produces an edge artifact. SemVer prereleases publish only to GitHub;
stable GitHub and Thunderstore publication remain disabled. See
[release operations](docs/operations/release.md).

## Troubleshooting

- No `Lethal Company Remote Voice Split` window: confirm that both packaged files
  remain together and inspect `BepInEx/LogOutput.log` for
  `com.aoirint.remotevoicesplit`.
- OBS shows the window but receives no voice: join a lobby with another player,
  verify that OBS targets `RemoteVoiceSplit.AudioHost.exe`, and check that the
  source is not muted.
- Remote voice remains in the game source: the mod deliberately fails open
  when the host is missing, exits, changes process ancestry, or loses its audio
  endpoint. The first warning in the BepInEx log identifies that transition.
- Duplicate remote voice: do not capture global Desktop Audio alongside both
  application sources, and remove other voice-routing mods while testing.

Developer documentation starts at [docs/README.md](docs/README.md).

## License

This project is released under the [MIT License](LICENSE).
