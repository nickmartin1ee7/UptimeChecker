
namespace UptimeChecker;

public class Settings
{
    public int? PingFrequencyMs { get; set; }
    public int? PingTimeoutMs { get; set; }
    public string? PingTargetHost { get; set; }
}

public static class SettingsExtensions
{
    public static Settings Validate(this Settings? settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(settings.PingFrequencyMs);
        ArgumentNullException.ThrowIfNull(settings.PingTimeoutMs);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.PingTargetHost);

        return settings;
    }
}