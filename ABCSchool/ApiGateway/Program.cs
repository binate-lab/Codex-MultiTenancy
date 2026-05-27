var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("ABCSchool", client =>
{
    var baseUrl = builder.Configuration["Services:ABCSchool:BaseUrl"]
        ?? "http://localhost:5067/";

    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/abcschool/swagger"));

app.Map("/{service}/{**path}", async (
    string service,
    string? path,
    HttpContext context,
    IHttpClientFactory httpClientFactory) =>
{
    if (!service.Equals("abcschool", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Unknown service.");
        return;
    }

    var client = httpClientFactory.CreateClient("ABCSchool");
    var targetPath = string.IsNullOrWhiteSpace(path) ? string.Empty : path;
    var targetUri = new Uri(targetPath + context.Request.QueryString, UriKind.Relative);

    using var proxyRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);
    CopyRequestHeaders(context, proxyRequest);

    if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
    {
        proxyRequest.Content = new StreamContent(context.Request.Body);
        CopyContentHeaders(context, proxyRequest);
    }

    using var proxyResponse = await client.SendAsync(
        proxyRequest,
        HttpCompletionOption.ResponseHeadersRead,
        context.RequestAborted);

    context.Response.StatusCode = (int)proxyResponse.StatusCode;
    CopyResponseHeaders(context, proxyResponse);

    await proxyResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
});

app.Run();

static void CopyRequestHeaders(HttpContext context, HttpRequestMessage proxyRequest)
{
    foreach (var header in context.Request.Headers)
    {
        if (IsHopByHopHeader(header.Key) || header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }
}

static void CopyContentHeaders(HttpContext context, HttpRequestMessage proxyRequest)
{
    if (proxyRequest.Content is null)
    {
        return;
    }

    foreach (var header in context.Request.Headers)
    {
        if (!header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        proxyRequest.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }
}

static void CopyResponseHeaders(HttpContext context, HttpResponseMessage proxyResponse)
{
    foreach (var header in proxyResponse.Headers)
    {
        if (!IsHopByHopHeader(header.Key))
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    foreach (var header in proxyResponse.Content.Headers)
    {
        if (!IsHopByHopHeader(header.Key))
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    context.Response.Headers.Remove("transfer-encoding");
}

static bool IsHopByHopHeader(string headerName)
{
    return headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase)
        || headerName.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase)
        || headerName.Equals("Proxy-Authenticate", StringComparison.OrdinalIgnoreCase)
        || headerName.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
        || headerName.Equals("TE", StringComparison.OrdinalIgnoreCase)
        || headerName.Equals("Trailer", StringComparison.OrdinalIgnoreCase)
        || headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
        || headerName.Equals("Upgrade", StringComparison.OrdinalIgnoreCase);
}
