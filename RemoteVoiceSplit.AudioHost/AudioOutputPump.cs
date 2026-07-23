using System;
using System.Diagnostics;
using System.Threading;
using RemoteVoiceSplit.AudioHost.Interop.WindowsAudio;

namespace RemoteVoiceSplit.AudioHost;

internal sealed class AudioOutputPump : IDisposable
{
    private static readonly long EndpointCheckIntervalTicks = Stopwatch.Frequency * 2;
    private readonly PcmAudioBuffer _buffer;
    private readonly int _sampleRate;
    private readonly Action<Exception> _onFailure;
    private readonly Func<string> _getDefaultEndpointId;
    private readonly ManualResetEvent _stop = new(initialState: false);
    private readonly ManualResetEventSlim _started = new(initialState: false);
    private readonly Thread _thread;
    private Exception? _startupFailure;
    private bool _disposed;

    public AudioOutputPump(PcmAudioBuffer buffer, int sampleRate, Action<Exception> onFailure)
        : this(buffer, sampleRate, onFailure, AudioEndpointService.GetDefaultRenderEndpointId)
    {
    }

    internal AudioOutputPump(
        PcmAudioBuffer buffer,
        int sampleRate,
        Action<Exception> onFailure,
        Func<string> getDefaultEndpointId)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        _sampleRate = sampleRate;
        _onFailure = onFailure ?? throw new ArgumentNullException(nameof(onFailure));
        _getDefaultEndpointId = getDefaultEndpointId ??
            throw new ArgumentNullException(nameof(getDefaultEndpointId));
        _thread = new Thread(ThreadMain)
        {
            IsBackground = true,
            Name = "RemoteVoiceSplit WASAPI",
        };
        _thread.Start();
    }

    public void WaitUntilStarted(TimeSpan timeout)
    {
        if (!_started.Wait(timeout))
        {
            throw new TimeoutException("The Windows audio renderer did not start in time.");
        }

        if (_startupFailure is not null)
        {
            throw new InvalidOperationException("The Windows audio renderer could not start.", _startupFailure);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stop.Set();
        if (_thread.Join(TimeSpan.FromSeconds(5)))
        {
            _started.Dispose();
            _stop.Dispose();
        }

        _buffer.Clear();
    }

    private void ThreadMain()
    {
        bool startupPublished = false;
        try
        {
            using ComApartmentScope apartment = ComApartmentScope.EnterMultithreaded();
            using WasapiRenderer renderer = WasapiRenderer.Open(_buffer, string.Empty, _sampleRate);
            startupPublished = true;
            _started.Set();
            long nextEndpointCheck = Stopwatch.GetTimestamp() + EndpointCheckIntervalTicks;
            var waits = new WaitHandle[] { _stop, renderer.RenderEvent };
            while (true)
            {
                int signaled = WaitHandle.WaitAny(waits, TimeSpan.FromSeconds(2));
                if (signaled == 0)
                {
                    return;
                }

                if (signaled == 1)
                {
                    renderer.Render();
                }

                if (Stopwatch.GetTimestamp() >= nextEndpointCheck)
                {
                    nextEndpointCheck = Stopwatch.GetTimestamp() + EndpointCheckIntervalTicks;
                    string currentEndpointId = _getDefaultEndpointId();
                    if (!string.Equals(renderer.EndpointId, currentEndpointId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            "The Windows multimedia default endpoint changed.");
                    }
                }
            }
        }
        catch (Exception exception)
        {
            if (!startupPublished)
            {
                _startupFailure = exception;
                _started.Set();
            }
            else if (!_stop.WaitOne(0))
            {
                try
                {
                    _onFailure(exception);
                }
                catch
                {
                    // Closing the pipe is a best-effort failure notification.
                }
            }
        }
    }
}
