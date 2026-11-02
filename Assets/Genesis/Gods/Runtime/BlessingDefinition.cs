using UnityEngine;
namespace Genesis.Gods
{
    [CreateAssetMenu(menuName = "Genesis/Gods/Blessing")]
    public sealed class BlessingDefinition : ScriptableObject
    {
        [SerializeField] private string blessingId;
        [SerializeField] private string displayName;
        [TextArea, SerializeField] private string description;
        [SerializeField, Min(1)] private float observationRangeMultiplier = 1;
        public string BlessingId => blessingId;
        public string DisplayName => displayName;
        public string Description => description;
        public float ObservationRangeMultiplier => Mathf.Max(1, observationRangeMultiplier);
    }
}
