# Domain Documentation

Domain documents answer which external game, platform, package, and automation
contracts the implementation depends on. These facts can change independently
of the mod.

- [Lethal Company voice playback](lethal-company-voice-playback.md) covers
  voice assignment, playback assets, and runtime boundaries.
- [BepInEx and Unity lifecycle](bepinex-unity-lifecycle.md) covers plugin
  component ownership, destruction callbacks, and application-quit signaling.
- [OBS process audio capture](obs-process-audio-capture.md) covers application
  loopback, process trees, and capture-window identity.
- [Windows Core Audio](windows-core-audio.md) covers endpoint identity, render
  format, COM ownership, and failure behavior.
- [Windows process creation](windows-process-creation.md) covers desktop-shell
  identity, explicit parent selection, and executable-path-safe launch.
- [Dependency baseline](dependencies.md) covers NuGet sources, versions,
  distribution roles, and adoption review.
- [GitHub automation dependencies](github-automation.md) covers pinned Actions,
  downloaded tools, and repository policy.
