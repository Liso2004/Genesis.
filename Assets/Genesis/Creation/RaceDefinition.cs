using System;
using UnityEngine;

namespace Genesis.Creation
{
    [Serializable] public sealed class SubRaceDefinition
    {
        public string id, displayName;
        public string[] traits = Array.Empty<string>();
    }

    [CreateAssetMenu(menuName="Genesis/Creation/Race")]
    public sealed class RaceDefinition : ScriptableObject
    {
        public string raceId, displayName;
        [TextArea] public string description;
        public string[] traits = Array.Empty<string>();
        public SubRaceDefinition[] subRaces = Array.Empty<SubRaceDefinition>();
        public GameObject compatibleVisualPrefab;
        public bool VisualPending => compatibleVisualPrefab == null;
        public bool AcceptsSubRace(string id)
        {
            if (subRaces.Length == 0) return string.IsNullOrEmpty(id);
            return Array.Exists(subRaces, sub => sub.id == id);
        }
    }
}
