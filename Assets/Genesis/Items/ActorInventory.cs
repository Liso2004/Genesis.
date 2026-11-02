using Genesis.Character;
using Genesis.Core;
using UnityEngine;

namespace Genesis.Items
{
    [DefaultExecutionOrder(50), RequireComponent(typeof(GenesisActor))]
    public sealed class ActorInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 16;
        public InventoryState Inventory { get; private set; }
        public EquipmentState Equipment { get; private set; }
        public string ActorId { get; private set; }
        public int Capacity => capacity;
        public void Restore(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<ItemDefinition,int>> entries,
            System.Collections.Generic.IEnumerable<ItemDefinition> equipped)
        {
            var bus = GenesisRuntime.Instance != null ? GenesisRuntime.Instance.Events : null;
            var inventory = InventoryState.Restored(ActorId, bus, capacity, entries);
            var equipment = EquipmentState.Restored(ActorId, inventory, bus, equipped);
            Inventory = inventory; Equipment = equipment;
        }
        private void Awake()
        {
            ActorId = GetComponent<GenesisActor>().ActorId;
            var bus = GenesisRuntime.Instance != null ? GenesisRuntime.Instance.Events : null;
            Inventory = new InventoryState(ActorId, bus, capacity);
            Equipment = new EquipmentState(ActorId, Inventory, bus);
        }
    }
}
