namespace ProxyServer;

public class ServerConfig
{
    public const string Name = "ProxyServer";
    public ServerConfigReverseProxy[] ReverseProxies { get; set; } = Array.Empty<ServerConfigReverseProxy>();
    public ServerConfigStaticContent[] StaticContents { get; set; } = Array.Empty<ServerConfigStaticContent>();
}

public class ServerConfigReverseProxy
{
    public string Location { get; set; } = string.Empty;
    public string ProxyPass { get; set; } = string.Empty;
    public Dictionary<string, string>? ProxySetHeader { get; set; }
}

public class ServerConfigStaticContent
{
    private string _location = string.Empty;

    public string Location { get => _location; set => _location = value.TrimEnd('/'); }
    public string Root { get; set; } = string.Empty;
}
