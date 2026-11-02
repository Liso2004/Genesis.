using System;
using System.Collections.Generic;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Skill.Definitions;
using Genesis.Skill.Events;
using UnityEngine;

namespace Genesis.Skill
{
    /// <summary>
    /// Runtime component responsible for managing one actor's
    /// skill state, meaningful use, progression, and mastery.
    /// </summary>
    public sealed class SkillSystem : MonoBehaviour
    {
        private readonly Dictionary<string, SkillDefinition> _definitions = new();
        private readonly Dictionary<string, SkillProgressionConfig> _progressionConfigs = new();
        private readonly Dictionary<string, SkillRuntimeState> _states = new();

        private IGenesisEventBus _eventBus;

        private void Awake()
        {
            if (GenesisRuntime.Instance == null)
            {
                Debug.LogError(
                    $"[Genesis Skill] {name} could not initialize because GenesisRuntime is missing."
                );

                return;
            }

            _eventBus = GenesisRuntime.Instance.Events;
        }

        public void RegisterSkill(
            SkillDefinition definition,
            SkillProgressionConfig progressionConfig)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (progressionConfig == null)
            {
                throw new ArgumentNullException(nameof(progressionConfig));
            }

            if (!definition.IsValid)
            {
                throw new ArgumentException(
                    "Skill definition must have a valid Skill ID.",
                    nameof(definition)
                );
            }

            string skillId = definition.SkillId;

            _definitions[skillId] = definition;
            _progressionConfigs[skillId] = progressionConfig;

            if (!_states.ContainsKey(skillId))
            {
                _states[skillId] = new SkillRuntimeState(skillId);
            }
        }

        public bool HasSkill(string skillId)
        {
            return !string.IsNullOrWhiteSpace(skillId)
                   && _states.ContainsKey(skillId);
        }

        public SkillRuntimeState GetState(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return null;
            }

            _states.TryGetValue(
                skillId,
                out SkillRuntimeState state
            );

            return state;
        }

        public bool RegisterMeaningfulUse(
            string skillId,
            float progressAmount)
        {
            if (string.IsNullOrWhiteSpace(skillId))
            {
                return false;
            }

            if (progressAmount <= 0f)
            {
                return false;
            }

            if (!_states.TryGetValue(
                    skillId,
                    out SkillRuntimeState state))
            {
                Debug.LogWarning(
                    $"[Genesis Skill] Cannot register use for unregistered skill: {skillId}"
                );

                return false;
            }

            if (!_progressionConfigs.TryGetValue(
                    skillId,
                    out SkillProgressionConfig progressionConfig))
            {
                Debug.LogError(
                    $"[Genesis Skill] No progression config found for: {skillId}"
                );

                return false;
            }

            state.RegisterMeaningfulUse(progressAmount);

            _eventBus?.Publish(
                new SkillMeaningfulUseEvent(
                    state.SkillId,
                    progressAmount,
                    state.MasteryStage,
                    state.MeaningfulUses
                )
            );

            ProcessProgression(
                state,
                progressionConfig
            );

            return true;
        }

        private void ProcessProgression(
            SkillRuntimeState state,
            SkillProgressionConfig progressionConfig)
        {
            while (
                state.MasteryStage !=
                SkillMasteryStage.Innovation)
            {
                float requiredProgress =
                    progressionConfig.GetRequiredProgress(
                        state.MasteryStage
                    );

                SkillMasteryStage previousStage =
                    state.MasteryStage;

                bool advanced =
                    state.AdvanceStage(requiredProgress);

                if (!advanced)
                {
                    break;
                }

                _eventBus?.Publish(
                    new SkillMasteryChangedEvent(
                        state.SkillId,
                        previousStage,
                        state.MasteryStage,
                        state.StageProgress,
                        state.MeaningfulUses
                    )
                );
            }
        }
    }
}