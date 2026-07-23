# Lethal Company Voice Playback

## Target

| Item | Value |
| --- | --- |
| Steam Build ID | `22825947` |
| Steam Manifest ID | `6423525044216269478` |

The supported game version is v81. The members and assets below are
compatibility targets for this build. Reconfirm them when changing the
supported game version.

## Voice playback assignment

`StartOfRound.RefreshPlayerVoicePlaybackObjects` connects Dissonance playback
objects to player objects:

1. It returns until `GameNetworkManager.Instance.localPlayerController` exists.
2. It finds active and inactive `PlayerVoiceIngameSettings` objects.
3. It skips a player only when that player is neither controlled nor dead.
4. It matches the Dissonance player state to a player object.
5. It assigns `currentVoiceChatAudioSource`,
   `currentVoiceChatIngameSettings`, and the per-player mixer group.

`StartOfRound.UpdatePlayerVoiceEffects` then updates the same sources for death,
spectating, walkie-talkie use, spatial blend, filters, pitch, and volume. It
explicitly excludes the local player.

The update has no server-authority guard. A host and a non-host client each
evaluate their own local listener and remote playback sources. When the local
player is dead and has a spectated player, that spectated player becomes the
listener reference used for walkie-talkie reception. Remote dead players keep
their assigned source: dead listeners receive it as two-dimensional audio,
while living listeners receive silence for that dead source.

For a living remote player, the walkie-talkie path is active when the speaker
is transmitting, the listener reference is holding a walkie-talkie, and the
speaker is not that listener reference. The game keeps the same playback
source and changes its high-pass and low-pass behavior, spatial blend, pan, and
per-player mixer assignment. Direct voice and walkie-talkie voice therefore do
not require separate source discovery.

`PlayerVoiceIngameSettings` belongs to the Dissonance playback object, owns its
`voiceAudio` source, and reconnects the player state when enabled. This makes
the assigned `AudioSource` a stable integration point after the refresh, while
the exact ordering of an added Unity audio filter remains a runtime concern.

## Playback prefab and audio settings

`SampleSceneRelay.unity` assigns the Dissonance playback prefab reference to
`SpatializedPlaybackPrefabModified.prefab`. The prefab contains:

- `PlayerVoiceIngameSettings`;
- an `AudioSource`;
- high-pass, low-pass, reverb, and chorus filters; and
- an initially inactive playback object used by Dissonance.

`AudioManager.asset` configures stereo default speaker mode, the system sample
rate (`m_SampleRate: 0`), a 1024-sample DSP buffer, and Steam Audio
spatialization. Integrations should read Unity's runtime sample rate rather
than assuming a fixed value.

## Integration targets

| Type | Members |
| --- | --- |
| `StartOfRound` | `RefreshPlayerVoicePlaybackObjects`, `UpdatePlayerVoiceEffects`, `allPlayerScripts`, `localPlayerController` |
| `PlayerControllerB` | `isPlayerControlled`, `isPlayerDead`, `currentVoiceChatAudioSource` |

The mod attaches after the playback refresh, preserves the game's source
assignment and voice-effect rules, and captures the resulting Unity audio from
eligible remote-player sources.

## Runtime boundaries

The following behavior must be checked in the game before a stable release:

- `OnAudioFilterRead` ordering relative to Steam Audio and the voice filters;
- audible channel ordering and spatial behavior on target hardware;
- audible host, client, death, spectating, and walkie-talkie behavior;
- audio-host closure, endpoint invalidation, and default-device changes; and
- interaction with other mods that patch the same methods or audio sources.

Follow the [pre-release runtime checks](../operations/release.md#pre-release-runtime-checks)
for the release procedure.

## Change checklist

When the supported game version changes:

1. update the target identifiers above;
2. confirm every integration target still exists with the same role;
3. confirm the relay scene, playback prefab, and audio settings still have the
   described relationships; and
4. repeat the pre-release runtime checks.
