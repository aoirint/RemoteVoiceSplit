using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RemoteVoiceSplit.Interop.ProcessAudio;

internal static class ShellAudioHostLauncher
{
    private const int ShowMinimizedWithoutActivation = 7;

    public static void Launch(string executablePath, string pipeName, int gameProcessId)
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

        Type shellType = Type.GetTypeFromProgID("Shell.Application", throwOnError: true);
        object shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("Windows Shell.Application could not be created.");
        try
        {
            string arguments = string.Format(
                CultureInfo.InvariantCulture,
                "--pipe {0} --game-process-id {1}",
                pipeName,
                gameProcessId);
            shellType.InvokeMember(
                "ShellExecute",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: new object[]
                {
                    executablePath,
                    arguments,
                    Path.GetDirectoryName(executablePath) ?? string.Empty,
                    "open",
                    ShowMinimizedWithoutActivation,
                },
                modifiers: null,
                culture: CultureInfo.InvariantCulture,
                namedParameters: null);
        }
        finally
        {
            if (Marshal.IsComObject(shell))
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
    }
}
