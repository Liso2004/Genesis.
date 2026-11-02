using Genesis.Items;
using UnityEngine;

namespace Genesis.Sandbox
{
    public enum SandboxStationKind { Pickup, Equipment, Observation, Training, Shrine, Authority }

    public sealed class SandboxStation : MonoBehaviour
    {
        [SerializeField] private string targetId;
        [SerializeField] private string displayName;
        [SerializeField] private SandboxStationKind kind;
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private Transform focus;
        [SerializeField] private Transform pulseVisual;
        private bool _collected;
        private Quaternion _restRotation;
        private float _pulse;
        public string TargetId => targetId;
        public string DisplayName => displayName;
        public SandboxStationKind Kind => kind;
        public bool Available => !_collected && isActiveAndEnabled;
        public bool Collected => _collected;
        public void RestoreCollected(bool collected)
        {
            if (kind != SandboxStationKind.Pickup) throw new System.InvalidOperationException("Only pickups have collection state.");
            _collected = collected; gameObject.SetActive(!collected);
        }
        public Vector3 FocusPoint => focus != null ? focus.position : transform.position + Vector3.up * 0.8f;
        private void Awake() { if (pulseVisual != null) _restRotation = pulseVisual.localRotation; }
        public void Pulse() => _pulse = 0.3f;
        private void Update()
        {
            if (_pulse <= 0 || pulseVisual == null) return;
            _pulse = Mathf.Max(0, _pulse - Time.deltaTime);
            pulseVisual.localRotation = _restRotation * Quaternion.Euler(Mathf.Sin(_pulse / 0.3f * Mathf.PI) * 6f, 0, 0);
        }
        public bool TryCollect(ActorInventory owner)
        {
            if (!Available || owner == null || kind != SandboxStationKind.Pickup) return false;
            _collected = true; // Reserve before publishing, including reentrant listeners.
            if (!owner.Inventory.TryAdd(item, amount)) { _collected = false; return false; }
            gameObject.SetActive(false); return true;
        }
        public void Interact(SandboxActor actor)
        {
            if (!Available || actor == null || !actor.CanInteract(this)) return;
            switch (kind)
            {
                case SandboxStationKind.Pickup:
                    actor.Report(TryCollect(actor.Items) ? "Collected " + item.DisplayName + "." : "Cannot collect: inventory capacity or stack limit reached."); break;
                case SandboxStationKind.Equipment: actor.OpenEquipment(); break;
                case SandboxStationKind.Observation: actor.Observe(this); break;
                case SandboxStationKind.Training: actor.Report("Equip Iron Sword, then left click. Practice from different sides."); break;
                case SandboxStationKind.Shrine: actor.VisitObserver(); break;
                case SandboxStationKind.Authority: actor.CheckAuthority(); break;
            }
        }
    }
}
