using Genesis.Core.Events;

namespace Genesis.Skill.Events
{
    /// <summary>
    /// Published when a character advances from one
    /// skill mastery stage to another.
    /// </summary>
    public readonly struct SkillMasteryChangedEvent : IGenesisEvent
    {
        public string SkillId { get; }

        public SkillMasteryStage PreviousStage { get; }

        public SkillMasteryStage NewStage { get; }

        public float RemainingProgress { get; }

        public int TotalMeaningfulUses { get; }

        public SkillMasteryChangedEvent(
            string skillId,
            SkillMasteryStage previousStage,
            SkillMasteryStage newStage,
            float remainingProgress,
            int totalMeaningfulUses)
        {
            SkillId = skillId;
            PreviousStage = previousStage;
            NewStage = newStage;
            RemainingProgress = remainingProgress;
            TotalMeaningfulUses = totalMeaningfulUses;
        }
    }
}