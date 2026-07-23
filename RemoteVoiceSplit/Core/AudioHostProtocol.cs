using System;
using System.IO;

namespace RemoteVoiceSplit.Core;

internal static class AudioHostProtocol
{
    public const int Magic = 0x31545652;
    public const int Version = 1;
    public const int Channels = 2;
    public const int Ready = 1;
    public const int MaximumBlockSamples = 16384;

    public static void WriteClientHello(BinaryWriter writer, int sampleRate)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (sampleRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        }

        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(sampleRate);
        writer.Write(Channels);
        writer.Flush();
    }

    public static int ReadClientHello(BinaryReader reader)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        RequireEqual(Magic, reader.ReadInt32(), "magic");
        RequireEqual(Version, reader.ReadInt32(), "version");
        int sampleRate = reader.ReadInt32();
        if (sampleRate <= 0)
        {
            throw new InvalidDataException("The audio host protocol sample rate is invalid.");
        }

        RequireEqual(Channels, reader.ReadInt32(), "channel count");
        return sampleRate;
    }

    public static void WriteServerReady(BinaryWriter writer, int processId)
    {
        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        if (processId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }

        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(Ready);
        writer.Write(processId);
        writer.Flush();
    }

    public static int ReadServerReady(BinaryReader reader)
    {
        if (reader is null)
        {
            throw new ArgumentNullException(nameof(reader));
        }

        RequireEqual(Magic, reader.ReadInt32(), "magic");
        RequireEqual(Version, reader.ReadInt32(), "version");
        RequireEqual(Ready, reader.ReadInt32(), "ready state");
        int processId = reader.ReadInt32();
        if (processId <= 0)
        {
            throw new InvalidDataException("The audio host process identifier is invalid.");
        }

        return processId;
    }

    private static void RequireEqual(int expected, int actual, string field)
    {
        if (actual != expected)
        {
            throw new InvalidDataException($"The audio host protocol {field} is incompatible.");
        }
    }
}
