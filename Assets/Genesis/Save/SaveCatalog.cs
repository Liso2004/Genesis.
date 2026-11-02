using System;
using System.Linq;
using Genesis.Creation;
using Genesis.Items;
using Genesis.Skill.Definitions;
using Genesis.Authorities;
using UnityEngine;

namespace Genesis.Save
{
    [Serializable] public sealed class SavedSkillBinding { public SkillDefinition skill; public SkillProgressionConfig progression; }
    /// <summary>Stable authored IDs resolve to assets in a player build; no AssetDatabase at runtime.</summary>
    [CreateAssetMenu(menuName="Genesis/Save/Catalog")]
    public sealed class SaveCatalog : ScriptableObject
    {
        public CreationDefinition creation;
        public ItemDefinition[] items = Array.Empty<ItemDefinition>();
        public SavedSkillBinding[] skills = Array.Empty<SavedSkillBinding>();
        public AuthorityDefinition[] authorities = Array.Empty<AuthorityDefinition>();
        public ItemDefinition Item(string id) => items.SingleOrDefault(i=>i.ItemId==id) ?? throw new ArgumentException("Unknown item ID: "+id);
        public SavedSkillBinding Skill(string id) => skills.SingleOrDefault(i=>i.skill.SkillId==id) ?? throw new ArgumentException("Unknown skill ID: "+id);
        public RaceDefinition Race(string id) => creation.races.SingleOrDefault(r=>r.raceId==id) ?? throw new ArgumentException("Unknown race ID: "+id);
        public void Validate()
        {
            if (creation==null || items.Any(i=>i==null||!i.IsValid) || items.Select(i=>i.ItemId).Distinct().Count()!=items.Length ||
                skills.Any(s=>s?.skill==null||!s.skill.IsValid||s.progression==null) || skills.Select(s=>s.skill.SkillId).Distinct().Count()!=skills.Length ||
                creation.races.Any(r=>r==null||string.IsNullOrWhiteSpace(r.raceId)) || creation.races.Select(r=>r.raceId).Distinct().Count()!=creation.races.Length ||
                authorities.Any(a=>a==null||string.IsNullOrWhiteSpace(a.AuthorityId)) || authorities.Select(a=>a.AuthorityId).Distinct().Count()!=authorities.Length)
                throw new ArgumentException("Save catalog contains missing or duplicate definitions.");
        }
    }
}
