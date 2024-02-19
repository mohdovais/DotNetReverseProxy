using System.Text.Json;

namespace ProxyServer.Tests;

public class ServerConfigShould
{
    [Fact]
    public void HaveName_ProxyServer()
    {
        Assert.IsType<string>(ServerConfig.Name);
    }

    [Fact]
    public void HavePropertyReverseProxies()
    {
        var json = "{}";
        var settings = System.Text.Json.JsonSerializer.Deserialize<ServerConfig>(json);
        Assert.Equal([], settings?.ReverseProxies);
    }

    [Fact]
    public void HavePropertyReverseProxies_WithDefaults()
    {
        var json = @"{
           ""ReverseProxies"": [{}]
        }";
        var settings = System.Text.Json.JsonSerializer.Deserialize<ServerConfig>(json);
        Assert.Equivalent(new ServerConfigReverseProxy { Location = "", ProxyPass = "" }, settings?.ReverseProxies[0]);
    }

    [Fact]
    public void HavePropertyReverseProxies_WithProxySetHeader()
    {
        var json = @"{
           ""ReverseProxies"": [{
              ""ProxySetHeader"": {
                 ""Abcd"": ""Efgh""
              }
           }]
        }";
        var expectedReverseProxySetting = new ServerConfigReverseProxy
        {
            ProxySetHeader = new()
            {
                ["Abcd"] = "Efgh",
            }
        };

        var config = JsonSerializer.Deserialize<ServerConfig>(json);
        var actualReverseProxySetting = config?.ReverseProxies[0];

        Assert.Equivalent(expectedReverseProxySetting, actualReverseProxySetting);
    }

    [Fact]
    public void HavePropertyStaticContents()
    {
        var settings = JsonSerializer.Deserialize<ServerConfig>("{}");
        Assert.Equal([], settings?.StaticContents);
    }

    [Fact]
    public void HavePropertyStaticContent_WithDefaults()
    {
        var json = @"{
           ""StaticContents"": [{}]
        }";
        var expectedStaticContentSetting = new ServerConfigStaticContent
        {
            Location = "",
            Root = ""
        };

        var actualStaticContentSetting = System.Text.Json.JsonSerializer.Deserialize<ServerConfig>(json)?.StaticContents[0];

        Assert.Equivalent(expectedStaticContentSetting, actualStaticContentSetting);
    }

    [Fact]
    public void HavePropertyStaticContents_WithoutTrailingSlashes()
    {
        var json = @"{
           ""StaticContents"": [{
              ""Location"": ""/static/"",
              ""Root"": ""/etc/usr/common/""
           }]
        }";
        var expectedStaticContentSetting = new ServerConfigStaticContent
        {
            Location = "/static",
            Root = "/etc/usr/common/"
        };

        var actualStaticContentSetting = System.Text.Json.JsonSerializer.Deserialize<ServerConfig>(json)?.StaticContents[0];

        Assert.Equivalent(expectedStaticContentSetting, actualStaticContentSetting);
    }


}
