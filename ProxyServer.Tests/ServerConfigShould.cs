namespace ProxyServer.Tests;

public class ServerConfigShould
{
    [Fact]
    public void HaveName_ProxyServer()
    {
        Assert.Equal("ProxyServer", ServerConfig.Name);
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
        Assert.Equivalent(new ReverseProxySetting { Location = "", ProxyPass = "" }, settings?.ReverseProxies[0]);
    }

    [Fact]
    public void HavePropertyReverseProxies_WithoutTrailingSlashes()
    {
        var json = @"{
           ""ReverseProxies"": [{
             ""Location"": ""/search/"",
             ""ProxyPass"": ""https://www.google.com/""
           }]
        }";
        var expectedReverseProxySetting = new ReverseProxySetting { Location = "/search", ProxyPass = "https://www.google.com" };

        var actualReverseProxySetting = System.Text.Json.JsonSerializer.Deserialize<ServerConfig>(json)?.ReverseProxies[0];

        Assert.Equivalent(expectedReverseProxySetting, actualReverseProxySetting);
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
        var expectedReverseProxySetting = new ReverseProxySetting
        {
            Location = "",
            ProxyPass = "",
            ProxySetHeader = new Dictionary<string, string>(){
                {"Abcd", "Efgh"}
            }
        };

        var actualReverseProxySetting = System.Text.Json.JsonSerializer.Deserialize<ServerConfig>(json)?.ReverseProxies[0];

        Assert.Equivalent(expectedReverseProxySetting, actualReverseProxySetting);
    }

    [Fact]
    public void HavePropertyStaticContents()
    {
        var settings = System.Text.Json.JsonSerializer.Deserialize<ServerConfig>("{}");
        Assert.Equal([], settings?.StaticContents);
    }

    [Fact]
    public void HavePropertyStaticContent_WithDefaults()
    {
        var json = @"{
           ""StaticContents"": [{}]
        }";
        var expectedStaticContentSetting = new StaticContentSetting
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
        var expectedStaticContentSetting = new StaticContentSetting
        {
            Location = "/static",
            Root = "/etc/usr/common/"
        };

        var actualStaticContentSetting = System.Text.Json.JsonSerializer.Deserialize<ServerConfig>(json)?.StaticContents[0];

        Assert.Equivalent(expectedStaticContentSetting, actualStaticContentSetting);
    }


}
