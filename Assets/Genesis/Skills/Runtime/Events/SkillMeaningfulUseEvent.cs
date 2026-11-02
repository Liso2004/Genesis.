using Genesis.Core.Events;

namespace Genesis.Skill.Events
{
    /// <summary>
    /// Published when a skill has been used in a way
    /// that genuinely contributes to progression.
    ///
    /// Observers such as Trials, the Director, Save,
    /// or debugging systems may react to this event
    /// without the Skill System knowing about them.
    /// </summary>
    public readonly struct SkillMeaningfulUseEvent : IGenesisEvent
    {
        public SkillSystem Source { get; }
        public string SkillId { get; }

        public float ProgressAmount { get; }

        public SkillMasteryStage MasteryStage { get; }

        public int TotalMeaningfulUses { get; }

        public SkillMeaningfulUseEvent(
            string skillId,
            float progressAmount,
            SkillMasteryStage masteryStage,
            int totalMeaningfulUses,
            SkillSystem source = null)
        {
            Source = source;
            SkillId = skillId;
            ProgressAmount = progressAmount;
            MasteryStage = masteryStage;
            TotalMeaningfulUses = totalMeaningfulUses;
        }
    }
}
