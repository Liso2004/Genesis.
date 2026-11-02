using UnityEngine;

namespace Genesis.Skill.Definitions
{
    /// <summary>
    /// Authored progression thresholds for Genesis skills.
    ///
    /// This contains tuning data only.
    /// It does not contain player-specific progression state.
    /// </summary>
    [CreateAssetMenu(
        fileName = "SkillProgressionConfig",
        menuName = "Genesis/Skills/Progression Config"
    )]
    public sealed class SkillProgressionConfig : ScriptableObject
    {
        [Header("Progress Required Per Stage")]

        [Min(0.01f)]
        [SerializeField]
        private float exposureToPractice = 20f;

        [Min(0.01f)]
        [SerializeField]
        private float practiceToCompetence = 50f;

        [Min(0.01f)]
        [SerializeField]
        private float competenceToConfidence = 100f;

        [Min(0.01f)]
        [SerializeField]
        private float confidenceToMastery = 175f;

        [Min(0.01f)]
        [SerializeField]
        private float masteryToInnovation = 300f;

        public float GetRequiredProgress(SkillMasteryStage stage)
        {
            return stage switch
            {
                SkillMasteryStage.Exposure =>
                    exposureToPractice,

                SkillMasteryStage.Practice =>
                    practiceToCompetence,

                SkillMasteryStage.Competence =>
                    competenceToConfidence,

                SkillMasteryStage.Confidence =>
                    confidenceToMastery,

                SkillMasteryStage.Mastery =>
                    masteryToInnovation,

                SkillMasteryStage.Innovation =>
                    float.PositiveInfinity,

                _ =>
                    float.PositiveInfinity
            };
        }
    }
}