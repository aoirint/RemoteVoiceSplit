# Implementation Evidence

This document records the evidence and unresolved gates used to create Remote
Voice Split. External assumptions are owned by the linked domain documents;
this document owns the product decisions made from those assumptions.

## Request classification

The primary request type is `implementation`.
The repository is an isolated new-mod candidate based on
VoiceOutputDeviceChanger revision
`49ab2d1e496afaad7e901285843f0ced73c0f636`.

Portable repository governance, APM deployment, CI structure, line-ending
policy, package-host conventions, and documentation topology use that revision
as the repository-family baseline. Product identity, audio transport, helper
process, tests, package contents, and user instructions are target-specific.

## Baseline delta

| Area | Treatment | Reason |
| --- | --- | --- |
| License, CLA terms, PR template, code ownership, line endings, Markdown policy | Reused exactly | Portable repository-family governance |
| Contributor verification commands | Target-specific | The package now contains a plugin and a .NET Framework host |
| APM dependency set and full commit pins | Reused exactly | The same reviewed development Skills apply |
| Event ownership, permissions, action pins, release classifier, checksums, and inert publication gate | Reused with target variables | Portable CI and supply-chain policy |
| Project, plugin, assembly, package, repository, and artifact identity | Replaced | Independent mod and distribution identity |
| Settings UI and selected-endpoint controller | Removed | This mod does not expose endpoint selection; BepInEx settings control enablement and unavailable-host fallback |
| Game-side WASAPI renderer | Replaced | Audio must render in a distinct OBS-selectable process |
| Core queues and lifecycle leases | Reused and extended | Proven portable concurrency primitives now feed pipe frames |
| Companion process, protocol, peer identity, process-tree check, tests, and package configuration | Added | Required by process-capture separation and configurable unavailable-host behavior |
| Domain and architecture documents | Reworked | OBS process trees replace endpoint selection as the governing external contract |

## Evidence ledger

| Fact | Status | Evidence and dependent decision |
| --- | --- | --- |
| Game build, runtime, and platform | Confirmed | Lethal Company v81, Steam Build `22825947`, Steam Manifest `6423525044216269478`, Windows; see [voice playback](../domain/lethal-company-voice-playback.md). |
| BepInEx major and version | Confirmed for build and startup lifecycle observation | The plugin compiles against BepInEx 5.4.21 and packages for BepInExPack 5.4.2305. Runtime instrumentation on BepInEx 5.4.23.5 and Unity 2022.3.62.7762112 recorded plugin-component destruction immediately after Chainloader startup while the game continued initialization. A v81 Remote Voice Split run separately confirmed native host launch, handshake readiness, the persistent OBS-titled window, and its former component-destruction cleanup around the title transition. BepInEx source and Unity lifecycle semantics are recorded under [BepInEx and Unity lifecycle](../domain/bepinex-unity-lifecycle.md). Process-lifetime routing after the component destruction still requires an in-game retest. |
| Plugin identity and version source | Confirmed | Assembly `RemoteVoiceSplit`, GUID `com.aoirint.remotevoicesplit`, display name `Remote Voice Split`, owner `aoirint`, and project `Version` as the release source. |
| Game API, patch timing, and mod set | Confirmed statically and by deterministic branch tests; audible runtime pending | Postfix `StartOfRound.RefreshPlayerVoicePlaybackObjects()` after v81 assigns remote `AudioSource` objects. Host/client, death, spectating, and walkie-talkie scenarios exercise the production selection policy. Clean BepInEx plus this mod is the supported validation set; Unity filter ordering and third-party patch interaction remain unverified. |
| OBS process capture | Confirmed statically; runtime pending | Windows captures a selected process and descendants. The audio host must be outside the game process tree; see [OBS process audio capture](../domain/obs-process-audio-capture.md). |
| Package host | Confirmed | Thunderstore Lethal Company package using the repository-family archive layout. |
| Release mode | Confirmed | Project version `0.1.0-alpha.4` publishes an immutable GitHub prerelease. Stable GitHub and Thunderstore publication remain disabled. |
| GitHub Actions and Releases | Confirmed through `0.1.0-alpha.3`; `0.1.0-alpha.4` pending | The public repository enforces protected-branch checks and pinned Actions. Workflows retain validated artifacts and publish SemVer prereleases only after integrated lint, test, plan, build, archive, and checksum gates pass. |
| Thunderstore | Yes; publication blocked | Package assets and inert publisher tooling are retained. Namespace authorization, runtime evidence, and publication authorization are blocked. |
| APM | Yes | The pinned family Skill set is retained. Project metadata changes without changing dependency pins. |

