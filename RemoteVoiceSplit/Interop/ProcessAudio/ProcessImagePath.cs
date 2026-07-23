using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace RemoteVoiceSplit.Interop.ProcessAudio;

internal static class ProcessImagePath
{
    private const uint QueryLimitedInformation = 0x1000;

    public static string Get(int processId)
    {
        using SafeProcessHandle process = OpenProcess(
            QueryLimitedInformation,
            inheritHandle: false,
            checked((uint)processId));
        if (process.IsInvalid)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Windows could not open a process for image-path verification.");
        }

        return Get(process);
    }

    public static string Get(SafeProcessHandle process)
    {
        if (process is null)
        {
            throw new System.ArgumentNullException(nameof(process));
        }

        if (process.IsInvalid || process.IsClosed)
        {
            throw new System.ArgumentException(
                "A live process handle is required.",
                nameof(process));
        }

        var path = new StringBuilder(32768);
        int capacity = path.Capacity;
        if (!QueryFullProcessImageName(process, flags: 0, path, ref capacity))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Windows could not read a process executable path.");
        }

        return path.ToString();
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    [SuppressMessage(
        "Performance",
        "CA1838:Avoid StringBuilder parameters for P/Invokes",
        Justification = "The netstandard2.1 plugin uses the bounded mutable buffer required by QueryFullProcessImageName.")]
    private static extern bool QueryFullProcessImageName(
        SafeProcessHandle process,
        uint flags,
        StringBuilder executableName,
        ref int size);
}
