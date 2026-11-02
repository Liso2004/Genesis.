using Genesis.Core.Events;
using UnityEngine;

namespace Genesis.Core
{
    [DefaultExecutionOrder(-10000)]
    public sealed class GenesisRuntime : MonoBehaviour
    {
        public static GenesisRuntime Instance { get; private set; }

        public IGenesisEventBus Events { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            Events = new GenesisEventBus();

            Debug.Log("[Genesis] Runtime initialized.");
        }

        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            Events?.Clear();

            Instance = null;
        }
    }
}