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
        bool keepVoiceOnGameOutputWhenHostUnavailable)
    {
        CaptureRegistration? current = _registration.Read();
        if (current is not null &&
            ReferenceEquals(current.Router, router) &&
            current.KeepVoiceOnGameOutputWhenHostUnavailable ==
            keepVoiceOnGameOutputWhenHostUnavailable)
        {
            enabled = true;
            return;
        }

        Deactivate();
        var registration = new CaptureRegistration(
            router,
            router.RegisterCapture(),
            keepVoiceOnGameOutputWhenHostUnavailable);
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

        using RoutingSubmissionLease? submission = registration.Router.TrySubmit(registration.Stream, data, channels);
        bool submissionAccepted = submission is not null;
        if (!RemoteVoiceFallbackPolicy.ShouldClearUnityOutput(
                submissionAccepted,
                registration.KeepVoiceOnGameOutputWhenHostUnavailable))
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
            bool keepVoiceOnGameOutputWhenHostUnavailable)
        {
            Router = router;
            Stream = stream;
            KeepVoiceOnGameOutputWhenHostUnavailable =
                keepVoiceOnGameOutputWhenHostUnavailable;
        }

        public VoiceProcessRouter Router { get; }

        public VoiceCaptureStream Stream { get; }

        public bool KeepVoiceOnGameOutputWhenHostUnavailable { get; }

        public AtomicCommitLease CommitLease { get; } = new();
    }
}
