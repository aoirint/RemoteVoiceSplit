using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using RemoteVoiceSplit.Interop.ProcessAudio;

namespace RemoteVoiceSplit.Interop.Game;

internal static class PluginRuntime
{
    private static readonly object Gate = new();
    private static Harmony? _harmony;
    private static ManualLogSource? _logger;
    private static VoiceProcessRouter? _router;
    private static bool _initialized;

    public static bool Initialize(
        ManualLogSource logger,
        int sampleRate,
        int gameProcessId,
        string audioHostPath,
        bool fallbackToGameOutput)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        lock (Gate)
        {
            if (_initialized)
            {
                return false;
            }

            VoiceProcessRouter? router = null;
            Harmony? harmony = null;
            try
            {
                router = new VoiceProcessRouter(
                    logger,
                    sampleRate,
                    gameProcessId,
                    audioHostPath,
                    fallbackToGameOutput);
                IntegrationContext.Initialize(
                    logger,
                    router,
                    fallbackToGameOutput);

                harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
                harmony.PatchAll(typeof(Plugin).Assembly);

                _logger = logger;
                _router = router;
                _harmony = harmony;
                Application.quitting += OnApplicationQuitting;
                _initialized = true;
                return true;
            }
            catch
            {
                IntegrationContext.Clear();
                TryUnpatch(harmony, logger);
                TryDispose(router, logger);
                _harmony = null;
                _router = null;
                _logger = null;
                throw;
            }
        }
    }

    private static void OnApplicationQuitting()
    {
        lock (Gate)
        {
            if (!_initialized)
            {
                return;
            }

            _initialized = false;
            Application.quitting -= OnApplicationQuitting;
            ManualLogSource? logger = _logger;
            Harmony? harmony = _harmony;
            VoiceProcessRouter? router = _router;
            _harmony = null;
            _router = null;
            _logger = null;

            TryLog(
                logger,
                LogLevel.Info,
                "Remote Voice Split is stopping because the game application is quitting.");
            IntegrationContext.Clear();
            TryUnpatch(harmony, logger);
            TryDispose(router, logger);
        }
    }

    private static void TryUnpatch(
        Harmony? harmony,
        ManualLogSource? logger)
    {
        try
        {
            harmony?.UnpatchSelf();
        }
        catch (Exception exception)
        {
            TryLog(
                logger,
                LogLevel.Warning,
                $"Harmony cleanup failed during application shutdown: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static void TryDispose(
        VoiceProcessRouter? router,
        ManualLogSource? logger)
    {
        try
        {
            router?.Dispose();
        }
        catch (Exception exception)
        {
            TryLog(
                logger,
                LogLevel.Warning,
                $"Audio-router cleanup failed during application shutdown: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static void TryLog(
        ManualLogSource? logger,
        LogLevel level,
        string message)
    {
        try
        {
            logger?.Log(level, message);
        }
        catch
        {
            // Diagnostics must not interrupt application shutdown.
        }
    }
}
