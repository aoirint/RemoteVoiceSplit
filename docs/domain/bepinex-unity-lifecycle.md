# BepInEx and Unity Lifecycle

## Scope

This document records the lifecycle facts used by Remote Voice Split for
BepInEx 5 Mono and Unity 2022.3.62. The plugin compiles against BepInEx
5.4.21. Runtime packaging selects BepInExPack 5.4.2305, and the observed
startup used BepInEx 5.4.23.5.

## BepInEx plugin ownership

BepInEx 5.4.21 `Chainloader.Start` creates a shared `BepInEx_Manager`
GameObject, calls `DontDestroyOnLoad` on it, and adds every
`BaseUnityPlugin` as a component. The same Chainloader has no plugin-unload
operation. Its public plugin list only removes Unity-null entries when read.

Source:
[BepInEx 5.4.21 Chainloader](https://github.com/BepInEx/BepInEx/blob/v5.4.21/BepInEx/Bootstrap/Chainloader.cs).
The inspected tag resolves to commit
`0d06996b52c0215a8327b8c69a747f425bbb0023`.
The observed BepInEx 5.4.23.5 runtime retains the same Manager creation,
`DontDestroyOnLoad`, and component attachment flow at commit
`57f1fb859bd4d0264cd2a59074d0e96c6a492a33`.

These facts establish the normal loader intent, but they do not guarantee that
another Unity or game path will never destroy the Manager or a plugin
component.

## Unity destruction and quit notifications

Unity invokes `MonoBehaviour.OnDestroy` when its attached component or
GameObject is destroyed. It can also be called when a Scene ends. It is
therefore not evidence that the player application is quitting.

Unity sends `Application.quitting` when the player application is quitting.
A static event subscription does not depend on the continued existence of the
plugin component that registered it.

Sources:

- [Unity `MonoBehaviour.OnDestroy`](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/MonoBehaviour.OnDestroy.html)
- [Unity `Application.quitting`](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Application-quitting.html)

## Target-build observation

On Lethal Company v81 Steam Build `22825947`, lifecycle instrumentation under
BepInEx 5.4.23.5 and Unity 2022.3.62.7762112 recorded plugin `OnDisable` and
`OnDestroy` immediately after `Chainloader startup complete`. The same log
then recorded normal game initialization and host startup. A later in-game run
also observed Remote Voice Split's former `OnDestroy` log around the title
transition while `Lethal Company.exe` continued running. The audio-host window
remained alive after its lifetime was moved to the verified game process.
This promotes startup component destruction to `runtime_observed`.

A bounded search of the supplied v81 decompilation found the expected startup
Scene loads but no `BepInEx_Manager` string, BepInEx reference, broad root
GameObject destruction loop, or explicit call that identifies the destroyer.
The precise upstream destroy call remains `unknown_runtime`; the integration
must not depend on it.
