using UnityEngine;
using Genesis.Skill;

public class DebugSkillBoost : MonoBehaviour
{
    private SkillSystem _skillSystem;

    void Start()
    {
        _skillSystem = GetComponent<SkillSystem>();
        if (_skillSystem == null)
        {
            Debug.LogError("DebugSkillBoost: No SkillSystem found!");
            return;
        }

        // Boost perception to Mastery (stage 3) - 100 uses at 50 progress each
        for (int i = 0; i < 100; i++)
        {
            _skillSystem.RegisterMeaningfulUse("perception", 50f);
        }

        var state = _skillSystem.GetState("perception");
        if (state != null)
        {
            Debug.Log($"Perception boosted to stage: {state.MasteryStage} | Progress: {state.StageProgress} | Uses: {state.MeaningfulUses}");
        }
        else
        {
            Debug.LogError("Perception state is null! Check Skill ID and definition.");
        }
    }
}