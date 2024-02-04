using ReverseProxy;
using ReverseProxy.Middlewares;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddHttpClient();
builder.Services.Configure<ReverseProxyConfig>(builder.Configuration.GetSection(ReverseProxyConfig.ReverseProxy));

var app = builder.Build();

app.UseMiddleware<ReverseProxyMiddleware>();
app.Use(async (context, next) =>
{
    context.Response.StatusCode = 404;
    await next();
});

app.Run();