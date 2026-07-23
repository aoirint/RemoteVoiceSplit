using System;
using RemoteVoiceSplit.Core;

namespace RemoteVoiceSplit.AudioHost;

internal sealed class PcmAudioBuffer
{
    private const int CapacitySamples = 1 << 18;
    private readonly AudioRingBuffer _buffer = new(CapacitySamples);

    public void Write(float[] samples, int sampleCount)
    {
        if (samples is null)
        {
            throw new ArgumentNullException(nameof(samples));
        }

        if (sampleCount < 0 || sampleCount > samples.Length || (sampleCount & 1) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleCount));
        }

        if (_buffer.TryWriteStereo(samples, sampleCount, AudioHostProtocol.Channels))
        {
            return;
        }

        _buffer.Clear();
        if (!_buffer.TryWriteStereo(samples, sampleCount, AudioHostProtocol.Channels))
        {
            throw new InvalidOperationException("An audio protocol block exceeds the host buffer capacity.");
        }
    }

    public bool Read(float[] destination, int sampleCount)
    {
        int read = _buffer.Read(destination, sampleCount);
        if (read < sampleCount)
        {
            Array.Clear(destination, read, sampleCount - read);
        }

        return read > 0;
    }

    public void Clear()
    {
        _buffer.Clear();
    }
}
