#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using Genesis.Character;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Items;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Genesis.Tests
{
    public sealed class InventoryEquipmentTests
    {
        private const string Content = "Assets/Content/Sandbox/Systems/";
        private static ItemDefinition Item(string id) => AssetDatabase.LoadAssetAtPath<ItemDefinition>(Content + "Item-" + id + ".asset");

        [Test] public void UnequipIsIdempotentAndNeverDuplicatesOwnedItems()
        {
            var bus = new GenesisEventBus();
            var inventory = new InventoryState("player", bus);
            var equipment = new EquipmentState("player", inventory, bus);
            var sword = Item("iron-sword"); int removed = 0;
            bus.Subscribe<ItemUnequippedEvent>(e => { Assert.That(e.ActorId, Is.EqualTo("player")); removed++; });
            Assert.That(equipment.TryEquip(sword.ItemId), Is.False);
            Assert.That(inventory.TryAdd(sword, 1), Is.True);
            Assert.That(equipment.TryEquip(sword.ItemId), Is.True);
            Assert.That(equipment.TryUnequip(EquipmentSlot.MainHand), Is.True);
            Assert.That(equipment.TryUnequip(EquipmentSlot.MainHand), Is.False);
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(inventory.Get(sword.ItemId).Count, Is.EqualTo(1));
        }

        [Test] public void SwappingRetainsBothItemsAndDoesNotAffectAnotherActor()
        {
            var bus = new GenesisEventBus(); var inventory = new InventoryState("a", bus);
            var equipment = new EquipmentState("a", inventory, bus);
            var other = new EquipmentState("b", new InventoryState("b",bus), bus);
            var first = Item("iron-sword"); var second = Object.Instantiate(first);
            try
            {
                var data = new SerializedObject(second); data.FindProperty("itemId").stringValue = "test-alternate-sword"; data.ApplyModifiedPropertiesWithoutUndo();
                inventory.TryAdd(first,1); inventory.TryAdd(second,1);
                equipment.TryEquip(first.ItemId); equipment.TryEquip(second.ItemId);
                Assert.That(equipment.Get(EquipmentSlot.MainHand), Is.SameAs(second));
                Assert.That(inventory.Get(first.ItemId).Count, Is.EqualTo(1));
                Assert.That(inventory.Get(second.ItemId).Count, Is.EqualTo(1));
                Assert.That(other.Get(EquipmentSlot.MainHand), Is.Null);
                Assert.That(other.TryEquip(second.ItemId), Is.False);
            }
            finally { Object.DestroyImmediate(second); }
        }

        [UnityTest] public IEnumerator FullOutfitRestoresIdentityAndLeavesNoOrphanWeaponMeshes()
        {
            var runtime = GenesisRuntime.Instance == null ? new GameObject("Inventory test runtime") : null;
            if (runtime != null) runtime.AddComponent<GenesisRuntime>();
            GameObject player = null;
            try
            {
                player = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab"));
                player.GetComponent<PlayerController>().enabled = false;
                var actor = player.GetComponent<ActorInventory>();
                var appearance = player.GetComponentInChildren<ModularAppearance>();
                var original = appearance.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r=>r.name).OrderBy(s=>s).ToArray();
                Assert.That(actor.Inventory.Entries.Count, Is.Zero);
                var ids = new[]{"iron-sword","blackwood-shield","training-head","training-chest","training-hands","training-legs","training-cape"};
                foreach (string id in ids)
                {
                    var item=Item(id); Assert.That(actor.Inventory.TryAdd(item,1),Is.True);
                    Assert.That(actor.Equipment.TryEquip(id),Is.True);
                    foreach(string part in item.VisibleParts) Assert.That(appearance.IsPartVisible(part),Is.True,part);
                }
                Assert.That(appearance.IsPartVisible("Chr_Hair_01"),Is.False);
                foreach (string name in new[]{"Equipped Iron Sword","Equipped Blackwood Shield"})
                {
                    var weapon=player.GetComponentsInChildren<Transform>().Single(t=>t.name==name);
                    Assert.That(weapon.GetComponentsInChildren<Renderer>().Max(r=>r.bounds.size.magnitude),Is.GreaterThan(.4f),"Weapon must remain visible on centimetre-scaled Humanoid bones");
                }
                foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot))) actor.Equipment.TryUnequip(slot);
                yield return null; // Deferred weapon destruction must have completed.
                CollectionAssert.AreEqual(original,appearance.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r=>r.name).OrderBy(s=>s).ToArray());
                Assert.That(player.GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Equipped ")),Is.False);
                Assert.That(actor.Inventory.Entries.Count,Is.EqualTo(7));
                Assert.That(player.GetComponentInChildren<Animator>().avatar.isValid,Is.True);
            }
            finally { if(player!=null) Object.DestroyImmediate(player); if(runtime!=null) Object.DestroyImmediate(runtime); }
        }
    }
}
#endif
