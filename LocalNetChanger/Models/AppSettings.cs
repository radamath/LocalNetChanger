namespace LocalNetChanger.Models;

public sealed class AppSettings
{
    public AppLanguage Language { get; set; } = AppLanguage.System;
    public bool StartWithWindows { get; set; }
}
