using System;
using System.IO;
using System.Linq;
using Genesis.Authorities;
using Genesis.Character;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Creation;
using Genesis.Gods;
using Genesis.Items;
using Genesis.Sandbox;
using Genesis.Skill;
using UnityEngine;

namespace Genesis.Save
{
    /// <summary>Explicit Sandbox quick checkpoints; no automatic reset, load, save or scene switching.</summary>
    [DefaultExecutionOrder(700)]
    public sealed class SandboxSaveController : MonoBehaviour
    {
        [SerializeField] private SaveCatalog catalog;
        [SerializeField] private SandboxStation[] stations=Array.Empty<SandboxStation>();
        [SerializeField] private KeyCode saveKey=KeyCode.F5,loadKey=KeyCode.F9;
        private ActorInventory _items;
        private SkillSystem _skills;
        private SkillDomainSystem _domains;
        private GodResonanceSystem _gods;
        private AuthoritySystem _authorities;
        private PlayerCreationBootstrap _creation;
        private ModularAppearance _appearance;
        private PlayerController _motor;
        private SandboxActor _sandbox;
        public string SlotPath => Path.Combine(Application.persistentDataPath,"Genesis","sandbox-player-v1.json");
        public string LastResult { get; private set; }
        private void Awake()
        {
            _items=GetComponent<ActorInventory>();_skills=GetComponent<SkillSystem>();_domains=GetComponent<SkillDomainSystem>();
            _gods=GetComponent<GodResonanceSystem>();_authorities=GetComponent<AuthoritySystem>();_creation=GetComponent<PlayerCreationBootstrap>();
            _appearance=GetComponentInChildren<ModularAppearance>();_motor=GetComponent<PlayerController>();_sandbox=GetComponent<SandboxActor>();
        }
        private void Update()
        {
            if(Input.GetKeyDown(saveKey))TrySave(SlotPath);
            else if(Input.GetKeyDown(loadKey))TryLoad(SlotPath);
        }
        public PlayerSaveData Capture()
        {
            if(_creation.Profile==null)throw new InvalidOperationException("Player identity is not initialized.");
            var p=_creation.Profile;
            return new PlayerSaveData
            {
                schemaVersion=1,actorId=_items.ActorId,scenePath=gameObject.scene.path,savedUtc=DateTime.UtcNow.ToString("O"),
                identity=new SavedIdentity{name=p.Name,race=p.Race.raceId,subRace=p.SubRaceId,appearance=p.AppearanceParts.ToArray(),answers=p.Answers.ToArray(),
                    basics=p.StartingBasicSkills.Select(s=>s.SkillId).ToArray(),unique=p.UniqueSkill.SkillId,personality=p.Personality.Select(v=>new TraitWeight{trait=v.Key,weight=v.Value}).ToArray(),
                    resonance=p.InitialResonance.Select(v=>new SavedAmount{id=v.Key,value=v.Value}).ToArray(),seed=p.Seed,fate=p.FateDecided,mode=p.SelectionMode},
                currentAppearance=_appearance.CaptureIdentity(),position=transform.position,rotation=transform.rotation,crouching=_motor.IsCrouching,
                inventory=_items.Inventory.Entries.Values.Select(i=>new SavedItem{id=i.Definition.ItemId,count=i.Count}).OrderBy(i=>i.id).ToArray(),
                equipped=Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().Select(_items.Equipment.Get).Where(i=>i!=null).Select(i=>i.ItemId).ToArray(),
                skills=_skills.LearnedSkillIds.OrderBy(id=>id).Select(id=>_skills.GetState(id).Capture()).ToArray(),
                domains=_domains.CaptureProgress().Select(v=>new SavedAmount{id=v.Key,value=v.Value}).OrderBy(v=>v.id).ToArray(),
                gods=_gods.Definitions.Select(g=>{var s=_gods.GetState(g.GodId);return new SavedGod{id=g.GodId,resonance=s?.Resonance??0,favor=s?.Favor??0,opportunity=s?.HasOpportunity??false,claimed=s?.BlessingClaimed??false};}).ToArray(),
                conditions=_authorities.CaptureConditions(),opportunities=_authorities.CaptureOpportunities(),
                uses=_sandbox.CaptureUses().Select(u=>new SavedUse{skill=u.skill,target=u.target,context=u.context}).ToArray(),
                pickups=stations.Where(s=>s.Kind==SandboxStationKind.Pickup).Select(s=>new SavedPickup{id=s.TargetId,collected=s.Collected}).OrderBy(s=>s.id).ToArray()
            };
        }
        private ResolvedCheckpoint Validate(PlayerSaveData data,bool placement)
        {
            var result=CheckpointValidation.Resolve(data,catalog,_items,_appearance,_gods,stations,gameObject.scene.path);
            if(placement&&!_motor.CanRestorePose(data.position,data.rotation,data.crouching))throw new InvalidOperationException("The saved player position is obstructed. Checkpoint not loaded.");
            return result;
        }
        public bool TrySave(string path)
        {
            try
            {
                if(!_motor.IsGrounded)throw new InvalidOperationException("Stand on the ground before saving a checkpoint.");
                var data=Capture();Validate(data,true);
                foreach(var skill in data.skills)if(_skills.GetProgressionConfig(skill.skillId)!=catalog.Skill(skill.skillId).progression)
                    throw new InvalidOperationException("Skill progression differs from the save catalog: "+skill.skillId);
                SaveFileStore.Write(path,data);Report("Checkpoint saved. F9 restores this checkpoint.");return true;
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException)
            { Report("Save failed: "+e.Message);return false; }
        }
        public bool TryLoad(string path)
        {
            try
            {
                var data=SaveFileStore.Read(path);var resolved=Validate(data,true);
                // All input and references are checked before this commit. No gameplay reward events are replayed.
                _items.Restore(resolved.inventory,resolved.equipped);
                _skills.Restore(data.skills,id=>catalog.Skill(id).skill,id=>catalog.Skill(id).progression);
                _domains.RestoreProgress(resolved.domains);_gods.RestoreRelationships(resolved.gods);
                _authorities.Restore(data.conditions,data.opportunities);
                _creation.RestoreIdentity(resolved.profile);_appearance.TrySetIdentity(data.currentAppearance);
                _sandbox.RestoreUses(data.uses.Select(u=>(u.skill,u.target,u.context)));
                foreach(var pickup in data.pickups)stations.Single(s=>s.TargetId==pickup.id).RestoreCollected(pickup.collected);
                _motor.RestorePose(data.position,data.rotation,data.crouching);
                GenesisRuntime.Instance.Events.Publish(new ActorStateRestoredEvent(_items.ActorId));
                Report("Checkpoint loaded.");return true;
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException||e is ArgumentException||e is InvalidOperationException)
            {Report("Load failed: "+e.Message);return false;}
        }
        private void Report(string message) { LastResult=message;_sandbox.Report(message); }
    }
}
