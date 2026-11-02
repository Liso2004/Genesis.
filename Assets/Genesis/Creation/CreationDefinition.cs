using System;
using Genesis.Skill.Definitions;
using UnityEngine;

namespace Genesis.Creation
{
    public enum PersonalityTrait { Observation, Patience, Leadership, Compassion, RiskTolerance, Creativity, Discipline, Curiosity, Ambition }
    public enum StartingSkillMode { Guided, Choice, Random }
    public enum CreationStage { NewGame, LoreIntro, Race, Appearance, PersonalityQuestions, StartingSkills, UniqueAwakening, CharacterSummary, EnterAethel }
    [Serializable] public struct TraitWeight { public PersonalityTrait trait; public float weight; }
    [Serializable] public sealed class PersonalityAnswer { public string text; public TraitWeight[] weights = Array.Empty<TraitWeight>(); }
    [Serializable] public sealed class PersonalityQuestion { public string id; [TextArea] public string text; public PersonalityAnswer[] answers = Array.Empty<PersonalityAnswer>(); }
    [Serializable] public struct RaceAffinity { public RaceDefinition race; public string subRaceId; [Min(.01f)] public float multiplier; }
    [Serializable] public struct ResonanceAffinity { public string domainId; public float weight; }
    [Serializable] public sealed class StartingSkillCandidate
    {
        public SkillDefinition skill;
        [Min(.01f)] public float baseWeight = 1;
        public TraitWeight[] personality = Array.Empty<TraitWeight>();
        public RaceAffinity[] races = Array.Empty<RaceAffinity>();
        public ResonanceAffinity[] resonance = Array.Empty<ResonanceAffinity>();
    }
    [CreateAssetMenu(menuName="Genesis/Creation/Definition")]
    public sealed class CreationDefinition : ScriptableObject
    {
        public RaceDefinition[] races = Array.Empty<RaceDefinition>();
        public PersonalityQuestion[] questions = Array.Empty<PersonalityQuestion>();
        public StartingSkillCandidate[] basicSkills = Array.Empty<StartingSkillCandidate>();
        public StartingSkillCandidate[] uniqueSkills = Array.Empty<StartingSkillCandidate>();
        [Range(2,4)] public int startingSkillCount = 3;
        [Range(4,8)] public int choiceCandidateCount = 5;
        public SkillProgressionConfig progression;
        [TextArea(3,8)] public string loreIntroduction;
    }
}
