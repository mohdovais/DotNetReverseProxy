namespace ProxyServer;

public class ServerConfig
{
    public const string Name = "ProxyServer";
    public ReverseProxySetting[] ReverseProxies { get; set; } = Array.Empty<ReverseProxySetting>();
    public StaticContentSetting[] StaticContents { get; set; } = Array.Empty<StaticContentSetting>();
}

public class ReverseProxySetting
{
    private string _location = string.Empty;
    private string _proxyPass = string.Empty;

    public string Location { get => _location; set => _location = value.TrimEnd('/'); }
    public string ProxyPass { get => _proxyPass; set => _proxyPass = value.TrimEnd('/'); }
    public Dictionary<string, string>? ProxySetHeader { get; set; }
}

public class StaticContentSetting
{
    private string _location = string.Empty;

    public string Location { get => _location; set => _location = value.TrimEnd('/'); }
    public string Root { get; set; } = string.Empty;
}
