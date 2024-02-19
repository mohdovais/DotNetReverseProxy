public record ReverseProxyMapping
{
    private string _location = string.Empty;
    private string _proxyPass = string.Empty;

    public string Location { get => _location; set => _location = value.TrimEnd('/'); }
    public string ProxyPass { get => _proxyPass; set => _proxyPass = value.TrimEnd('/'); }
    public Dictionary<string, string>? ProxySetHeader;
    public HttpClient? HttpClient;
}
