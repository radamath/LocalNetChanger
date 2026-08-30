namespace LocalNetChanger;

internal static class AppIcon
{
    private static Icon? _cachedIcon;

    public static Icon GetIcon()
    {
        if (_cachedIcon != null)
            return _cachedIcon;

        var assembly = typeof(AppIcon).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(".icon.ico", StringComparison.OrdinalIgnoreCase));

        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                _cachedIcon = new Icon(stream, 32, 32);
                return _cachedIcon;
            }
        }

        var icoPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");
        if (File.Exists(icoPath))
        {
            _cachedIcon = new Icon(icoPath, 32, 32);
            return _cachedIcon;
        }

        _cachedIcon = SystemIcons.Application;
        return _cachedIcon;
    }
}
