using System;
using System.Collections.Generic;

namespace BetterLoad
{
    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
        private readonly object _lock = new();

        public void Subscribe<T>(Action<T> handler) where T : class
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                if (!_subscribers.TryGetValue(eventType, out var handlers))
                {
                    handlers = new List<Delegate>();
                    _subscribers.Add(eventType, handlers);
                }
                handlers.Add(handler);
            }
        }

        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            lock (_lock)
            {
                var eventType = typeof(T);
                if (_subscribers.TryGetValue(eventType, out var handlers))
                {
                    handlers.Remove(handler);
                }
            }
        }

        public void Publish<T>(T eventData) where T : class
        {
            if (eventData == null) return;

            List<Delegate> handlers;
            lock (_lock)
            {
                if (!_subscribers.TryGetValue(typeof(T), out handlers) || handlers.Count == 0)
                    return;
            }

            foreach (var handler in handlers)
            {
                try
                {
                    ((Action<T>)handler)(eventData);
                }
                catch (Exception ex)
                {
                    ModuleManager.Logger?.LogError($"Event handler error: {ex.Message}");
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
                _subscribers.Clear();
        }
    }

    public static class GameEvents
    {
        public class RaidStartEvent
        {
            public long Timestamp { get; set; }
        }

        public class RaidEndEvent
        {
            public bool IsDeath { get; set; }
            public string ExitLocation { get; set; }
        }

        public class ModuleLoadedEvent
        {
            public IModule Module { get; set; }
        }
    }
}
