namespace RemoteVoiceSplit.Core;

internal static class RemoteVoiceFallbackPolicy
{
    public const bool DefaultKeepVoiceOnGameOutputWhenHostUnavailable = false;

    public static bool ShouldClearUnityOutput(
        bool submissionAccepted,
        bool keepVoiceOnGameOutputWhenHostUnavailable)
    {
        return submissionAccepted || !keepVoiceOnGameOutputWhenHostUnavailable;
    }
}
