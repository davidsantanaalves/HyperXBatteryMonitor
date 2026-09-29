using System.Diagnostics;
using HyperXBatteryTray.Devices;
using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray.Monitoring;

public sealed class BatteryRemainingTimeEstimator
{
    public const int MaximumSamplesPerDevice = 10;
    public const int ObservedDropForFullConfidencePercent = 10;

    private readonly object _sync = new();
    private readonly BatteryHistoryData _history;

    private string? _activeDevice;
    private int? _referenceBattery;
    private long _referenceTimestamp;

    public BatteryRemainingTimeEstimator(BatteryHistoryData history)
    {
        _history = history ?? new BatteryHistoryData();
        NormalizeHistory(_history);
    }

    public bool Observe(
        string? deviceName,
        int battery,
        bool connected,
        bool charging)
    {
        lock (_sync)
        {
            double? nominalBatteryLifeHours =
                HyperXDeviceManager.GetNominalBatteryLifeHours(deviceName);

            if (!connected ||
                charging ||
                battery is < 0 or > 100 ||
                !nominalBatteryLifeHours.HasValue)
            {
                ResetSessionCore();
                return false;
            }

            string normalizedDevice =
                HyperXDeviceManager.NormalizeSupportedDeviceName(deviceName!);
            long now = Stopwatch.GetTimestamp();

            if (!string.Equals(
                    _activeDevice,
                    normalizedDevice,
                    StringComparison.OrdinalIgnoreCase) ||
                !_referenceBattery.HasValue)
            {
                StartSessionCore(normalizedDevice, battery, now);
                return false;
            }

            if (battery > _referenceBattery.Value)
            {
                StartSessionCore(normalizedDevice, battery, now);
                return false;
            }

            if (battery == _referenceBattery.Value)
                return false;

            TimeSpan elapsed = Stopwatch.GetElapsedTime(
                _referenceTimestamp,
                now);
            int dropPercent = _referenceBattery.Value - battery;

            StartSessionCore(normalizedDevice, battery, now);

            if (dropPercent <= 0 || elapsed <= TimeSpan.Zero)
                return false;

            AddSampleCore(
                normalizedDevice,
                new BatteryDischargeSample
                {
                    DropPercent = dropPercent,
                    ElapsedSeconds = elapsed.TotalSeconds
                });

            return true;
        }
    }

    public TimeSpan? EstimateRemaining(
        string? deviceName,
        int battery,
        bool connected,
        bool charging)
    {
        lock (_sync)
        {
            if (!connected || charging || battery is < 0 or > 100)
                return null;

            double? nominalBatteryLifeHours =
                HyperXDeviceManager.GetNominalBatteryLifeHours(deviceName);

            if (!nominalBatteryLifeHours.HasValue || nominalBatteryLifeHours.Value <= 0)
                return null;

            string normalizedDevice =
                HyperXDeviceManager.NormalizeSupportedDeviceName(deviceName!);
            double nominalRatePercentPerHour =
                100d / nominalBatteryLifeHours.Value;
            double effectiveRatePercentPerHour = nominalRatePercentPerHour;

            if (_history.Devices.TryGetValue(
                    normalizedDevice,
                    out List<BatteryDischargeSample>? samples) &&
                samples.Count > 0)
            {
                int observedDropPercent = samples.Sum(sample => sample.DropPercent);
                double observedHours = samples.Sum(sample => sample.ElapsedSeconds) / 3600d;

                if (observedDropPercent > 0 && observedHours > 0)
                {
                    double observedRatePercentPerHour =
                        observedDropPercent / observedHours;
                    double confidence = Math.Clamp(
                        observedDropPercent /
                        (double)ObservedDropForFullConfidencePercent,
                        0d,
                        1d);

                    effectiveRatePercentPerHour =
                        (nominalRatePercentPerHour * (1d - confidence)) +
                        (observedRatePercentPerHour * confidence);
                }
            }

            if (!double.IsFinite(effectiveRatePercentPerHour) ||
                effectiveRatePercentPerHour <= 0)
            {
                return null;
            }

            double remainingPercent = battery;

            if (string.Equals(
                    _activeDevice,
                    normalizedDevice,
                    StringComparison.OrdinalIgnoreCase) &&
                _referenceBattery == battery &&
                _referenceTimestamp != 0)
            {
                TimeSpan elapsedSinceReference = Stopwatch.GetElapsedTime(
                    _referenceTimestamp,
                    Stopwatch.GetTimestamp());

                remainingPercent -=
                    elapsedSinceReference.TotalHours * effectiveRatePercentPerHour;
            }

            remainingPercent = Math.Clamp(remainingPercent, 0d, 100d);
            double remainingHours =
                remainingPercent / effectiveRatePercentPerHour;

            if (!double.IsFinite(remainingHours) ||
                remainingHours < 0 ||
                remainingHours > TimeSpan.MaxValue.TotalHours)
            {
                return null;
            }

            return TimeSpan.FromHours(remainingHours);
        }
    }

    public BatteryHistoryData CreateHistorySnapshot()
    {
        lock (_sync)
        {
            BatteryHistoryData snapshot = new();

            foreach ((string device, List<BatteryDischargeSample> samples) in _history.Devices)
            {
                snapshot.Devices[device] = samples
                    .Select(sample => new BatteryDischargeSample
                    {
                        DropPercent = sample.DropPercent,
                        ElapsedSeconds = sample.ElapsedSeconds
                    })
                    .ToList();
            }

            return snapshot;
        }
    }

    public void ResetSession()
    {
        lock (_sync)
            ResetSessionCore();
    }

    private void AddSampleCore(
        string deviceName,
        BatteryDischargeSample sample)
    {
        if (!_history.Devices.TryGetValue(
                deviceName,
                out List<BatteryDischargeSample>? samples))
        {
            samples = new List<BatteryDischargeSample>();
            _history.Devices[deviceName] = samples;
        }

        samples.Add(sample);

        if (samples.Count > MaximumSamplesPerDevice)
        {
            samples.RemoveRange(
                0,
                samples.Count - MaximumSamplesPerDevice);
        }
    }

    private void StartSessionCore(
        string deviceName,
        int battery,
        long timestamp)
    {
        _activeDevice = deviceName;
        _referenceBattery = battery;
        _referenceTimestamp = timestamp;
    }

    private void ResetSessionCore()
    {
        _activeDevice = null;
        _referenceBattery = null;
        _referenceTimestamp = 0;
    }

    private static void NormalizeHistory(BatteryHistoryData history)
    {
        Dictionary<string, List<BatteryDischargeSample>> normalized =
            new(StringComparer.OrdinalIgnoreCase);

        foreach ((string device, List<BatteryDischargeSample> samples) in history.Devices)
        {
            if (string.IsNullOrWhiteSpace(device) || samples == null)
                continue;

            List<BatteryDischargeSample> validSamples = samples
                .Where(sample =>
                    sample != null &&
                    sample.DropPercent is > 0 and <= 100 &&
                    double.IsFinite(sample.ElapsedSeconds) &&
                    sample.ElapsedSeconds > 0)
                .TakeLast(MaximumSamplesPerDevice)
                .Select(sample => new BatteryDischargeSample
                {
                    DropPercent = sample.DropPercent,
                    ElapsedSeconds = sample.ElapsedSeconds
                })
                .ToList();

            if (validSamples.Count > 0)
                normalized[device.Trim()] = validSamples;
        }

        history.Devices = normalized;
    }
}
