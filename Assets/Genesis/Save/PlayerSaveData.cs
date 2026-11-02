using System;
using Genesis.Creation;
using Genesis.Skill;
using UnityEngine;

namespace Genesis.Save
{
    [Serializable] public sealed class SavedAmount { public string id; public float value; }
    [Serializable] public sealed class SavedItem { public string id; public int count; }
    [Serializable] public sealed class SavedGod { public string id; public float resonance,favor; public bool opportunity,claimed; }
    [Serializable] public sealed class SavedUse { public string skill,target; public int context; }
    [Serializable] public sealed class SavedPickup { public string id; public bool collected; }
    [Serializable] public sealed class SavedIdentity
    {
        public string name,race,subRace,unique;
        public string[] appearance,basics;
        public int[] answers;
        public TraitWeight[] personality;
        public SavedAmount[] resonance;
        public int seed;
        public bool fate;
        public StartingSkillMode mode;
    }
    /// <summary>Versioned data only: no Unity object references or mutable shared assets.</summary>
    [Serializable] public sealed class PlayerSaveData
    {
        public int schemaVersion;
        public string actorId,scenePath,savedUtc;
        public SavedIdentity identity;
        public string[] currentAppearance;
        public Vector3 position;
        public Quaternion rotation;
        public bool crouching;
        public SavedItem[] inventory;
        public string[] equipped;
        public SkillCheckpoint[] skills;
        public SavedAmount[] domains;
        public SavedGod[] gods;
        public string[] conditions,opportunities;
        public SavedUse[] uses;
        public SavedPickup[] pickups;
    }
}
