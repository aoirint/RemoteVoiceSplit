using System;
using System.Reflection;
using HarmonyLib;

namespace RemoteVoiceSplit.Interop.Game;

internal static class GameReflection
{
    public static Type StartOfRoundType { get; } = RequireType("StartOfRound");

    public static FieldInfo AllPlayerScriptsField { get; } = RequireField(StartOfRoundType, "allPlayerScripts");

    public static FieldInfo LocalPlayerControllerField { get; } = RequireField(StartOfRoundType, "localPlayerController");

    public static FieldInfo IsPlayerControlledField { get; } = RequireField(RequireType("GameNetcodeStuff.PlayerControllerB"), "isPlayerControlled");

    public static FieldInfo IsPlayerDeadField { get; } = RequireField(RequireType("GameNetcodeStuff.PlayerControllerB"), "isPlayerDead");

    public static FieldInfo CurrentVoiceSourceField { get; } = RequireField(RequireType("GameNetcodeStuff.PlayerControllerB"), "currentVoiceChatAudioSource");

    public static MethodInfo StartOfRoundRefreshMethod { get; } = RequireMethod(StartOfRoundType, "RefreshPlayerVoicePlaybackObjects");

    private static Type RequireType(string name)
    {
        return AccessTools.TypeByName(name) ?? throw new TypeLoadException($"Lethal Company v81 type '{name}' was not found.");
    }

    private static FieldInfo RequireField(Type type, string name)
    {
        return AccessTools.Field(type, name) ?? throw new MissingFieldException(type.FullName, name);
    }

    private static MethodInfo RequireMethod(Type type, string name)
    {
        return AccessTools.Method(type, name, Type.EmptyTypes)
            ?? throw new MissingMethodException(type.FullName, name);
    }
}
