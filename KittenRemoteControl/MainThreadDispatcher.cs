using System.Collections.Concurrent;

namespace KittenRemoteControl
{
    /// <summary>
    /// Marshals work from HTTP handler threads onto the game's main/simulation thread.
    ///
    /// The HTTP servers run on background threads. Simple field pokes (throttle, engine flag) are
    /// benign there, but heavier game mutations — staging in particular — must run on the main
    /// thread or they race the simulation and wedge the game. Handlers enqueue an Action here and
    /// it is executed from the per-frame hook (RemoteControlMain.OnAfterUi) on the main thread.
    /// </summary>
    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> Queue = new();

        /// <summary>Queue an action to run on the next main-thread frame.</summary>
        public static void Enqueue(Action action) => Queue.Enqueue(action);

        /// <summary>Discard actions when the mod unloads.</summary>
        public static void Clear()
        {
            while (Queue.TryDequeue(out _)) { }
        }

        /// <summary>Run all queued actions. Call this from the main thread once per frame.</summary>
        public static void Drain()
        {
            while (Queue.TryDequeue(out var action))
            {
                try { action(); }
                catch (Exception ex) { Console.WriteLine($"[KittenRemoteControl] Main-thread action failed: {ex.Message}"); }
            }
        }
    }
}
