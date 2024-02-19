using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using ProxyServer;

public class ReverseProxyMiddlewareShould
{
    [Fact]
    public async Task ReturnsNotFoundForRequest()
    {
        using var host = await new HostBuilder().ConfigureWebHost(hostBuilder =>
            {
                hostBuilder.UseTestServer().Configure(app =>
                {
                    app.UseReverseProxy(Array.Empty<ReverseProxyMapping>());
                });
            }).StartAsync();

        var response = await host.GetTestClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
