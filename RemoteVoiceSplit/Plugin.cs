using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using RemoteVoiceSplit.Interop.Game;
using RemoteVoiceSplit.Interop.ProcessAudio;

namespace RemoteVoiceSplit;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Lethal Company.exe")]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Unity owns MonoBehaviour lifetime; OnDestroy performs the deterministic rollback.")]
public sealed class Plugin : BaseUnityPlugin
{
    private Harmony? _harmony;
    private VoiceProcessRouter? _router;

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
            _router = new VoiceProcessRouter(
                Logger,
                sampleRate,
                Process.GetCurrentProcess().Id,
                audioHostPath);
            IntegrationContext.Initialize(Logger, _router);

            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo(
                $"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded for Lethal Company v81.");
        }
        catch (Exception exception)
        {
            IntegrationContext.Clear();
            _harmony?.UnpatchSelf();
            _router?.Dispose();
            _harmony = null;
            _router = null;
            Logger.LogError(
                $"Remote Voice Split could not initialize and was disabled: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private void OnDestroy()
    {
        IntegrationContext.Clear();
        _harmony?.UnpatchSelf();
        _router?.Dispose();
        _harmony = null;
        _router = null;
    }
}
