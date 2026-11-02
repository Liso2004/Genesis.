using System.Text;
using Genesis.Authorities;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Gods;
using Genesis.Items;
using Genesis.Skill;
using Genesis.Skill.Events;
using UnityEngine;

namespace Genesis.Sandbox
{
    /// <summary>Small test readout and scoped event trace, not a production inventory UI.</summary>
    [RequireComponent(typeof(SandboxActor), typeof(SandboxInteractor))]
    public sealed class SandboxFeedback : MonoBehaviour
    {
        [SerializeField] private bool logEvents = true;
        private SandboxActor _actor;
        private SandboxInteractor _input;
        private IGenesisEventBus _bus;
        private bool _details;
        private float _refresh;
        private string _status = "", _prompt = "", _lastEvent = "No events yet";
        private readonly StringBuilder _text = new();
        private GUIStyle _body;
        public int ObservedEvents { get; private set; }
        private void Awake() { _actor = GetComponent<SandboxActor>(); _input = GetComponent<SandboxInteractor>(); }
        private void OnEnable()
        {
            _bus = GenesisRuntime.Instance != null ? GenesisRuntime.Instance.Events : null;
            if (_bus == null) return;
            _bus.Subscribe<SkillMeaningfulUseEvent>(Use);
            _bus.Subscribe<SkillMasteryChangedEvent>(Mastery);
            _bus.Subscribe<DomainProgressChangedEvent>(Domain);
            _bus.Subscribe<GodResonanceChangedEvent>(Resonance);
            _bus.Subscribe<ItemAddedEvent>(Added);
            _bus.Subscribe<ItemEquippedEvent>(Equipped);
            _bus.Subscribe<BlessingOpportunityEvent>(Opportunity);
            _bus.Subscribe<BlessingGrantedEvent>(Blessing);
            _bus.Subscribe<AuthorityUnlockedEvent>(Authority);
        }
        private void OnDisable()
        {
            if (_bus == null) return;
            _bus.Unsubscribe<SkillMeaningfulUseEvent>(Use);
            _bus.Unsubscribe<SkillMasteryChangedEvent>(Mastery);
            _bus.Unsubscribe<DomainProgressChangedEvent>(Domain);
            _bus.Unsubscribe<GodResonanceChangedEvent>(Resonance);
            _bus.Unsubscribe<ItemAddedEvent>(Added);
            _bus.Unsubscribe<ItemEquippedEvent>(Equipped);
            _bus.Unsubscribe<BlessingOpportunityEvent>(Opportunity);
            _bus.Unsubscribe<BlessingGrantedEvent>(Blessing);
            _bus.Unsubscribe<AuthorityUnlockedEvent>(Authority);
        }
        private void Trace(string value)
        {
            _lastEvent = value; ObservedEvents++;
            if (logEvents) Debug.Log("[Genesis Sandbox] " + value, this);
        }
        private void Use(SkillMeaningfulUseEvent e) { if (e.Source == _actor.Skills) Trace("SkillMeaningfulUseEvent / " + e.SkillId); }
        private void Mastery(SkillMasteryChangedEvent e) { if (e.Source == _actor.Skills) Trace("SkillMasteryChangedEvent / " + e.SkillId + " / " + e.NewStage); }
        private void Domain(DomainProgressChangedEvent e) { if (e.Source == _actor.Skills) Trace("DomainProgressChangedEvent / " + e.DomainId); }
        private void Resonance(GodResonanceChangedEvent e) { if (e.Source == _actor.Skills) Trace("GodResonanceChangedEvent / " + e.God.DisplayName + " / " + e.Resonance.ToString("0.#")); }
        private void Added(ItemAddedEvent e) { if (e.ActorId == _actor.Items.ActorId) Trace("ItemAddedEvent / " + e.Item.DisplayName); }
        private void Equipped(ItemEquippedEvent e) { if (e.ActorId == _actor.Items.ActorId) Trace("ItemEquippedEvent / " + e.Item.DisplayName); }
        private void Opportunity(BlessingOpportunityEvent e) { if (e.Source == _actor.Skills) Trace("BlessingOpportunityEvent / " + e.God.DisplayName); }
        private void Blessing(BlessingGrantedEvent e) { if (e.Source == _actor.Skills) Trace("BlessingGrantedEvent / " + e.Blessing.DisplayName); }
        private void Authority(AuthorityUnlockedEvent e) { if (e.Source == _actor.Skills) Trace("AuthorityUnlockedEvent / " + e.Authority.DisplayName); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) _details = !_details;
            if (Time.unscaledTime < _refresh || _actor.Scenario == null) return;
            _refresh = Time.unscaledTime + 0.2f;
            _prompt = _input.Selected != null ? "E  /  " + _input.Selected.DisplayName : "Explore the nearby test stations";
            _text.Clear();
            _text.AppendLine("GENESIS / SYSTEMS TEST");
            _text.AppendLine("WASD move  |  Shift run  |  Space jump  |  C crouch");
            _text.AppendLine("E interact  |  I inventory  |  Left click practice  |  F1 details");
            _text.AppendLine("F5 save checkpoint  |  F9 load checkpoint");
            _text.AppendLine(_prompt);
            _text.AppendLine(_actor.Feedback);
            if (_details)
            {
                _text.AppendLine("\nOWNED ITEMS");
                foreach (var entry in _actor.Items.Inventory.Entries.Values)
                    _text.AppendLine(entry.Definition.DisplayName + " x" + entry.Count);
                _text.AppendLine("Main hand: " + (_actor.Items.Equipment.Get(EquipmentSlot.MainHand)?.DisplayName ?? "Empty"));
                _text.AppendLine("Off hand: " + (_actor.Items.Equipment.Get(EquipmentSlot.OffHand)?.DisplayName ?? "Empty"));
                AppendSkill(_actor.Scenario.perception.SkillId);
                AppendSkill(_actor.Scenario.swordsmanship.SkillId);
                var god = _actor.Gods.GetState(_actor.Scenario.observer.GodId);
                _text.AppendLine("Observer resonance / favor: " + (god?.Resonance ?? 0).ToString("0.#") + " / " + (god?.Favor ?? 0).ToString("0.#"));
                _text.AppendLine("Insight: " + (god?.BlessingClaimed == true ? "Active" : god?.HasOpportunity == true ? "Available at shrine" : "Not yet earned"));
                _text.AppendLine("Events: " + ObservedEvents + " / " + _lastEvent);
            }
            _status = _text.ToString();
        }
        private void AppendSkill(string id)
        {
            var state = _actor.Skills.GetState(id); var definition = _actor.Skills.GetDefinition(id);
            if (state != null) _text.AppendLine(definition.DisplayName + ": " + definition.Tier + " tier / " + state.MasteryStage + " / " + state.StageProgress.ToString("0.#") + " progress");
        }
        private void OnGUI()
        {
            _body ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUI.Box(new Rect(12, 12, Mathf.Min(620, Screen.width - 24), _details ? 450 : 124), GUIContent.none);
            GUI.Label(new Rect(24, 20, Mathf.Min(590, Screen.width - 48), _details ? 415 : 95), _status, _body);
        }
    }
}
