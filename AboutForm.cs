using System.Diagnostics;
using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray;

public sealed class AboutForm : Form
{
    private const string BuyMeACoffeeUrl =
        "https://buymeacoffee.com/davesantana";

    private const string PixCopyPasteKey =
        "00020126480014br.gov.bcb.pix0126davesantana@outlook.com.br5204000053039865802BR5901N6001C62110507HXTDAVE63040E13";

    private readonly AppLanguage _language;
    private readonly AppTheme _theme;

    private readonly Label _appNameLabel;
    private readonly Label _versionLabel;
    private readonly Label _createdByLabel;
    private readonly Label _supportLabel;
    private readonly LinkLabel _buyMeACoffeeLink;
    private readonly Label _pixLabel;
    private readonly Label _pixCopyPasteLabel;
    private readonly LinkLabel _pixKeyLink;
    private readonly PictureBox _pixQrCode;
    private readonly Button _closeButton;
	private readonly System.Windows.Forms.Timer _pixCopiedTimer;

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

        _versionLabel = CreateLabel(L("AboutVersion"));

        _createdByLabel = CreateLabel(
            L("AboutCreatedBy"),
            false);

        _supportLabel = CreateLabel(
            $"☕ {L("AboutSupport")}",
            true);

        _buyMeACoffeeLink = new LinkLabel
		{
			Text = BuyMeACoffeeUrl,
			AutoSize = true,
			TabStop = true,
			Margin = new Padding(0, 2, 0, 12)
		};

        _buyMeACoffeeLink.LinkClicked +=
            BuyMeACoffeeLink_LinkClicked;

        _pixLabel = CreateLabel(
            $"💴 {L("AboutPixEmail")}",
            true);

        _pixCopyPasteLabel = CreateLabel(
            L("AboutPixCopyPaste"),
            false);

        _pixKeyLink = new LinkLabel
        {
            Text = PixCopyPasteKey,
            AutoSize = false,
            Width = 448,
            Height = 48,
            TabStop = true,
            Margin = new Padding(0, 2, 0, 0),
            Font = new Font("Segoe UI", 8.5F),
            LinkBehavior = LinkBehavior.AlwaysUnderline
        };

        _pixKeyLink.LinkClicked +=
            PixKeyLink_LinkClicked;
			
		_pixCopiedTimer = new System.Windows.Forms.Timer
		{
			Interval = 3000
		};

		_pixCopiedTimer.Tick += (_, _) =>
		{
			_pixCopiedTimer.Stop();
			_pixKeyLink.Text = PixCopyPasteKey;
		};

        _pixQrCode = new PictureBox
        {
            Size = new Size(180, 180),
            SizeMode = PictureBoxSizeMode.Zoom,
            Margin = new Padding(0),
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

    private string L(string key) =>
        Localization.Get(key, _language);

    private static Label CreateLabel(
        string text,
        bool bold = false)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font(
                "Segoe UI",
                9F,
                bold
                    ? FontStyle.Bold
                    : FontStyle.Regular),
            Margin = new Padding(0)
        };
    }

    private void BuildLayout()
    {
        TableLayoutPanel root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9,
            Padding = new Padding(26, 18, 26, 14),
            AutoSize = false
        };

        root.RowStyles.Add(
            new RowStyle(SizeType.AutoSize)); // title

        root.RowStyles.Add(
            new RowStyle(SizeType.AutoSize)); // version

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 38)); // author

        root.RowStyles.Add(
            new RowStyle(SizeType.AutoSize)); // support

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 48)); // coffee

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 190)); // PIX + QR

        root.RowStyles.Add(
            new RowStyle(SizeType.AutoSize)); // instruction

        root.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 55)); // key

        root.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100)); // bottom

        root.Controls.Add(
            _appNameLabel,
            0,
            0);

        root.Controls.Add(
            _versionLabel,
            0,
            1);

        root.Controls.Add(
            _createdByLabel,
            0,
            2);

        root.Controls.Add(
            _supportLabel,
            0,
            3);

        root.Controls.Add(
            _buyMeACoffeeLink,
            0,
            4);

        // PIX + QR Code
        TableLayoutPanel pixPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        pixPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 50));

        pixPanel.ColumnStyles.Add(
            new ColumnStyle(SizeType.Absolute, 190));

        _pixLabel.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left;

        _pixLabel.Margin =
            new Padding(0, 12, 0, 0);

        pixPanel.Controls.Add(
            _pixLabel,
            0,
            0);

        _pixQrCode.Anchor =
            AnchorStyles.Top |
            AnchorStyles.Left;

        pixPanel.Controls.Add(
            _pixQrCode,
            1,
            0);

        root.Controls.Add(
            pixPanel,
            0,
            5);

        root.Controls.Add(
            _pixCopyPasteLabel,
            0,
            6);

        root.Controls.Add(
            _pixKeyLink,
            0,
            7);

        Panel bottomPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };

        _closeButton.Anchor =
            AnchorStyles.Bottom |
            AnchorStyles.Right;

        bottomPanel.Controls.Add(
            _closeButton);

        bottomPanel.Resize += (_, _) =>
        {
            _closeButton.Location = new Point(
                bottomPanel.ClientSize.Width -
                    _closeButton.Width,
                bottomPanel.ClientSize.Height -
                    _closeButton.Height);
        };

        root.Controls.Add(
            bottomPanel,
            0,
            8);

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

        ApplyThemeToControls(
            Controls,
            background,
            foreground);

        _buyMeACoffeeLink.LinkColor = linkColor;
        _buyMeACoffeeLink.ActiveLinkColor = linkColor;
        _buyMeACoffeeLink.VisitedLinkColor = linkColor;

        _pixKeyLink.LinkColor = linkColor;
        _pixKeyLink.ActiveLinkColor = linkColor;
        _pixKeyLink.VisitedLinkColor = linkColor;

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
            if (control is PictureBox ||
                control is LinkLabel ||
                control is Button)
            {
                continue;
            }

            control.BackColor = background;
            control.ForeColor = foreground;

            if (control.HasChildren)
            {
                ApplyThemeToControls(
                    control.Controls,
                    background,
                    foreground);
            }
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
            using FileStream stream =
                File.OpenRead(path);

            using Image source =
                Image.FromStream(stream);

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

	private void PixKeyLink_LinkClicked(
		object? sender,
		LinkLabelLinkClickedEventArgs e)
	{
		try
		{
			Clipboard.SetText(PixCopyPasteKey);

			_pixKeyLink.Text = L("AboutPixCopied");

			_pixCopiedTimer.Stop();
			_pixCopiedTimer.Start();
		}
		catch
		{
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_pixCopiedTimer.Stop();
			_pixCopiedTimer.Dispose();
			_pixQrCode.Image?.Dispose();
		}

		base.Dispose(disposing);
	}
}