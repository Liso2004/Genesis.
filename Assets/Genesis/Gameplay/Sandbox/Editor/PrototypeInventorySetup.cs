using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Genesis.Character;
using Genesis.Items;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Genesis.Sandbox.Editor
{
    public static class PrototypeInventorySetup
    {
        internal const string PlayerPath = "Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab";
        internal const string ModelPath = "Assets/Genesis/Character/Prefabs/GenesisModularHuman.prefab";
        private const string Source = "Assets/Synty/PolygonFantasyHeroCharacters/Prefabs/ModularCharacters.prefab";
        private const string StaticParts = "Assets/Synty/PolygonFantasyHeroCharacters/Prefabs/Characters_ModularParts_Static/";
        private const string Content = "Assets/Content/Sandbox/Systems/";
        public static readonly string[] BaseParts = {
            "Chr_Head_Male_00", "Chr_Eyebrow_Male_01", "Chr_Hair_01", "Chr_Torso_Male_04",
            "Chr_ArmUpperRight_Male_00", "Chr_ArmUpperLeft_Male_00", "Chr_ArmLowerRight_Male_00", "Chr_ArmLowerLeft_Male_00",
            "Chr_HandRight_Male_00", "Chr_HandLeft_Male_00", "Chr_Hips_Male_01", "Chr_LegRight_Male_01", "Chr_LegLeft_Male_01"
        };
        private static readonly string[][] ShowParts = {
            new[]{"Chr_HeadCoverings_No_Hair_04"},
            new[]{"Chr_Torso_Male_02","Chr_ArmUpperRight_Male_01","Chr_ArmUpperLeft_Male_01","Chr_ArmLowerRight_Male_01","Chr_ArmLowerLeft_Male_01"},
            new[]{"Chr_HandRight_Male_02","Chr_HandLeft_Male_02"},
            new[]{"Chr_Hips_Male_02","Chr_LegRight_Male_02","Chr_LegLeft_Male_02"},
            new[]{"Chr_BackAttachment_03"}
        };
        private static readonly string[][] HideParts = {
            new[]{"Chr_Hair_01"},
            new[]{"Chr_Torso_Male_04","Chr_ArmUpperRight_Male_00","Chr_ArmUpperLeft_Male_00","Chr_ArmLowerRight_Male_00","Chr_ArmLowerLeft_Male_00"},
            new[]{"Chr_HandRight_Male_00","Chr_HandLeft_Male_00"},
            new[]{"Chr_Hips_Male_01","Chr_LegRight_Male_01","Chr_LegLeft_Male_01"},
            Array.Empty<string>()
        };
        private static readonly EquipmentSlot[] ArmorSlots = { EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Hands, EquipmentSlot.Legs, EquipmentSlot.Cape };
        private static readonly string[] ArmorNames = { "Training Helmet", "Training Chestpiece", "Training Gloves", "Training Legwear", "Training Cape" };

        [MenuItem("Genesis/Prototype/2 Configure Modular Player")]
        public static void ConfigureModel()
        {
            RequireSandbox();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) != null)
                throw new InvalidOperationException("Modular player already authored; edit the existing prefab.");
            Directory.CreateDirectory("Validation/GenesisPolish/Before");
            File.Copy(PlayerPath, "Validation/GenesisPolish/Before/GenesisPlayer.prefab", false);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source));
            try
            {
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = "Genesis Modular Human";
                foreach (var script in model.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
                var keep = new HashSet<string>(BaseParts.Concat(ShowParts.SelectMany(a => a)));
                var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (string id in keep) if (!renderers.Any(r => r.name == id)) throw new InvalidOperationException("Missing inspected modular part: " + id);
                foreach (var r in renderers)
                {
                    if (!keep.Contains(r.name)) { Object.DestroyImmediate(r.gameObject); continue; }
                    for (Transform parent = r.transform.parent; parent != model.transform; parent = parent.parent) parent.gameObject.SetActive(true);
                    r.gameObject.SetActive(BaseParts.Contains(r.name));
                    r.updateWhenOffscreen = false;
                }
                var active = model.GetComponentsInChildren<Renderer>();
                Bounds bounds = active[0].bounds; foreach (var r in active) bounds.Encapsulate(r.bounds);
                model.transform.localScale *= 1.9f / bounds.size.y;
                var appearance = model.AddComponent<ModularAppearance>();
                References(appearance, "parts", model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(r => (Object)r.gameObject).ToArray());
                Strings(appearance, "baseParts", BaseParts);
                var animator = model.GetComponentInChildren<Animator>(true);
                if (animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid) throw new InvalidOperationException("Valid Synty Humanoid Avatar required.");
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Genesis/Character/Animatinos/PlayerAnimator.controller");
                animator.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(model, ModelPath);
            }
            finally { Object.DestroyImmediate(model); }
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            var player = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                Transform root = player.transform.Find("ModelRoot");
                foreach (Transform child in root) child.gameObject.SetActive(false); // Original hero remains recoverable.
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                var animator = visual.GetComponentInChildren<Animator>();
                SandboxSystemsAssets.Set(player.GetComponent<PlayerAnimation>(), "animator", animator);
                var equipment = player.GetComponent<EquipmentPresentation>();
                SandboxSystemsAssets.Set(equipment, "animator", animator);
                SandboxSystemsAssets.Set(equipment, "appearance", visual.GetComponent<ModularAppearance>());
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            AssetDatabase.SaveAssets();
            Debug.Log("[Genesis Prototype] Canonical player uses the Synty Humanoid with base clothing, no equipped gear, and the existing locomotion controller.");
        }

        [MenuItem("Genesis/Prototype/3 Add Inventory UI And Equipment Tests")]
        public static void ConfigureScene()
        {
            RequireSandbox();
            if (Object.FindFirstObjectByType<InventoryPanel>() != null) throw new InvalidOperationException("Inventory UI already exists.");
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player.GetComponentInChildren<ModularAppearance>() == null) throw new InvalidOperationException("Wait for the canonical prefab update before wiring the scene.");
            var equipmentAnimator = new SerializedObject(player.GetComponent<EquipmentPresentation>()).FindProperty("animator");
            if (equipmentAnimator.prefabOverride) PrefabUtility.RevertPropertyOverride(equipmentAnimator, InteractionMode.AutomatedAction);
            var items = BuildArmor();
            var courtyard = GameObject.Find("Genesis Crouch Courtyard").transform;
            var stations = courtyard.Find("Genesis Systems Stations");
            for (int i = 0; i < items.Length; i++) AddArmorPickup(items[i], i, stations);
            References(player.GetComponent<SandboxInteractor>(), "stations", courtyard.GetComponentsInChildren<SandboxStation>(true).Cast<Object>().ToArray());
            var panel = PrototypeInventoryView.Build(player);
            SandboxSystemsAssets.Set(player.GetComponent<SandboxActor>(), "inventoryPanel", panel);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("[Genesis Prototype] Inventory Canvas, focus handling and five modular armor pickups saved in Sandbox.");
        }

        private static ItemDefinition[] BuildArmor()
        {
            var items = new ItemDefinition[ArmorSlots.Length];
            for (int i = 0; i < items.Length; i++)
            {
                string id = "training-" + ArmorSlots[i].ToString().ToLowerInvariant();
                string path = Content + "Item-" + id + ".asset";
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item == null) { item = ScriptableObject.CreateInstance<ItemDefinition>(); AssetDatabase.CreateAsset(item, path); }
                SandboxSystemsAssets.Set(item, "itemId", id); SandboxSystemsAssets.Set(item, "displayName", ArmorNames[i]);
                SandboxSystemsAssets.Set(item, "description", "Simple " + ArmorSlots[i].ToString().ToLowerInvariant() + " equipment for the training ground.");
                SandboxSystemsAssets.Set(item, "category", (int)ItemCategory.Armor); SandboxSystemsAssets.Set(item, "equipmentSlot", (int)ArmorSlots[i]);
                SandboxSystemsAssets.Set(item, "visualPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(StaticParts + ShowParts[i][0] + "_Static.prefab"));
                Strings(item, "visibleParts", ShowParts[i]); Strings(item, "hiddenParts", HideParts[i]);
                EditorUtility.SetDirty(item); items[i] = item;
            }
            return items;
        }

        private static void AddArmorPickup(ItemDefinition item, int index, Transform parent)
        {
            var go = new GameObject("Collect " + item.DisplayName);
            Undo.RegisterCreatedObjectUndo(go, "Add armor test pickup");
            go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(-9.5f + index * 1.1f, 0, -3.8f);
            var station = go.AddComponent<SandboxStation>();
            SandboxSystemsAssets.Set(station, "targetId", "pickup-" + item.ItemId); SandboxSystemsAssets.Set(station, "displayName", "Collect " + item.DisplayName);
            SandboxSystemsAssets.Set(station, "kind", (int)SandboxStationKind.Pickup); SandboxSystemsAssets.Set(station, "item", item);
            var focus = new GameObject("Interaction focus").transform; focus.SetParent(go.transform, false); focus.localPosition = Vector3.up * .9f;
            SandboxSystemsAssets.Set(station, "focus", focus);
            Display(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolygonDungeonRealms/Prefabs/Props/SM_Prop_Crate_01.prefab"), go.transform, .6f, 0);
            Display(item.VisualPrefab, go.transform, .45f, .61f);
        }
        private static void Display(GameObject asset, Transform parent, float height, float bottom)
        {
            if (asset == null) throw new InvalidOperationException("Inspected equipment display prefab missing.");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            var rs = go.GetComponentsInChildren<Renderer>(true); Bounds b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            go.transform.localScale *= height / b.size.y;
            b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            go.transform.position += parent.position + Vector3.up * bottom - new Vector3(b.center.x,b.min.y,b.center.z);
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
        internal static void Strings(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field); p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        internal static void References(Object target, string field, Object[] values) => SandboxSystemsAssets.References(target, field, values);
        private static void RequireSandbox()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != "Assets/Scenes/Sandbox/Sandbox.unity")
                throw new InvalidOperationException("Existing Sandbox must be open in Edit mode.");
        }
    }
}
