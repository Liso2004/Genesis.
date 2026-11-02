#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Genesis.Character;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Gods;
using Genesis.Items;
using Genesis.Save;
using Genesis.Sandbox;
using Genesis.Skill;
using Genesis.Skill.Events;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Genesis.Tests
{
    public sealed class PlayerSaveTests
    {
        private GameObject _runtime,_player,_floor;
        private SandboxSaveController _save;
        private SaveCatalog _catalog;
        private string _directory,_path;
        [SetUp] public void SetUp()
        {
            if(GenesisRuntime.Instance==null){_runtime=new GameObject("Save test runtime");_runtime.AddComponent<GenesisRuntime>();}
            _catalog=AssetDatabase.LoadAssetAtPath<SaveCatalog>("Assets/Content/Creation/SandboxSaveCatalog.asset");
            _player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab"));
            var motor=_player.GetComponent<PlayerController>();motor.enabled=false;
            _player.AddComponent<SandboxActor>();_save=_player.AddComponent<SandboxSaveController>();
            var data=new SerializedObject(_save);data.FindProperty("catalog").objectReferenceValue=_catalog;data.ApplyModifiedPropertiesWithoutUndo();
            _floor=GameObject.CreatePrimitive(PrimitiveType.Cube);_floor.name="Save test floor";_floor.transform.position=new Vector3(2000,-.1f,2000);_floor.transform.localScale=new Vector3(20,.2f,20);
            Physics.SyncTransforms();motor.RestorePose(new Vector3(2000,.03f,2000),Quaternion.identity,false);motor.Simulate(default,1f/60f);
            _directory=Path.Combine(Application.temporaryCachePath,"GenesisSaveTests",Guid.NewGuid().ToString("N"));_path=Path.Combine(_directory,"test.json");
        }
        [TearDown] public void TearDown()
        {
            if(_player!=null)Object.DestroyImmediate(_player);if(_floor!=null)Object.DestroyImmediate(_floor);if(_runtime!=null)Object.DestroyImmediate(_runtime);
            // Only this test's concrete files are removed; never recursively delete a computed directory.
            foreach(var suffix in new[]{"",".bak",".tmp"})if(File.Exists(_path+suffix))File.Delete(_path+suffix);
            if(Directory.Exists(_directory))Directory.Delete(_directory,false);
        }
        private ActorInventory Items=>_player.GetComponent<ActorInventory>();
        private SkillSystem Skills=>_player.GetComponent<SkillSystem>();
        private PlayerSaveData Clone(PlayerSaveData data)=>JsonUtility.FromJson<PlayerSaveData>(JsonUtility.ToJson(data));
        private void AssertRejectedWithoutMutation(PlayerSaveData invalid)
        {
            var before=_save.Capture();SaveFileStore.Write(_path,invalid);Assert.That(_save.TryLoad(_path),Is.False,_save.LastResult);
            var after=_save.Capture();before.savedUtc=after.savedUtc;Assert.That(JsonUtility.ToJson(after),Is.EqualTo(JsonUtility.ToJson(before)));
        }
        [Test] public void RoundTripRestoresItemsSkillsAndAppearanceWithoutRewardEvents()
        {
            var sword=_catalog.Item("iron-sword");var helmet=_catalog.Item("training-head");Items.Inventory.TryAdd(sword,1);Items.Inventory.TryAdd(helmet,1);Items.Equipment.TryEquip(sword.ItemId);
            Skills.RegisterMeaningfulUse("perception",37);var before=_save.Capture();Assert.That(_save.TrySave(_path),Is.True,_save.LastResult);
            Items.Equipment.TryEquip(helmet.ItemId);Skills.RegisterMeaningfulUse("perception",50);
            int rewards=0,restores=0;Action<SkillMeaningfulUseEvent> use=e=>rewards++;Action<ItemAddedEvent> added=e=>rewards++;Action<ActorStateRestoredEvent> restored=e=>restores++;
            var bus=GenesisRuntime.Instance.Events;bus.Subscribe(use);bus.Subscribe(added);bus.Subscribe(restored);
            try
            {
                Assert.That(_save.TryLoad(_path),Is.True,_save.LastResult);
                Assert.That(rewards,Is.Zero);Assert.That(restores,Is.EqualTo(1));
                Assert.That(Items.Equipment.Get(EquipmentSlot.Head),Is.Null);Assert.That(Items.Equipment.Get(EquipmentSlot.MainHand),Is.SameAs(sword));
                Assert.That(_player.GetComponentInChildren<ModularAppearance>().IsPartVisible("Chr_Hair_01"),Is.True);
                var after=_save.Capture();before.savedUtc=after.savedUtc;Assert.That(JsonUtility.ToJson(after),Is.EqualTo(JsonUtility.ToJson(before)));
            }
            finally{bus.Unsubscribe(use);bus.Unsubscribe(added);bus.Unsubscribe(restored);}
        }
        [Test] public void UnknownVersionActorSceneAndAssetsAreRejectedBeforeMutation()
        {
            var original=_save.Capture();var invalid=Clone(original);invalid.schemaVersion=99;AssertRejectedWithoutMutation(invalid);
            invalid=Clone(original);invalid.actorId="another-actor";AssertRejectedWithoutMutation(invalid);
            invalid=Clone(original);invalid.scenePath="another-scene";AssertRejectedWithoutMutation(invalid);
            invalid=Clone(original);invalid.inventory=new[]{new SavedItem{id="missing-item",count=1}};AssertRejectedWithoutMutation(invalid);
            invalid=Clone(original);invalid.identity.unique="missing-skill";AssertRejectedWithoutMutation(invalid);
        }
        [Test] public void InvalidOwnershipStacksSkillsAndAppearanceAreRejected()
        {
            var original=_save.Capture();var invalid=Clone(original);invalid.equipped=new[]{"iron-sword"};AssertRejectedWithoutMutation(invalid);
            invalid=Clone(original);invalid.inventory=new[]{new SavedItem{id="iron-sword",count=2}};AssertRejectedWithoutMutation(invalid);
            invalid=Clone(original);invalid.skills[0].progress=-1;AssertRejectedWithoutMutation(invalid);
            invalid=Clone(original);invalid.skills=invalid.skills.Concat(new[]{invalid.skills[0]}).ToArray();AssertRejectedWithoutMutation(invalid);
            invalid=Clone(original);invalid.currentAppearance=new[]{"Chr_BackAttachment_03"};AssertRejectedWithoutMutation(invalid);
        }
        [Test] public void CorruptAndMissingFilesDoNotResetTheActor()
        {
            Assert.That(_save.TryLoad(_path),Is.False);Assert.That(_save.TrySave(_path),Is.True,_save.LastResult);
            var before=_save.Capture();File.WriteAllText(_path,"{broken");Assert.That(_save.TryLoad(_path),Is.False);
            var after=_save.Capture();before.savedUtc=after.savedUtc;Assert.That(JsonUtility.ToJson(after),Is.EqualTo(JsonUtility.ToJson(before)));
        }
        [Test] public void AtomicReplacementRetainsReadablePreviousCheckpoint()
        {
            var initial=_save.Capture();SaveFileStore.Write(_path,initial);Items.Inventory.TryAdd(_catalog.Item("soul-coins"),25);SaveFileStore.Write(_path,_save.Capture());
            Assert.That(SaveFileStore.Read(_path).inventory.Single().count,Is.EqualTo(25));Assert.That(SaveFileStore.Read(_path+".bak").inventory,Is.Empty);Assert.That(File.Exists(_path+".tmp"),Is.False);
            string text=File.ReadAllText(_path);File.WriteAllText(_path,text.Replace("Sandbox Wanderer","Changed Wanderer"));Assert.Throws<InvalidDataException>(()=>SaveFileStore.Read(_path));
        }
        [Test] public void BlockedPlacementAndAirborneSaveAreRejected()
        {
            Assert.That(_save.TrySave(_path),Is.True,_save.LastResult);var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
            try{wall.transform.position=_player.transform.position+Vector3.up;Physics.SyncTransforms();Assert.That(_save.TryLoad(_path),Is.False);}
            finally{Object.DestroyImmediate(wall);}
            var motor=_player.GetComponent<PlayerController>();motor.Simulate(new PlayerMovementInput.Frame(Vector2.zero,jump:true),1f/60f);
            Assert.That(_save.TrySave(_path),Is.False);
        }
        [Test] public void CrouchPoseRestoresColliderAndClearsOldMotion()
        {
            var motor=_player.GetComponent<PlayerController>();motor.Simulate(new PlayerMovementInput.Frame(Vector2.zero,toggleCrouch:true),1f/60f);
            Assert.That(_save.TrySave(_path),Is.True,_save.LastResult);motor.TryToggleCrouch();
            Assert.That(_save.TryLoad(_path),Is.True,_save.LastResult);Assert.That(motor.IsCrouching,Is.True);Assert.That(_player.GetComponent<CharacterController>().height,Is.EqualTo(1.25f).Within(.001));Assert.That(motor.PlanarSpeed,Is.Zero);
        }
    }
}
#endif
