using System;
using System.Drawing;
using System.Windows.Forms;

namespace RemoteVoiceSplit.AudioHost;

internal sealed class AudioHostForm : Form
{
    public AudioHostForm()
    {
        Text = AudioHostWindowIdentity.Title;
        AccessibleName = AudioHostWindowIdentity.Title;
        ClientSize = new Size(420, 92);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Minimized;

        var label = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            Text = "Remote Voice Split audio host.\r\n" +
                   "Select this window in OBS Application Audio Capture.",
            TextAlign = ContentAlignment.MiddleLeft,
        };
        Controls.Add(label);
    }
}
