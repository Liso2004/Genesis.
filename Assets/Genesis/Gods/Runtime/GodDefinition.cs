using Genesis.Skill.Definitions;
using UnityEngine;

namespace Genesis.Gods
{
    [CreateAssetMenu(menuName = "Genesis/Gods/God")]
    public sealed class GodDefinition : ScriptableObject
    {
        [SerializeField] private string godId;
        [SerializeField] private string displayName;
        [TextArea, SerializeField] private string description;
        [SerializeField] private Color presentationColor = Color.cyan;
        [SerializeField] private DomainContribution[] affinities = System.Array.Empty<DomainContribution>();
        [SerializeField, Min(0)] private float favorPerResonance = 1;
        [SerializeField, Min(1)] private float maximumResonance = 100;
        [SerializeField, Min(1)] private float maximumFavor = 1000;
        [SerializeField, Min(1)] private float blessingFavorThreshold = 30;
        [SerializeField] private BlessingDefinition blessing;
        public string GodId => godId;
        public string DisplayName => displayName;
        public string Description => description;
        public Color PresentationColor => presentationColor;
        public System.Collections.Generic.IReadOnlyList<DomainContribution> Affinities => affinities;
        public float FavorPerResonance => Mathf.Max(0, favorPerResonance);
        public float MaximumResonance => Mathf.Max(1, maximumResonance);
        public float MaximumFavor => Mathf.Max(1, maximumFavor);
        public float BlessingThreshold => Mathf.Max(1, blessingFavorThreshold);
        public BlessingDefinition Blessing => blessing;
    }
}
