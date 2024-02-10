using ProxyServer;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddHttpClient();
builder.Services.Configure<ServerConfig>(builder.Configuration.GetSection(ServerConfig.Name));

var app = builder.Build();

var settings = builder.Configuration
    .GetSection(ServerConfig.Name)
    .Get<ServerConfig>();

if (settings != null)
{
    if (settings.ReverseProxies.Length > 0)
    {
        app.UseMiddleware<ReverseProxyMiddleware>();
    }

    Array.ForEach(settings.StaticContents, setting =>
    {
        var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(setting.Root);
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
}

app.Run();
