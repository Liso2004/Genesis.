using System;
using System.Linq;
using Genesis.Character;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Items;
using Genesis.Skill;
using Genesis.Skill.Definitions;
using UnityEngine;

namespace Genesis.Creation
{
    public readonly struct UniqueSkillAwakenedEvent : IGenesisEvent
    {
        public readonly string ActorId;
        public readonly SkillDefinition Skill;
        public UniqueSkillAwakenedEvent(string actorId,SkillDefinition skill) { ActorId=actorId; Skill=skill; }
    }

    /// <summary>Applies an initial profile once. Rejects non-empty actors instead of rewriting their history.</summary>
    [DefaultExecutionOrder(400), RequireComponent(typeof(ActorInventory),typeof(SkillSystem))]
    public sealed class PlayerCreationBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerCreationPreset testPreset;
        [SerializeField, TextArea(5,12)] private string initialSummary;
        public PlayerCreationProfile Profile { get; private set; }
        /// <summary>Checkpoint coordinator has validated the entire actor before committing.</summary>
        public void RestoreIdentity(PlayerCreationProfile profile)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            initialSummary = profile.Summary;
        }
        private void Awake()
        {
            if(testPreset==null)return;
            if(!TryApply(testPreset.Generate(),testPreset.definition.progression,out string error))
                Debug.LogError("[Genesis Creation] "+error,this);
        }
        public bool TryApply(PlayerCreationProfile profile,SkillProgressionConfig progression,out string error)
        {
            error=null;
            if(Profile!=null) { error="This actor already has an initial identity."; return false; }
            if(profile==null||progression==null){error="A profile and progression definition are required.";return false;}
            if(profile.Race.VisualPending){error="The selected race is supported as data; its visual implementation is pending.";return false;}
            var inventory=GetComponent<ActorInventory>(); var skills=GetComponent<SkillSystem>();
            var appearance=GetComponentInChildren<ModularAppearance>();
            if(inventory.Inventory==null||inventory.Inventory.Entries.Count!=0||Enum.GetValues(typeof(EquipmentSlot)).Cast<EquipmentSlot>().Any(s=>inventory.Equipment.Get(s)!=null))
            {error="Creation requires an empty actor; existing belongings will not be removed.";return false;}
            if(skills.LearnedSkillIds.Any()) {error="Creation requires a new actor; existing skill progress will not be overwritten.";return false;}
            if(appearance==null||!appearance.SupportsIdentity(profile.AppearanceParts)) {error="The actor cannot represent this appearance.";return false;}
            appearance.TrySetIdentity(profile.AppearanceParts);
            foreach(var basic in profile.StartingBasicSkills)skills.RegisterSkill(basic,progression);
            skills.RegisterSkill(profile.UniqueSkill,progression);
            Profile=profile; initialSummary=profile.Summary;
            GenesisRuntime.Instance?.Events.Publish(new UniqueSkillAwakenedEvent(inventory.ActorId,profile.UniqueSkill));
            return true;
        }
    }
}
