using System.Threading;

namespace RemoteVoiceSplit.Core;

internal static class RemoteVoiceOutputPolicy
{
    public const bool DefaultEnabled = true;
    public const bool DefaultFallbackToGameOutput = false;

    public static bool ShouldClearUnityOutput(
        bool enabled,
        bool submissionAccepted,
        bool fallbackToGameOutput)
    {
        return enabled &&
               (submissionAccepted || !fallbackToGameOutput);
    }
}

internal sealed class RemoteVoiceSettingsState
{
    private int _enabled;
    private int _fallbackToGameOutput;

    public RemoteVoiceSettingsState(
        bool enabled,
        bool fallbackToGameOutput)
    {
        Update(
            enabled,
            fallbackToGameOutput);
    }

    public bool Enabled =>
        Volatile.Read(ref _enabled) != 0;

    public bool FallbackToGameOutput =>
        Volatile.Read(ref _fallbackToGameOutput) != 0;

    public void Update(
        bool enabled,
        bool fallbackToGameOutput)
    {
        Volatile.Write(
            ref _enabled,
            enabled ? 1 : 0);
        Volatile.Write(
            ref _fallbackToGameOutput,
            fallbackToGameOutput ? 1 : 0);
    }
}
