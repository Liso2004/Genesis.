using System;

namespace Genesis.Skill
{
    /// <summary>
    /// Mutable runtime state for one character's relationship
    /// with a skill.
    ///
    /// Mutation is controlled by the Genesis Skill assembly.
    /// Other systems should observe the state rather than
    /// directly changing progression.
    /// </summary>
    [Serializable]
    public sealed class SkillRuntimeState
    {
        private string _skillId;
        private SkillMasteryStage _masteryStage;

        private float _stageProgress;
        private int _meaningfulUses;

        public string SkillId => _skillId;

        public SkillMasteryStage MasteryStage => _masteryStage;

        public float StageProgress => _stageProgress;

        public int MeaningfulUses => _meaningfulUses;
        public SkillCheckpoint Capture() => new SkillCheckpoint { skillId = _skillId, mastery = _masteryStage, progress = _stageProgress, meaningfulUses = _meaningfulUses };
        internal static SkillRuntimeState Restore(SkillCheckpoint state) => new SkillRuntimeState(state.skillId)
        { _masteryStage = state.mastery, _stageProgress = state.progress, _meaningfulUses = state.meaningfulUses };

        public SkillRuntimeState(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                throw new ArgumentException(
                    "Skill ID cannot be null or empty.",
                    nameof(skillId)
                );
            }

            _skillId = skillId.Trim();

            _masteryStage = SkillMasteryStage.Exposure;
            _stageProgress = 0f;
            _meaningfulUses = 0;
        }

        internal void RegisterMeaningfulUse(float progressAmount)
        {
            if (progressAmount <= 0f)
            {
                return;
            }

            _meaningfulUses++;
            _stageProgress += progressAmount;
        }

        internal bool AdvanceStage(float requiredProgress)
        {
            if (_masteryStage == SkillMasteryStage.Innovation)
            {
                return false;
            }

            if (requiredProgress <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredProgress),
                    "Required progress must be greater than zero."
                );
            }

            if (_stageProgress < requiredProgress)
            {
                return false;
            }

            // Preserve any progress earned beyond the threshold.
            _stageProgress -= requiredProgress;

            _masteryStage =
                (SkillMasteryStage)((int)_masteryStage + 1);

            return true;
        }
    }
}
