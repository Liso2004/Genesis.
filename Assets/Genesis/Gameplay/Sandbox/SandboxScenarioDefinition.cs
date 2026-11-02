using Genesis.Authorities;
using Genesis.Gods;
using Genesis.Items;
using Genesis.Skill.Definitions;
using UnityEngine;

namespace Genesis.Sandbox
{
    [CreateAssetMenu(menuName = "Genesis/Sandbox/Scenario")]
    public sealed class SandboxScenarioDefinition : ScriptableObject
    {
        public SkillDefinition perception;
        public SkillDefinition swordsmanship;
        public SkillProgressionConfig progression;
        public ItemDefinition ironSword;
        public ItemDefinition blackwoodShield;
        public GodDefinition observer;
        public AuthorityDefinition sovereignServant;
        [Min(0.1f)] public float meaningfulProgress = 10;
        [Min(0.1f)] public float interactionDistance = 2.8f;
        [Min(0.1f)] public float observationDistance = 8;
        [Range(1, 180)] public float observationFieldOfView = 100;
        [Min(0.1f)] public float practiceCooldown = 0.7f;
        [Min(0.1f)] public float practiceDistance = 2.8f;
    }
}
