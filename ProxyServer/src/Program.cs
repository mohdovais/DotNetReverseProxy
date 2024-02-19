using ProxyServer;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.Configure<ServerConfig>(builder.Configuration.GetSection(ServerConfig.Name));

var app = builder.Build();

var settings = builder.Configuration.GetSection(ServerConfig.Name).Get<ServerConfig>();

if (settings != null)
{
    Array.ForEach(settings.StaticContents, setting =>
    {
        var contentRoot = Path.Combine(builder.Environment.ContentRootPath, setting.Root);
        var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(contentRoot);

        app.UseDefaultFiles(new DefaultFilesOptions()
        {
            RequestPath = setting.Location,
            FileProvider = fileProvider,
        });

        app.UseStaticFiles(new StaticFileOptions()
        {
            RequestPath = setting.Location,
            FileProvider = fileProvider,
        });
    });

    if (settings.ReverseProxies.Length > 0)
    {
        app.UseReverseProxy(
            settings.ReverseProxies.Select(config => new ReverseProxyMapping()
            {
                Location = config.Location,
                ProxyPass = config.ProxyPass,
                ProxySetHeader = config.ProxySetHeader
            }).ToArray());
    }
}

app.Run();
