using UnityEngine;

namespace Genesis.Items
{
    public enum ItemCategory { Weapon, Shield, KeyItem, Armor, Accessory, Currency, Consumable, Material, QuestItem, Artifact }
    public enum EquipmentSlot { None, MainHand, OffHand, Head, Chest, Hands, Legs, Cape, Accessory }

    [CreateAssetMenu(menuName = "Genesis/Items/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [TextArea, SerializeField] private string description;
        [SerializeField] private ItemCategory category;
        [SerializeField] private EquipmentSlot equipmentSlot;
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private Sprite icon;
        [SerializeField] private Vector3 equippedPosition;
        [SerializeField] private Vector3 equippedRotation;
        [SerializeField] private Vector3 equippedScale = Vector3.one;
        [Header("Modular appearance (authored part IDs)")]
        [SerializeField] private string[] visibleParts = System.Array.Empty<string>();
        [SerializeField] private string[] hiddenParts = System.Array.Empty<string>();
        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemCategory Category => category;
        public EquipmentSlot Slot => equipmentSlot;
        public int MaxStack => Mathf.Max(1, maxStack);
        public GameObject VisualPrefab => visualPrefab;
        public Sprite Icon => icon;
        public Vector3 EquippedPosition => equippedPosition;
        public Vector3 EquippedRotation => equippedRotation;
        public Vector3 EquippedScale => equippedScale;
        public System.Collections.Generic.IReadOnlyList<string> VisibleParts => visibleParts;
        public System.Collections.Generic.IReadOnlyList<string> HiddenParts => hiddenParts;
        public bool IsValid => !string.IsNullOrWhiteSpace(itemId);
    }
}
