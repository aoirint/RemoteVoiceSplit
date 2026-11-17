# Development Operations

## Prerequisites

- Windows PowerShell.
- The SDK selected by `global.json`.
- Network access to the mapped NuGet sources for the first restore.

Game code and assets are research inputs, not build inputs. The solution must
restore and compile without a local Lethal Company installation.

## Reproduce the build

Run from the repository root:

```powershell
dotnet restore RemoteVoiceSplit.slnx --locked-mode
dotnet format RemoteVoiceSplit.slnx --no-restore --verify-no-changes
dotnet build RemoteVoiceSplit.slnx --no-restore -c Debug -p:BepInExPluginVersion=0.0.0
$projectVersion = ([xml](Get-Content -Raw RemoteVoiceSplit/RemoteVoiceSplit.csproj)).Project.PropertyGroup.Version | Select-Object -First 1
dotnet run --project RemoteVoiceSplit.Tests --no-build -c Debug -- RemoteVoiceSplit/bin/Debug/netstandard2.1/RemoteVoiceSplit.dll RemoteVoiceSplit.AudioHost/bin/Debug/net48/RemoteVoiceSplit.AudioHost.exe $projectVersion $projectVersion
dotnet build RemoteVoiceSplit.slnx --no-restore -c Release -p:BepInExPluginVersion=0.0.0
dotnet run --project RemoteVoiceSplit.Tests --no-build -c Release -- RemoteVoiceSplit/bin/Release/netstandard2.1/RemoteVoiceSplit.dll RemoteVoiceSplit.AudioHost/bin/Release/net48/RemoteVoiceSplit.AudioHost.exe $projectVersion $projectVersion
```

The console harness validates deterministic buffering, mixing, race,
host/client, death, spectating, walkie-talkie, routing lifecycle, handshake,
ancestry, host-buffer, assembly-identity, and package mutation cases. It does
not require a game installation, OBS, or active render endpoint.

On a Windows development machine with an active default render endpoint, add
`--live-audio` to the test arguments. This starts the production WASAPI pump
and audio-host executable, then verifies endpoint-change retirement,
Explorer-parent launch outside the test process tree, same-PID pipe
reconnection, forced host termination, and crash recovery. Deterministic tests
separately verify the default-silent and opt-out fallback decisions. The
injected endpoint change does not alter the user's Windows default-device
setting.

For runtime configuration validation, make the process route unavailable and
toggle `General.FallbackToGameOutput` through a BepInEx configuration UI. The
next remote-voice block must switch between silent and normal Unity output
without source reattachment or a game restart. Toggle `General.Enabled` while
routing is ready; disabled blocks must remain on Unity output and enabled
blocks must resume separate routing without restarting. External edits to the
generated configuration file are outside this live-update check.

Use `--live-audio-soak-seconds 60` instead of `--live-audio` to keep the first
verified connection open for sixty seconds before exercising the remaining
lifecycle checks. Increase the positive integer when investigating
time-dependent host termination.

## Dependency updates

Use the SDK selected by `global.json`. Identify the project that declares
the package, its `packages.lock.json`, and the applicable `nuget.config`.
Use the checked-in package sources and source mappings.

1. Review the package identity, publisher, source, license, advisories, and
   publication date. Verify the configured seven-day minimum before accepting
   a version. Explicit version requests and ordinary restore do not enforce
   the minimum publish age.
2. Update the intended package reference to an exact version. Refresh the
   lockfile for the affected project with `--force-evaluate --use-lock-file`
   and the correct `--configfile`. Do not refresh unrelated projects.
3. Review the project and lockfile diffs. Check resolved versions, transitive
   changes, and content hashes. Investigate unexpected changes before
   keeping the update.
4. Run a locked restore, then the repository's format, build, and applicable
   tests. Keep the project and lockfile changes together in the PR and verify
   its CI before merging.

Run from the repository root in PowerShell. The plugin, audio host, and test
harness use the root `nuget.config` and have separate lockfiles.

