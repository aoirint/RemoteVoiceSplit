using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace RemoteVoiceSplit.Interop.ProcessAudio;

internal static class DetachedAudioHostLauncher
{
    private const uint ProcessCreateProcess = 0x0080;
    private const uint QueryLimitedInformation = 0x1000;
    private const uint ExtendedStartupInfoPresent = 0x00080000;
    private const int ParentProcessAttribute = 0x00020000;

    public static int Launch(string executablePath, string pipeName, int gameProcessId)
    {
        ValidateArguments(executablePath, pipeName, gameProcessId);

        uint shellProcessId = GetShellProcessId();
        using SafeProcessHandle shellProcess = OpenProcess(
            ProcessCreateProcess | QueryLimitedInformation,
            inheritHandle: false,
            shellProcessId);
        if (shellProcess.IsInvalid)
        {
            throw CreateWin32Exception(
                "Windows could not open the desktop shell as the audio host parent.");
        }

        VerifyShellImage(shellProcess);

        IntPtr attributeList = IntPtr.Zero;
        IntPtr parentProcessValue = IntPtr.Zero;
        bool attributeListInitialized = false;
        try
        {
            IntPtr attributeListSize = IntPtr.Zero;
            _ = InitializeProcThreadAttributeList(
                IntPtr.Zero,
                attributeCount: 1,
                flags: 0,
                ref attributeListSize);
            if (attributeListSize == IntPtr.Zero)
            {
                throw CreateWin32Exception(
                    "Windows could not size the audio host process attribute list.");
            }

            attributeList = Marshal.AllocHGlobal(attributeListSize);
            if (!InitializeProcThreadAttributeList(
                    attributeList,
                    attributeCount: 1,
                    flags: 0,
                    ref attributeListSize))
            {
                throw CreateWin32Exception(
                    "Windows could not initialize the audio host process attribute list.");
            }

            attributeListInitialized = true;
            parentProcessValue = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(parentProcessValue, shellProcess.DangerousGetHandle());
            if (!UpdateProcThreadAttribute(
                    attributeList,
                    flags: 0,
                    new IntPtr(ParentProcessAttribute),
                    parentProcessValue,
                    new IntPtr(IntPtr.Size),
                    IntPtr.Zero,
                    IntPtr.Zero))
            {
                throw CreateWin32Exception(
                    "Windows could not assign the desktop shell as the audio host parent.");
            }

            return LaunchWithAttributes(
                executablePath,
                pipeName,
                gameProcessId,
                attributeList);
        }
        finally
        {
            if (attributeListInitialized)
            {
                DeleteProcThreadAttributeList(attributeList);
            }

            if (parentProcessValue != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(parentProcessValue);
            }

            if (attributeList != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(attributeList);
            }
        }
    }

