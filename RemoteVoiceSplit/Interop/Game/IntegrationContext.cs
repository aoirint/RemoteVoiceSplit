using System;
using BepInEx.Logging;
using RemoteVoiceSplit.Core;
using RemoteVoiceSplit.Interop.ProcessAudio;

namespace RemoteVoiceSplit.Interop.Game;

internal static class IntegrationContext
{
    private static ManualLogSource? _logger;
    private static VoiceRoutingContext? _routing;

    public static void Initialize(
        ManualLogSource logger,
        VoiceProcessRouter router,
        RemoteVoiceFallbackState fallback)
    {
        _logger = logger;
        _routing = new VoiceRoutingContext(
            router,
            fallback);
    }

    public static void Clear()
    {
        _routing = null;
        _logger = null;
    }

    public static bool TryGetRouting(out VoiceRoutingContext? routing)
    {
        routing = _routing;
        return routing is not null;
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

internal sealed class VoiceRoutingContext
{
    public VoiceRoutingContext(
        VoiceProcessRouter router,
        RemoteVoiceFallbackState fallback)
    {
        Router = router;
        Fallback = fallback;
    }

    public VoiceProcessRouter Router { get; }

    public RemoteVoiceFallbackState Fallback { get; }
}
