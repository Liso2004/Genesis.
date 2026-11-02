using System.Collections.Generic;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Skill.Events;
using UnityEngine;

namespace Genesis.Skill
{
    public readonly struct DomainProgressChangedEvent : IGenesisEvent
    {
        public readonly SkillSystem Source;
        public readonly string DomainId;
        public readonly float Amount, Total;
        public DomainProgressChangedEvent(SkillSystem source, string domainId, float amount, float total)
        { Source = source; DomainId = domainId; Amount = amount; Total = total; }
    }

    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(SkillSystem))]
    public sealed class SkillDomainSystem : MonoBehaviour
    {
        private readonly Dictionary<string, float> _progress = new();
        private SkillSystem _skills;
        private IGenesisEventBus _bus;
        private void Awake() => _skills = GetComponent<SkillSystem>();
        private void OnEnable()
        {
            _bus = GenesisRuntime.Instance != null ? GenesisRuntime.Instance.Events : null;
            _bus?.Subscribe<SkillMeaningfulUseEvent>(OnUse);
        }
        private void OnDisable() => _bus?.Unsubscribe<SkillMeaningfulUseEvent>(OnUse);
        public float GetProgress(string id) => id != null && _progress.TryGetValue(id, out var value) ? value : 0;
        public Dictionary<string,float> CaptureProgress() => new(_progress);
        public void RestoreProgress(IReadOnlyDictionary<string,float> values)
        {
            foreach (var pair in values) if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 0 || float.IsNaN(pair.Value) || float.IsInfinity(pair.Value))
                throw new System.ArgumentException("Invalid saved domain progress.");
            _progress.Clear(); foreach (var pair in values) _progress.Add(pair.Key,pair.Value);
        }
        private void OnUse(SkillMeaningfulUseEvent use)
        {
            if (use.Source != _skills) return;
            var definition = _skills.GetDefinition(use.SkillId);
            if (definition == null) return;
            foreach (var contribution in definition.Domains)
            {
                if (contribution.domain == null || string.IsNullOrWhiteSpace(contribution.domain.DomainId)) continue;
                float amount = use.ProgressAmount * contribution.weight;
                if (amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) continue;
                string id = contribution.domain.DomainId;
                float total = GetProgress(id) + amount;
                if (float.IsInfinity(total)) continue;
                _progress[id] = total;
                _bus?.Publish(new DomainProgressChangedEvent(_skills, id, amount, total));
            }
        }
    }
}
