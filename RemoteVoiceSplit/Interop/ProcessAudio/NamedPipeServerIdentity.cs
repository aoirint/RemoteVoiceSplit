using System;
using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RemoteVoiceSplit.Interop.ProcessAudio;

internal static class NamedPipeServerIdentity
{
    public static int GetServerProcessId(NamedPipeClientStream pipe)
    {
        if (pipe is null)
        {
            throw new ArgumentNullException(nameof(pipe));
        }

        if (!GetNamedPipeServerProcessId(pipe.SafePipeHandle, out uint processId))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Windows could not identify the audio host pipe server.");
        }

        return checked((int)processId);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeServerProcessId(
        SafePipeHandle pipe,
        out uint serverProcessId);
}
