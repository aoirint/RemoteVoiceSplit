using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using RemoteVoiceSplit.Core;
using RemoteVoiceSplit.Interop.Game;

namespace RemoteVoiceSplit;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Lethal Company.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Logger.LogError("Remote Voice Split requires Windows process audio capture and has been disabled on this platform.");
            return;
        }

        try
        {
            ConfigEntry<bool> enabled = Config.Bind(
                "General",
                "Enabled",
                RemoteVoiceOutputPolicy.DefaultEnabled,
                "Set to false to disable this mod. Changes made through BepInEx configuration APIs apply immediately.");

            ConfigEntry<bool> fallbackToGameOutput = Config.Bind(
                "General",
                "FallbackToGameOutput",
                RemoteVoiceOutputPolicy.DefaultFallbackToGameOutput,
                "Keep remote voices on the normal game output whenever separate process output cannot accept them. " +
                "The default false value prevents remote voice from leaking into the game-audio recording track, " +
                "but also makes remote voice inaudible until separate output recovers. " +
                "Changes made through BepInEx configuration APIs apply immediately.");

            int sampleRate = AudioSettings.outputSampleRate;
            if (sampleRate <= 0)
            {
                sampleRate = 48000;
                Logger.LogWarning("Unity did not report an output sample rate; falling back to 48000 Hz for remote voice process output.");
            }

            string pluginDirectory = Path.GetDirectoryName(typeof(Plugin).Assembly.Location)
                ?? throw new InvalidOperationException("The plugin assembly directory is unavailable.");
            string audioHostPath = Path.Combine(pluginDirectory, "RemoteVoiceSplit.AudioHost.exe");
            bool initialized = PluginRuntime.Initialize(
                Logger,
                sampleRate,
                Process.GetCurrentProcess().Id,
                audioHostPath,
                enabled,
                fallbackToGameOutput);
            if (initialized)
            {
                Logger.LogInfo(
                    $"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded.");
            }
            else
            {
                Logger.LogInfo(
                    "Remote Voice Split process-lifetime routing was already initialized.");
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(
                $"Remote Voice Split could not initialize and was disabled: {exception.GetType().Name}: {exception.Message}");
        }
    }
}
