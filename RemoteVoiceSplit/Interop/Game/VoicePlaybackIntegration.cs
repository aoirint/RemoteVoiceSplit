using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using RemoteVoiceSplit.Core;
using RemoteVoiceSplit.Interop.ProcessAudio;

namespace RemoteVoiceSplit.Interop.Game;

internal static class VoicePlaybackIntegration
{
    public static void Refresh(
        object startOfRound,
        VoiceProcessRouter router,
        RemoteVoiceFallbackState fallback)
    {
        object? localPlayer = GameReflection.LocalPlayerControllerField.GetValue(startOfRound);
        if (localPlayer is null)
        {
            return;
        }

        if (GameReflection.AllPlayerScriptsField.GetValue(startOfRound) is not Array players)
        {
            return;
        }

        var routedSources = new HashSet<AudioSource>();
        foreach (object? player in players)
        {
            if (player is null)
            {
                continue;
            }

            bool isLocalPlayer = IsSameUnityObject(player, localPlayer);
            bool isControlled = (bool)(GameReflection.IsPlayerControlledField.GetValue(player) ?? false);
            bool isDead = (bool)(GameReflection.IsPlayerDeadField.GetValue(player) ?? false);
            AudioSource? source = GameReflection.CurrentVoiceSourceField.GetValue(player) as AudioSource;
            if (!RemoteVoiceSelectionPolicy.ShouldCapture(
                    isLocalPlayer,
                    isControlled,
                    isDead,
                    source != null))
            {
                continue;
            }

            // The policy guarantees a live source before this branch is reached.
            if (source == null)
            {
                continue;
            }

            routedSources.Add(source);
            VoiceCaptureFilter filter = source.GetComponent<VoiceCaptureFilter>() ?? source.gameObject.AddComponent<VoiceCaptureFilter>();
            filter.Initialize(
                router,
                fallback);
        }

        VoiceCaptureFilter[] filters = UnityEngine.Object.FindObjectsOfType<VoiceCaptureFilter>(true);
        foreach (VoiceCaptureFilter filter in filters)
        {
            AudioSource? source = filter.GetComponent<AudioSource>();
            if (source == null || !routedSources.Contains(source))
            {
                filter.Deactivate();
            }
        }
    }

    private static bool IsSameUnityObject(object left, object right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        return left is UnityEngine.Object leftObject &&
               right is UnityEngine.Object rightObject &&
               leftObject == rightObject;
    }
}

[HarmonyPatch]
internal static class RefreshPlayerVoicePlaybackObjectsPatch
{
    private static MethodInfo TargetMethod()
    {
        return GameReflection.StartOfRoundRefreshMethod;
    }

    [HarmonyPostfix]
    private static void Postfix(object __instance)
    {
        IntegrationContext.RunGuarded(
            "Remote voice source attachment",
            () =>
            {
                if (IntegrationContext.TryGetRouting(
                        out VoiceRoutingContext? routing) &&
                    routing is not null)
                {
                    VoicePlaybackIntegration.Refresh(
                        __instance,
                        routing.Router,
                        routing.Fallback);
                }
            });
    }
}
