namespace LocalNetChanger.Models;

public enum AdapterCategory
{
    Ethernet,
    Wireless
}

public sealed class NetworkProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public AdapterCategory Category { get; set; }
    public string AdapterId { get; set; } = string.Empty;
    public string AdapterName { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string SubnetMask { get; set; } = string.Empty;
    public string DefaultGateway { get; set; } = string.Empty;
    public string Dns1 { get; set; } = string.Empty;
    public string Dns2 { get; set; } = string.Empty;
}

public sealed class ProfileStore
{
    public List<NetworkProfile> Profiles { get; set; } = [];
}
