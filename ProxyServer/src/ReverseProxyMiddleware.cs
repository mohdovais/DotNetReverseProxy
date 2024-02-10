using Microsoft.Extensions.Options;

namespace ProxyServer;

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

    public async Task InvokeAsync(HttpContext context, HttpClient httpClient, IOptions<ServerConfig> settings)
    {
        var proxySettings = settings.Value.ReverseProxies;
        var mappingAndTargetUri = GetMappingAndTargetUri(context.Request, proxySettings);

        if (mappingAndTargetUri != null)
        {
            var (mapping, targetUri) = mappingAndTargetUri ?? default;
            var requestMessage = CreateTargetRequestMessage(context.Request, targetUri);
            var sourceResponse = context.Response;

            using var targetResponse = await httpClient.SendAsync(
                requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            foreach (var header in targetResponse.Headers)
            {
                sourceResponse.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in targetResponse.Content.Headers)
            {
                sourceResponse.Headers[header.Key] = header.Value.ToArray();
            }

            if (mapping.ProxySetHeader != null)
            {
                foreach (var keyValuePair in mapping.ProxySetHeader)
                {
                    if (string.IsNullOrEmpty(keyValuePair.Value))
                    {
                        sourceResponse.Headers.Remove(keyValuePair.Key);
                    }
                    else
                    {
                        sourceResponse.Headers[keyValuePair.Key] = new string[] { keyValuePair.Value };
                    }

                }
            }

            sourceResponse.StatusCode = (int)targetResponse.StatusCode;
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

    private static (ReverseProxySetting mapping, Uri targetUri)? GetMappingAndTargetUri(HttpRequest request, ReverseProxySetting[] mappings)
    {
        foreach (var mapping in mappings)
        {
            if (request.Path.StartsWithSegments(mapping.Location, out var remainingPath))
            {
                return (mapping, new Uri(mapping.ProxyPass + remainingPath + request.QueryString.Value));
            }
        }
        return null;
    }
}
