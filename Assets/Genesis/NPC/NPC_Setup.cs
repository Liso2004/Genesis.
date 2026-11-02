using UnityEngine;
using Genesis.Skill;
using Genesis.Skill.Definitions;

namespace Genesis.NPC
{
    [DefaultExecutionOrder(-100)] // Runs before most other scripts
    public class NPC_Setup : MonoBehaviour
    {
        [Header("Perception Skill")]
        [SerializeField] private SkillDefinition perceptionDefinition;
        [SerializeField] private SkillProgressionConfig perceptionProgression;

        private SkillSystem _skillSystem;
        [SerializeField] private bool registerPerceptionOnAwake = true;

        private void Awake()
        {
            if (!registerPerceptionOnAwake) return;
            _skillSystem = GetComponent<SkillSystem>();
            if (_skillSystem == null)
            {
                Debug.LogError("NPC_Setup: No SkillSystem component found!");
                return;
            }

            if (perceptionDefinition != null && perceptionProgression != null)
            {
                _skillSystem.RegisterSkill(perceptionDefinition, perceptionProgression);
                Debug.Log($"[NPC_Setup] Registered perception skill: {perceptionDefinition.SkillId}");
            }
            else
            {
                Debug.LogWarning("NPC_Setup: Perception definition or progression not assigned.");
            }
        }
    }
}
