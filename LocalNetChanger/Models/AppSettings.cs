namespace LocalNetChanger.Models;

public sealed class AppSettings
{
    public AppLanguage Language { get; set; } = AppLanguage.System;
    public bool StartWithWindows { get; set; }
    public LastNetworkChoice? LastWired { get; set; }
    public LastNetworkChoice? LastWireless { get; set; }
}

public sealed class LastNetworkChoice
{
    public bool IsDhcp { get; set; }
    public string? ProfileId { get; set; }
}
