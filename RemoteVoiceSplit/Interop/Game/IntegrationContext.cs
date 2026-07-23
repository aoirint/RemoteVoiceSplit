using System;
using BepInEx.Logging;
using RemoteVoiceSplit.Interop.ProcessAudio;

namespace RemoteVoiceSplit.Interop.Game;

internal static class IntegrationContext
{
    private static ManualLogSource? _logger;
    private static VoiceProcessRouter? _router;

    public static void Initialize(ManualLogSource logger, VoiceProcessRouter router)
    {
        _logger = logger;
        _router = router;
    }

    public static void Clear()
    {
        _router = null;
        _logger = null;
    }

    public static bool TryGetRouter(out VoiceProcessRouter? router)
    {
        router = _router;
        return router is not null;
    }

    public static void RunGuarded(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            TryLog(LogLevel.Error, $"{operation} failed without interrupting the game: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private static void TryLog(LogLevel level, string message)
    {
        try
        {
            _logger?.Log(level, message);
        }
        catch
        {
            // Diagnostics are secondary to preserving the game callback.
        }
    }
}
