using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using RemoteVoiceSplit.Core;

namespace RemoteVoiceSplit.AudioHost;

internal sealed class AudioHostSession : IDisposable
{
    private readonly string _pipeName;
    private readonly int _gameProcessId;
    private readonly Action _onExit;
    private readonly ManualResetEvent _stop = new(initialState: false);
    private readonly Thread _thread;
    private NamedPipeServerStream? _activePipe;
    private bool _disposed;

    public AudioHostSession(string pipeName, int gameProcessId, Action onExit)
    {
        _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
        _gameProcessId = gameProcessId;
        _onExit = onExit ?? throw new ArgumentNullException(nameof(onExit));
        _thread = new Thread(ThreadMain)
        {
            IsBackground = true,
            Name = "RemoteVoiceSplit pipe receiver",
        };
    }

    public void Start()
    {
        _thread.Start();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stop.Set();
        Interlocked.Exchange(ref _activePipe, null)?.Dispose();
        if (_thread.Join(TimeSpan.FromSeconds(5)))
        {
            _stop.Dispose();
        }
    }

    private void ThreadMain()
    {
        try
        {
            Run();
        }
        catch (Exception exception)
        {
            Trace.WriteLine(exception);
        }
        finally
        {
            try
            {
                _onExit();
            }
            catch
            {
                // The UI may already be closing.
            }
        }
    }

    private void Run()
    {
        using Process gameProcess = Process.GetProcessById(_gameProcessId);
        using var pipe = new NamedPipeServerStream(
            _pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
        Interlocked.Exchange(ref _activePipe, pipe);

        IAsyncResult connection = pipe.BeginWaitForConnection(callback: null, state: null);
        using var gameExited = new ManualResetEvent(initialState: gameProcess.HasExited);
        gameProcess.EnableRaisingEvents = true;
        gameProcess.Exited += (_, _) => gameExited.Set();
        if (gameProcess.HasExited)
        {
            gameExited.Set();
        }

        int signaled = WaitHandle.WaitAny(
            new WaitHandle[] { _stop, connection.AsyncWaitHandle, gameExited },
            TimeSpan.FromSeconds(10));
        if (signaled != 1)
        {
            return;
        }

        pipe.EndWaitForConnection(connection);
        if (NamedPipeClientIdentity.GetClientProcessId(pipe) != _gameProcessId)
        {
            throw new InvalidOperationException(
                "The connected pipe client is not the requested game process.");
        }

        using var reader = new BinaryReader(pipe, System.Text.Encoding.UTF8, leaveOpen: true);
        using var writer = new BinaryWriter(pipe, System.Text.Encoding.UTF8, leaveOpen: true);
        int sampleRate = AudioHostProtocol.ReadClientHello(reader);
        var buffer = new PcmAudioBuffer();
        using var pump = new AudioOutputPump(buffer, sampleRate, _ => pipe.Dispose());
        pump.WaitUntilStarted(TimeSpan.FromSeconds(5));
        AudioHostProtocol.WriteServerReady(writer, Process.GetCurrentProcess().Id);

        var bytes = new byte[checked(AudioHostProtocol.MaximumBlockSamples * sizeof(float))];
        var samples = new float[AudioHostProtocol.MaximumBlockSamples];
        while (!_stop.WaitOne(0) && !gameProcess.HasExited)
        {
            int sampleCount = reader.ReadInt32();
            if (sampleCount == 0)
            {
                continue;
            }

            if (sampleCount < 0 ||
                sampleCount > AudioHostProtocol.MaximumBlockSamples ||
                (sampleCount & 1) != 0)
            {
                throw new InvalidDataException("The audio protocol block length is invalid.");
            }

            int byteCount = checked(sampleCount * sizeof(float));
            ReadExactly(reader, bytes, byteCount);
            Buffer.BlockCopy(bytes, 0, samples, 0, byteCount);
            buffer.Write(samples, sampleCount);
        }
    }

    private static void ReadExactly(BinaryReader reader, byte[] destination, int count)
    {
        int offset = 0;
        while (offset < count)
        {
            int read = reader.Read(destination, offset, count - offset);
            if (read == 0)
            {
                throw new EndOfStreamException("The game-side audio pipe disconnected.");
            }

            offset += read;
        }
    }
}