    private static void ValidateArguments(
        string executablePath,
        string pipeName,
        int gameProcessId)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("The audio host path is required.", nameof(executablePath));
        }

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("The packaged audio host was not found.", executablePath);
        }

        if (string.IsNullOrWhiteSpace(pipeName))
        {
            throw new ArgumentException("The pipe name is required.", nameof(pipeName));
        }

        if (gameProcessId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gameProcessId));
        }
    }

    private static uint GetShellProcessId()
    {
        IntPtr shellWindow = GetShellWindow();
        if (shellWindow == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                "Windows did not report an interactive desktop shell.");
        }

        _ = GetWindowThreadProcessId(shellWindow, out uint shellProcessId);
        if (shellProcessId == 0)
        {
            throw CreateWin32Exception(
                "Windows did not report the desktop shell process.");
        }

        return shellProcessId;
    }

    private static void VerifyShellImage(SafeProcessHandle shellProcess)
    {
        string windowsDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.Windows);
        if (string.IsNullOrWhiteSpace(windowsDirectory))
        {
            throw new InvalidOperationException(
                "Windows did not report its installation directory.");
        }

        string expectedShellPath = Path.GetFullPath(
            Path.Combine(windowsDirectory, "explorer.exe"));
        string actualShellPath = Path.GetFullPath(
            ProcessImagePath.Get(shellProcess));
        if (!string.Equals(
                expectedShellPath,
                actualShellPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The interactive desktop shell is not the Windows Explorer executable.");
        }
    }

    private static int LaunchWithAttributes(
        string executablePath,
        string pipeName,
        int gameProcessId,
        IntPtr attributeList)
    {
        string fullExecutablePath = Path.GetFullPath(executablePath);
        string commandLine = string.Format(
            CultureInfo.InvariantCulture,
            "{0} --pipe {1} --game-process-id {2}",
            QuoteCommandLineArgument(fullExecutablePath),
            QuoteCommandLineArgument(pipeName),
            gameProcessId);
        var mutableCommandLine = new StringBuilder(commandLine);
        var startupInfo = new StartupInfoEx
        {
            StartupInfo = new StartupInfo
            {
                Size = checked((uint)Marshal.SizeOf<StartupInfoEx>()),
            },
            AttributeList = attributeList,
        };

        if (!CreateProcess(
                fullExecutablePath,
                mutableCommandLine,
                IntPtr.Zero,
                IntPtr.Zero,
                inheritHandles: false,
                ExtendedStartupInfoPresent,
                IntPtr.Zero,
                Path.GetDirectoryName(fullExecutablePath),
                ref startupInfo,
                out ProcessInformation processInformation))
        {
            throw CreateWin32Exception("Windows could not start the audio host process.");
        }

        _ = CloseHandle(processInformation.Thread);
        _ = CloseHandle(processInformation.Process);
        return checked((int)processInformation.ProcessId);
    }

    private static string QuoteCommandLineArgument(string value)
    {
        var quoted = new StringBuilder(value.Length + 2);
        quoted.Append('"');
        int backslashCount = 0;
        foreach (char character in value)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                quoted.Append('\\', checked((backslashCount * 2) + 1));
                quoted.Append(character);
                backslashCount = 0;
                continue;
            }

            quoted.Append('\\', backslashCount);
            backslashCount = 0;
            quoted.Append(character);
        }

        quoted.Append('\\', checked(backslashCount * 2));
        quoted.Append('"');
        return quoted.ToString();
    }

    private static Win32Exception CreateWin32Exception(string message)
    {
        return new Win32Exception(Marshal.GetLastWin32Error(), message);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(
        IntPtr window,
        out uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InitializeProcThreadAttributeList(
        IntPtr attributeList,
        int attributeCount,
        uint flags,
        ref IntPtr size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UpdateProcThreadAttribute(
        IntPtr attributeList,
        uint flags,
        IntPtr attribute,
        IntPtr value,
        IntPtr size,
        IntPtr previousValue,
        IntPtr returnSize);

    [DllImport("kernel32.dll")]
    private static extern void DeleteProcThreadAttributeList(IntPtr attributeList);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    [SuppressMessage(
        "Performance",
        "CA1838:Avoid StringBuilder parameters for P/Invokes",
        Justification = "CreateProcessW requires a writable command-line buffer and the plugin targets netstandard2.1.")]
    private static extern bool CreateProcess(
        string applicationName,
        StringBuilder commandLine,
        IntPtr processAttributes,
        IntPtr threadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
        uint creationFlags,
        IntPtr environment,
        string? currentDirectory,
        ref StartupInfoEx startupInfo,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfo
    {
        public uint Size;
        public IntPtr Reserved;
        public IntPtr Desktop;
        public IntPtr Title;
        public uint X;
        public uint Y;
        public uint XSize;
        public uint YSize;
        public uint XCountChars;
        public uint YCountChars;
        public uint FillAttribute;
        public uint Flags;
        public ushort ShowWindow;
        public ushort ReservedSize;
        public IntPtr ReservedData;
        public IntPtr StandardInput;
        public IntPtr StandardOutput;
        public IntPtr StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct StartupInfoEx
    {
        public StartupInfo StartupInfo;
        public IntPtr AttributeList;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public IntPtr Process;
        public IntPtr Thread;
        public uint ProcessId;
        public uint ThreadId;
    }
}
