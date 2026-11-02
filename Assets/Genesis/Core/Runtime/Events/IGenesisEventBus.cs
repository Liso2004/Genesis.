using System;

namespace Genesis.Core.Events
{
    public interface IGenesisEventBus
    {
        void Subscribe<T>(Action<T> handler)
            where T : IGenesisEvent;

        void Unsubscribe<T>(Action<T> handler)
            where T : IGenesisEvent;

        void Publish<T>(T genesisEvent)
            where T : IGenesisEvent;

        void Clear();
    }
}