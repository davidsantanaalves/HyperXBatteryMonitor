using System.Diagnostics;
using System.Reflection;
using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray;

public sealed class AboutForm : Form
{
    private const string BuyMeACoffeeUrl = "https://buymeacoffee.com/davesantana";
    private const string PixEmail = "davesantana@outlook.com.br";
    private const string PixCopyPasteKey =
        "00020126480014br.gov.bcb.pix0126davesantana@outlook.com.br5204000053039865802BR5901N6001C62110507HXTDAVE63040E13";

    private readonly AppLanguage _language;
    private readonly AppTheme _theme;

    private readonly Label _appNameLabel;
    private readonly Label _versionLabel;
    private readonly Label _createdByLabel;
    private readonly Label _supportLabel;
    private readonly LinkLabel _buyMeACoffeeLink;
    private readonly Label _pixEmailLabel;
    private readonly LinkLabel _pixEmailLink;
    private readonly Label _pixCopyPasteLabel;
    private readonly TextBox _pixKeyTextBox;
    private readonly Button _copyPixButton;
    private readonly PictureBox _pixQrCode;
    private readonly Button _closeButton;

    public AboutForm(AppLanguage language, AppTheme theme)
    {
        _language = language;
        _theme = theme;

        Text = L("AboutTitle");
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(500, 500);

        _appNameLabel = new Label
        {
            Text = "HyperX Battery Tray",
            AutoSize = true,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 2)
        };

        _versionLabel = CreateLabel(GetApplicationVersionText());
        _createdByLabel = CreateLabel(L("AboutCreatedBy"));
        _supportLabel = CreateLabel(L("AboutSupport"), true);

        _buyMeACoffeeLink = new LinkLabel
        {
            Text = L("AboutBuyMeACoffee"),
            AutoSize = true,
            TabStop = true,
            Margin = new Padding(0, 1, 0, 6)
        };
        _buyMeACoffeeLink.LinkClicked += BuyMeACoffeeLink_LinkClicked;

        _pixEmailLabel = CreateLabel(L("AboutPixEmail"), true);
        _pixEmailLink = new LinkLabel
        {
            Text = PixEmail,
            AutoSize = true,
            TabStop = true,
            Margin = new Padding(0, 1, 0, 7)
        };
        _pixEmailLink.LinkClicked += PixEmailLink_LinkClicked;

        _pixCopyPasteLabel = CreateLabel(L("AboutPixCopyPaste"), true);

        _pixKeyTextBox = new TextBox
        {
            Text = PixCopyPasteKey,
            ReadOnly = true,
            Multiline = false,
            ScrollBars = ScrollBars.Horizontal,
            Width = 355,
            Height = 24,
            Margin = new Padding(0, 3, 8, 0),
            Font = new Font("Segoe UI", 8.5F),
            BorderStyle = BorderStyle.FixedSingle
        };

        _copyPixButton = new Button
        {
            Text = L("AboutPixCopy"),
            AutoSize = true,
            Height = 26,
            Margin = new Padding(0, 2, 0, 0)
        };
        _copyPixButton.Click += CopyPixButton_Click;

        _pixQrCode = new PictureBox
        {
            Size = new Size(150, 150),
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = new Padding(0, 10, 0, 0),
            Image = LoadPixQrCode()
        };

        _closeButton = new Button
        {
            Text = L("AboutClose"),
            AutoSize = true,
            DialogResult = DialogResult.OK
        };

        AcceptButton = _closeButton;
        CancelButton = _closeButton;

