using Genesis.Gods;
using Genesis.Skill;
using Genesis.Skill.Definitions;
using UnityEngine;

namespace Genesis.Authorities
{
    [System.Serializable]
    public struct AuthoritySkillRequirement
    {
        public SkillDefinition skill;
        public SkillMasteryStage mastery;
    }
    [CreateAssetMenu(menuName = "Genesis/Authorities/Authority")]
    public sealed class AuthorityDefinition : ScriptableObject
    {
        [SerializeField] private string authorityId;
        [SerializeField] private string displayName;
        [SerializeField] private AuthoritySkillRequirement[] skills = System.Array.Empty<AuthoritySkillRequirement>();
        [SerializeField] private string[] requiredConditions = System.Array.Empty<string>();
        [SerializeField] private GodDefinition resonanceGod;
        [SerializeField, Min(0)] private float minimumResonance;
        public string AuthorityId => authorityId;
        public string DisplayName => displayName;
        public System.Collections.Generic.IReadOnlyList<AuthoritySkillRequirement> Skills => skills;
        public System.Collections.Generic.IReadOnlyList<string> RequiredConditions => requiredConditions;
        public GodDefinition ResonanceGod => resonanceGod;
        public float MinimumResonance => minimumResonance;
    }
}
