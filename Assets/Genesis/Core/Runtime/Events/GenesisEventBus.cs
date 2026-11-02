using System;
using System.Collections.Generic;

namespace Genesis.Core.Events
{
    public sealed class GenesisEventBus : IGenesisEventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _handlers = new();

        public void Subscribe<T>(Action<T> handler)
            where T : IGenesisEvent
        {
            Type eventType = typeof(T);

            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                handlers = new List<Delegate>();
                _handlers[eventType] = handlers;
            }

            if (!handlers.Contains(handler))
            {
                handlers.Add(handler);
            }
        }

        public void Unsubscribe<T>(Action<T> handler)
            where T : IGenesisEvent
        {
            Type eventType = typeof(T);

            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                return;
            }

            handlers.Remove(handler);

            if (handlers.Count == 0)
            {
                _handlers.Remove(eventType);
            }
        }

        public void Publish<T>(T genesisEvent)
            where T : IGenesisEvent
        {
            Type eventType = typeof(T);

            if (!_handlers.TryGetValue(eventType, out List<Delegate> handlers))
            {
                return;
            }

            Delegate[] snapshot = handlers.ToArray();

            foreach (Delegate listener in snapshot)
            {
                if (listener is Action<T> handler)
                {
                    handler.Invoke(genesisEvent);
                }
            }
        }

        public void Clear()
        {
            _handlers.Clear();
        }
    }
}