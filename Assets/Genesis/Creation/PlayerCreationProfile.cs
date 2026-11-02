using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Genesis.Skill.Definitions;

namespace Genesis.Creation
{
    /// <summary>An immutable starting snapshot. SkillRuntimeState and actor systems own all later progress.</summary>
    public sealed class PlayerCreationProfile
    {
        public string Name { get; }
        public RaceDefinition Race { get; }
        public string SubRaceId { get; }
        public IReadOnlyList<string> AppearanceParts { get; }
        public IReadOnlyList<int> Answers { get; }
        public IReadOnlyDictionary<PersonalityTrait,float> Personality { get; }
        public IReadOnlyList<SkillDefinition> StartingBasicSkills { get; }
        public SkillDefinition UniqueSkill { get; }
        public IReadOnlyDictionary<string,float> InitialResonance { get; }
        public int Seed { get; }
        public bool FateDecided { get; }
        public StartingSkillMode SelectionMode { get; }
        public string InitialAllegiance => "None";
        public string InitialReputation => "Unknown";
        public string Summary => Name + "\n" + Race.displayName + (string.IsNullOrEmpty(SubRaceId) ? "" : " / " + SubRaceId) +
            "\nStarting skills: " + string.Join(", ",StartingBasicSkills.Select(s=>s.DisplayName)) +
            "\nUnique: " + UniqueSkill.DisplayName + "\nEquipment: None\nAllegiance: None\nReputation: Unknown";

        public PlayerCreationProfile(string name, RaceDefinition race, string subRace, string[] appearance, int[] answers,
            Dictionary<PersonalityTrait,float> personality, SkillDefinition[] basics, SkillDefinition unique,
            Dictionary<string,float> resonance, int seed, bool fate, StartingSkillMode mode)
        {
            Name=name; Race=race; SubRaceId=subRace;
            AppearanceParts=System.Array.AsReadOnly((string[])appearance.Clone()); Answers=System.Array.AsReadOnly((int[])answers.Clone());
            Personality=new ReadOnlyDictionary<PersonalityTrait,float>(new Dictionary<PersonalityTrait,float>(personality));
            StartingBasicSkills=System.Array.AsReadOnly((SkillDefinition[])basics.Clone()); UniqueSkill=unique;
            InitialResonance=new ReadOnlyDictionary<string,float>(new Dictionary<string,float>(resonance));
            Seed=seed; FateDecided=fate; SelectionMode=mode;
        }
    }
}
