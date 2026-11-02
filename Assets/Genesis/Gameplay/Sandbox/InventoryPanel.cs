using System;
using System.Collections.Generic;
using Genesis.Character;
using Genesis.Core;
using Genesis.Core.Events;
using Genesis.Items;
using UnityEngine;
using UnityEngine.UI;

namespace Genesis.Sandbox
{
    /// <summary>Inventory view and input focus. ActorInventory remains the sole item-state owner.</summary>
    [DefaultExecutionOrder(-20)]
    public sealed class InventoryPanel : MonoBehaviour
    {
        [SerializeField] private ActorInventory owner;
        [SerializeField] private GameObject window;
        [SerializeField] private Text coins, summary, details, actionLabel, emptyHint;
        [SerializeField] private Button actionButton, closeButton;
        [SerializeField] private Button[] itemButtons = Array.Empty<Button>();
        [SerializeField] private Text[] itemLabels = Array.Empty<Text>();
        [SerializeField] private Image[] itemIcons = Array.Empty<Image>();
        [SerializeField] private Text[] itemInitials = Array.Empty<Text>();
        [SerializeField] private Button[] slotButtons = Array.Empty<Button>();
        [SerializeField] private Text[] slotLabels = Array.Empty<Text>();
        [SerializeField] private EquipmentSlot[] slots = Array.Empty<EquipmentSlot>();
        [SerializeField] private ThirdPersonCamera cameraControl;
        [SerializeField] private SandboxInteractor interaction;
        [SerializeField] private SandboxFeedback feedback;
        private readonly List<ItemDefinition> _visibleItems = new();
        private PlayerMovementInput _input;
        private IGenesisEventBus _bus;
        private ItemDefinition _selected;
        private string _actionFeedback;
        private float _feedbackUntil;
        private CursorLockMode _cursorLock;
        private bool _cursorVisible, _inputWasSuspended, _cameraWasEnabled, _interactionWasEnabled, _feedbackWasEnabled;
        public bool IsOpen { get; private set; }
        public ItemDefinition Selected => _selected;

