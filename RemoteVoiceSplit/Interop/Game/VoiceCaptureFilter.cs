using System;
using UnityEngine;
using RemoteVoiceSplit.Core;
using RemoteVoiceSplit.Interop.ProcessAudio;

namespace RemoteVoiceSplit.Interop.Game;

internal sealed class VoiceCaptureFilter : MonoBehaviour
{
    private readonly AtomicRegistration<CaptureRegistration> _registration = new();

    public void Initialize(
        VoiceProcessRouter router,
        RemoteVoiceSettingsState settings)
    {
        CaptureRegistration? current = _registration.Read();
        if (current is not null &&
            ReferenceEquals(current.Router, router) &&
            ReferenceEquals(current.Settings, settings))
        {
            enabled = true;
            return;
        }

        Deactivate();
        var registration = new CaptureRegistration(
            router,
            router.RegisterCapture(),
            settings);
        _registration.Exchange(registration);
        enabled = true;
    }

    public void Deactivate()
    {
        enabled = false;
        CaptureRegistration? registration = _registration.Exchange(null);
        if (registration is not null)
        {
            registration.CommitLease.Retire();
            registration.Router.UnregisterCapture(registration.Stream);
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        CaptureRegistration? registration = _registration.Read();
        if (registration is null)
        {
            return;
        }

        bool enabled = registration.Settings.Enabled;
        if (!enabled)
        {
            registration.Stream.Clear();
            return;
        }

        using RoutingSubmissionLease? submission = registration.Router.TrySubmit(registration.Stream, data, channels);
        bool submissionAccepted = submission is not null;
        if (!RemoteVoiceOutputPolicy.ShouldClearUnityOutput(
                enabled,
                submissionAccepted,
                registration.Settings.FallbackToGameOutput))
        {
            return;
        }

        if (!submissionAccepted)
        {
            registration.Stream.Clear();
        }

        if (!registration.CommitLease.TryBegin())
        {
            registration.Stream.Clear();
            return;
        }

        try
        {
            if (!_registration.IsCurrent(registration))
            {
                registration.Stream.Clear();
                return;
            }

            Array.Clear(data, 0, data.Length);
        }
        finally
        {
            registration.CommitLease.End();
        }
    }

    private void OnDestroy()
    {
        Deactivate();
    }

    private sealed class CaptureRegistration
    {
        public CaptureRegistration(
            VoiceProcessRouter router,
            VoiceCaptureStream stream,
            RemoteVoiceSettingsState settings)
        {
            Router = router;
            Stream = stream;
            Settings = settings;
        }

        public VoiceProcessRouter Router { get; }

        public VoiceCaptureStream Stream { get; }

        public RemoteVoiceSettingsState Settings { get; }

        public AtomicCommitLease CommitLease { get; } = new();
    }
}
