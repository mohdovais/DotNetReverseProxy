namespace ReverseProxy;

public class ReverseProxyConfig
{
    public const string ReverseProxy = "ReverseProxy";
    public Mapping[] Mappings { get; set; } = Array.Empty<Mapping>();
}


public class Mapping
{
    public string Location { get; set; } = string.Empty;
    public string ProxyPass { get; set; } = string.Empty;
}