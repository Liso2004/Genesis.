using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Genesis.Authorities;
using Genesis.Character;
using Genesis.Gods;
using Genesis.Items;
using Genesis.Skill;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using A = Genesis.Sandbox.Editor.SandboxSystemsAssets;

namespace Genesis.Sandbox.Editor
{
    /// <summary>Explicit, guarded authoring of the existing Sandbox. Never runs on import.</summary>
    public static class SandboxSystemsSetup
    {
        private const string PlayerPath = "Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab";
        private static Transform _root;
        private static readonly List<SandboxStation> Stations = new();
        [MenuItem("Genesis/Systems Sandbox/2 Apply to Existing Sandbox")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Sandbox/Sandbox.unity")
                throw new InvalidOperationException("Open the existing Sandbox in Edit mode.");
            var courtyard = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Genesis Crouch Courtyard");
            if (courtyard == null) throw new InvalidOperationException("Existing courtyard required.");
            var partial = courtyard.transform.Find("Genesis Systems Stations");
            if (partial != null) {
                if (File.Exists("Validation/GenesisSystems/Applied.txt") || partial.GetComponentsInChildren<SandboxStation>(true).Length > 1)
                    throw new InvalidOperationException("Completed stations already exist; edit instead of duplicating.");
                Undo.DestroyObjectImmediate(partial.gameObject);
            }
            if (!File.Exists("Validation/GenesisSystems/Imported-Assets.json")) throw new InvalidOperationException("Inspect imported assets first.");
            foreach (string path in A.Models) A.Load<GameObject>(path);
            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null) throw new InvalidOperationException("Existing player required.");
            var scenario = A.BuildData();
            ConfigurePrefab(scenario);
            foreach (var type in new[] { typeof(AuthoritySystem), typeof(GodResonanceSystem), typeof(SkillDomainSystem), typeof(ActorInventory) })
                foreach (var extra in player.GetComponents(type).Where(c => PrefabUtility.IsAddedComponentOverride(c)).ToArray())
                    if (player.GetComponents(type).Length > 1) Object.DestroyImmediate(extra);
            var actor = Add<SandboxActor>(player.gameObject); A.Set(actor, "scenario", scenario);
            var interactor = Add<SandboxInteractor>(player.gameObject); Add<SandboxFeedback>(player.gameObject);
            var group = new GameObject("Genesis Systems Stations"); group.transform.SetParent(courtyard.transform, false); _root = group.transform;
            Undo.RegisterCreatedObjectUndo(group, "Add systems stations"); Stations.Clear();
            Ground(courtyard.transform);
            var equipment = Station("equipment", "Equipment stand", SandboxStationKind.Equipment, new Vector3(-3.8f, 0, .5f));
            Visual(6, equipment.transform, 1.2f, Vector3.zero);
            string[] ids = { "iron-sword", "blackwood-shield", "healing-potion", "soul-coins", "ash-kin-ember" };
            for (int i = 0; i < ids.Length; i++) {
                var item = A.Asset<ItemDefinition>("Item-" + ids[i]);
                var pickup = Station("pickup-" + ids[i], "Collect " + item.DisplayName, SandboxStationKind.Pickup, new Vector3(2 + i * 1.2f, 0, -2));
                A.Set(pickup, "item", item); A.Set(pickup, "amount", i == 3 ? 25 : 1);
                Visual(i, pickup.transform, i < 2 ? .85f : i == 3 ? .04f : .38f, Vector3.up * .6f);
                Pedestal(pickup.transform, .5f);
            }
            var dummy = Station("training-dummy", "Sword practice / left click", SandboxStationKind.Training, new Vector3(4, 0, 2));
            var dummyVisual = Visual(5, dummy.transform, 1.85f, Vector3.zero); A.Set(dummy, "pulseVisual", dummyVisual);
            var shrine = Station("observer-shrine", "Observer / claim earned Insight", SandboxStationKind.Shrine, new Vector3(-4, 0, 8));
            Visual(8, shrine.transform, .5f, Vector3.zero); Visual(7, shrine.transform, 2.1f, Vector3.up * .5f);
            LightAt(shrine.transform, new Color(.25f, .7f, 1), 2.2f);
            var authority = Station("authority-gate", "Sovereign Servant / check requirements", SandboxStationKind.Authority, new Vector3(3, 0, 9));
            Visual(11, authority.transform, 2.8f, Vector3.zero); LightAt(authority.transform, new Color(.6f, .35f, 1), 2);
            var observationPositions = new[] { new Vector3(-3, 0, 4.8f), new Vector3(0, 0, 11), new Vector3(4, 0, 5.5f) };
            string[] observations = { "Ancient stone / observation", "Crystal formation / observation", "Kingdom banner / observation" };
            int[] models = { 10, 4, 13 };
            for (int i = 0; i < 3; i++) {
                var station = Station("observation-" + i, observations[i], SandboxStationKind.Observation, observationPositions[i]);
                Visual(models[i], station.transform, i == 2 ? 2.2f : 1.2f, Vector3.zero);
            }
            foreach (var pos in new[] { new Vector3(-15,0,10), new Vector3(15,0,10), new Vector3(-14,0,-14), new Vector3(15,0,-14) }) {
                var tree = new GameObject("Pine border"); tree.transform.SetParent(_root, false); tree.transform.localPosition = pos; Visual(9, tree.transform, 6, Vector3.zero);
            }
            foreach (var pos in new[] { new Vector3(-5.7f,0,8), new Vector3(4.8f,0,9) }) {
                var prop = new GameObject("Brazier"); prop.transform.SetParent(_root, false); prop.transform.localPosition = pos; Visual(12, prop.transform, 1.3f, Vector3.zero);
            }
            A.References(interactor, "stations", Stations.Cast<Object>().ToArray());
            Vector3 start = courtyard.transform.TransformPoint(new Vector3(0, .03f, -2));
            Vector3 delta = start - player.transform.position; player.transform.position = start;
            if (Camera.main != null) Camera.main.transform.position += delta;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText("Validation/GenesisSystems/Applied.txt", "Applied to " + scene.path + "\nPrefab: " + PlayerPath + "\nStations: " + Stations.Count + "\n" + DateTime.UtcNow.ToString("O"));
            Debug.Log("[Genesis Systems] Saved " + Stations.Count + " stations in the existing Sandbox, preserving the crouch course and NPCs.");
        }
        private static T Add<T>(GameObject target) where T : Component => target.GetComponent<T>() ?? target.AddComponent<T>();
        private static void ConfigurePrefab(SandboxScenarioDefinition scenario)
        {
            var prefab = PrefabUtility.LoadPrefabContents(PlayerPath);
            try {
                Add<ActorInventory>(prefab); Add<SkillDomainSystem>(prefab); Add<AuthoritySystem>(prefab);
                var gods = Add<GodResonanceSystem>(prefab); A.References(gods, "gods", new Object[] { scenario.observer });
                var visual = Add<EquipmentPresentation>(prefab); A.Set(visual, "animator", prefab.GetComponentInChildren<Animator>(true));
                var old = prefab.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "PresentationWeapon");
                if (old != null) A.Set(visual, "previousPresentationWeapon", old.gameObject);
                PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPath);
                AssetDatabase.ImportAsset(PlayerPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            } finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        private static SandboxStation Station(string id, string label, SandboxStationKind kind, Vector3 position)
        {
            var go = new GameObject(label); go.transform.SetParent(_root, false); go.transform.localPosition = position;
            var station = go.AddComponent<SandboxStation>(); A.Set(station, "targetId", id); A.Set(station, "displayName", label); A.Set(station, "kind", (int)kind);
            var focus = new GameObject("Interaction focus"); focus.transform.SetParent(go.transform, false); focus.transform.localPosition = Vector3.up * .9f; A.Set(station, "focus", focus.transform);
            var text = new GameObject("Station label"); text.transform.SetParent(go.transform, false); text.transform.localPosition = new Vector3(0, 2.9f, 0);
            var mesh = text.AddComponent<TextMesh>(); mesh.text = label; mesh.fontSize = 48; mesh.characterSize = .025f; mesh.anchor = TextAnchor.MiddleCenter; mesh.color = new Color(1, .86f, .5f);
            text.AddComponent<SandboxLabel>(); Stations.Add(station); return station;
        }
        private static Transform Visual(int index, Transform parent, float height, Vector3 offset)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(A.Load<GameObject>(A.Models[index])); model.transform.SetParent(parent, false);
            var renderers = model.GetComponentsInChildren<Renderer>(); if (renderers.Length == 0) throw new InvalidOperationException("No mesh in " + A.Models[index]);
            Bounds bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            model.transform.localScale *= height / Mathf.Max(.001f, bounds.size.y);
            bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            model.transform.position += parent.TransformPoint(offset) - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            // Pickup proxies use their pedestal; retain authored collisions on environment props.
            if (index < 5) foreach (var c in model.GetComponentsInChildren<Collider>()) c.enabled = false;
            return model.transform;
        }
        private static void Pedestal(Transform parent, float height)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name = "Pickup pedestal"; go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * (height / 2); go.transform.localScale = new Vector3(.7f, height / 2, .7f);
        }
        private static void LightAt(Transform parent, Color color, float height)
        {
            var go = new GameObject("Resonance light"); go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0, height, -.8f);
            var light = go.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.range = 4; light.intensity = 3;
        }
        private static void Ground(Transform courtyard)
        {
            var ground = courtyard.Find("Courtyard ground"); if (ground == null) return;
            var renderer = ground.GetComponent<Renderer>(); if (renderer == null) return;
            string path = A.Root + "/CourtyardGrass.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(renderer.sharedMaterial); material.name = "Courtyard Grass"; material.color = new Color(.24f, .36f, .12f); AssetDatabase.CreateAsset(material, path); }
            renderer.sharedMaterial = material;
        }
    }
}
