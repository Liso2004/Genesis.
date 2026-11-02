using System.Collections.Generic;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Gods;
using Genesis.Skill;
using UnityEngine;

namespace Genesis.Authorities
{
    /// <summary>Eligibility opportunity, not activation of reality-altering powers.</summary>
    public readonly struct AuthorityUnlockedEvent : IGenesisEvent
    {
        public readonly SkillSystem Source;
        public readonly AuthorityDefinition Authority;
        public AuthorityUnlockedEvent(SkillSystem source, AuthorityDefinition authority) { Source = source; Authority = authority; }
    }
    [RequireComponent(typeof(SkillSystem))]
    public sealed class AuthoritySystem : MonoBehaviour
    {
        private readonly HashSet<string> _conditions = new();
        private readonly HashSet<string> _opportunities = new();
        private SkillSystem _skills;
        private GodResonanceSystem _gods;
        private void Awake() { _skills = GetComponent<SkillSystem>(); _gods = GetComponent<GodResonanceSystem>(); }
        // Called by a future Trial/quest outcome; ordinary shrine interaction never seeds this state.
        public void RecordCondition(string condition) { if (!string.IsNullOrWhiteSpace(condition)) _conditions.Add(condition); }
        public bool HasOpportunity(string id) => id != null && _opportunities.Contains(id);
        public string[] CaptureConditions() => new List<string>(_conditions).ToArray();
        public string[] CaptureOpportunities() => new List<string>(_opportunities).ToArray();
        public void Restore(IEnumerable<string> conditions, IEnumerable<string> opportunities)
        {
            _conditions.Clear(); _opportunities.Clear();
            foreach (var value in conditions) _conditions.Add(value);
            foreach (var value in opportunities) _opportunities.Add(value);
        }
        public List<string> GetMissingRequirements(AuthorityDefinition definition)
        {
            var missing = new List<string>();
            if (definition == null || string.IsNullOrWhiteSpace(definition.AuthorityId))
            { missing.Add("Valid Authority definition"); return missing; }
            foreach (var requirement in definition.Skills)
            {
                var state = requirement.skill != null ? _skills.GetState(requirement.skill.SkillId) : null;
                if (state == null || state.MasteryStage < requirement.mastery)
                    missing.Add((requirement.skill != null ? requirement.skill.DisplayName : "Missing skill definition") + " / " + requirement.mastery);
            }
            foreach (var condition in definition.RequiredConditions)
                if (string.IsNullOrWhiteSpace(condition) || !_conditions.Contains(condition)) missing.Add(condition ?? "Missing condition ID");
            if (definition.ResonanceGod != null && (_gods?.GetState(definition.ResonanceGod.GodId)?.Resonance ?? 0) < definition.MinimumResonance)
                missing.Add(definition.ResonanceGod.DisplayName + " resonance " + definition.MinimumResonance);
            return missing;
        }
        public bool TryUnlock(AuthorityDefinition definition, out List<string> missing)
        {
            missing = GetMissingRequirements(definition);
            if (missing.Count > 0) return false;
            if (_opportunities.Add(definition.AuthorityId))
                GenesisRuntime.Instance?.Events.Publish(new AuthorityUnlockedEvent(_skills, definition));
            return true;
        }
    }
}
