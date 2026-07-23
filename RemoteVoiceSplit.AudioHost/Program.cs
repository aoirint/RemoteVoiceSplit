using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace RemoteVoiceSplit.AudioHost;

internal static class Program
{
    private static readonly Regex PipeNamePattern = new(
        @"\ARemoteVoiceSplit-[0-9]+-[0-9a-f]{32}\z",
        RegexOptions.CultureInvariant);

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            (string pipeName, int gameProcessId) = ParseArguments(args);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(defaultValue: false);
            using var form = new AudioHostForm();
            using var session = new AudioHostSession(
                pipeName,
                gameProcessId,
                () =>
                {
                    if (form.IsHandleCreated && !form.IsDisposed)
                    {
                        form.BeginInvoke(new Action(form.Close));
                    }
                });
            form.Shown += (_, _) => session.Start();
            Application.Run(form);
            return 0;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.WriteLine(exception);
            return 1;
        }
    }

    private static (string PipeName, int GameProcessId) ParseArguments(string[] args)
    {
        if (args is null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        string? pipeName = null;
        int gameProcessId = 0;
        for (int index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException("Every audio host option requires a value.", nameof(args));
            }

            switch (args[index])
            {
                case "--pipe":
                    pipeName = args[index + 1];
                    break;
                case "--game-process-id":
                    gameProcessId = int.Parse(
                        args[index + 1],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException($"Unknown audio host option '{args[index]}'.", nameof(args));
            }
        }

        if (pipeName is null || !PipeNamePattern.IsMatch(pipeName))
        {
            throw new ArgumentException("The audio host pipe name is invalid.", nameof(args));
        }

        if (gameProcessId <= 0)
        {
            throw new ArgumentException("The game process identifier is invalid.", nameof(args));
        }

        return (pipeName, gameProcessId);
    }
}
