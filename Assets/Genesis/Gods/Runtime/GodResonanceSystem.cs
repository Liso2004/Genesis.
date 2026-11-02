using System.Collections.Generic;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Skill;
using UnityEngine;

namespace Genesis.Gods
{
    public readonly struct GodResonanceChangedEvent : IGenesisEvent
    {
        public readonly SkillSystem Source;
        public readonly GodDefinition God;
        public readonly float Resonance, Favor;
        public GodResonanceChangedEvent(SkillSystem source, GodDefinition god, float resonance, float favor)
        { Source = source; God = god; Resonance = resonance; Favor = favor; }
    }
    public readonly struct BlessingOpportunityEvent : IGenesisEvent
    {
        public readonly SkillSystem Source;
        public readonly GodDefinition God;
        public BlessingOpportunityEvent(SkillSystem source, GodDefinition god) { Source = source; God = god; }
    }
    public readonly struct BlessingGrantedEvent : IGenesisEvent
    {
        public readonly SkillSystem Source;
        public readonly BlessingDefinition Blessing;
        public BlessingGrantedEvent(SkillSystem source, BlessingDefinition blessing) { Source = source; Blessing = blessing; }
    }
    public sealed class GodRelationshipState
    {
        public float Resonance { get; internal set; }
        public float Favor { get; internal set; }
        public bool HasOpportunity { get; internal set; }
        public bool BlessingClaimed { get; internal set; }
        public static GodRelationshipState Restored(GodDefinition god, float resonance, float favor, bool opportunity, bool claimed)
        {
            if (god == null || float.IsNaN(resonance) || float.IsInfinity(resonance) || resonance < 0 || resonance > god.MaximumResonance ||
                float.IsNaN(favor) || float.IsInfinity(favor) || favor < 0 || favor > god.MaximumFavor ||
                claimed && !opportunity || opportunity && (god.Blessing == null || favor < god.BlessingThreshold))
                throw new System.ArgumentException("Invalid saved god relationship.");
            return new GodRelationshipState { Resonance=resonance, Favor=favor, HasOpportunity=opportunity, BlessingClaimed=claimed };
        }
    }

    [DefaultExecutionOrder(200), RequireComponent(typeof(SkillDomainSystem))]
    public sealed class GodResonanceSystem : MonoBehaviour
    {
        [SerializeField] private GodDefinition[] gods = System.Array.Empty<GodDefinition>();
        private readonly Dictionary<string, GodRelationshipState> _states = new();
        private SkillSystem _skills;
        private IGenesisEventBus _bus;
        private void Awake() => _skills = GetComponent<SkillSystem>();
        private void OnEnable()
        {
            _bus = GenesisRuntime.Instance != null ? GenesisRuntime.Instance.Events : null;
            _bus?.Subscribe<DomainProgressChangedEvent>(OnDomainProgress);
        }
        private void OnDisable() => _bus?.Unsubscribe<DomainProgressChangedEvent>(OnDomainProgress);
        public GodRelationshipState GetState(string id) => id != null && _states.TryGetValue(id, out var state) ? state : null;
        public IReadOnlyList<GodDefinition> Definitions => gods;
        public void RestoreRelationships(IReadOnlyDictionary<string,GodRelationshipState> values)
        {
            _states.Clear();
            foreach (var pair in values) _states.Add(pair.Key,new GodRelationshipState { Resonance=pair.Value.Resonance, Favor=pair.Value.Favor,
                HasOpportunity=pair.Value.HasOpportunity, BlessingClaimed=pair.Value.BlessingClaimed });
        }
        private GodRelationshipState State(string id)
        {
            if (!_states.TryGetValue(id, out var state)) _states.Add(id, state = new GodRelationshipState());
            return state;
        }
        private void OnDomainProgress(DomainProgressChangedEvent progress)
        {
            if (progress.Source != _skills) return;
            foreach (var god in gods)
            {
                if (god == null || string.IsNullOrWhiteSpace(god.GodId)) continue;
                float contribution = 0;
                foreach (var affinity in god.Affinities)
                    if (affinity.domain != null && affinity.domain.DomainId == progress.DomainId)
                        contribution += progress.Amount * Mathf.Max(0, affinity.weight);
                if (contribution <= 0 || float.IsNaN(contribution) || float.IsInfinity(contribution)) continue;
                var state = State(god.GodId);
                float accepted = Mathf.Min(contribution, god.MaximumResonance - state.Resonance);
                if (accepted <= 0) continue;
                state.Resonance += accepted;
                state.Favor = Mathf.Min(god.MaximumFavor, state.Favor + accepted * god.FavorPerResonance);
                _bus?.Publish(new GodResonanceChangedEvent(_skills, god, state.Resonance, state.Favor));
                if (!state.HasOpportunity && god.Blessing != null && state.Favor >= god.BlessingThreshold)
                {
                    state.HasOpportunity = true;
                    _bus?.Publish(new BlessingOpportunityEvent(_skills, god));
                }
            }
        }
        public bool TryClaimBlessing(GodDefinition god)
        {
            if (god == null || god.Blessing == null || System.Array.IndexOf(gods, god) < 0) return false;
            var state = GetState(god.GodId);
            if (state == null || !state.HasOpportunity || state.BlessingClaimed) return false;
            state.BlessingClaimed = true;
            _bus?.Publish(new BlessingGrantedEvent(_skills, god.Blessing));
            return true;
        }
        public float ObservationRangeMultiplier
        {
            get
            {
                float result = 1;
                foreach (var god in gods)
                    if (god != null && god.Blessing != null && GetState(god.GodId)?.BlessingClaimed == true)
                        result = Mathf.Max(result, god.Blessing.ObservationRangeMultiplier);
                return result;
            }
        }
    }
}
