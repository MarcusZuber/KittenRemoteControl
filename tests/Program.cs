using System.Net;
using System.Net.Sockets;
using System.Text;
using KittenRemoteControl;

static void Check(bool value, string name)
{
    if (!value) throw new Exception(name);
    Console.WriteLine("PASS " + name);
}

static int AvailablePort()
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    return ((IPEndPoint)listener.LocalEndpoint).Port;
}

static async Task<TcpClient> Connect(int port, string fragment)
{
    var client = new TcpClient();
    await client.ConnectAsync(IPAddress.Loopback, port);
    if (fragment.Length > 0)
        await client.GetStream().WriteAsync(Encoding.ASCII.GetBytes(fragment));
    return client;
}

static async Task<string> ReadResponse(TcpClient client)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    using var reader = new StreamReader(client.GetStream(), Encoding.ASCII);
    return await reader.ReadToEndAsync(timeout.Token);
}

static async Task ExpectClosed(TcpClient client)
{
    try
    {
        Check(await ReadResponse(client) == "", "abandoned client closed without response");
    }
    catch (IOException) { Console.WriteLine("PASS abandoned client reset"); }
    catch (SocketException) { Console.WriteLine("PASS abandoned client reset"); }
}

var body = new TestBody();
Check(OrbitTelemetry.MeanRadius(body) == 6371000, "mean radius uses sea level, not near-surface radius");
Check(6578000 - OrbitTelemetry.MeanRadius(body) == 207000, "orbital elevation is 207 km");
Check(OrbitTelemetry.MeanRadius(null) == 0, "missing body retains zero fallback");

var tracker = new StageDeltaVTracker();
var craftA = new object();
var craftB = new object();
Check(tracker.Update(craftA, 0, 9000).max == 9000, "first stage peak");
Check(tracker.Update(craftA, 0, 6000).max == 9000, "burn retains peak");
Check(tracker.Update(craftB, 0, 3000).max == 3000, "switching craft at same stage resets peak");
Check(tracker.Update(craftB, 1, 1500).max == 1500, "switching stage resets peak");
Check(tracker.Update(null, -1, -1) == (-1, -1), "no vehicle clears cache");
Check(tracker.Update(craftB, 1, 1000).max == 1000, "reacquiring vehicle starts new peak");
tracker.Reset();
Check(tracker.Update(craftB, 1, 500).max == 500, "unload clears peak");

int actions = 0;
MainThreadDispatcher.Enqueue(() => actions++);
MainThreadDispatcher.Clear();
MainThreadDispatcher.Drain();
Check(actions == 0, "unload discards queued commands");

var port = AvailablePort();
int handled = 0;
using (var server = new TcpHttpServer(port))
{
    server.MapPost("/probe", request => { Interlocked.Increment(ref handled); return new HttpResponse { Body = request.Body }; });
    server.Start();
    using (var client = await Connect(port, "POST /probe HTTP/1.0\r\nHost: localhost\r\nContent-Length: 2\r\n\r\n{"))
    {
        await client.GetStream().WriteAsync(Encoding.ASCII.GetBytes("}"));
        var response = await ReadResponse(client);
        Check(response.StartsWith("HTTP/1.1 200") && response.EndsWith("{}"), "fragmented MicroBlocks-style POST works");
    }
    using (var client = await Connect(port, "OPTIONS /probe HTTP/1.1\r\nHost: localhost\r\n\r\n"))
        Check((await ReadResponse(client)).StartsWith("HTTP/1.1 204"), "CORS preflight works");

    using var idle = await Connect(port, "");
    using var header = await Connect(port, "POST /probe HTTP/1.1\r\nHost:");
    using var partialBody = await Connect(port, "POST /probe HTTP/1.1\r\nHost: localhost\r\nContent-Length: 2\r\n\r\n{");
    await Task.Delay(100);
    await Task.Run(server.Stop).WaitAsync(TimeSpan.FromSeconds(3));
    await ExpectClosed(idle);
    await ExpectClosed(header);
    await ExpectClosed(partialBody);
    Check(handled == 1, "no incomplete request executes after Stop");
    server.Stop();
    server.Start();
    using var restarted = await Connect(port, "POST /probe HTTP/1.0\r\nContent-Length: 2\r\n\r\n{}");
    Check((await ReadResponse(restarted)).EndsWith("{}"), "server can restart after Stop");
}

port = AvailablePort();
using (var server = new TcpHttpServer(port, TimeSpan.FromMilliseconds(200)))
{
    server.Start();
    using var client = await Connect(port, "GET /probe HTTP/1.1\r\nHost:");
    await ExpectClosed(client);
    Console.WriteLine("PASS incomplete request times out");
}

port = AvailablePort();
using (var server = new TcpHttpServer(port))
using (var entered = new ManualResetEventSlim())
using (var release = new ManualResetEventSlim())
{
    server.MapGet("/blocking", _ =>
    {
        entered.Set();
        if (!release.Wait(TimeSpan.FromSeconds(3))) throw new TimeoutException();
        return new HttpResponse { Body = "OK" };
    });
    server.Start();
    using var client = await Connect(port, "GET /blocking HTTP/1.1\r\nHost: localhost\r\n\r\n");
    Check(entered.Wait(TimeSpan.FromSeconds(3)), "handler started");
    var stopped = Task.Run(server.Stop);
    await Task.Delay(100);
    Check(!stopped.IsCompleted, "Stop waits for an executing handler");
    release.Set();
    await stopped.WaitAsync(TimeSpan.FromSeconds(3));
    Console.WriteLine("PASS executing handler finished before Stop returned");
}
Console.WriteLine("All regression checks passed.");

public sealed class TestBody
{
    public double MeanRadius => 6371000;
    public double GetNearSurfaceRadius() => 6538410;
}