For a plugin dependency update:

```powershell
dotnet restore RemoteVoiceSplit/RemoteVoiceSplit.csproj `
  --configfile nuget.config --force-evaluate --use-lock-file
```

For an audio-host dependency update:

```powershell
dotnet restore RemoteVoiceSplit.AudioHost/RemoteVoiceSplit.AudioHost.csproj `
  --configfile nuget.config --force-evaluate --use-lock-file
```

For a test dependency update:

```powershell
dotnet restore RemoteVoiceSplit.Tests/RemoteVoiceSplit.Tests.csproj `
  --configfile nuget.config --force-evaluate --use-lock-file
```

After reviewing the diff, verify the solution's lockfiles:

```powershell
dotnet restore RemoteVoiceSplit.slnx `
  --configfile nuget.config --locked-mode
```

Use [Reproduce the build](#reproduce-the-build) for the format, build, and Windows
test-harness commands.

## Dependency checks

```powershell
dotnet list RemoteVoiceSplit.slnx package --vulnerable --include-transitive --source https://nuget.bepinex.dev/v3/index.json --source https://api.nuget.org/v3/index.json
dotnet list RemoteVoiceSplit.slnx package --deprecated --include-transitive --source https://nuget.bepinex.dev/v3/index.json --source https://api.nuget.org/v3/index.json
```

Review any declaration or lockfile change for publisher, source, license,
publication cooldown, runtime behavior, and content hash.

## Restore Agent Skills

Use APM CLI 0.35.0:

```powershell
apm install --frozen
apm audit --ci
```

Do not edit `.agents/skills/` directly.

## Inspect packaged identity

The test harness requires:

- plugin assembly `RemoteVoiceSplit`;
- GUID `com.aoirint.remotevoicesplit`;
- name `Remote Voice Split`;
- version matching the project;
- process filter `Lethal Company.exe`; and
- host assembly `RemoteVoiceSplit.AudioHost` with the same version.

It uses Mono.Cecil rather than loading either output into the test process.

## Debugging

Start with a clean BepInEx 5 profile containing only this mod. Reproduce once,
exit, and inspect `BepInEx/LogOutput.log` for
`com.aoirint.remotevoicesplit`. Keep the first launch, handshake, ancestry,
pipe, or render failure and enough surrounding lines to establish ordering.

Before sharing logs, remove personal paths, account or lobby identifiers, and
unrelated plugin output. The mod does not log the random pipe name.

Use the deterministic harness for queues, protocol, ancestry, and lifecycle
failures. Gameplay hook, audio ordering, OBS enumeration, endpoint, and
recorded-track failures require the clean-profile checks in
[release operations](release.md#pre-release-runtime-checks).

## Documentation and workflow checks

Use pnpm 11 or newer with the repository's fail-closed cooldown:

```powershell
pnpm --config.minimumReleaseAge=10080 --config.minimumReleaseAgeStrict=true --config.minimumReleaseAgeIgnoreMissingTime=false --config.minimumReleaseAgeExclude= dlx markdownlint-cli2@0.22.0 --config .markdownlint-cli2.yaml "**/*.md"
```

Workflow changes additionally require:

```powershell
actionlint -color -pyflakes=
pinact run --check --min-age 7
```

Run ShellCheck for standalone shell and inline workflow Bash.

## Install for runtime testing

Copy only:

```text
RemoteVoiceSplit/bin/Release/netstandard2.1/RemoteVoiceSplit.dll
RemoteVoiceSplit.AudioHost/bin/Release/net48/RemoteVoiceSplit.AudioHost.exe
RemoteVoiceSplit.AudioHost/bin/Release/net48/RemoteVoiceSplit.AudioHost.exe.config
```

into:

```text
Lethal Company/BepInEx/plugins/RemoteVoiceSplit/
```

Keep all three files together. Do not copy reference assemblies, `.deps.json`,
`.pdb`, BepInEx packages, Unity packages, or game-derived files.
