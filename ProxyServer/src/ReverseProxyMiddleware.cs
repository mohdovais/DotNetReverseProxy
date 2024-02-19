using Microsoft.Extensions.Options;

namespace ProxyServer;

public class ReverseProxyMiddleware
{
    private const string HEADER_TRANSFER_ENCODING = "Transfer-Encoding";
    private const string HEADER_LOCATION = "Location";
    private const string HEADER_SET_COOKIE = "Set-Cookie";
    private const string HTTP_METHOD_GET = "GET";
    private const string HTTP_METHOD_POST = "POST";
    private const string HTTP_METHOD_PUT = "PUT";
    private const string HTTP_METHOD_DELETE = "DELETE";
    private const string HTTP_METHOD_OPTIONS = "OPTIONS";
    private const string HTTP_METHOD_HEAD = " HEAD";
    private const string HTTP_METHOD_TRACE = "TRACE";
    private const string DOMAIN = "domain";
    private const string D = "d";

    private static readonly string[] _httpMethodsWithoutBody = [
        HTTP_METHOD_GET,
        HTTP_METHOD_HEAD,
        HTTP_METHOD_DELETE,
        HTTP_METHOD_OPTIONS,
        HTTP_METHOD_TRACE
    ];

    //@TODO how to configure proxy and certificates
    private static readonly HttpClient _httpClient = new HttpClient();

    private readonly ReverseProxyMapping[] _mappings;
    private readonly RequestDelegate _next;

    public ReverseProxyMiddleware(RequestDelegate next, IOptions<ReverseProxyMapping[]> option)
    {
        _next = next;
        _mappings = option.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip if the server has this endpoint (route) defined
        if (context.GetEndpoint() != null)
        {
            await _next(context);
            return;
        }

        var mappingAndTargetUri = GetMappingAndTargetUri(context.Request);

        if (mappingAndTargetUri != null)
        {
            var (mapping, targetUri) = mappingAndTargetUri ?? default;
            var requestMessage = CreateTargetRequestMessage(context.Request, targetUri);

            using var targetResponse = await _httpClient.SendAsync(
                requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

            var sourceResponse = context.Response;
            ApplyResponseHeaders(sourceResponse.Headers, targetResponse, mapping);
            sourceResponse.StatusCode = (int)targetResponse.StatusCode;

            await targetResponse.Content.CopyToAsync(sourceResponse.Body);
            targetResponse.Content = null;
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
            case HTTP_METHOD_DELETE:
                return HttpMethod.Delete;
            case HTTP_METHOD_GET:
                return HttpMethod.Get;
            case HTTP_METHOD_HEAD:
                return HttpMethod.Head;
            case HTTP_METHOD_OPTIONS:
                return HttpMethod.Options;
            case HTTP_METHOD_POST:
                return HttpMethod.Post;
            case HTTP_METHOD_PUT:
                return HttpMethod.Put;
            case HTTP_METHOD_TRACE:
                return HttpMethod.Trace;
            default:
                return new HttpMethod(method);
        };

    }

    private (ReverseProxyMapping mapping, Uri targetUri)? GetMappingAndTargetUri(HttpRequest request)
    {
        foreach (var mapping in _mappings)
        {
            if (request.Path.StartsWithSegments(mapping.Location, out var remainingPath))
            {
                return (mapping, new Uri(mapping.ProxyPass + remainingPath + request.QueryString.Value));
            }
        }
        return null;
    }

    private static void ApplyResponseHeaders(
        IHeaderDictionary headers,
        HttpResponseMessage targetResponse,
        ReverseProxyMapping mapping
    )
    {
        /*
        * HttpResponseHeaders
        * Accept-Ranges, Age, Cache-Control, Connection, Date, ETag,
        * Location, Pragma, Proxy-Authenticate, Retry-After, Server,
        * Trailer, Transfer-Encoding, Upgrade, Vary, Via, Warning,
        * WWW-Authenticate
        */
        foreach (var header in targetResponse.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        /*
        * HttpContentHeaders
        * Content-Disposition, Content-Encoding, Content-Language,
        * Content-Length, Content-Location, Content-MD5, Content-Range,
        * Content-Type, Expires, Last-Modified
        */
        foreach (var header in targetResponse.Content.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        if (mapping.ProxySetHeader != null)
        {
            foreach (var keyValuePair in mapping.ProxySetHeader)
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

        if (headers.ContainsKey(HEADER_LOCATION))
        {
            headers.Location = headers.Location
                .Select(location => location?.Replace(mapping.ProxyPass, mapping.Location))
                .ToArray();
        }

        if (headers.ContainsKey(HEADER_SET_COOKIE))
        {
            headers.SetCookie = headers.SetCookie
                .Select(cookie => cookie?.Replace(DOMAIN, D))
                .ToArray();
        }

        // Ignore Transfer-Encoding from target server
        // This server will set its own
        headers.Remove(HEADER_TRANSFER_ENCODING);
    }
}

public static class ReverseProxyMiddlewareExtensions
{
    public static IApplicationBuilder UseReverseProxy(
        this IApplicationBuilder builder, ReverseProxyMapping[] config)
    {
        return builder.UseMiddleware<ReverseProxyMiddleware>(Options.Create(config));
    }
}
