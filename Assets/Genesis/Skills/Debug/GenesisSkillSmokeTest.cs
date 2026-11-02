using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Skill.Definitions;
using Genesis.Skill.Events;
using UnityEngine;

namespace Genesis.Skill.Debugging
{
    /// <summary>
    /// Integration test for the Genesis Skill system.
    /// Uses real authored SkillDefinition and
    /// SkillProgressionConfig assets.
    /// </summary>
    public sealed class GenesisSkillSmokeTest : MonoBehaviour
    {
        [Header("Test Content")]

        [SerializeField]
        private SkillDefinition perceptionDefinition;

        [SerializeField]
        private SkillProgressionConfig perceptionProgression;

        private SkillSystem _skillSystem;

        private void Start()
        {
            if (GenesisRuntime.Instance == null)
            {
                Debug.LogError(
                    "[Genesis Skill Test] GenesisRuntime is not initialized."
                );

                return;
            }

            if (perceptionDefinition == null)
            {
                Debug.LogError(
                    "[Genesis Skill Test] Perception definition is missing."
                );

                return;
            }

            if (perceptionProgression == null)
            {
                Debug.LogError(
                    "[Genesis Skill Test] Perception progression config is missing."
                );

                return;
            }

            IGenesisEventBus eventBus =
                GenesisRuntime.Instance.Events;

            eventBus.Subscribe<SkillMeaningfulUseEvent>(
                HandleMeaningfulUse
            );

            eventBus.Subscribe<SkillMasteryChangedEvent>(
                HandleMasteryChanged
            );

            // SkillSystem is now a component owned by this test actor.
            _skillSystem =
                gameObject.AddComponent<SkillSystem>();

            _skillSystem.RegisterSkill(
                perceptionDefinition,
                perceptionProgression
            );

            Debug.Log(
                "[Genesis Skill Test] Perception registered."
            );

            RunProgressionTest();
        }

        private void RunProgressionTest()
        {
            Debug.Log(
                "[Genesis Skill Test] Beginning Perception progression test."
            );

            // 5 / 20
            _skillSystem.RegisterMeaningfulUse(
                "perception",
                5f
            );

            // 15 / 20
            _skillSystem.RegisterMeaningfulUse(
                "perception",
                10f
            );

            // 25 / 20
            // Expected:
            // Exposure → Practice
            // 5 progress remains.
            _skillSystem.RegisterMeaningfulUse(
                "perception",
                10f
            );

            SkillRuntimeState state =
                _skillSystem.GetState("perception");

            if (state == null)
            {
                Debug.LogError(
                    "[Genesis Skill Test] Runtime state was not created."
                );

                return;
            }

            Debug.Log(
                $"[Genesis Skill Test] FINAL STATE | " +
                $"Skill: {state.SkillId} | " +
                $"Stage: {state.MasteryStage} | " +
                $"Progress: {state.StageProgress} | " +
                $"Uses: {state.MeaningfulUses}"
            );
        }

        private void HandleMeaningfulUse(
            SkillMeaningfulUseEvent eventData)
        {
            Debug.Log(
                $"[Genesis Skill Event] Meaningful Use | " +
                $"Skill: {eventData.SkillId} | " +
                $"Progress: +{eventData.ProgressAmount} | " +
                $"Stage: {eventData.MasteryStage} | " +
                $"Uses: {eventData.TotalMeaningfulUses}"
            );
        }

        private void HandleMasteryChanged(
            SkillMasteryChangedEvent eventData)
        {
            Debug.Log(
                $"[Genesis Skill Event] MASTERY CHANGED | " +
                $"Skill: {eventData.SkillId} | " +
                $"{eventData.PreviousStage} → " +
                $"{eventData.NewStage} | " +
                $"Remaining Progress: {eventData.RemainingProgress}"
            );
        }

        private void OnDestroy()
        {
            if (GenesisRuntime.Instance == null)
            {
                return;
            }

            IGenesisEventBus eventBus =
                GenesisRuntime.Instance.Events;

            eventBus.Unsubscribe<SkillMeaningfulUseEvent>(
                HandleMeaningfulUse
            );

            eventBus.Unsubscribe<SkillMasteryChangedEvent>(
                HandleMasteryChanged
            );
        }
    }
}