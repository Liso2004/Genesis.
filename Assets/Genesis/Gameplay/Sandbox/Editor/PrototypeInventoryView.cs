using System.Collections.Generic;
using Genesis.Character;
using Genesis.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Genesis.Sandbox.Editor
{
    internal static class PrototypeInventoryView
    {
        private static readonly Color Navy = new(.035f,.065f,.10f,.98f);
        private static readonly Color Panel = new(.065f,.10f,.14f,1);
        private static readonly Color Gold = new(.77f,.62f,.33f,1);
        private static readonly Color White = new(.9f,.9f,.85f,1);
        private static Font _font;

        internal static InventoryPanel Build(PlayerController player)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("Genesis Inventory UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create prototype inventory UI");
            go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.GetComponent<Canvas>().sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,800); scaler.matchWidthOrHeight = 1f;
            var controller = go.AddComponent<InventoryPanel>();
            var root = go.GetComponent<RectTransform>();
            var coins = Label(root, "Soul coins", "SOUL COINS   0", 18, Gold, 980, 744, 270, 32);
            coins.alignment = TextAnchor.MiddleRight;
            coins.rectTransform.anchorMin = coins.rectTransform.anchorMax = new Vector2(1,0);
            coins.rectTransform.pivot = new Vector2(1,0); coins.rectTransform.anchoredPosition = new Vector2(-24,14);
            var window = Box(root, "Inventory window", Gold, 60, 60, 1160, 660);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(.5f,.5f);
            window.anchoredPosition = Vector2.zero;
            var bg = Box(window, "Interior", Navy, 2, 2, 1156, 656);
            Label(bg, "Title", "BELONGINGS", 34, White, 28, 20, 550, 48);
            Label(bg, "Subtitle", "PROJECT GENESIS   /   INVENTORY & EQUIPMENT", 15, Gold, 30, 68, 900, 28);
            var close = Button(bg, "Close inventory", "CLOSE  [I / ESC]", 18, 936, 26, 192, 46);
            var summary = Label(bg, "Summary", "", 16, White, 30, 105, 1000, 28);
            var inventory = Box(bg, "Owned items", Panel, 24, 148, 386, 476);
            var inspect = Box(bg, "Item details", Panel, 422, 148, 344, 476);
            var equipment = Box(bg, "Equipment", Panel, 778, 148, 354, 476);
            Label(inventory, "Owned heading", "INVENTORY", 20, Gold, 14, 10, 350, 30);
            var empty = Label(inventory, "Empty inventory", "Your inventory is empty.\n\nCollect items from the equipment\nstand and supplies area.", 19, White, 18, 92, 352, 190);
            var buttons = new List<Object>(); var labels = new List<Object>(); var icons = new List<Object>(); var initials = new List<Object>();
            for (int i=0;i<16;i++)
            {
                var button = Button(inventory, "Inventory row " + i, "", 17, 12, 47+i*26, 362, 25);
                var label = button.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.anchoredPosition = new Vector2(36,0);
                label.rectTransform.sizeDelta = new Vector2(326,25);
                var icon = Box(button.transform, "Item icon", Color.white, 3, 2, 22, 22).GetComponent<Image>();
                icon.preserveAspect = true;
                var initial = Label(button.transform, "Icon placeholder", "", 18, Gold, 4, 0, 24, 25);
                initial.alignment = TextAnchor.MiddleCenter;
                buttons.Add(button); labels.Add(label); icons.Add(icon); initials.Add(initial);
            }
            var details = Label(inspect, "Selected item details", "", 20, White, 20, 18, 304, 345);
            var action = Button(inspect, "Equip or unequip", "SELECT AN ITEM", 22, 18, 386, 308, 60);
            action.image.color = new Color(.3f,.25f,.14f);
            Label(equipment, "Equipment heading", "EQUIPMENT", 20, Gold, 16, 10, 315, 30);
            var slots = new[] { EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Hands, EquipmentSlot.Legs, EquipmentSlot.Cape, EquipmentSlot.MainHand, EquipmentSlot.OffHand, EquipmentSlot.Accessory };
            var slotButtons = new List<Object>(); var slotLabels = new List<Object>();
            for (int i=0;i<slots.Length;i++)
            {
                var b = Button(equipment, slots[i].ToString(), InventoryPanel.SlotName(slots[i])+"\nEmpty", 17, 12, 48+i*50, 330, 46);
                var text = b.GetComponentInChildren<Text>(); text.alignment = TextAnchor.MiddleLeft;
                text.rectTransform.anchoredPosition = new Vector2(14,0);
                text.rectTransform.sizeDelta = new Vector2(316,46);
                slotButtons.Add(b); slotLabels.Add(text);
            }
            var set = new SerializedObject(controller);
            set.FindProperty("owner").objectReferenceValue = player.GetComponent<ActorInventory>();
            set.FindProperty("window").objectReferenceValue = window.gameObject;
            set.FindProperty("coins").objectReferenceValue = coins;
            set.FindProperty("summary").objectReferenceValue = summary;
            set.FindProperty("details").objectReferenceValue = details;
            set.FindProperty("emptyHint").objectReferenceValue = empty;
            set.FindProperty("actionButton").objectReferenceValue = action;
            set.FindProperty("actionLabel").objectReferenceValue = action.GetComponentInChildren<Text>();
            set.FindProperty("closeButton").objectReferenceValue = close;
            set.FindProperty("cameraControl").objectReferenceValue = Camera.main.GetComponent<ThirdPersonCamera>();
            set.FindProperty("interaction").objectReferenceValue = player.GetComponent<SandboxInteractor>();
            set.FindProperty("feedback").objectReferenceValue = player.GetComponent<SandboxFeedback>();
            var slotArray = set.FindProperty("slots"); slotArray.arraySize = slots.Length;
            for(int i=0;i<slots.Length;i++) slotArray.GetArrayElementAtIndex(i).enumValueIndex = (int)slots[i];
            set.ApplyModifiedPropertiesWithoutUndo();
            PrototypeInventorySetup.References(controller,"itemButtons",buttons.ToArray());
            PrototypeInventorySetup.References(controller,"itemLabels",labels.ToArray());
            PrototypeInventorySetup.References(controller,"itemIcons",icons.ToArray());
            PrototypeInventorySetup.References(controller,"itemInitials",initials.ToArray());
            PrototypeInventorySetup.References(controller,"slotButtons",slotButtons.ToArray());
            PrototypeInventorySetup.References(controller,"slotLabels",slotLabels.ToArray());
            window.gameObject.SetActive(false);
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("Genesis UI EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                Undo.RegisterCreatedObjectUndo(events,"Create UI input adapter");
            }
            return controller;
        }
        private static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin = rect.anchorMax = new Vector2(0,1); rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(width,height); return rect;
        }
        private static RectTransform Box(Transform parent,string name,Color color,float x,float y,float width,float height)
        {
            var rect = Rect(parent,name,x,y,width,height); rect.gameObject.AddComponent<Image>().color = color; return rect;
        }
        private static Text Label(Transform parent,string name,string value,int size,Color color,float x,float y,float width,float height)
        {
            var rect = Rect(parent,name,x,y,width,height); var text = rect.gameObject.AddComponent<Text>();
            text.text = value; text.font = _font; text.fontSize = size; text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false; return text;
        }
        private static Button Button(Transform parent,string name,string label,int size,float x,float y,float width,float height)
        {
            var rect = Box(parent,name,new Color(.09f,.14f,.19f),x,y,width,height);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>();
            var colors = button.colors; colors.highlightedColor = new Color(1,.86f,.58f); colors.pressedColor = new Color(.72f,.62f,.4f); button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var text = Label(rect,"Label",label,size,White,0,0,width,height); text.alignment = TextAnchor.MiddleCenter;
            return button;
        }
    }
}
