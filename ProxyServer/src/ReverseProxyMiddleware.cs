using Microsoft.Extensions.Options;

namespace ProxyServer;

public class ReverseProxyMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] _httpMethodsWithoutBody = [
        Constants.GET, Constants.HEAD, Constants.DELETE, Constants.DELETE
    ];
    private const string DOMAIN = "domain";
    private const string D = "d";

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

            using var targetResponse = await httpClient.SendAsync(
                requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            var sourceResponse = context.Response;
            ApplyResponseHeaders(sourceResponse.Headers, targetResponse, mapping);
            sourceResponse.StatusCode = (int)targetResponse.StatusCode;

            await targetResponse.Content.CopyToAsync(sourceResponse.Body);

            return;
        }

        await _next(context);
    }

    private static HttpRequestMessage CreateTargetRequestMessage(HttpRequest request, Uri targetUri)
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

    private static HttpMethod GetHttpMethod(string method)
    {
        switch (method)
        {
            case Constants.DELETE:
                return HttpMethod.Delete;
            case Constants.GET:
                return HttpMethod.Get;
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
                return new HttpMethod(method);
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

    private void ApplyResponseHeaders(IHeaderDictionary headers, HttpResponseMessage targetResponse,
        ReverseProxySetting setting)
    {
        foreach (var header in targetResponse.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in targetResponse.Content.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        if (setting.ProxySetHeader != null)
        {
            foreach (var keyValuePair in setting.ProxySetHeader)
            {
                if (string.IsNullOrEmpty(keyValuePair.Value))
                {
                    headers.Remove(keyValuePair.Key);
                }
                else
                {
                    headers[keyValuePair.Key] = new string[] { keyValuePair.Value };
                }

            }
        }

        if (headers.ContainsKey(Constants.COOKIE_NAME_LOCATION))
        {
            headers.Location = headers.Location.Select(location => location?.Replace(setting.ProxyPass, setting.Location)).ToArray();
        }

        if (headers.ContainsKey(Constants.COOKIE_NAME_SET_COOKIE))
        {
            headers.SetCookie = headers.SetCookie.Select(cookie => cookie?.Replace(DOMAIN, D)).ToArray();
        }

        headers.Remove(Constants.COOKIE_NAME_SERVER);
        headers.Remove(Constants.COOKIE_NAME_TRANSFER_ENCODING);
    }
}
