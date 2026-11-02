#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Genesis.Authorities;
using Genesis.Character;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Gods;
using Genesis.Items;
using Genesis.Sandbox;
using Genesis.Skill;
using Genesis.Skill.Definitions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Genesis.Tests
{
    public sealed class GenesisSystemsTests
    {
        private readonly List<GameObject> _objects = new();
        private SandboxScenarioDefinition _scenario;
        [SetUp] public void Setup()
        {
            if (GenesisRuntime.Instance == null) New("Systems runtime").AddComponent<GenesisRuntime>();
            _scenario = AssetDatabase.LoadAssetAtPath<SandboxScenarioDefinition>("Assets/Content/Sandbox/Systems/SandboxScenario.asset");
            Assert.That(_scenario, Is.Not.Null, "Apply Systems Sandbox authoring first.");
        }
        [TearDown] public void Cleanup()
        {
            for (int i = _objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
        }
        private GameObject New(string name) { var go = new GameObject(name); _objects.Add(go); return go; }
        private static void Ref(Object target, string field, Object value)
        { var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private SandboxActor Actor()
        {
            var go = New("Systems test actor"); go.SetActive(false); go.AddComponent<GenesisActor>(); go.AddComponent<SkillSystem>();
            go.AddComponent<ActorInventory>(); go.AddComponent<SkillDomainSystem>(); var gods = go.AddComponent<GodResonanceSystem>();
            var so = new SerializedObject(gods); var p = so.FindProperty("gods"); p.arraySize = 1; p.GetArrayElementAtIndex(0).objectReferenceValue = _scenario.observer; so.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<AuthoritySystem>(); var actor = go.AddComponent<SandboxActor>(); Ref(actor, "scenario", _scenario);
            go.AddComponent<SandboxInteractor>(); go.SetActive(true);
            actor.Skills.RegisterSkill(_scenario.perception, _scenario.progression); actor.Skills.RegisterSkill(_scenario.swordsmanship, _scenario.progression);
            return actor;
        }
        private SandboxStation Station(SandboxStationKind kind, Vector3 position, string id = "test-target")
        {
            var go = New(id); go.transform.position = position; var station = go.AddComponent<SandboxStation>();
            var so = new SerializedObject(station); so.FindProperty("kind").intValue = (int)kind; so.FindProperty("targetId").stringValue = id;
            so.FindProperty("displayName").stringValue = id; so.ApplyModifiedPropertiesWithoutUndo(); return station;
        }
        [Test] public void InventoryIsBoundedAndEquipmentRequiresOwnership()
        {
            var bus = new GenesisEventBus(); int added = 0, equipped = 0;
            bus.Subscribe<ItemAddedEvent>(_ => added++); bus.Subscribe<ItemEquippedEvent>(_ => equipped++);
            var inventory = new InventoryState("test", bus, 1); var gear = new EquipmentState("test", inventory, bus);
            Assert.That(gear.TryEquip(_scenario.ironSword.ItemId), Is.False);
            Assert.That(inventory.TryAdd(_scenario.ironSword, -1), Is.False);
            Assert.That(inventory.TryAdd(_scenario.ironSword, int.MaxValue), Is.False);
            Assert.That(inventory.TryAdd(_scenario.ironSword, 1), Is.True);
            Assert.That(inventory.TryAdd(_scenario.ironSword, 1), Is.False);
            Assert.That(inventory.TryAdd(_scenario.blackwoodShield, 1), Is.False);
            Assert.That(gear.TryEquip(_scenario.ironSword.ItemId), Is.True); Assert.That(gear.TryEquip(_scenario.ironSword.ItemId), Is.True);
            Assert.That(added, Is.EqualTo(1)); Assert.That(equipped, Is.EqualTo(1)); Assert.That(_scenario.ironSword.MaxStack, Is.EqualTo(1));
        }
        [Test] public void PickupCanOnlyBeClaimedOnceAcrossActors()
        {
            var first = Actor(); var second = Actor(); var pickup = Station(SandboxStationKind.Pickup, Vector3.forward);
            Ref(pickup, "item", _scenario.ironSword);
            Assert.That(pickup.TryCollect(first.Items), Is.True); Assert.That(pickup.TryCollect(second.Items), Is.False);
            Assert.That(first.Items.Inventory.Get(_scenario.ironSword.ItemId).Count, Is.EqualTo(1));
            Assert.That(second.Items.Inventory.Get(_scenario.ironSword.ItemId), Is.Null);
        }
        [Test] public void DomainAndGodProgressAreActorScopedAndBlessingRequiresAcceptance()
        {
            var a = Actor(); var b = Actor(); Assert.That(a.Gods.TryClaimBlessing(_scenario.observer), Is.False);
            for (int i = 0; i < 3; i++) a.Skills.RegisterMeaningfulUse(_scenario.perception.SkillId, 10);
            Assert.That(a.GetComponent<SkillDomainSystem>().GetProgress("knowledge"), Is.EqualTo(15));
            Assert.That(a.Gods.GetState("observer").Favor, Is.EqualTo(30)); Assert.That(a.Gods.GetState("observer").HasOpportunity, Is.True);
            Assert.That(a.Gods.ObservationRangeMultiplier, Is.EqualTo(1)); Assert.That(b.Gods.GetState("observer"), Is.Null);
            Assert.That(b.Skills.GetState("perception").MeaningfulUses, Is.Zero);
            Assert.That(a.Gods.TryClaimBlessing(_scenario.observer), Is.True); Assert.That(a.Gods.TryClaimBlessing(_scenario.observer), Is.False);
            Assert.That(a.Gods.ObservationRangeMultiplier, Is.EqualTo(1.15f).Within(.001));
        }
        [Test] public void NonFiniteProgressIsRejectedAndMasteryDoesNotChangeTier()
        {
            var a = Actor(); Assert.That(a.Skills.RegisterMeaningfulUse("perception", float.NaN), Is.False);
            Assert.That(a.Skills.RegisterMeaningfulUse("perception", float.PositiveInfinity), Is.False);
            Assert.That(a.Skills.GetState("perception").MeaningfulUses, Is.Zero); a.Skills.RegisterMeaningfulUse("perception", 400);
            Assert.That(a.Skills.GetState("perception").MasteryStage, Is.EqualTo(SkillMasteryStage.Mastery)); Assert.That(_scenario.perception.Tier, Is.EqualTo(SkillTier.Basic));
        }
        [Test] public void AuthorityRequiresBothMasteriesAndConditionAndPublishesOnce()
        {
            var a = Actor(); var other = Actor(); int count = 0;
            System.Action<AuthorityUnlockedEvent> listener = e => { if (e.Source == a.Skills) count++; };
            GenesisRuntime.Instance.Events.Subscribe(listener);
            try {
                Assert.That(a.Authorities.TryUnlock(_scenario.sovereignServant, out var missing), Is.False); Assert.That(missing.Count, Is.EqualTo(3));
                foreach (var req in _scenario.sovereignServant.Skills) { a.Skills.RegisterSkill(req.skill, _scenario.progression); a.Skills.RegisterMeaningfulUse(req.skill.SkillId, 400); }
                Assert.That(a.Authorities.TryUnlock(_scenario.sovereignServant, out missing), Is.False); Assert.That(missing, Does.Contain("trial-of-understanding"));
                a.Authorities.RecordCondition("trial-of-understanding");
                Assert.That(a.Authorities.TryUnlock(_scenario.sovereignServant, out missing), Is.True); Assert.That(a.Authorities.TryUnlock(_scenario.sovereignServant, out missing), Is.True);
                Assert.That(count, Is.EqualTo(1)); Assert.That(other.Authorities.HasOpportunity("sovereign-servant"), Is.False);
            } finally { GenesisRuntime.Instance.Events.Unsubscribe(listener); }
        }
        [UnityTest] public IEnumerator ObservationRequiresRangeVisibilityAndNovelty()
        {
            var a = Actor(); var camera = Camera.main; if (camera != null) camera.transform.rotation = Quaternion.identity;
            var target = Station(SandboxStationKind.Observation, Vector3.forward * 20);
            Assert.That(a.Observe(target), Is.False); target.transform.position = Vector3.forward * 3;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); _objects.Add(wall); wall.transform.position = new Vector3(0,1,1.5f); wall.transform.localScale = new Vector3(3,3,.2f); Physics.SyncTransforms();
            Assert.That(a.Observe(target), Is.False); wall.SetActive(false); Physics.SyncTransforms();
            Assert.That(a.Observe(target), Is.True); Assert.That(a.Observe(target), Is.False); Assert.That(a.Skills.GetState("perception").MeaningfulUses, Is.EqualTo(1));
            target.transform.position = -Vector3.forward * 3; Assert.That(a.CanInteract(target), Is.False); yield return null;
        }
        [UnityTest] public IEnumerator PracticeNeedsEquipmentAndRejectsIdenticalRepeatedContext()
        {
            var a = Actor(); var camera = Camera.main; if (camera != null) camera.transform.rotation = Quaternion.identity;
            var target = Station(SandboxStationKind.Training, Vector3.forward * 2); Assert.That(a.Practice(target, 1), Is.False);
            a.Items.Inventory.TryAdd(_scenario.ironSword, 1); a.Items.Equipment.TryEquip(_scenario.ironSword.ItemId);
            Assert.That(a.Practice(target, 2), Is.True); Assert.That(a.Practice(target, 4), Is.False);
            Assert.That(a.GetComponent<SkillDomainSystem>().GetProgress("warfare"), Is.EqualTo(10)); Assert.That(a.Gods.GetState("observer"), Is.Null); yield return null;
        }
    }
}
#endif
