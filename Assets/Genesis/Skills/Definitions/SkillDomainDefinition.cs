using UnityEngine;

namespace Genesis.Skill.Definitions
{
    [CreateAssetMenu(menuName = "Genesis/Skills/Domain")]
    public sealed class SkillDomainDefinition : ScriptableObject
    {
        [SerializeField] private string domainId;
        [SerializeField] private string displayName;
        [SerializeField] private Color presentationColor = Color.white;
        public string DomainId => domainId;
        public string DisplayName => displayName;
        public Color PresentationColor => presentationColor;
    }

    [System.Serializable]
    public struct DomainContribution
    {
        public SkillDomainDefinition domain;
        [Min(0)] public float weight;
    }
}
