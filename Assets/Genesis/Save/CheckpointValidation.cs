using System;
using System.Collections.Generic;
using System.Linq;
using Genesis.Authorities;
using Genesis.Character;
using Genesis.Creation;
using Genesis.Gods;
using Genesis.Items;
using Genesis.Sandbox;
using Genesis.Skill.Definitions;
using UnityEngine;

namespace Genesis.Save
{
    /// <summary>Resolves and validates the entire checkpoint before any runtime mutation.</summary>
    public sealed class ResolvedCheckpoint
    {
        public PlayerCreationProfile profile;
        public KeyValuePair<ItemDefinition,int>[] inventory;
        public ItemDefinition[] equipped;
        public Dictionary<string,float> domains;
        public Dictionary<string,GodRelationshipState> gods;
    }
    public static class CheckpointValidation
    {
        private static void Require(bool condition,string message) { if(!condition)throw new ArgumentException(message); }
        private static bool Finite(float x) => !float.IsNaN(x)&&!float.IsInfinity(x);
        private static bool Finite(Vector3 p) => Finite(p.x)&&Finite(p.y)&&Finite(p.z);
        private static void Unique(IEnumerable<string> ids)
        { var set=new HashSet<string>();foreach(var id in ids)Require(!string.IsNullOrWhiteSpace(id)&&set.Add(id),"Missing or duplicate saved ID."); }
        private static Dictionary<string,float> Amounts(SavedAmount[] values)
        {
            Require(values!=null&&values.All(v=>v!=null),"Missing saved amounts.");Unique(values.Select(v=>v.id));
            Require(values.All(v=>Finite(v.value)&&v.value>=0),"Invalid saved amount.");return values.ToDictionary(v=>v.id,v=>v.value);
        }
        public static ResolvedCheckpoint Resolve(PlayerSaveData data,SaveCatalog catalog,ActorInventory owner,
            ModularAppearance appearance,GodResonanceSystem gods,SandboxStation[] stations,string scenePath)
        {
            Require(data!=null&&data.schemaVersion==1,"Unsupported checkpoint schema. No migration exists for this version.");
            catalog.Validate();
            Require(data.actorId==owner.ActorId,"Checkpoint belongs to another actor.");
            Require(data.scenePath==scenePath,"Checkpoint belongs to another scene; open that scene first.");
            Require(Finite(data.position)&&Finite(new Vector3(data.rotation.x,data.rotation.y,data.rotation.z))&&Finite(data.rotation.w),"Invalid saved transform.");
            float norm=data.rotation.x*data.rotation.x+data.rotation.y*data.rotation.y+data.rotation.z*data.rotation.z+data.rotation.w*data.rotation.w;
            Require(Mathf.Abs(norm-1)<.01f,"Saved rotation is not normalized.");
            var identity=data.identity;Require(identity!=null,"Missing creation identity.");
            var race=catalog.Race(identity.race);
            Require(!race.VisualPending&&race.AcceptsSubRace(identity.subRace),"Race visual or sub-race is unsupported.");
            Require(!string.IsNullOrWhiteSpace(identity.name)&&identity.name.Length<=40,"Invalid character name.");
            Require(appearance.SupportsIdentity(identity.appearance)&&appearance.SupportsIdentity(data.currentAppearance),"Saved appearance is unsupported.");
            Require(identity.personality!=null&&identity.personality.Length==Enum.GetValues(typeof(PersonalityTrait)).Length&&
                identity.personality.Select(p=>p.trait).Distinct().Count()==identity.personality.Length&&
                identity.personality.All(p=>Enum.IsDefined(typeof(PersonalityTrait),p.trait)&&Finite(p.weight)&&p.weight>=0),"Invalid personality profile.");
            Require(Enum.IsDefined(typeof(StartingSkillMode),identity.mode)&&identity.answers!=null,"Invalid creation choices.");
            Require(identity.fate?identity.answers.Length==0:identity.answers.Length==catalog.creation.questions.Length,"Saved question count is incompatible.");
            for(int i=0;i<identity.answers.Length;i++)Require(identity.answers[i]>=0&&identity.answers[i]<catalog.creation.questions[i].answers.Length,"Invalid saved answer.");
            Require(identity.basics!=null&&identity.basics.Length>=2&&identity.basics.Length<=4,"Invalid initial Basic skill count.");Unique(identity.basics);
            var basics=identity.basics.Select(id=>catalog.Skill(id).skill).ToArray();var unique=catalog.Skill(identity.unique).skill;
            Require(basics.All(s=>s.Tier==SkillTier.Basic)&&unique.Tier==SkillTier.Unique,"Initial Basic/Unique tiers do not match.");
            var initialResonance=Amounts(identity.resonance);
            var result=new ResolvedCheckpoint{profile=new PlayerCreationProfile(identity.name,race,identity.subRace,identity.appearance,identity.answers,
                identity.personality.ToDictionary(p=>p.trait,p=>p.weight),basics,unique,initialResonance,identity.seed,identity.fate,identity.mode)};
            Require(data.inventory!=null&&data.equipped!=null&&data.inventory.All(i=>i!=null),"Missing inventory/equipment state.");
            Unique(data.inventory.Select(i=>i.id));Unique(data.equipped);
            result.inventory=data.inventory.Select(i=>new KeyValuePair<ItemDefinition,int>(catalog.Item(i.id),i.count)).ToArray();
            result.equipped=data.equipped.Select(catalog.Item).ToArray();
            // Construct detached state to validate stack limits, ownership and slot conflicts without events.
            var inventory=InventoryState.Restored(owner.ActorId,null,owner.Capacity,result.inventory);
            EquipmentState.Restored(owner.ActorId,inventory,null,result.equipped);
            Require(data.skills!=null&&data.skills.All(s=>s!=null),"Missing saved skills.");Unique(data.skills.Select(s=>s.skillId));
            foreach(var state in data.skills)Require(state.IsValid(catalog.Skill(state.skillId).progression),"Invalid saved skill progress: "+state.skillId);
            Require(identity.basics.Concat(new[]{identity.unique}).All(id=>data.skills.Any(s=>s.skillId==id)),"Starting skills are missing from runtime state.");
            result.domains=Amounts(data.domains);
            var domainIds=new HashSet<string>(catalog.skills.SelectMany(s=>s.skill.Domains).Where(d=>d.domain!=null).Select(d=>d.domain.DomainId));
            Require(result.domains.Keys.Concat(initialResonance.Keys).All(domainIds.Contains),"Unknown saved domain.");
            Require(data.gods!=null&&data.gods.All(g=>g!=null),"Missing saved god state.");Unique(data.gods.Select(g=>g.id));
            Require(new HashSet<string>(data.gods.Select(g=>g.id)).SetEquals(gods.Definitions.Select(g=>g.GodId)),"God catalog differs from checkpoint.");
            result.gods=new Dictionary<string,GodRelationshipState>();
            foreach(var state in data.gods)result.gods.Add(state.id,GodRelationshipState.Restored(gods.Definitions.Single(g=>g.GodId==state.id),state.resonance,state.favor,state.opportunity,state.claimed));
            Require(data.conditions!=null&&data.opportunities!=null,"Missing authority state.");Unique(data.conditions);Unique(data.opportunities);
            var knownConditions=new HashSet<string>(catalog.authorities.SelectMany(a=>a.RequiredConditions));
            Require(data.conditions.All(knownConditions.Contains)&&data.opportunities.All(id=>catalog.authorities.Any(a=>a.AuthorityId==id)),"Unknown authority state.");
            Require(stations!=null&&stations.All(s=>s!=null),"Missing scene station reference.");Unique(stations.Select(s=>s.TargetId));
            Require(data.pickups!=null&&data.pickups.All(p=>p!=null),"Missing pickup state.");Unique(data.pickups.Select(p=>p.id));
            Require(new HashSet<string>(data.pickups.Select(p=>p.id)).SetEquals(stations.Where(s=>s.Kind==SandboxStationKind.Pickup).Select(s=>s.TargetId)),"Pickup IDs differ from the checkpoint.");
            Require(data.uses!=null&&data.uses.All(u=>u!=null)&&data.uses.Select(u=>(u.skill,u.target,u.context)).Distinct().Count()==data.uses.Length,"Invalid meaningful-use history.");
            foreach(var use in data.uses)
            {
                var station=stations.SingleOrDefault(s=>s.TargetId==use.target);
                Require(data.skills.Any(s=>s.skillId==use.skill)&&station!=null&&
                    (station.Kind==SandboxStationKind.Observation&&use.context==0||station.Kind==SandboxStationKind.Training&&use.context>=0&&use.context<4),"Unknown saved meaningful-use context.");
            }
            return result;
        }
    }
}
