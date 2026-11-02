using System.Collections.Generic;
using System.Collections.ObjectModel;
using Genesis.Core.Events;

namespace Genesis.Items
{
    public readonly struct ItemAddedEvent : IGenesisEvent
    {
        public readonly string ActorId;
        public readonly ItemDefinition Item;
        public readonly int Amount, Total;
        public ItemAddedEvent(string actorId, ItemDefinition item, int amount, int total)
        { ActorId = actorId; Item = item; Amount = amount; Total = total; }
    }
    public readonly struct ItemEquippedEvent : IGenesisEvent
    {
        public readonly string ActorId;
        public readonly ItemDefinition Item;
        public ItemEquippedEvent(string actorId, ItemDefinition item) { ActorId = actorId; Item = item; }
    }
    public readonly struct ItemUnequippedEvent : IGenesisEvent
    {
        public readonly string ActorId;
        public readonly ItemDefinition Item;
        public ItemUnequippedEvent(string actorId, ItemDefinition item) { ActorId = actorId; Item = item; }
    }
    public sealed class InventoryEntry
    {
        public ItemDefinition Definition { get; }
        public int Count { get; internal set; }
        internal InventoryEntry(ItemDefinition definition, int count) { Definition = definition; Count = count; }
    }

    /// <summary>One actor's bounded stack store; definitions are never mutated.</summary>
    public sealed class InventoryState
    {
        private readonly Dictionary<string, InventoryEntry> _entries = new();
        private readonly string _actorId;
        private readonly IGenesisEventBus _bus;
        private readonly int _capacity;
        public IReadOnlyDictionary<string, InventoryEntry> Entries { get; }
        public InventoryState(string actorId, IGenesisEventBus bus, int capacity = 16)
        {
            _actorId = actorId; _bus = bus; _capacity = System.Math.Max(1, capacity);
            Entries = new ReadOnlyDictionary<string, InventoryEntry>(_entries);
        }
        public InventoryEntry Get(string id) => id != null && _entries.TryGetValue(id, out var entry) ? entry : null;
        public static InventoryState Restored(string actorId, IGenesisEventBus bus, int capacity, IEnumerable<KeyValuePair<ItemDefinition,int>> entries)
        {
            var result = new InventoryState(actorId, bus, capacity);
            foreach (var pair in entries)
            {
                if (pair.Key == null || !pair.Key.IsValid || pair.Value < 1 || pair.Value > pair.Key.MaxStack ||
                    result._entries.Count >= result._capacity || result._entries.ContainsKey(pair.Key.ItemId))
                    throw new System.ArgumentException("Invalid saved inventory.");
                result._entries.Add(pair.Key.ItemId, new InventoryEntry(pair.Key, pair.Value));
            }
            return result;
        }
        public bool TryAdd(ItemDefinition item, int amount)
        {
            if (item == null || !item.IsValid || amount <= 0) return false;
            var existing = Get(item.ItemId);
            if (existing != null && existing.Definition != item) return false; // Conflicting authored IDs.
            int count = existing?.Count ?? 0;
            if (amount > item.MaxStack - count || (existing == null && _entries.Count >= _capacity)) return false;
            if (existing == null) _entries.Add(item.ItemId, new InventoryEntry(item, amount));
            else existing.Count += amount;
            _bus?.Publish(new ItemAddedEvent(_actorId, item, amount, count + amount));
            return true;
        }
    }

    public sealed class EquipmentState
    {
        private readonly Dictionary<EquipmentSlot, ItemDefinition> _slots = new();
        private readonly InventoryState _inventory;
        private readonly string _actorId;
        private readonly IGenesisEventBus _bus;
        public EquipmentState(string actorId, InventoryState inventory, IGenesisEventBus bus)
        { _actorId = actorId; _inventory = inventory; _bus = bus; }
        public ItemDefinition Get(EquipmentSlot slot) => _slots.TryGetValue(slot, out var item) ? item : null;
        public static EquipmentState Restored(string actorId, InventoryState inventory, IGenesisEventBus bus, IEnumerable<ItemDefinition> items)
        {
            var result = new EquipmentState(actorId, inventory, bus);
            foreach (var item in items)
            {
                if (item == null || item.Slot == EquipmentSlot.None || inventory.Get(item.ItemId)?.Definition != item || result._slots.ContainsKey(item.Slot))
                    throw new System.ArgumentException("Invalid saved equipment.");
                result._slots.Add(item.Slot, item);
            }
            return result;
        }
        public bool TryEquip(string itemId)
        {
            var item = _inventory.Get(itemId)?.Definition;
            if (item == null || item.Slot == EquipmentSlot.None) return false;
            if (Get(item.Slot) == item) return true;
            _slots[item.Slot] = item;
            _bus?.Publish(new ItemEquippedEvent(_actorId, item));
            return true;
        }
        public bool TryUnequip(EquipmentSlot slot)
        {
            var item = Get(slot);
            if (item == null) return false;
            _slots.Remove(slot);
            // Equipped items remain owned in the inventory; unequipping never adds a duplicate.
            _bus?.Publish(new ItemUnequippedEvent(_actorId, item));
            return true;
        }
    }
}
