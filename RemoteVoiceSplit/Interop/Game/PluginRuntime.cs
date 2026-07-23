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
    private static RemoteVoiceConfiguration? _configuration;
    private static bool _initialized;

    public static bool Initialize(
        ManualLogSource logger,
        int sampleRate,
        int gameProcessId,
        string audioHostPath,
        ConfigEntry<bool> enabled,
        ConfigEntry<bool> fallbackToGameOutput)
    {
        if (logger is null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        if (enabled is null)
        {
            throw new ArgumentNullException(nameof(enabled));
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
            RemoteVoiceConfiguration? configuration = null;
            try
            {
                configuration = new RemoteVoiceConfiguration(
                    enabled,
                    fallbackToGameOutput,
                    logger);
                router = new VoiceProcessRouter(
                    logger,
                    sampleRate,
                    gameProcessId,
                    audioHostPath,
                    configuration.State);
                IntegrationContext.Initialize(
                    logger,
                    router,
                    configuration.State);

                harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
                harmony.PatchAll(typeof(Plugin).Assembly);

                _logger = logger;
                _router = router;
                _harmony = harmony;
                _configuration = configuration;
                Application.quitting += OnApplicationQuitting;
                _initialized = true;
                return true;
            }
            catch
            {
                IntegrationContext.Clear();
                TryUnpatch(harmony, logger);
                TryDisposeRouter(router, logger);
                TryDisposeConfiguration(configuration, logger);
                _harmony = null;
                _router = null;
                _logger = null;
                _configuration = null;
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
            RemoteVoiceConfiguration? configuration =
                _configuration;
            _harmony = null;
            _router = null;
            _logger = null;
            _configuration = null;

            TryLog(
                logger,
                LogLevel.Info,
                "Remote Voice Split is stopping because the game application is quitting.");
            IntegrationContext.Clear();
            TryUnpatch(harmony, logger);
            TryDisposeRouter(router, logger);
            TryDisposeConfiguration(configuration, logger);
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

    private static void TryDisposeConfiguration(
        RemoteVoiceConfiguration? configuration,
        ManualLogSource? logger)
    {
        try
        {
            configuration?.Dispose();
        }
        catch (Exception exception)
        {
            TryLog(
                logger,
                LogLevel.Warning,
                $"Configuration cleanup failed during application shutdown: {exception.GetType().Name}: {exception.Message}");
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
