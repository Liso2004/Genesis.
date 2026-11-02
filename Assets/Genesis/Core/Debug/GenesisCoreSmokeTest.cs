using Genesis.Core.Events;
using UnityEngine;

namespace Genesis.Core.Debugging
{
    public sealed class GenesisCoreSmokeTest : MonoBehaviour
    {
        private void Start()
        {
            IGenesisEventBus eventBus = GenesisRuntime.Instance.Events;

            eventBus.Subscribe<CorePingEvent>(HandlePing);

            eventBus.Publish(
                new CorePingEvent("Genesis.Core event system is working.")
            );
        }

        private void HandlePing(CorePingEvent ping)
        {
            UnityEngine.Debug.Log(
                $"[Genesis Core Test] {ping.Message}"
            );
        } 

        private void OnDestroy()
        {
            if (GenesisRuntime.Instance == null)
            {
                return;
            }

            GenesisRuntime.Instance.Events
                .Unsubscribe<CorePingEvent>(HandlePing);
        }

        private readonly struct CorePingEvent : IGenesisEvent
        {
            public string Message { get; }

            public CorePingEvent(string message)
            {
                Message = message;
            }
        }
    }
}