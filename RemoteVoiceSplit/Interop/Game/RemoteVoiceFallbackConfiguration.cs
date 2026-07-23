using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using RemoteVoiceSplit.Core;

namespace RemoteVoiceSplit.Interop.Game;

internal sealed class RemoteVoiceFallbackConfiguration : IDisposable
{
    private readonly ConfigEntry<bool> _entry;
    private readonly ManualLogSource _logger;
    private bool _disposed;

    public RemoteVoiceFallbackConfiguration(
        ConfigEntry<bool> entry,
        ManualLogSource logger)
    {
        _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        State = new RemoteVoiceFallbackState(entry.Value);
        _entry.SettingChanged += OnSettingChanged;

        // Close the small window between the initial read and subscription.
        State.Update(_entry.Value);
    }

    public RemoteVoiceFallbackState State { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _entry.SettingChanged -= OnSettingChanged;
    }

    private void OnSettingChanged(object sender, EventArgs args)
    {
        bool fallbackToGameOutput;
        try
        {
            fallbackToGameOutput = _entry.Value;
            State.Update(fallbackToGameOutput);
        }
        catch (Exception exception)
        {
            TryLog(
                LogLevel.Error,
                $"Remote voice fallback configuration could not be applied: {exception.GetType().Name}: {exception.Message}");
            return;
        }

        TryLog(
            LogLevel.Info,
            fallbackToGameOutput
                ? "Remote voice fallback to normal game output is enabled."
                : "Remote voice fallback to normal game output is disabled; unavailable remote voice will remain silent.");
    }

    private void TryLog(LogLevel level, string message)
    {
        try
        {
            _logger.Log(level, message);
        }
        catch
        {
            // Diagnostics must not prevent a configuration change from applying.
        }
    }
}
