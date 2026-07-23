using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using RemoteVoiceSplit.Core;

namespace RemoteVoiceSplit.Interop.ProcessAudio;

internal static class ProcessTreeSnapshot
{
    private const uint SnapshotProcesses = 0x00000002;

    public static bool IsSelfOrDescendant(int candidateProcessId, int ancestorProcessId)
    {
        return ProcessAncestry.IsSelfOrDescendant(
            candidateProcessId,
            ancestorProcessId,
            Capture());
    }

    private static Dictionary<int, int> Capture()
    {
        using SafeSnapshotHandle snapshot = CreateToolhelp32Snapshot(SnapshotProcesses, 0);
        if (snapshot.IsInvalid)
        {
            throw new InvalidOperationException(
                $"Windows could not snapshot the process tree. Error {Marshal.GetLastWin32Error()}.");
        }

        var parents = new Dictionary<int, int>();
        var entry = new ProcessEntry32
        {
            Size = checked((uint)Marshal.SizeOf<ProcessEntry32>()),
        };

        if (!Process32First(snapshot, ref entry))
        {
            throw new InvalidOperationException(
                $"Windows could not read the process tree. Error {Marshal.GetLastWin32Error()}.");
        }

        do
        {
            parents[checked((int)entry.ProcessId)] = checked((int)entry.ParentProcessId);
            entry.Size = checked((uint)Marshal.SizeOf<ProcessEntry32>());
        }
        while (Process32Next(snapshot, ref entry));

        int error = Marshal.GetLastWin32Error();
        const int noMoreFiles = 18;
        if (error != noMoreFiles)
        {
            throw new InvalidOperationException($"Windows process enumeration failed. Error {error}.");
        }

        return parents;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeSnapshotHandle CreateToolhelp32Snapshot(uint flags, uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32First(SafeSnapshotHandle snapshot, ref ProcessEntry32 entry);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Process32Next(SafeSnapshotHandle snapshot, ref ProcessEntry32 entry);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ProcessEntry32
    {
        public uint Size;
        public uint Usage;
        public uint ProcessId;
        public IntPtr DefaultHeapId;
        public uint ModuleId;
        public uint Threads;
        public uint ParentProcessId;
        public int BasePriority;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExecutableFile;
    }

    private sealed class SafeSnapshotHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeSnapshotHandle()
            : base(ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