## Selected design

The plugin captures post-effect remote-player Unity audio, mixes it on a
background thread, and sends float stereo frames through a session-scoped named
pipe. `RemoteVoiceSplit.AudioHost.exe` receives those frames and renders them
through the current Windows multimedia default endpoint.

The plugin identifies the interactive Windows Explorer shell, verifies its
image path using the opened parent-process handle, and starts the host through
native extended process creation with Explorer as the explicit parent. This
avoids managed Shell COM activation, which produced
`NotImplementedException` in the target Unity Mono runtime. It also keeps the
host outside the game process tree required by OBS capture.

The explicit parent is only a launch attempt, not proof of separation. The
host returns its process identifier during the handshake, and the plugin walks
the actual process ancestry. Routing becomes ready only when:

1. the protocol handshake succeeds;
2. Windows reports the expected game and host PIDs for the pipe peers;
3. the server image path equals the packaged host;
4. the host has opened and started WASAPI rendering; and
5. the host is not the game process or one of its descendants.

If any condition fails, the plugin clears Unity's callback buffer by default,
so remote voice does not leak into the game-audio track. Users can set
`General.FallbackToGameOutput` to `true` through a BepInEx configuration API
to preserve Unity output immediately. Setting `General.Enabled` to `false`
also preserves Unity output and stops new process-audio submissions.

The verified host PID remains stable across recoverable pipe and WASAPI
failures. The host accepts a replacement session on the same unguessable pipe
name, and both sides repeat their peer, image, and ancestry checks. Between a
disconnect and the replacement ready message, the game side applies the
current atomic fallback policy. After the first verified session,
the host waits for reconnection until the verified game-process handle signals
exit. This avoids treating slow Unity startup or scene transitions as
permission to remove the OBS window.

The game-side runtime likewise outlives the BepInEx component. `PluginRuntime`
holds the Harmony instance, router, logger, and integration context in static
process-lifetime state. `Plugin.OnDestroy` no longer exists.
`Application.quitting` is the only normal teardown signal. The built-assembly
test verifies both the absence of component-destruction cleanup and the
application-quit cleanup call path.

## Blocked release branches

- Clean-profile two-player runtime validation has not run.
- OBS source enumeration and two-track recording have not been observed.
- Persistent same-process session recovery and process-lifetime plugin
  ownership have passed the harness but have not been retested through a
  complete target-game startup.
- Physical default-endpoint changes, endpoint disconnection, and game-process
  crashes have not been observed.
- Default-silent and opt-out fallback behavior have not been observed with a
  remote player in the target game.
- Publication credentials and namespace authorization are not configured.

These blockers prevent a compatibility approval, stable GitHub Release, or
Thunderstore upload. They do not block a clearly labeled GitHub alpha,
deterministic implementation, or package validation.

## Completed static verification

- Locked restore, Debug and Release builds, and formatting complete with zero
  warnings.
- Deterministic core, host/client, death, spectating, walkie-talkie, routing
  lifecycle, plugin component/application lifecycle, protocol, ancestry,
  host-buffer, managed-identity, and package mutation tests pass.
- The completed eight-file ZIP passes the production archive validator.
- A local Windows live test started the default-endpoint WASAPI renderer,
  exercised its endpoint-change retirement callback, and started a replacement
  renderer. Deterministic policy tests cover default-silent and opt-out
  decisions.
- Local live audio-host tests used the production native launcher, verified the
  Windows Explorer image before launch, proved the host was outside the test
  process tree, held one connection for sixty seconds, kept the host alive for
  seventeen disconnected seconds, and completed the peer-identity handshake,
  same-PID reconnect, forced host termination, broken-pipe observation, exact
  OBS window-title check, and a new host handshake after crash recovery.
- NuGet reports no known vulnerable or deprecated package in the locked graph.
- ShellCheck, actionlint, pinact, canonical repository-file rendering, and
  Markdown lint pass.
- APM 0.25.0 reproduced the locked Skill files without drift. Its CI audit
  currently reports a `config-consistency` failure because the installed
  subpath dependency directories do not contain their source `apm.yml`; the
  same audit reports ref consistency, deployed files, package set, subset
  selection, and on-disk drift as clean.
