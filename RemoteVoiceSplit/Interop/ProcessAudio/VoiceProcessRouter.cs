using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using BepInEx.Logging;
using RemoteVoiceSplit.Core;

namespace RemoteVoiceSplit.Interop.ProcessAudio;

internal sealed class VoiceProcessRouter : IDisposable
{
    private const int CaptureCapacitySamples = 1 << 17;
    private static readonly TimeSpan SessionRetryDelay = TimeSpan.FromSeconds(1);
    private readonly ManualLogSource _logger;
    private readonly VoiceAudioMixer _mixer = new();
    private readonly ManualResetEvent _stop = new(initialState: false);
    private readonly RoutingSessionGate _routingSession = new();
    private readonly Thread _worker;
    private readonly int _sampleRate;
    private readonly int _gameProcessId;
    private readonly string _audioHostPath;
    private readonly RemoteVoiceFallbackState _fallback;
    private bool _disposed;

    public VoiceProcessRouter(
        ManualLogSource logger,
        int sampleRate,
        int gameProcessId,
        string audioHostPath,
        RemoteVoiceFallbackState fallback)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        if (gameProcessId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gameProcessId));
        }

        if (string.IsNullOrWhiteSpace(audioHostPath))
        {
            throw new ArgumentException("The audio host path is required.", nameof(audioHostPath));
        }

        _sampleRate = sampleRate;
        _gameProcessId = gameProcessId;
        _audioHostPath = audioHostPath;
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _worker = new Thread(WorkerMain)
        {
            IsBackground = true,
            Name = "RemoteVoiceSplit pipe sender",
        };
        _worker.Start();
    }

    public bool IsReady => _routingSession.IsReady;

    public VoiceCaptureStream RegisterCapture()
    {
        ThrowIfDisposed();
        return _mixer.Register(CaptureCapacitySamples);
    }

    public void UnregisterCapture(VoiceCaptureStream stream)
    {
        _mixer.Unregister(stream);
    }

    public RoutingSubmissionLease? TrySubmit(VoiceCaptureStream stream, float[] samples, int channels)
    {
        RoutingSubmissionLease? submission = _routingSession.TryBeginSubmission();
        if (submission is null)
        {
            return null;
        }

        if (!stream.TryWrite(samples, channels))
        {
            submission.Dispose();
            return null;
        }

        return submission;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SetNotReady();
        _stop.Set();
        if (_worker.Join(TimeSpan.FromSeconds(7)))
        {
            _stop.Dispose();
        }
        else
        {
            TryLog(
                LogLevel.Warning,
                "The remote voice audio-host thread did not stop within seven seconds; its stop handle was left intact.");
        }
    }

    private void WorkerMain()
    {
        string pipeName = $"RemoteVoiceSplit-{_gameProcessId}-{Guid.NewGuid():N}";
        int hostProcessId = 0;
        string? lastFailure = null;
        while (!_stop.WaitOne(0))
        {
            try
            {
                if (!IsExpectedHostProcess(hostProcessId))
                {
                    hostProcessId = DetachedAudioHostLauncher.Launch(
                        _audioHostPath,
                        pipeName,
                        _gameProcessId);
                }

                RunSession(pipeName, hostProcessId);
                lastFailure = null;
            }
            catch (Exception exception)
            {
                string failure = $"{exception.GetType().Name}: {exception.Message}";
                if (!string.Equals(lastFailure, failure, StringComparison.Ordinal))
                {
                    TryLog(
                        LogLevel.Error,
                        _fallback.FallbackToGameOutput
                            ? $"Remote voice process output is unavailable; Unity output remains enabled. {failure}"
                            : $"Remote voice process output is unavailable; remote voice remains silent until it recovers. {failure}");
                    lastFailure = failure;
                }
            }
            finally
            {
                SetNotReady();
            }

            if (!_stop.WaitOne(SessionRetryDelay))
            {
                continue;
            }
        }
    }

    private void RunSession(string pipeName, int expectedHostProcessId)
    {
        using var pipe = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous);
        pipe.Connect(timeout: 5000);
        using var reader = new BinaryReader(pipe, System.Text.Encoding.UTF8, leaveOpen: true);
        using var writer = new BinaryWriter(pipe, System.Text.Encoding.UTF8, leaveOpen: true);
        AudioHostProtocol.WriteClientHello(writer, _sampleRate);
        int hostProcessId = AudioHostProtocol.ReadServerReady(reader);
        if (hostProcessId != expectedHostProcessId)
        {
            throw new InvalidOperationException(
                "The audio host handshake did not match the launched process.");
        }

        int pipeServerProcessId = NamedPipeServerIdentity.GetServerProcessId(pipe);
        if (hostProcessId != pipeServerProcessId)
        {
            throw new InvalidOperationException(
                "The audio host handshake did not match the actual pipe server process.");
        }

        VerifyExpectedHostProcess(hostProcessId);

        SetReady();
        TryLog(
            LogLevel.Info,
            "Remote voice process output is ready. Select 'Lethal Company Remote Voice Split' in OBS Application Audio Capture.");
        SendAudio(pipe, writer);
    }

    private bool IsExpectedHostProcess(int processId)
    {
        if (processId <= 0)
        {
            return false;
        }

        try
        {
            VerifyExpectedHostProcess(processId);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void VerifyExpectedHostProcess(int processId)
    {
        string expectedHostPath = Path.GetFullPath(_audioHostPath);
        string actualHostPath = Path.GetFullPath(ProcessImagePath.Get(processId));
        if (!string.Equals(
                expectedHostPath,
                actualHostPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The actual pipe server is not the packaged Remote Voice Split audio host.");
        }

        if (ProcessTreeSnapshot.IsSelfOrDescendant(processId, _gameProcessId))
        {
            throw new InvalidOperationException(
                "The audio host remained inside the game process tree and cannot provide an isolated OBS source.");
        }
    }

    private void SendAudio(NamedPipeClientStream pipe, BinaryWriter writer)
    {
        int framesPerBlock = Math.Max(1, _sampleRate / 100);
        int sampleCount = checked(framesPerBlock * AudioHostProtocol.Channels);
        if (sampleCount > AudioHostProtocol.MaximumBlockSamples)
        {
            throw new InvalidOperationException("The runtime sample rate exceeds the audio host protocol block limit.");
        }

        var samples = new float[sampleCount];
        var bytes = new byte[checked(sampleCount * sizeof(float))];
        var heartbeat = Stopwatch.StartNew();
        while (!_stop.WaitOne(TimeSpan.FromMilliseconds(10)))
        {
            if (_mixer.Mix(samples, sampleCount))
            {
                Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
                writer.Write(sampleCount);
                writer.Write(bytes, 0, bytes.Length);
                writer.Flush();
                heartbeat.Restart();
            }
            else if (heartbeat.Elapsed >= TimeSpan.FromSeconds(1))
            {
                writer.Write(0);
                writer.Flush();
                heartbeat.Restart();
            }

            if (!pipe.IsConnected)
            {
                throw new EndOfStreamException("The audio host disconnected.");
            }
        }
    }

    private void SetReady()
    {
        _routingSession.Activate();
    }

    private void SetNotReady()
    {
        _routingSession.Deactivate();
        _mixer.Clear();
    }

    private void TryLog(LogLevel level, string message)
    {
        try
        {
            _logger.Log(level, message);
        }
        catch
        {
            // Diagnostics must not terminate routing or escape a game callback.
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(VoiceProcessRouter));
        }
    }
}
