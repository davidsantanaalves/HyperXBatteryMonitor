using HyperXBatteryTray.Settings;

namespace HyperXBatteryTray.Monitoring;

public static class BatteryRemainingTimeFormatter
{
    public const int MinuteRoundingStep = 5;

    public static string? Format(
        TimeSpan? remainingTime,
        AppLanguage language)
    {
        if (!remainingTime.HasValue || remainingTime.Value < TimeSpan.Zero)
            return null;

        double totalMinutes = remainingTime.Value.TotalMinutes;

        if (!double.IsFinite(totalMinutes))
            return null;

        if (totalMinutes <= 0d)
        {
            return string.Format(
                Localization.Get("BatteryRemainingMinutes", language),
                0);
        }

        if (totalMinutes >= 60d)
        {
            int hours = Math.Max(
                1,
                (int)Math.Round(
                    remainingTime.Value.TotalHours,
                    MidpointRounding.AwayFromZero));

            return string.Format(
                Localization.Get("BatteryRemainingHours", language),
                hours);
        }

        int minutes = Math.Max(
            MinuteRoundingStep,
            (int)Math.Round(
                totalMinutes / MinuteRoundingStep,
                MidpointRounding.AwayFromZero) * MinuteRoundingStep);

        if (minutes >= 60)
        {
            return string.Format(
                Localization.Get("BatteryRemainingHours", language),
                1);
        }

        return string.Format(
            Localization.Get("BatteryRemainingMinutes", language),
            minutes);
    }
}
