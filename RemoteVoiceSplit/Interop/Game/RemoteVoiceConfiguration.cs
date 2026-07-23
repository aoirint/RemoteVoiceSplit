using System;
using BepInEx.Configuration;
using BepInEx.Logging;
using RemoteVoiceSplit.Core;

namespace RemoteVoiceSplit.Interop.Game;

internal sealed class RemoteVoiceConfiguration : IDisposable
{
    private readonly ConfigEntry<bool> _enabledEntry;
    private readonly ConfigEntry<bool> _fallbackEntry;
    private readonly ManualLogSource _logger;
    private bool _disposed;

    public RemoteVoiceConfiguration(
        ConfigEntry<bool> enabledEntry,
        ConfigEntry<bool> fallbackEntry,
        ManualLogSource logger)
    {
        _enabledEntry = enabledEntry ?? throw new ArgumentNullException(nameof(enabledEntry));
        _fallbackEntry = fallbackEntry ?? throw new ArgumentNullException(nameof(fallbackEntry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        State = new RemoteVoiceSettingsState(
            enabledEntry.Value,
            fallbackEntry.Value);
        _enabledEntry.SettingChanged += OnEnabledSettingChanged;
        _fallbackEntry.SettingChanged += OnFallbackSettingChanged;

        // Close the small window between the initial read and subscription.
        State.Update(
            _enabledEntry.Value,
            _fallbackEntry.Value);
    }

    public RemoteVoiceSettingsState State { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _enabledEntry.SettingChanged -= OnEnabledSettingChanged;
        _fallbackEntry.SettingChanged -= OnFallbackSettingChanged;
    }

    private void OnEnabledSettingChanged(object sender, EventArgs args)
    {
        if (!TryApply())
        {
            return;
        }

        TryLog(
            LogLevel.Info,
            State.Enabled
                ? "Remote Voice Split is enabled."
                : "Remote Voice Split is disabled; remote voices remain on the normal game output.");
    }

    private void OnFallbackSettingChanged(object sender, EventArgs args)
    {
        if (!TryApply())
        {
            return;
        }

        TryLog(
            LogLevel.Info,
            State.FallbackToGameOutput
                ? "Remote voice fallback to normal game output is enabled."
                : "Remote voice fallback to normal game output is disabled; unavailable remote voice will remain silent.");
    }

    private bool TryApply()
    {
        try
        {
            State.Update(
                _enabledEntry.Value,
                _fallbackEntry.Value);
            return true;
        }
        catch (Exception exception)
        {
            TryLog(
                LogLevel.Error,
                $"Remote voice configuration could not be applied: {exception.GetType().Name}: {exception.Message}");
            return false;
        }
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
