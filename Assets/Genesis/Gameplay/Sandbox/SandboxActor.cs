using Genesis.Authorities;
using Genesis.Character;
using Genesis.Gods;
using Genesis.Items;
using Genesis.Skill;
using UnityEngine;

namespace Genesis.Sandbox
{
    [DefaultExecutionOrder(500)]
    [RequireComponent(typeof(ActorInventory), typeof(GodResonanceSystem), typeof(AuthoritySystem))]
    public sealed class SandboxActor : MonoBehaviour
    {
        [SerializeField] private SandboxScenarioDefinition scenario;
        [SerializeField] private InventoryPanel inventoryPanel;
        [SerializeField] private bool registerScenarioSkills = true;
        private readonly MeaningfulUseTracker _uses = new();
        private float _nextPractice;
        private SandboxInteractor _interaction;
        public SandboxScenarioDefinition Scenario => scenario;
        public SkillSystem Skills { get; private set; }
        public ActorInventory Items { get; private set; }
        public GodResonanceSystem Gods { get; private set; }
        public AuthoritySystem Authorities { get; private set; }
        public PlayerController Motor { get; private set; }
        public string Feedback { get; private set; } = "E interacts. Left click practices at the dummy. C toggles crouch.";
        private void Awake()
        {
            Skills = GetComponent<SkillSystem>(); Items = GetComponent<ActorInventory>();
            Gods = GetComponent<GodResonanceSystem>(); Authorities = GetComponent<AuthoritySystem>();
            Motor = GetComponent<PlayerController>();
            _interaction = GetComponent<SandboxInteractor>();
        }
        private void Start()
        {
            if (scenario == null) { enabled = false; return; }
            if (!registerScenarioSkills) return;
            if (!Skills.HasSkill(scenario.perception.SkillId)) Skills.RegisterSkill(scenario.perception, scenario.progression);
            if (!Skills.HasSkill(scenario.swordsmanship.SkillId)) Skills.RegisterSkill(scenario.swordsmanship, scenario.progression);
        }
        public void Report(string message) => Feedback = message;
        public (string skill, string target, int context)[] CaptureUses() => _uses.Capture();
        public void RestoreUses(System.Collections.Generic.IEnumerable<(string skill,string target,int context)> values)
        { _uses.Restore(values); _nextPractice = 0; }
        public bool CanInteract(SandboxStation target, bool training = false) => _interaction != null && _interaction.CanReach(target, training);
        public bool Observe(SandboxStation target)
        {
            if (scenario == null || target == null || target.Kind != SandboxStationKind.Observation ||
                !CanInteract(target) || !Skills.HasSkill(scenario.perception.SkillId)) return false;
            if (!_uses.TryRecord(scenario.perception.SkillId, target.TargetId, 0))
            { Report("Already observed. Discover another target for meaningful progress."); return false; }
            Skills.RegisterMeaningfulUse(scenario.perception.SkillId, scenario.meaningfulProgress);
            Report("Observed " + target.DisplayName + ". Perception and aligned domains progressed.");
            target.Pulse(); return true;
        }
        public bool Practice(SandboxStation target, float now)
        {
            if (scenario == null || target == null || target.Kind != SandboxStationKind.Training ||
                !CanInteract(target, true) || now < _nextPractice) return false;
            if (Items.Equipment.Get(EquipmentSlot.MainHand) != scenario.ironSword)
            { Report("Acquire Iron Sword, then equip it at the equipment stand."); return false; }
            if (Motor != null && (!Motor.IsGrounded || Motor.IsCrouching))
            { Report("Stand on the ground to practice."); return false; }
            if (!Skills.HasSkill(scenario.swordsmanship.SkillId)) return false;
            _nextPractice = now + scenario.practiceCooldown;
            Vector3 approach = transform.position - target.transform.position;
            int quadrant = Mathf.FloorToInt(Mathf.Repeat(Mathf.Atan2(approach.x, approach.z) * Mathf.Rad2Deg + 45, 360) / 90);
            target.Pulse();
            if (!_uses.TryRecord(scenario.swordsmanship.SkillId, target.TargetId, quadrant))
            { Report("Practice contact. Change your approach to earn new progress; repetition alone earns none."); return false; }
            Skills.RegisterMeaningfulUse(scenario.swordsmanship.SkillId, scenario.meaningfulProgress);
            Report("Varied sword practice: Swordsmanship and Warfare progressed."); return true;
        }
        public void EquipOwnedItems()
        {
            bool sword = Items.Equipment.TryEquip(scenario.ironSword.ItemId);
            bool shield = Items.Equipment.TryEquip(scenario.blackwoodShield.ItemId);
            Report(sword || shield ? "Equipped owned items. Left click near the dummy to practice." : "Collect the sword or shield beside this stand first.");
        }
        public void OpenEquipment()
        {
            if (inventoryPanel != null) inventoryPanel.Open();
            else Report("Press I to inspect and equip owned items.");
        }
        public void VisitObserver()
        {
            if (Gods.TryClaimBlessing(scenario.observer))
                Report("Insight accepted. Observation range has increased.");
            else if (Gods.GetState(scenario.observer.GodId)?.BlessingClaimed == true)
                Report("Insight is already active. The shrine grants no repeat rewards.");
            else Report("The Observer awaits meaningful discovery. Observe distinct targets to earn favor; visiting grants none.");
        }
        public void CheckAuthority()
        {
            bool unlocked = Authorities.TryUnlock(scenario.sovereignServant, out var missing);
            Report(unlocked ? "Sovereign Servant opportunity unlocked. No reality-altering power is activated in this prototype."
                : "Requirements missing: " + string.Join(", ", missing));
        }
    }
}
