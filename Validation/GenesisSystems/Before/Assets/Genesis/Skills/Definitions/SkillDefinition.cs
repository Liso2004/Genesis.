using UnityEngine;

namespace Genesis.Skill.Definitions
{
    /// <summary>
    /// Authored definition of a Genesis skill.
    ///
    /// This asset describes what a skill IS.
    /// It does not contain player-specific runtime progression.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillDefinition",
        menuName = "Genesis/Skills/Skill Definition"
    )]
    public sealed class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]

        [SerializeField]
        private string skillId;

        [SerializeField]
        private string displayName;

        [TextArea(3, 6)]
        [SerializeField]
        private string description;

        public string SkillId => skillId;

        public string DisplayName => displayName;

        public string Description => description;

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(skillId);

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!string.IsNullOrWhiteSpace(skillId))
            {
                skillId = skillId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                displayName = displayName.Trim();
            }
        }
#endif
    }
}