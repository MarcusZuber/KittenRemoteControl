using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KittenRemoteControl
{
    /// <summary>
    /// Simple TCP Server.
    /// Allows registering GET and SET handlers.
    /// </summary>
    public class SimpleSocketServer(int port = 8080) : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Any, port);
        private readonly Dictionary<string, Func<string>> _getHandlers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Action<string>> _setHandlers = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource? _cts;
        private Task? _listenerTask;

        /// <summary>
        /// Registers a GET-Handler
        /// </summary>
        public void RegisterGet(string path, Func<string?> handler)
        {
            _getHandlers[path] = handler;
        }

        /// <summary>
        /// Registers a SET-Handler
        /// </summary>
        public void RegisterSet(string path, Action<string> handler)
        {
            _setHandlers[path] = handler;
        }

        /// <summary>
        /// Starts the Server
        /// </summary>
        public void Start()
        {
            if (_listenerTask != null)
                return;

            _listener.Start();
            _cts = new CancellationTokenSource();
            _listenerTask = Task.Run(() => ListenAsync(_cts.Token));
            Console.WriteLine($"Socket Server started on port {port}");
            Console.WriteLine("Commands: GET /path | SET /path value");
        }

        /// <summary>
        /// Stops the Server
        /// </summary>
        private void Stop()
        {
            _cts?.Cancel();
            _listener.Stop();
            _listenerTask?.Wait(TimeSpan.FromSeconds(5));
            _listenerTask = null;
            Console.WriteLine("Socket Server stopped");
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    _ = Task.Run(() => HandleClientAsync(client), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    var buffer = new byte[4096];
                    var bytesRead = await stream.ReadAsync(buffer);
                    
                    if (bytesRead > 0)
                    {
                        var command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        var response = ProcessCommand(command);
                        
                        var responseBytes = Encoding.UTF8.GetBytes(response + "\n");
                        await stream.WriteAsync(responseBytes);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
        }

        private string ProcessCommand(string command)
        {
            try
            {
                var parts = command.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length == 0)
                {
                    return "ERROR: Empty command";
                }

                var verb = parts[0].ToUpper();

                switch (verb)
                {
                    case "GET" when parts.Length >= 2:
                    {
                        var path = parts[1];
                        if (!_getHandlers.TryGetValue(path, out var handler)) return $"ERROR: Unknown path '{path}'";
                        var result = handler();
                        return $"OK {result}";
                    }
                    case "SET" when parts.Length >= 3:
                    {
                        var path = parts[1];
                        var value = parts[2];

                        if (!_setHandlers.TryGetValue(path, out var handler)) return $"ERROR: Unknown path '{path}'";
                        handler(value);
                        return "OK";
                    }
                    default:
                        return $"ERROR: Invalid command format. Use: GET /path or SET /path value";
                }
            }
            catch (Exception ex)
            {
                return $"ERROR: {ex.Message}";
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}

