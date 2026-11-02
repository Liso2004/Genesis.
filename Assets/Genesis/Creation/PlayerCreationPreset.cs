using System;
using Genesis.Skill.Definitions;
using UnityEngine;

namespace Genesis.Creation
{
    [CreateAssetMenu(menuName="Genesis/Creation/Test Preset")]
    public sealed class PlayerCreationPreset : ScriptableObject
    {
        public CreationDefinition definition;
        public string playerName = "Sandbox Wanderer";
        public RaceDefinition race;
        public string subRaceId;
        public string[] appearanceParts = Array.Empty<string>();
        public int[] answers = Array.Empty<int>();
        public bool letFateDecide;
        public StartingSkillMode mode;
        public SkillDefinition[] choices = Array.Empty<SkillDefinition>();
        public int seed;

        public PlayerCreationProfile Generate()
        {
            var session=new PlayerCreationSession(definition,seed);
            session.Begin(); session.CompleteLoreIntro(); session.SelectRace(race,subRaceId);
            session.SetAppearance(playerName,appearanceParts); session.AnswerQuestions(answers,letFateDecide);
            session.DetermineStartingSkills(mode,choices); session.AwakenUnique();
            return session.ConfirmSummary();
        }
    }
}
