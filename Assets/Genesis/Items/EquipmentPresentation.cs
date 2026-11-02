using System.Collections.Generic;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Character;
using UnityEngine;

namespace Genesis.Items
{
    [DefaultExecutionOrder(60), RequireComponent(typeof(ActorInventory))]
    public sealed class EquipmentPresentation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject previousPresentationWeapon;
        [SerializeField] private ModularAppearance appearance;
        private ActorInventory _owner;
        private IGenesisEventBus _bus;
        private readonly Dictionary<EquipmentSlot, GameObject> _visuals = new();
        private void Awake()
        {
            _owner = GetComponent<ActorInventory>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (previousPresentationWeapon != null) previousPresentationWeapon.SetActive(false);
            if (appearance == null) appearance = GetComponentInChildren<ModularAppearance>();
        }
        private void OnEnable()
        {
            _bus = GenesisRuntime.Instance != null ? GenesisRuntime.Instance.Events : null;
            _bus?.Subscribe<ItemEquippedEvent>(OnEquip);
            _bus?.Subscribe<ItemUnequippedEvent>(OnUnequip);
            _bus?.Subscribe<ActorStateRestoredEvent>(OnRestored);
            if (_owner?.Equipment != null) RefreshAll();
        }
        private void OnDisable()
        {
            _bus?.Unsubscribe<ItemEquippedEvent>(OnEquip);
            _bus?.Unsubscribe<ItemUnequippedEvent>(OnUnequip);
            _bus?.Unsubscribe<ActorStateRestoredEvent>(OnRestored);
        }
        private void RefreshAll()
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                if (slot != EquipmentSlot.None) RefreshSlot(slot);
        }
        private void OnRestored(ActorStateRestoredEvent e) { if (e.ActorId == _owner.ActorId) RefreshAll(); }
        private void OnEquip(ItemEquippedEvent e)
        {
            if (e.ActorId == _owner.ActorId) RefreshSlot(e.Item.Slot);
        }
        private void OnUnequip(ItemUnequippedEvent e)
        {
            if (e.ActorId == _owner.ActorId) RefreshSlot(e.Item.Slot);
        }
        private void RefreshSlot(EquipmentSlot slot)
        {
            if (_visuals.TryGetValue(slot, out var old))
            { old.SetActive(false); Destroy(old); _visuals.Remove(slot); }
            var item = _owner.Equipment.Get(slot);
            appearance?.SetEquipmentLayer(slot.ToString(), item?.VisibleParts, item?.HiddenParts);
            if (item == null || item.VisualPrefab == null || animator == null || !animator.isHuman) return;
            if (slot != EquipmentSlot.MainHand && slot != EquipmentSlot.OffHand) return;
            var bone = animator.GetBoneTransform(item.Slot == EquipmentSlot.MainHand ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            if (bone == null) return;
            var visual = Instantiate(item.VisualPrefab, bone);
            visual.name = "Equipped " + item.DisplayName;
            // Imported Humanoid skeletons can use centimetre-scaled bones. Keep the authored
            // mesh size and grip offset relative to the actor, not the importer's bone units.
            Vector3 boneScale = bone.lossyScale;
            Vector3 actorScale = transform.lossyScale;
            Vector3 unitScale = new Vector3(actorScale.x / boneScale.x, actorScale.y / boneScale.y, actorScale.z / boneScale.z);
            visual.transform.localPosition = Vector3.Scale(item.EquippedPosition, unitScale);
            visual.transform.localEulerAngles = item.EquippedRotation;
            visual.transform.localScale = Vector3.Scale(item.EquippedScale, unitScale);
            foreach (var collider in visual.GetComponentsInChildren<Collider>()) collider.enabled = false;
            _visuals[item.Slot] = visual;
        }
    }
}
