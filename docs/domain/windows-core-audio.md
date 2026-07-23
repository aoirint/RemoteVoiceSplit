# Windows Core Audio Contract

## Endpoint and render format

The companion host opens
`IMMDeviceEnumerator.GetDefaultAudioEndpoint(eRender, eMultimedia)`.
It does not persist an endpoint identifier or enumerate friendly names. A new
session resolves the then-current multimedia default.

The renderer requests two-channel 32-bit IEEE float samples at Unity's runtime
`AudioSettings.outputSampleRate`. It opens shared event-driven WASAPI with
Windows format and source-quality conversion flags. The Windows audio engine
can therefore adapt this contract to the physical endpoint mix format.

The format is an implementation contract, not a claim that every endpoint runs
natively in stereo float at Unity's rate. Target-device conversion remains a
runtime check.

## COM and thread ownership

- The audio host's render worker initializes COM, resolves the default
  endpoint, and creates, starts, renders, stops, resets, and releases WASAPI
  objects on the same thread.
- Initialization transfers ownership only after all interfaces and the event
  handle are acquired. Failed startup disposes the partial renderer.
- The session periodically compares the active renderer endpoint ID with the
  current multimedia default. A change closes the session so a replacement can
  resolve the new endpoint.
- Cleanup retains the first reported failure while continuing to release the
  remaining COM objects and wait handles.
- Shutdown signals the worker and waits up to five seconds. It never uses
  `Thread.Abort`.

All declared MMDevice and audio-client IIDs were compared with Windows SDK
10.0.26100.0 headers in the repository-family baseline. This static comparison
does not replace runtime endpoint testing. On 2026-07-23, the built companion
host completed a local peer-PID handshake, opened and started the current
multimedia default renderer, and shut down after pipe closure on the
development machine. Other target hardware remains unverified.

## Failure policy

The host reports ready only after `IAudioClient.Start` succeeds. A render,
device, endpoint-change, protocol, pipe, window-close, or game-exit failure
ends the host session and discards queued samples. The game plugin retires
readiness as soon as the pipe fails.

Until a replacement host is connected and verified, Unity callback data
remains unchanged. This fail-open policy prioritizes audible communication
over strict track separation.
