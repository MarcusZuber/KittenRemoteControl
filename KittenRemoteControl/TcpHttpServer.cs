using System.IO;
using System.Net;          // IPAddress — lives in System.Net.Primitives, which resolves fine.
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace KittenRemoteControl
{
    /// <summary>
    /// A parsed HTTP request handed to a route handler.
    /// </summary>
    public sealed class HttpRequest
    {
        public required string Method { get; init; }
        public required string Path { get; init; }
        /// <summary>Raw (still URL-encoded) query string, i.e. everything after '?'. Empty if none.</summary>
        public string RawQuery { get; init; } = "";
        public string Body { get; init; } = "";
        public Dictionary<string, string> Headers { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The response a route handler returns.
    /// </summary>
    public sealed class HttpResponse
    {
        public int StatusCode { get; set; } = 200;
        public string ContentType { get; set; } = "application/json";
        public string Body { get; set; } = "";
    }

    /// <summary>
    /// A minimal, dependency-free HTTP/1.1 server.
    ///
    /// IMPORTANT — why this uses reflection for the socket types:
    /// This mod is loaded by StarMap into a load context that cannot bind a direct
    /// <c>System.Net.Sockets</c> member reference from this assembly's IL. On CrossOver/Wine the JIT
    /// throws "MissingMethodException: System.Net.Sockets.TcpListener..ctor(IPAddress, Int32)" the
    /// moment it compiles a method that names a System.Net.Sockets type — even though the type
    /// exists in the runtime (System.Net.Primitives types such as IPAddress resolve fine; only
    /// System.Net.Sockets fails). We therefore never put a System.Net.Sockets token in our IL:
    /// TcpListener/TcpClient are created and driven via reflection (resolved at runtime through the
    /// default load context), and all I/O goes through the base System.IO.Stream type, which is in
    /// the core library and always resolvable. HttpListener is likewise avoided — its Start() calls
    /// the Windows HTTP Server API (http.sys), which Wine does not implement.
    /// </summary>
    public sealed class TcpHttpServer : IDisposable
    {
        private readonly int _port;
        private readonly Dictionary<string, Func<HttpRequest, HttpResponse>> _routes =
            new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _cts;
        private Task? _listenerTask;
        private readonly object _lifecycleGate = new();
        private readonly object _clientsGate = new();
        private readonly Dictionary<object, Task> _clients = new();
        private readonly TimeSpan _requestTimeout;
        private bool _disposed;

        // Reflection handles for System.Net.Sockets, resolved in Start().
        private object? _listener;                 // System.Net.Sockets.TcpListener
        private MethodInfo? _acceptMethod;         // TcpListener.AcceptTcpClientAsync()
        private MethodInfo? _stopMethod;           // TcpListener.Stop()
        private PropertyInfo? _taskResultProp;     // Task<TcpClient>.Result
        private MethodInfo? _getStreamMethod;      // TcpClient.GetStream()

        public TcpHttpServer(int port = 8080, TimeSpan? requestTimeout = null)
        {
            _port = port;
            _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(15);
            if (_requestTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(requestTimeout));
        }

        public int Port => _port;
        public int RouteCount => _routes.Count;

        /// <summary>Registers a handler for a given HTTP method and exact path.</summary>
        public void Map(string method, string path, Func<HttpRequest, HttpResponse> handler)
            => _routes[$"{method.ToUpperInvariant()} {path}"] = handler;

        public void MapGet(string path, Func<HttpRequest, HttpResponse> handler) => Map("GET", path, handler);
        public void MapPut(string path, Func<HttpRequest, HttpResponse> handler) => Map("PUT", path, handler);
        public void MapPost(string path, Func<HttpRequest, HttpResponse> handler) => Map("POST", path, handler);

        /// <summary>Starts accepting connections on a background task.</summary>
        public void Start()
        {
            lock (_lifecycleGate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_listenerTask != null)
                    return;

                // Resolve System.Net.Sockets purely via reflection so no S.N.Sockets token appears in
                // this assembly's IL (see the class remarks for why that matters under StarMap/Wine).
                var socketsAsm = Assembly.Load("System.Net.Sockets");
                var tcpListenerType = socketsAsm.GetType("System.Net.Sockets.TcpListener", throwOnError: true)!;

                // new TcpListener(IPAddress.Any, _port)
                // NOTE: bound to IPAddress.Any (0.0.0.0) so the API is reachable from other machines on
                // the LAN. This API has no authentication or TLS and can control the vessel, so anyone
                // who can reach this port can command it.
                _listener = Activator.CreateInstance(tcpListenerType, IPAddress.Any, _port)!;
                _acceptMethod = tcpListenerType.GetMethod("AcceptTcpClientAsync", Type.EmptyTypes)!;
                _stopMethod = tcpListenerType.GetMethod("Stop", Type.EmptyTypes)!;
                tcpListenerType.GetMethod("Start", Type.EmptyTypes)!.Invoke(_listener, null);

                _cts = new CancellationTokenSource();
                var token = _cts.Token;
                _listenerTask = Task.Run(() => ListenAsync(token));
            }
        }

        /// <summary>Stop accepting, cancel/close clients, and finish handlers before returning.</summary>
        public void Stop()
        {
            lock (_lifecycleGate)
            {
                if (_cts == null) return;
                _cts.Cancel();
                try { _stopMethod?.Invoke(_listener, null); } catch { /* already stopped */ }
                // No client registration can happen after the accept loop has finished.
                _listenerTask?.GetAwaiter().GetResult();
                KeyValuePair<object, Task>[] clients;
                lock (_clientsGate) clients = _clients.ToArray();
                foreach (var client in clients)
                {
                    try { (client.Key as IDisposable)?.Dispose(); } catch { /* already closed */ }
                }
                Task.WhenAll(clients.Select(client => client.Value)).GetAwaiter().GetResult();
                _listenerTask = null;
                _listener = null;
                _cts.Dispose();
                _cts = null;
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                object client;
                try
                {
                    // TcpClient client = await listener.AcceptTcpClientAsync();
                    var acceptTask = (Task)_acceptMethod!.Invoke(_listener, null)!;
                    await acceptTask.ConfigureAwait(false);

                    _taskResultProp ??= acceptTask.GetType().GetProperty("Result")!;
                    client = _taskResultProp.GetValue(acceptTask)!;
                }
                catch (Exception) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[KittenRemoteControl] Error accepting client: {Unwrap(ex).Message}");
                    if (cancellationToken.IsCancellationRequested) break;
                    continue;
                }

                lock (_clientsGate)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        (client as IDisposable)?.Dispose();
                        break;
                    }
                    // Do not cancel task scheduling: its finally block must always close the socket.
                    _clients.Add(client, Task.Run(() => HandleClientAsync(client, cancellationToken)));
                }
            }
        }

        private async Task HandleClientAsync(object client, CancellationToken serverToken)
        {
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(serverToken);
            requestCts.CancelAfter(_requestTimeout);
            var token = requestCts.Token;
            try
            {
                using (client as IDisposable) // TcpClient implements IDisposable; closes the socket.
                {
                    // NetworkStream stream = client.GetStream();  (NetworkStream is a System.IO.Stream)
                    _getStreamMethod ??= client.GetType().GetMethod("GetStream", Type.EmptyTypes)!;
                    var stream = (Stream)_getStreamMethod.Invoke(client, null)!;

                    var request = await ReadRequestAsync(stream, token);
                    token.ThrowIfCancellationRequested();
                    if (request == null)
                        return;

                    ConnectionTracker.Record(RemoteIp(client), _port, $"{request.Method} {request.Path}");

                    HttpResponse response;
                    if (request.Method == "OPTIONS")
                    {
                        response = new HttpResponse { StatusCode = 204, Body = "" };
                    }
                    else if (_routes.TryGetValue($"{request.Method} {request.Path}", out var handler))
                    {
                        try
                        {
                            response = handler(request);
                        }
                        catch (Exception ex)
                        {
                            response = new HttpResponse
                            {
                                StatusCode = 500,
                                Body = JsonSerializer.Serialize(new { error = ex.Message })
                            };
                        }
                    }
                    else
                    {
                        response = new HttpResponse
                        {
                            StatusCode = 404,
                            Body = JsonSerializer.Serialize(new { error = $"No route for {request.Method} {request.Path}" })
                        };
                    }

                    await WriteResponseAsync(stream, response, token);
                }
            }
            catch (Exception) when (requestCts.IsCancellationRequested)
            {
                // Expected on server shutdown or an abandoned/incomplete request.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KittenRemoteControl] Error handling client: {Unwrap(ex).Message}");
            }
            finally
            {
                lock (_clientsGate) _clients.Remove(client);
            }
        }

        private static Exception Unwrap(Exception ex)
            => ex is TargetInvocationException { InnerException: { } inner } ? inner : ex;

        // Extract the client IP from the accepted TcpClient via reflection (no System.Net.Sockets
        // token in our IL). IPEndPoint/IPAddress live in System.Net.Primitives, which resolves fine.
        private static string RemoteIp(object client)
        {
            try
            {
                var socket = client.GetType().GetProperty("Client")?.GetValue(client);
                var endpoint = socket?.GetType().GetProperty("RemoteEndPoint")?.GetValue(socket);
                if (endpoint is IPEndPoint ip)
                    return ip.Address.ToString();
                return endpoint?.ToString() ?? "?";
            }
            catch
            {
                return "?";
            }
        }

        private static async Task<HttpRequest?> ReadRequestAsync(Stream stream, CancellationToken token)
        {
            var buffer = new byte[8192];
            var received = new List<byte>();
            int headerEnd = -1;

            // Read until the end of the header block (\r\n\r\n) is found.
            while (headerEnd < 0)
            {
                var read = await stream.ReadAsync(buffer, token);
                if (read == 0)
                    return null;

                received.AddRange(new ArraySegment<byte>(buffer, 0, read));
                headerEnd = IndexOfDoubleCrlf(received);

                if (received.Count > 1024 * 1024) // 1 MB header safety cap
                    break;
            }

            if (headerEnd < 0)
                return null;

            var headerText = Encoding.ASCII.GetString(received.ToArray(), 0, headerEnd);
            var lines = headerText.Split("\r\n");
            if (lines.Length == 0)
                return null;

            var requestLine = lines[0].Split(' ');
            if (requestLine.Length < 2)
                return null;

            var method = requestLine[0].ToUpperInvariant();
            var rawPath = requestLine[1];
            var queryIdx = rawPath.IndexOf('?');
            var path = queryIdx >= 0 ? rawPath[..queryIdx] : rawPath;
            var query = queryIdx >= 0 ? rawPath[(queryIdx + 1)..] : "";

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < lines.Length; i++)
            {
                var colon = lines[i].IndexOf(':');
                if (colon > 0)
                {
                    var key = lines[i][..colon].Trim();
                    var value = lines[i][(colon + 1)..].Trim();
                    headers[key] = value;
                }
            }

            var contentLength = 0;
            if (headers.TryGetValue("Content-Length", out var clStr))
                int.TryParse(clStr, out contentLength);

            // Body bytes already read past the header terminator.
            var bodyStart = headerEnd + 4;
            var bodyBytes = new List<byte>();
            if (received.Count > bodyStart)
                bodyBytes.AddRange(received.GetRange(bodyStart, received.Count - bodyStart));

            // Read the rest of the body up to Content-Length.
            while (bodyBytes.Count < contentLength)
            {
                var read = await stream.ReadAsync(buffer, token);
                if (read == 0)
                    return null;
                bodyBytes.AddRange(new ArraySegment<byte>(buffer, 0, read));
            }

            var body = contentLength > 0
                ? Encoding.UTF8.GetString(bodyBytes.ToArray(), 0, Math.Min(contentLength, bodyBytes.Count))
                : "";

            return new HttpRequest { Method = method, Path = path, RawQuery = query, Body = body, Headers = headers };
        }

        private static int IndexOfDoubleCrlf(List<byte> data)
        {
            for (var i = 0; i + 3 < data.Count; i++)
            {
                if (data[i] == (byte)'\r' && data[i + 1] == (byte)'\n' &&
                    data[i + 2] == (byte)'\r' && data[i + 3] == (byte)'\n')
                    return i;
            }
            return -1;
        }

        private static async Task WriteResponseAsync(Stream stream, HttpResponse response, CancellationToken token)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(response.Body ?? "");

            var head = new StringBuilder();
            head.Append("HTTP/1.1 ").Append(response.StatusCode).Append(' ')
                .Append(ReasonPhrase(response.StatusCode)).Append("\r\n");
            head.Append("Content-Type: ").Append(response.ContentType).Append("; charset=utf-8\r\n");
            head.Append("Content-Length: ").Append(bodyBytes.Length).Append("\r\n");
            // Permissive CORS so browser-based clients can call the API.
            head.Append("Access-Control-Allow-Origin: *\r\n");
            head.Append("Access-Control-Allow-Methods: GET, PUT, POST, OPTIONS\r\n");
            head.Append("Access-Control-Allow-Headers: Content-Type\r\n");
            head.Append("Connection: close\r\n");
            head.Append("\r\n");

            var headBytes = Encoding.ASCII.GetBytes(head.ToString());
            await stream.WriteAsync(headBytes, token);
            if (bodyBytes.Length > 0)
                await stream.WriteAsync(bodyBytes, token);
            await stream.FlushAsync(token);
        }

        private static string ReasonPhrase(int code) => code switch
        {
            200 => "OK",
            204 => "No Content",
            400 => "Bad Request",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "Status"
        };

        public void Dispose()
        {
            lock (_lifecycleGate)
            {
                if (_disposed) return;
                Stop();
                _disposed = true;
            }
        }
    }
}
