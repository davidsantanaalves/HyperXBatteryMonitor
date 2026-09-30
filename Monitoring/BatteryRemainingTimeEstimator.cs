using System.Diagnostics;
using HyperXBatteryTray.Devices;
using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray.Monitoring;

public sealed class BatteryRemainingTimeEstimator
{
    public const int MaximumSamplesPerDevice = 10;
    public const int MinimumValidatedWindowDropPercent = 2;
    public const int ObservedDropForFullHistoricalConfidencePercent = 10;
    public const int SessionDropForFullHistoricalInfluencePercent = 5;
    public const double MinimumPlausibleRateMultiplier = 1d / 3d;
    public const double MaximumPlausibleRateMultiplier = 3d;
    public const double MaximumHistoricalWeight = 0.80d;

    private readonly object _sync = new();
    private readonly BatteryHistoryData _history;

    private string? _activeDevice;
    private int? _windowStartBattery;
    private long _windowStartTimestamp;
    private int? _lastObservedBattery;
    private long _lastObservedBatteryTimestamp;
    private int _sessionValidatedDropPercent;

    public BatteryRemainingTimeEstimator(BatteryHistoryData history)
    {
        _history = history ?? BatteryHistoryData.CreateCurrent();
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
                !nominalBatteryLifeHours.HasValue ||
                nominalBatteryLifeHours.Value <= 0)
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
                !_windowStartBattery.HasValue ||
                !_lastObservedBattery.HasValue)
            {
                StartSessionCore(normalizedDevice, battery, now);
                return false;
            }

            if (battery > _lastObservedBattery.Value)
            {
                StartLearningWindowCore(battery, now);
                SetLastObservedBatteryCore(battery, now);
                return false;
            }

            if (battery < _lastObservedBattery.Value)
                SetLastObservedBatteryCore(battery, now);

            int dropPercent = _windowStartBattery.Value - battery;
            if (dropPercent < MinimumValidatedWindowDropPercent)
                return false;

            TimeSpan elapsed = Stopwatch.GetElapsedTime(
                _windowStartTimestamp,
                now);

            if (!IsPlausibleSample(
                    dropPercent,
                    elapsed.TotalSeconds,
                    nominalBatteryLifeHours.Value))
            {
                return false;
            }

            AddSampleCore(
                normalizedDevice,
                new BatteryDischargeSample
                {
                    DropPercent = dropPercent,
                    ElapsedSeconds = elapsed.TotalSeconds
                });

            _sessionValidatedDropPercent += dropPercent;
            StartLearningWindowCore(battery, now);
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

            if (!nominalBatteryLifeHours.HasValue ||
                nominalBatteryLifeHours.Value <= 0)
            {
                return null;
            }

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
                double? observedRatePercentPerHour =
                    CalculateMedianRatePercentPerHour(samples);

                if (observedRatePercentPerHour.HasValue)
                {
                    int observedDropPercent = samples.Sum(sample => sample.DropPercent);
                    double historicalConfidence = Math.Clamp(
                        observedDropPercent /
                        (double)ObservedDropForFullHistoricalConfidencePercent,
                        0d,
                        1d);
                    double sessionConfidence =
                        string.Equals(
                            _activeDevice,
                            normalizedDevice,
                            StringComparison.OrdinalIgnoreCase)
                            ? Math.Clamp(
                                _sessionValidatedDropPercent /
                                (double)SessionDropForFullHistoricalInfluencePercent,
                                0d,
                                1d)
                            : 0d;
                    double historicalWeight =
                        MaximumHistoricalWeight *
                        historicalConfidence *
                        sessionConfidence;

                    effectiveRatePercentPerHour =
                        (nominalRatePercentPerHour * (1d - historicalWeight)) +
                        (observedRatePercentPerHour.Value * historicalWeight);
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
                _lastObservedBattery == battery &&
                _lastObservedBatteryTimestamp != 0)
            {
                TimeSpan elapsedSinceBatteryChange = Stopwatch.GetElapsedTime(
                    _lastObservedBatteryTimestamp,
                    Stopwatch.GetTimestamp());

                remainingPercent -=
                    elapsedSinceBatteryChange.TotalHours *
                    effectiveRatePercentPerHour;
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
            BatteryHistoryData snapshot = BatteryHistoryData.CreateCurrent();

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
        _sessionValidatedDropPercent = 0;
        StartLearningWindowCore(battery, timestamp);
        SetLastObservedBatteryCore(battery, timestamp);
    }

    private void StartLearningWindowCore(
        int battery,
        long timestamp)
    {
        _windowStartBattery = battery;
        _windowStartTimestamp = timestamp;
    }

    private void SetLastObservedBatteryCore(
        int battery,
        long timestamp)
    {
        _lastObservedBattery = battery;
        _lastObservedBatteryTimestamp = timestamp;
    }

    private void ResetSessionCore()
    {
        _activeDevice = null;
        _windowStartBattery = null;
        _windowStartTimestamp = 0;
        _lastObservedBattery = null;
        _lastObservedBatteryTimestamp = 0;
        _sessionValidatedDropPercent = 0;
    }

    private static bool IsPlausibleSample(
        int dropPercent,
        double elapsedSeconds,
        double nominalBatteryLifeHours)
    {
        if (dropPercent < MinimumValidatedWindowDropPercent ||
            dropPercent > 100 ||
            !double.IsFinite(elapsedSeconds) ||
            elapsedSeconds <= 0 ||
            !double.IsFinite(nominalBatteryLifeHours) ||
            nominalBatteryLifeHours <= 0)
        {
            return false;
        }

        double elapsedHours = elapsedSeconds / 3600d;
        double observedRatePercentPerHour = dropPercent / elapsedHours;
        double nominalRatePercentPerHour = 100d / nominalBatteryLifeHours;
        double minimumPlausibleRate =
            nominalRatePercentPerHour * MinimumPlausibleRateMultiplier;
        double maximumPlausibleRate =
            nominalRatePercentPerHour * MaximumPlausibleRateMultiplier;

        return double.IsFinite(observedRatePercentPerHour) &&
               observedRatePercentPerHour >= minimumPlausibleRate &&
               observedRatePercentPerHour <= maximumPlausibleRate;
    }

    private static double? CalculateMedianRatePercentPerHour(
        IEnumerable<BatteryDischargeSample> samples)
    {
        double[] rates = samples
            .Where(sample =>
                sample.DropPercent > 0 &&
                double.IsFinite(sample.ElapsedSeconds) &&
                sample.ElapsedSeconds > 0)
            .Select(sample =>
                sample.DropPercent /
                (sample.ElapsedSeconds / 3600d))
            .Where(rate => double.IsFinite(rate) && rate > 0)
            .OrderBy(rate => rate)
            .ToArray();

        if (rates.Length == 0)
            return null;

        int middle = rates.Length / 2;

        return rates.Length % 2 == 1
            ? rates[middle]
            : (rates[middle - 1] + rates[middle]) / 2d;
    }

    private static void NormalizeHistory(BatteryHistoryData history)
    {
        Dictionary<string, List<BatteryDischargeSample>> normalized =
            new(StringComparer.OrdinalIgnoreCase);

        foreach ((string device, List<BatteryDischargeSample> samples) in history.Devices)
        {
            if (string.IsNullOrWhiteSpace(device) || samples == null)
                continue;

            string normalizedDevice =
                HyperXDeviceManager.NormalizeSupportedDeviceName(device.Trim());
            double? nominalBatteryLifeHours =
                HyperXDeviceManager.GetNominalBatteryLifeHours(normalizedDevice);

            if (!nominalBatteryLifeHours.HasValue ||
                nominalBatteryLifeHours.Value <= 0)
            {
                continue;
            }

            List<BatteryDischargeSample> validSamples = samples
                .Where(sample =>
                    sample != null &&
                    IsPlausibleSample(
                        sample.DropPercent,
                        sample.ElapsedSeconds,
                        nominalBatteryLifeHours.Value))
                .TakeLast(MaximumSamplesPerDevice)
                .Select(sample => new BatteryDischargeSample
                {
                    DropPercent = sample.DropPercent,
                    ElapsedSeconds = sample.ElapsedSeconds
                })
                .ToList();

            if (validSamples.Count == 0)
                continue;

            if (!normalized.TryGetValue(
                    normalizedDevice,
                    out List<BatteryDischargeSample>? existing))
            {
                normalized[normalizedDevice] = validSamples;
                continue;
            }

            existing.AddRange(validSamples);

            if (existing.Count > MaximumSamplesPerDevice)
            {
                existing.RemoveRange(
                    0,
                    existing.Count - MaximumSamplesPerDevice);
            }
        }

        history.Version = BatteryHistoryData.CurrentVersion;
        history.Devices = normalized;
    }
}
