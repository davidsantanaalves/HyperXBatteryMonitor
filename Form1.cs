using HyperXBatteryTray.Devices;
using HyperXBatteryTray.Monitoring;

namespace HyperXBatteryTray;

public partial class Form1 : Form
{
    private Cloud3WirelessDevice? _device;
    private BatteryMonitor? _batteryMonitor;
    private Label? _statusLabel;
    private Label? _batteryLabel;

    public Form1()
    {
        InitializeComponent();
        InitializeTestInterface();
        StartDeviceMonitoring();
    }

    private void InitializeTestInterface()
    {
        Text = "HyperX Battery Tray - Teste";
        Width = 500;
        Height = 220;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        _statusLabel = new Label
        {
            AutoSize = true,
            Left = 30,
            Top = 30,
            Text = "Procurando HyperX Cloud III Wireless..."
        };

        _batteryLabel = new Label
        {
            AutoSize = true,
            Left = 30,
            Top = 80,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            Text = "N/A"
        };

        Controls.Add(_statusLabel);
        Controls.Add(_batteryLabel);
    }

    private void StartDeviceMonitoring()
    {
        _device = new Cloud3WirelessDevice();

        _batteryMonitor = new BatteryMonitor(_device)
        {
            IntervalMilliseconds = 5000
        };

        _batteryMonitor.BatteryChanged +=
            BatteryMonitor_BatteryChanged;

        _batteryMonitor.ConnectionChanged +=
            BatteryMonitor_ConnectionChanged;

        _batteryMonitor.Start();
    }

    private void BatteryMonitor_ConnectionChanged(
        object? sender,
        bool connected)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() =>
                BatteryMonitor_ConnectionChanged(
                    sender,
                    connected));

            return;
        }

        if (_statusLabel == null)
            return;

        _statusLabel.Text = connected
            ? "HyperX Cloud III Wireless conectado"
            : "HyperX Cloud III Wireless não conectado";

        if (!connected && _batteryLabel != null)
            _batteryLabel.Text = "N/A";
    }

    private void BatteryMonitor_BatteryChanged(
        object? sender,
        int battery)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() =>
                BatteryMonitor_BatteryChanged(
                    sender,
                    battery));

            return;
        }

        if (_batteryLabel == null)
            return;

        _batteryLabel.Text = battery < 0
            ? "N/A"
            : $"{battery}%";
    }

    protected override void OnFormClosed(
        FormClosedEventArgs e)
    {
        _batteryMonitor?.Dispose();

        _batteryMonitor = null;
        _device = null;

        base.OnFormClosed(e);
    }
}