        private void Start()
        {
            _input = owner.GetComponent<PlayerMovementInput>();
            _bus = GenesisRuntime.Instance.Events;
            _bus.Subscribe<ItemAddedEvent>(OnAdded);
            _bus.Subscribe<ItemEquippedEvent>(OnEquipped);
            _bus.Subscribe<ItemUnequippedEvent>(OnUnequipped);
            _bus.Subscribe<ActorStateRestoredEvent>(OnRestored);
            closeButton.onClick.AddListener(Close);
            actionButton.onClick.AddListener(ActivateSelected);
            for (int i = 0; i < itemButtons.Length; i++)
            {
                int row = i;
                itemButtons[i].onClick.AddListener(() => { if (row < _visibleItems.Count) Select(_visibleItems[row]); });
            }
            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slot = i;
                slotButtons[i].onClick.AddListener(() => Select(owner.Equipment.Get(slots[slot])));
            }
            window.SetActive(false);
            Refresh();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I)) { if (IsOpen) Close(); else Open(); }
            else if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
            if (IsOpen && _actionFeedback != null && Time.unscaledTime >= _feedbackUntil)
            { _actionFeedback = null; Refresh(); }
        }
        private void OnDestroy()
        {
            Close();
            _bus?.Unsubscribe<ItemAddedEvent>(OnAdded);
            _bus?.Unsubscribe<ItemEquippedEvent>(OnEquipped);
            _bus?.Unsubscribe<ItemUnequippedEvent>(OnUnequipped);
            _bus?.Unsubscribe<ActorStateRestoredEvent>(OnRestored);
        }
        private void OnDisable() { if (IsOpen) Close(); }
        private void OnAdded(ItemAddedEvent e) { if (e.ActorId == owner.ActorId) Refresh(); }
        private void OnRestored(ActorStateRestoredEvent e)
        {
            if (e.ActorId != owner.ActorId) return;
            _selected = null; _actionFeedback = null; Refresh();
        }
        private void OnEquipped(ItemEquippedEvent e) { if (e.ActorId == owner.ActorId) ShowFeedback(e.Item.DisplayName + " equipped"); }
        private void OnUnequipped(ItemUnequippedEvent e) { if (e.ActorId == owner.ActorId) ShowFeedback(e.Item.DisplayName + " unequipped"); }
        private void ShowFeedback(string message)
        { _actionFeedback = message; _feedbackUntil = Time.unscaledTime + 3f; Refresh(); }

        public void Open()
        {
            if (IsOpen || _input == null) return;
            IsOpen = true;
            _cursorLock = Cursor.lockState; _cursorVisible = Cursor.visible;
            _inputWasSuspended = _input.Suspended; _input.Suspended = true;
            _cameraWasEnabled = cameraControl.enabled; cameraControl.enabled = false;
            _interactionWasEnabled = interaction.enabled; interaction.enabled = false;
            _feedbackWasEnabled = feedback.enabled; feedback.enabled = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            window.SetActive(true); Refresh();
        }
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (_input != null) _input.Suspended = _inputWasSuspended;
            if (cameraControl != null) cameraControl.enabled = _cameraWasEnabled;
            if (interaction != null) interaction.enabled = _interactionWasEnabled;
            if (feedback != null) feedback.enabled = _feedbackWasEnabled;
            Cursor.lockState = _cursorLock; Cursor.visible = _cursorVisible;
            if (window != null) window.SetActive(false);
        }
        public void Select(ItemDefinition item)
        {
            _selected = item != null && owner.Inventory.Get(item.ItemId) != null ? item : null;
            Refresh();
        }
        public void ActivateSelected()
        {
            if (_selected == null || _selected.Slot == EquipmentSlot.None) return;
            if (owner.Equipment.Get(_selected.Slot) == _selected) owner.Equipment.TryUnequip(_selected.Slot);
            else owner.Equipment.TryEquip(_selected.ItemId);
        }
        private void Refresh()
        {
            if (owner.Inventory == null) return;
            coins.text = "SOUL COINS   " + (owner.Inventory.Get("soul-coins")?.Count ?? 0);
            _visibleItems.Clear();
            foreach (var entry in owner.Inventory.Entries.Values) _visibleItems.Add(entry.Definition);
            _visibleItems.Sort((a,b) => string.Compare(a.DisplayName,b.DisplayName,StringComparison.Ordinal));
            emptyHint.gameObject.SetActive(_visibleItems.Count == 0);
            summary.text = _actionFeedback ?? (_visibleItems.Count + " owned items  /  select an item to inspect");
            for (int i = 0; i < itemButtons.Length; i++)
            {
                bool active = i < _visibleItems.Count;
                itemButtons[i].gameObject.SetActive(active);
                if (!active) continue;
                var item = _visibleItems[i];
                bool equipped = item.Slot != EquipmentSlot.None && owner.Equipment.Get(item.Slot) == item;
                itemLabels[i].text = item.DisplayName + "  x" + owner.Inventory.Get(item.ItemId).Count + (equipped ? "  [E]" : "");
                itemButtons[i].image.color = item == _selected ? new Color(.30f,.27f,.17f) : new Color(.09f,.14f,.19f);
                itemIcons[i].sprite = item.Icon; itemIcons[i].enabled = item.Icon != null;
                itemInitials[i].text = item.DisplayName.Substring(0,1);
                itemInitials[i].enabled = item.Icon == null;
            }
            for (int i = 0; i < slots.Length; i++)
            {
                var equipped = owner.Equipment.Get(slots[i]);
                slotLabels[i].text = SlotName(slots[i]) + "\n" + (equipped != null ? equipped.DisplayName : "Empty");
                slotButtons[i].image.color = equipped == _selected && equipped != null ? new Color(.3f,.27f,.17f) : new Color(.09f,.14f,.19f);
            }
            actionButton.interactable = _selected != null && _selected.Slot != EquipmentSlot.None;
            if (_selected == null)
            { details.text = "YOUR BELONGINGS\n\nSelect an item on the left, or an occupied equipment slot.\n\nCollect equipment from the nearby stand to begin.\n\nEquipping keeps the item in your inventory."; actionLabel.text = "SELECT AN ITEM"; return; }
            bool isEquipped = _selected.Slot != EquipmentSlot.None && owner.Equipment.Get(_selected.Slot) == _selected;
            details.text = _selected.DisplayName.ToUpperInvariant() + "\n\n" + _selected.Category + "\nOwned: " + owner.Inventory.Get(_selected.ItemId).Count +
                "\nSlot: " + SlotName(_selected.Slot) + "\nStatus: " + (isEquipped ? "Equipped" : "In inventory") + "\n\n" + _selected.Description;
            actionLabel.text = _selected.Slot != EquipmentSlot.None ? isEquipped ? "UNEQUIP" : "EQUIP" : _selected.Category == ItemCategory.Consumable ? "USE — NOT AVAILABLE" : "NOT EQUIPPABLE";
        }
        public static string SlotName(EquipmentSlot slot) => slot == EquipmentSlot.MainHand ? "Main hand" : slot == EquipmentSlot.OffHand ? "Off hand" : slot == EquipmentSlot.None ? "None" : slot.ToString();
    }
}