        BuildLayout();
        ApplyTheme();
    }

    private string L(string key) => Localization.Get(key, _language);

    private static string GetApplicationVersionText()
    {
        Version? version = Assembly.GetEntryAssembly()?.GetName().Version;

        if (version == null)
            return "Version: vN/A";

        return $"Version: v{version.Major}.{version.Minor}.{version.Build}";
    }

    private static Label CreateLabel(string text, bool bold = false)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(
                "Segoe UI",
                9F,
                bold ? FontStyle.Bold : FontStyle.Regular),
            Margin = new Padding(0)
        };
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 12,
            Padding = new Padding(26, 18, 26, 14),
            AutoSize = false
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 188));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        root.Controls.Add(_appNameLabel, 0, 0);
        root.Controls.Add(_versionLabel, 0, 1);
        root.Controls.Add(_createdByLabel, 0, 2);
        root.Controls.Add(_supportLabel, 0, 3);
        root.Controls.Add(_buyMeACoffeeLink, 0, 4);
        root.Controls.Add(_pixEmailLabel, 0, 5);
        root.Controls.Add(_pixEmailLink, 0, 6);
        root.Controls.Add(_pixCopyPasteLabel, 0, 7);

        FlowLayoutPanel copyPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        copyPanel.Controls.Add(_pixKeyTextBox);
        copyPanel.Controls.Add(_copyPixButton);
        root.Controls.Add(copyPanel, 0, 8);

        FlowLayoutPanel qrPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0, 4, 0, 4)
        };
        qrPanel.Controls.Add(_pixQrCode);
        root.Controls.Add(qrPanel, 0, 9);

        root.Controls.Add(new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) }, 0, 10);

        Panel buttonPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        _closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        buttonPanel.Controls.Add(_closeButton);
        buttonPanel.Resize += (_, _) =>
        {
            _closeButton.Location = new Point(
                buttonPanel.ClientSize.Width - _closeButton.Width,
                buttonPanel.ClientSize.Height - _closeButton.Height);
        };
        root.Controls.Add(buttonPanel, 0, 11);

        Controls.Add(root);
    }

    private void ApplyTheme()
    {
        bool dark = _theme == AppTheme.Dark;

        Color background = dark
            ? Color.FromArgb(45, 45, 48)
            : SystemColors.Control;
        Color foreground = dark
            ? Color.WhiteSmoke
            : SystemColors.ControlText;
        Color linkColor = dark
            ? Color.FromArgb(110, 180, 255)
            : Color.FromArgb(0, 102, 204);

        BackColor = background;
        ForeColor = foreground;

        ApplyThemeToControls(Controls, background, foreground);

        _buyMeACoffeeLink.LinkColor = linkColor;
        _buyMeACoffeeLink.ActiveLinkColor = linkColor;
        _buyMeACoffeeLink.VisitedLinkColor = linkColor;

        _pixEmailLink.LinkColor = linkColor;
        _pixEmailLink.ActiveLinkColor = linkColor;
        _pixEmailLink.VisitedLinkColor = linkColor;

        _pixKeyTextBox.BackColor = dark
            ? Color.FromArgb(30, 30, 32)
            : SystemColors.Window;
        _pixKeyTextBox.ForeColor = foreground;

        _copyPixButton.BackColor = dark
            ? Color.FromArgb(60, 60, 64)
            : SystemColors.Control;
        _copyPixButton.ForeColor = foreground;

        _closeButton.BackColor = dark
            ? Color.FromArgb(45, 45, 48)
            : SystemColors.Control;
        _closeButton.ForeColor = foreground;
    }

    private static void ApplyThemeToControls(
        Control.ControlCollection controls,
        Color background,
        Color foreground)
    {
        foreach (Control control in controls)
        {
            if (control is PictureBox || control is TextBox || control is Button)
                continue;

            control.BackColor = background;
            control.ForeColor = foreground;

            if (control.HasChildren)
                ApplyThemeToControls(control.Controls, background, foreground);
        }
    }

    private static Image? LoadPixQrCode()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "PixQrCode.png");

        if (!File.Exists(path))
            return null;

        try
        {
            using FileStream stream = File.OpenRead(path);
            using Image source = Image.FromStream(stream);
            return new Bitmap(source);
        }
        catch
        {
            return null;
        }
    }

    private static void BuyMeACoffeeLink_LinkClicked(
        object? sender,
        LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = BuyMeACoffeeUrl,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private static void PixEmailLink_LinkClicked(
        object? sender,
        LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            Clipboard.SetText(PixEmail);
        }
        catch
        {
        }
    }

    private void CopyPixButton_Click(object? sender, EventArgs e)
    {
        try
        {
            Clipboard.SetText(PixCopyPasteKey);
            MessageBox.Show(
                L("AboutPixCopied"),
                L("AboutTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch
        {
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _pixQrCode.Image?.Dispose();

        base.Dispose(disposing);
    }
}
