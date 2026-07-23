# BepInEx Configuration

## Scope

This document covers the BepInEx 5.4.21 configuration API used by Remote Voice
Split.

## Runtime changes

`ConfigEntry<T>.SettingChanged` fires when its `Value` changes. This includes
changes made through a BepInEx configuration UI or another caller using the
configuration API. `ConfigFile` invokes setting callbacks individually and
contains callback exceptions instead of propagating them to the caller.

The event does not watch the configuration file. An external file edit becomes
visible only when another component calls `ConfigFile.Reload` or when the file
is loaded during a later startup. Remote Voice Split deliberately does not add
its own file watcher.

The BepInEx 5.4.21 source establishes both behaviors in
[`ConfigEntry<T>`](https://github.com/BepInEx/BepInEx/blob/v5.4.21/BepInEx/Configuration/ConfigEntryBase.cs)
and
[`ConfigFile`](https://github.com/BepInEx/BepInEx/blob/v5.4.21/BepInEx/Configuration/ConfigFile.cs).
