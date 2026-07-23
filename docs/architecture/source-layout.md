# Source Layout

```text
RemoteVoiceSplit/
  Core/
  Interop/
    Game/
    ProcessAudio/
  Plugin.cs
  RemoteVoiceSplit.csproj
RemoteVoiceSplit.AudioHost/
  Interop/WindowsAudio/
  RemoteVoiceSplit.AudioHost.csproj
RemoteVoiceSplit.Tests/
RemoteVoiceSplit.slnx
assets/
docs/
```

`RemoteVoiceSplit/` is the BepInEx plugin. `Plugin.cs` owns only environment
discovery, BepInEx configuration binding, and process-lifetime runtime
initialization.
`Interop/Game/PluginRuntime.cs` owns the router, Harmony registration, static
application-quit subscription, and transactional initialization cleanup.
`Interop/Game/RemoteVoiceConfiguration.cs` owns the BepInEx
setting-change subscriptions and updates the atomic enabled and fallback state
shared with the audio paths.
`Core/` owns framework-independent buffering, mixing, protocol framing,
registration leases, unavailable-host policy and state, and process-ancestry
logic.
`Interop/Game/` otherwise owns reflection and Unity callbacks.
`Interop/ProcessAudio/` owns detached host launch, process-tree inspection, and
named-pipe session control. Its launcher uses native Windows extended process
creation rather than managed Shell COM activation.

`RemoteVoiceSplit.AudioHost/` is a Windows .NET Framework 4.8 executable. It
owns the capture window, named-pipe server, bounded receive buffer, Core Audio
COM boundary, and WASAPI thread. It links the protocol and ring-buffer sources
from `Core/`; it does not reference BepInEx, Harmony, Unity, or game code.

`RemoteVoiceSplit.Tests/` is a deterministic Windows-targeted console harness.
It links framework-independent production sources, validates race and protocol
invariants, inspects both managed assemblies with Mono.Cecil, and owns the ZIP
contract mutation suite. It neither launches the game nor requires audio
hardware.

The solution has no base-game assembly dependency. Decompiled code and
serialized assets are research evidence only. CI packages exactly the built
plugin DLL, built host EXE and its generated runtime configuration, package
assets, and license.

Dependency direction is:

```text
Game Interop -> Core <- Process Audio Interop
Audio Host Windows Interop -> Audio Host buffer -> shared Core
Tests -> selected shared Core and package outputs
```
