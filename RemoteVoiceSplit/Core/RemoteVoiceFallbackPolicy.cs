namespace RemoteVoiceSplit.Core;

internal static class RemoteVoiceFallbackPolicy
{
    public const bool DefaultFallbackToGameOutput = false;

    public static bool ShouldClearUnityOutput(
        bool submissionAccepted,
        bool fallbackToGameOutput)
    {
        return submissionAccepted || !fallbackToGameOutput;
    }
}
