using System.Collections.Concurrent;
using System.Linq;

namespace KittenRemoteControl
{
    /// <summary>
    /// Records which clients have talked to the HTTP servers, for display in the in-game window.
    /// Shared across both servers (control API + Telemachus datalink). Thread-safe.
    /// </summary>
    public static class ConnectionTracker
    {
        public sealed class Entry
        {
            public string Ip = "";
            public int Port;
            public long Count;
            public long LastSeenTick;      // Environment.TickCount64 at last request
            public string LastRequest = "";
        }

        private static readonly ConcurrentDictionary<string, Entry> ByIp = new();

        public static void Record(string ip, int port, string request)
        {
            var entry = ByIp.GetOrAdd(ip, key => new Entry { Ip = key });
            entry.Port = port;
            entry.LastRequest = request;
            entry.LastSeenTick = Environment.TickCount64;
            System.Threading.Interlocked.Increment(ref entry.Count);
        }

        public static IReadOnlyList<Entry> Snapshot() =>
            ByIp.Values.OrderByDescending(e => e.LastSeenTick).ToArray();
    }
}
