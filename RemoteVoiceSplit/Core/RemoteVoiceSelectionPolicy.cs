namespace RemoteVoiceSplit.Core;

internal static class RemoteVoiceSelectionPolicy
{
    public static bool ShouldCapture(
        bool isLocalPlayer,
        bool isPlayerControlled,
        bool isPlayerDead,
        bool hasVoiceSource)
    {
        return !isLocalPlayer &&
               (isPlayerControlled || isPlayerDead) &&
               hasVoiceSource;
    }
}
