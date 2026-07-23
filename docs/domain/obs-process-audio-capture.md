# OBS Process Audio Capture

## Target

This integration targets OBS Studio application audio capture on Windows 10
version 2004 or later and Windows 11.

OBS Studio exposes per-application audio sources and permits those sources to
be assigned to separate recording tracks. OBS selects the application through
a window, so a capture-oriented audio process needs a stable executable and
window title across game sessions.

Authoritative references:

- [OBS Application Audio Capture Guide](https://obsproject.com/kb/application-audio-capture-guide)
- [Microsoft application loopback audio sample](https://learn.microsoft.com/en-us/samples/microsoft/windows-classic-samples/applicationloopbackaudio-sample/)

## Process-tree boundary

Windows process loopback capture includes audio rendered by the selected
process and its child processes. It is endpoint-independent: the capture
continues to follow the process even when that process renders through another
physical output endpoint.

This creates two requirements for a game-audio separator:

1. the remote-voice renderer must be a different process so OBS can select it;
2. that renderer must not be a descendant of the game process, or a capture of
   the game process can include the remote-voice renderer as well.

Creating a new console or process group does not change the parent/descendant
relationship. The integration must verify the actual process ancestry instead
of treating process-creation flags as proof of separation.

## Capture-window identity

The dedicated renderer exposes a normal top-level window with stable identity:

| Property | Value |
| --- | --- |
| Executable | `RemoteVoiceSplit.AudioHost.exe` |
| Window title | `Lethal Company Remote Voice Split` |

The window starts minimized but remains available to the OBS application
selector. Closing it disconnects alternate playback; the game-side integration
must then leave later voice samples on Unity's normal output.

## Runtime boundaries

Static documentation cannot establish:

- whether a particular OBS release enumerates the minimized host window on a
  specific Windows installation;
- whether another capture source also includes the host through a broader
  desktop or process-tree selection;
- track timing after OBS resampling, monitoring, filters, and encoder buffering;
  or
- behavior when third-party capture plugins replace OBS Studio's built-in
  process capture.

The release runtime protocol therefore checks source selection, process-tree
separation, duplicate/missing audio, and the recorded track contents.
