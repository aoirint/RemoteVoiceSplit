using System;
using BepInEx.Configuration;
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
    private static RemoteVoiceFallbackConfiguration? _fallbackConfiguration;
    private static bool _initialized;

    public static bool Initialize(
        ManualLogSource logger,
        int sampleRate,
        int gameProcessId,
        string audioHostPath,
        ConfigEntry<bool> fallbackToGameOutput)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (fallbackToGameOutput is null)
        {
            throw new ArgumentNullException(nameof(fallbackToGameOutput));
        }

        lock (Gate)
        {
            if (_initialized)
            {
                return false;
            }

            VoiceProcessRouter? router = null;
            Harmony? harmony = null;
            RemoteVoiceFallbackConfiguration? fallbackConfiguration = null;
            try
            {
                fallbackConfiguration = new RemoteVoiceFallbackConfiguration(
                    fallbackToGameOutput,
                    logger);
                router = new VoiceProcessRouter(
                    logger,
                    sampleRate,
                    gameProcessId,
                    audioHostPath,
                    fallbackConfiguration.State);
                IntegrationContext.Initialize(
                    logger,
                    router,
                    fallbackConfiguration.State);

                harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
                harmony.PatchAll(typeof(Plugin).Assembly);

                _logger = logger;
                _router = router;
                _harmony = harmony;
                _fallbackConfiguration = fallbackConfiguration;
                Application.quitting += OnApplicationQuitting;
                _initialized = true;
                return true;
            }
            catch
            {
                IntegrationContext.Clear();
                TryUnpatch(harmony, logger);
                TryDisposeRouter(router, logger);
                TryDisposeFallbackConfiguration(fallbackConfiguration, logger);
                _harmony = null;
                _router = null;
                _logger = null;
                _fallbackConfiguration = null;
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
            RemoteVoiceFallbackConfiguration? fallbackConfiguration =
                _fallbackConfiguration;
            _harmony = null;
            _router = null;
            _logger = null;
            _fallbackConfiguration = null;

            TryLog(
                logger,
                LogLevel.Info,
                "Remote Voice Split is stopping because the game application is quitting.");
            IntegrationContext.Clear();
            TryUnpatch(harmony, logger);
            TryDisposeRouter(router, logger);
            TryDisposeFallbackConfiguration(fallbackConfiguration, logger);
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

    private static void TryDisposeRouter(
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

    private static void TryDisposeFallbackConfiguration(
        RemoteVoiceFallbackConfiguration? fallbackConfiguration,
        ManualLogSource? logger)
    {
        try
        {
            fallbackConfiguration?.Dispose();
        }
        catch (Exception exception)
        {
            TryLog(
                logger,
                LogLevel.Warning,
                $"Fallback-configuration cleanup failed during application shutdown: {exception.GetType().Name}: {exception.Message}");
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
