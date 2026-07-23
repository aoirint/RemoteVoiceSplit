using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx;
using UnityEngine;
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
                audioHostPath);
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
