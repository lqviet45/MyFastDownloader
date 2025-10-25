using System.IO;
using System.Net;

namespace MyFastDownloader.App.Services;

public class LocalHttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    private CancellationTokenSource? _cts;

    public Func<string, Task>? OnAddUrl; // callback: nhận URL

    public LocalHttpServer(int port = 4153)
    {
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _listener.Prefixes.Add($"http://localhost:{port}/");
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener.Start();
        _ = Task.Run(() => Loop(_cts.Token));
    }

    private async Task Loop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();
                if (ctx.Request.Url is { } url && url.AbsolutePath.Equals("/add", StringComparison.OrdinalIgnoreCase))
                {
                    var q = System.Web.HttpUtility.ParseQueryString(url.Query);
                    var u = q.Get("url");
                    if (!string.IsNullOrWhiteSpace(u) && OnAddUrl is not null)
                    {
                        await OnAddUrl(u);
                        await Respond(ctx, 200, "OK - Download added!");
                    }
                    else await Respond(ctx, 400, "Missing url parameter");
                }
                else await Respond(ctx, 404, "Not found - Use /add?url=YOUR_URL");
            }
            catch when (token.IsCancellationRequested) { }
            catch { /* Ignore other exceptions */ }
        }
    }

    private static async Task Respond(HttpListenerContext ctx, int code, string msg)
    {
        ctx.Response.StatusCode = code;
        ctx.Response.ContentType = "text/plain; charset=utf-8";
        await using var sw = new StreamWriter(ctx.Response.OutputStream);
        await sw.WriteAsync(msg);
        ctx.Response.Close();
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listener.Close();
    }
}
