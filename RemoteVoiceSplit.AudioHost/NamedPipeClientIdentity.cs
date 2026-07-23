using System;
using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace RemoteVoiceSplit.AudioHost;

internal static class NamedPipeClientIdentity
{
    public static int GetClientProcessId(NamedPipeServerStream pipe)
    {
        if (pipe is null)
        {
            throw new ArgumentNullException(nameof(pipe));
        }

        if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out uint processId))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Windows could not identify the game-side pipe client.");
        }

        return checked((int)processId);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(
        SafePipeHandle pipe,
        out uint clientProcessId);
}
