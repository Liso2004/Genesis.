using System;
using Genesis.Skill.Definitions;

namespace Genesis.Skill
{
    [Serializable]
    public sealed class SkillCheckpoint
    {
        public string skillId;
        public SkillMasteryStage mastery;
        public float progress;
        public int meaningfulUses;
        public bool IsValid(SkillProgressionConfig config) => config != null && !string.IsNullOrWhiteSpace(skillId) &&
            Enum.IsDefined(typeof(SkillMasteryStage), mastery) && !float.IsNaN(progress) && !float.IsInfinity(progress) &&
            progress >= 0 && meaningfulUses >= 0 && (mastery == SkillMasteryStage.Innovation || progress < config.GetRequiredProgress(mastery));
    }
}
