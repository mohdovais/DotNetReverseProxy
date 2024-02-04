using Microsoft.Extensions.Options;

namespace ReverseProxy.Middlewares;

public class ReverseProxyMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] _httpMethodsWithoutBody = [
        Constants.GET, Constants.HEAD, Constants.DELETE, Constants.DELETE
    ];

    public ReverseProxyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, HttpClient httpClient, IOptions<ReverseProxyConfig> settings)
    {
        var proxySettings = settings.Value.Mappings;
        var targetUri = GetTargetUri(context.Request, proxySettings);

        if (targetUri != null)
        {
            var requestMessage = CreateTargetRequestMessage(context.Request, targetUri);
            var sourceResponse = context.Response;

            using var targetResponse = await httpClient.SendAsync(
                requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            sourceResponse.StatusCode = (int)targetResponse.StatusCode;

            foreach (var header in targetResponse.Headers)
            {
                sourceResponse.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in targetResponse.Content.Headers)
            {
                sourceResponse.Headers[header.Key] = header.Value.ToArray();
            }
            
            sourceResponse.Headers.Remove(Constants.TRANSFER_ENCODING);

            await targetResponse.Content.CopyToAsync(sourceResponse.Body);

            return;
        }

        await _next(context);
    }

    private HttpRequestMessage CreateTargetRequestMessage(HttpRequest request, Uri targetUri)
    {
        var message = new HttpRequestMessage(GetHttpMethod(request.Method), targetUri);

        if (!_httpMethodsWithoutBody.Contains(request.Method))
        {
            message.Content = new StreamContent(request.Body);
        }

        foreach (var header in request.Headers)
        {
            message.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        message.Headers.Host = targetUri.Host;

        return message;
    }

    private HttpMethod GetHttpMethod(string method)
    {
        switch (method)
        {
            case Constants.DELETE:
                return HttpMethod.Delete;
            case Constants.HEAD:
                return HttpMethod.Head;
            case Constants.OPTIONS:
                return HttpMethod.Options;
            case Constants.POST:
                return HttpMethod.Post;
            case Constants.PUT:
                return HttpMethod.Put;
            case Constants.TRACE:
                return HttpMethod.Trace;
            default:
                return HttpMethod.Get;
        };

    }

    private Uri? GetTargetUri(HttpRequest request, Mapping[] mappings)
    {
        foreach (var mapping in mappings)
        {
            if (request.Path.StartsWithSegments(mapping.Location, out var remainingPath))
            {
                return new Uri(mapping.ProxyPass + remainingPath + request.QueryString.Value);
            }
        }
        return null;
    }
}
