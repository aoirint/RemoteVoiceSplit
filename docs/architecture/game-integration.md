# Game Integration Architecture

This design assumes the base-game behavior documented under
[Lethal Company voice playback](../domain/lethal-company-voice-playback.md)
and the loader behavior documented under
[BepInEx and Unity lifecycle](../domain/bepinex-unity-lifecycle.md).

## Compatibility boundary

The mod compiles without `Assembly-CSharp.dll`. At startup, `GameReflection`
resolves `StartOfRound.RefreshPlayerVoicePlaybackObjects`, player arrays,
local-player identity, eligibility flags, and assigned voice sources by name.
Missing members fail initialization as one guarded transaction instead of
leaving partial patches.

The only Harmony patch is a postfix on the no-argument voice-playback refresh.
Changing the supported game version requires reconfirming that exact overload
and every reflected member before updating the compatibility claim.

## Runtime ownership

The BepInEx `Plugin` component initializes one static process-lifetime
`PluginRuntime`. That runtime owns the Harmony instance, process-audio router,
and `IntegrationContext`. Repeated component initialization is idempotent.

Unity component destruction is not an unload boundary. The plugin therefore
has no `OnDestroy` teardown. The runtime subscribes a static handler to
`Application.quitting` and performs guarded, idempotent cleanup only from that
application-level signal. If the game crashes before Unity sends the signal,
the background router thread ends with the game process and the external host
observes the verified game-process handle becoming signaled.

## Player and network role policy

The integration makes no RPC, ownership, host, server-state, or network
variable change. It waits until the local player object exists.

For each player, the postfix mirrors the game's eligibility rule: controlled
or dead. It excludes the exact local Unity object and activates one
`VoiceCaptureFilter` on the assigned `AudioSource` of each remaining eligible
player. It deactivates filters on pooled sources that are no longer assigned
to an eligible remote player, preventing a reused local source from retaining
remote routing.

The production selection policy is deliberately independent of lobby role and
voice-effect path:

| Local view | Candidate player | Assigned source | Result |
| --- | --- | --- | --- |
| Host or client | Living remote player | Yes | Capture |
| Host or client | Dead remote player | Yes | Capture |
| Dead spectator | Living spectated remote player | Yes | Capture |
| Any | Remote walkie-talkie speaker | Yes | Capture the same processed source |
| Any | Local player, alive or dead | Yes | Keep in Unity |
| Any | Unused slot or missing source | No | Ignore |

Host/client, death, spectating, and walkie-talkie scenarios exercise this exact
production policy in the deterministic harness. The remaining release gate is
an audible two-player observation of Unity filter ordering, not an unimplemented
routing branch.

## Callback containment

The Harmony postfix runs through `IntegrationContext.RunGuarded`. Its body and
diagnostic sink have separate exception boundaries, so logging failure cannot
escape into the game method.

`OnAudioFilterRead` uses a captured registration and commit lease. It clears
Unity's block after the entire block is accepted by a verified ready routing
epoch. When `General.Enabled` is `false`, it submits no new block and preserves
Unity output. If submission is unavailable while enabled, it clears the block
under the default silent policy or preserves it when
`General.FallbackToGameOutput` is enabled. Deactivation
retires that registration and waits for an active commit before unregistering
its queue. The audio callback performs no logging, reflection, COM work,
process enumeration, or steady-state allocation.

This containment does not claim compatibility with arbitrary third-party
transpilers or source replacements. Patch interaction remains part of the
pre-release runtime checks.
