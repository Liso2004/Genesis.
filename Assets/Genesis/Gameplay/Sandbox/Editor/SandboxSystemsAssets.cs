using System;
using System.IO;
using System.Linq;
using Genesis.Authorities;
using Genesis.Gods;
using Genesis.Items;
using Genesis.Skill;
using Genesis.Skill.Definitions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genesis.Sandbox.Editor
{
    internal static class SandboxSystemsAssets
    {
        private static readonly System.Collections.Generic.Dictionary<string, ScriptableObject> Created = new();
        internal const string Root = "Assets/Content/Sandbox/Systems";
        internal const string Kingdom = "Assets/PolygonFantasyKingdom/Prefabs/";
        internal const string Dungeon = "Assets/PolygonDungeonRealms/Prefabs/";
        internal static readonly string[] Models = {
            "Assets/Synty/PolygonFantasyHeroCharacters/Prefabs/Weapons/SM_Wep_Sword_01.prefab",
            "Assets/Synty/PolygonFantasyHeroCharacters/Prefabs/Weapons/SM_Wep_Shield_01.prefab",
            Kingdom + "Items/SM_Item_Bottle_01.prefab", Dungeon + "Props/SM_Prop_Gold_Coins_01.prefab",
            Kingdom + "Items/SM_Item_Crystal_01.prefab",
            Kingdom + "Props/BattleGround/SM_Prop_Dummy_Armour_01.prefab",
            Kingdom + "Props/BattleGround/SM_Prop_Weapon_Rack_01.prefab",
            Kingdom + "Props/SM_Prop_Statue_Angel_01.prefab",
            Kingdom + "Props/SM_Prop_Statue_Plinth_01.prefab",
            Kingdom + "Environments/SM_Env_Tree_Pine_Cluster_01.prefab",
            Kingdom + "Environments/SM_Env_Rock_01.prefab",
            Dungeon + "Environments/SM_Env_Dwarf_Wall_DoorFrame_Single_01.prefab",
            Kingdom + "Props/BattleGround/SM_Prop_Camp_Brazier_01.prefab",
            Dungeon + "Props/SM_Prop_Banner_01.prefab"
        };
        [Serializable] private class Inspection { public Entry[] assets; }
        [Serializable] private class Entry { public string path; public string guid; public int renderers; public int colliders; public Vector3 size; }
        [MenuItem("Genesis/Systems Sandbox/1 Inspect Imported Assets")]
        public static void Inspect()
        {
            var entries = Models.Select(path => {
                var prefab = Load<GameObject>(path);
                var renderers = prefab.GetComponentsInChildren<Renderer>(true);
                Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds();
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                return new Entry { path = path, guid = AssetDatabase.AssetPathToGUID(path), renderers = renderers.Length,
                    colliders = prefab.GetComponentsInChildren<Collider>(true).Length, size = bounds.size };
            }).ToArray();
            Directory.CreateDirectory("Validation/GenesisSystems");
            File.WriteAllText("Validation/GenesisSystems/Imported-Assets.json", JsonUtility.ToJson(new Inspection { assets = entries }, true));
            Debug.Log("[Genesis Systems] Inspected " + entries.Length + " imported prefabs. See Validation/GenesisSystems/Imported-Assets.json.");
        }
        internal static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing actual asset: " + path);
        internal static T Asset<T>(string name) where T : ScriptableObject
        {
            Directory.CreateDirectory(Root);
            string path = Root + "/" + name + ".asset";
            if (Created.TryGetValue(path, out var cached)) return (T)cached;
            var value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value == null && File.Exists(path)) { AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate); value = AssetDatabase.LoadAssetAtPath<T>(path); }
            if (value == null) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); }
            Created[path] = value; return value;
        }
        internal static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field);
            if (p == null) throw new InvalidOperationException(target.GetType().Name + "." + field);
            if (value is Object obj) p.objectReferenceValue = obj;
            else if (value is string s) p.stringValue = s;
            else if (value is int i) p.intValue = i;
            else if (value is float f) p.floatValue = f;
            else if (value is bool b) p.boolValue = b;
            else if (value is Vector3 v) p.vector3Value = v;
            else if (value is Color c) p.colorValue = c;
            else if (value == null) p.objectReferenceValue = null;
            else throw new InvalidOperationException("Unsupported field value " + field);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        internal static void References(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field); p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void Domains(Object target, string field, SkillDomainDefinition[] domains, float weight)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field); p.arraySize = domains.Length;
            for (int i = 0; i < domains.Length; i++) { var e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("domain").objectReferenceValue = domains[i]; e.FindPropertyRelative("weight").floatValue = weight; }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static SkillDomainDefinition Domain(string name)
        {
            var d = Asset<SkillDomainDefinition>("Domain-" + name.Replace(" ", "")); Set(d, "domainId", name.ToLowerInvariant().Replace(" ", "-")); Set(d, "displayName", name); return d;
        }
        internal static SandboxScenarioDefinition BuildData()
        {
            Created.Clear();
            var knowledge = Domain("Knowledge"); var curiosity = Domain("Curiosity"); var warfare = Domain("Warfare");
            var perception = Load<SkillDefinition>("Assets/Content/Skills/Perception/SkillDefinition.asset");
            Set(perception, "category", (int)SkillCategory.GiftedKnowledge); Domains(perception, "domains", new[] { knowledge, curiosity }, .5f);
            var sword = Asset<SkillDefinition>("Swordsmanship"); Set(sword, "skillId", "swordsmanship"); Set(sword, "displayName", "Swordsmanship");
            Set(sword, "category", (int)SkillCategory.Active); Domains(sword, "domains", new[] { warfare }, 1);
            var pride = Asset<SkillDefinition>("Pride"); Set(pride, "skillId", "pride"); Set(pride, "displayName", "Pride — Sovereign's Will"); Set(pride, "series", (int)SkillSeries.Sin);
            var humility = Asset<SkillDefinition>("Humility"); Set(humility, "skillId", "humility"); Set(humility, "displayName", "Humility — Servant's Heart"); Set(humility, "series", (int)SkillSeries.Holy);
            var insight = Asset<BlessingDefinition>("Insight"); Set(insight, "blessingId", "observer-insight"); Set(insight, "displayName", "Insight");
            Set(insight, "description", "Sandbox tuning: increases observation range by 15% after accepting an earned blessing."); Set(insight, "observationRangeMultiplier", 1.15f);
            string[] names = { "The Observer", "The Forge-Mother", "The Dungeon's Will", "The Watcher of Thresholds", "The Weeping Mother", "The Laughing Trickster", "The Silent Judge", "The First Flame" };
            string[] ids = { "observer", "forge-mother", "dungeons-will", "watcher-of-thresholds", "weeping-mother", "laughing-trickster", "silent-judge", "first-flame" };
            string[][] affinities = { new[] { "Knowledge", "Curiosity", "Time" }, new[] { "Creation", "Transformation", "Resistance" }, new[] { "The Dungeon" }, new[] { "Boundaries", "Transitions", "Choices" }, new[] { "Sacrifice", "Loss", "Rebirth" }, new[] { "Chaos", "Luck", "Unexpected Outcomes" }, new[] { "Justice", "Consequence", "Balance" }, new[] { "Beginnings", "Potential", "Hope" } };
            GodDefinition observer = null;
            for (int i = 0; i < names.Length; i++) {
                var god = Asset<GodDefinition>("God-" + ids[i]); Set(god, "godId", ids[i]); Set(god, "displayName", names[i]);
                Set(god, "description", string.Join(", ", affinities[i])); Domains(god, "affinities", affinities[i].Select(Domain).ToArray(), 1);
                if (i == 0) { observer = god; Set(god, "blessing", insight); Set(god, "blessingFavorThreshold", 30f); }
            }
            string[] itemNames = { "Iron Sword", "Blackwood Shield", "Healing Potion", "Soul Coins", "Ash-Kin's Ember" };
            string[] itemIds = { "iron-sword", "blackwood-shield", "healing-potion", "soul-coins", "ash-kin-ember" };
            var items = new ItemDefinition[5];
            for (int i = 0; i < items.Length; i++) {
                var item = items[i] = Asset<ItemDefinition>("Item-" + itemIds[i]); Set(item, "itemId", itemIds[i]); Set(item, "displayName", itemNames[i]);
                Set(item, "visualPrefab", Load<GameObject>(Models[i])); Set(item, "category", (int)new[] { ItemCategory.Weapon, ItemCategory.Shield, ItemCategory.Consumable, ItemCategory.Currency, ItemCategory.KeyItem }[i]);
                Set(item, "equipmentSlot", (int)(i == 0 ? EquipmentSlot.MainHand : i == 1 ? EquipmentSlot.OffHand : EquipmentSlot.None));
                Set(item, "maxStack", i == 2 ? 10 : i == 3 ? 999 : 1); Set(item, "equippedScale", Vector3.one * .7f);
                if (i < 2) Set(item, "equippedRotation", new Vector3(0, 0, -90));
                Set(item, "description", "Genesis starter content. Imported presentation proxy; item identity is authored here. " + (i == 2 ? "Consumption is outside this foundation." : ""));
            }
            var authority = Asset<AuthorityDefinition>("Authority-SovereignServant"); Set(authority, "authorityId", "sovereign-servant"); Set(authority, "displayName", "Authority of the Sovereign Servant");
            var a = new SerializedObject(authority); var req = a.FindProperty("skills"); req.arraySize = 2;
            for (int i = 0; i < 2; i++) { var e = req.GetArrayElementAtIndex(i); e.FindPropertyRelative("skill").objectReferenceValue = i == 0 ? pride : humility; e.FindPropertyRelative("mastery").intValue = (int)SkillMasteryStage.Mastery; }
            var conditions = a.FindProperty("requiredConditions"); conditions.arraySize = 1; conditions.GetArrayElementAtIndex(0).stringValue = "trial-of-understanding"; a.ApplyModifiedPropertiesWithoutUndo();
            var scenario = Asset<SandboxScenarioDefinition>("SandboxScenario");
            scenario.perception = perception; scenario.swordsmanship = sword; scenario.progression = Load<SkillProgressionConfig>("Assets/Content/Skills/Perception/PerceptionProgression.asset");
            scenario.ironSword = items[0]; scenario.blackwoodShield = items[1]; scenario.observer = observer; scenario.sovereignServant = authority;
            EditorUtility.SetDirty(scenario); AssetDatabase.SaveAssets(); return scenario;
        }
    }
}
