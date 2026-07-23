using System.Threading;

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

internal sealed class RemoteVoiceFallbackState
{
    private int _fallbackToGameOutput;

    public RemoteVoiceFallbackState(bool fallbackToGameOutput)
    {
        Update(fallbackToGameOutput);
    }

    public bool FallbackToGameOutput =>
        Volatile.Read(ref _fallbackToGameOutput) != 0;

    public void Update(bool fallbackToGameOutput)
    {
        Volatile.Write(
            ref _fallbackToGameOutput,
            fallbackToGameOutput ? 1 : 0);
    }
}
