using System;
using System.IO;
using System.Linq;
using Genesis.Character;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Genesis.Sandbox.Editor
{
    /// <summary>Explicit, undoable dressing of the existing course. Never runs on import.</summary>
    public static class SandboxPolishSetup
    {
        private const string Kingdom = "Assets/PolygonFantasyKingdom/Prefabs/";
        private const string Dungeon = "Assets/PolygonDungeonRealms/Prefabs/";
        private static Transform _courtyard, _art;
        private static readonly string[] ArtPaths = {
            Kingdom + "Environments/SM_Env_Rock_Cliff_01.prefab",
            Kingdom + "Environments/SM_Env_Bush_01.prefab",
            Kingdom + "Generic/SM_Generic_Mountains_Grass_01.prefab",
            Kingdom + "Props/Furniture/SM_Prop_Workbench_01.prefab",
            Dungeon + "Props/SM_Prop_Crate_01.prefab",
            Kingdom + "Props/SM_Prop_Barrel_01.prefab",
            Dungeon + "Buildings/SM_Bld_Ruin_Wall_Broken_01.prefab",
            Dungeon + "Props/SM_Prop_Camp_Lantern_01.prefab",
            Kingdom + "Environments/SM_Env_Path_01.prefab",
            Kingdom + "Environments/SM_Env_Tree_Pine_Cluster_01.prefab",
            Kingdom + "Props/BattleGround/SM_Prop_Weapon_Rack_01.prefab",
            Dungeon + "Environments/SM_Env_Dwarf_Floor_01.prefab",
            Kingdom + "Castle/SM_Bld_Castle_Wall_01.prefab"
        };

        [MenuItem("Genesis/Prototype/1 Polish Existing Sandbox")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Sandbox/Sandbox.unity")
                throw new InvalidOperationException("Open the existing Sandbox in Edit mode.");
            _courtyard = scene.GetRootGameObjects().Single(g => g.name == "Genesis Crouch Courtyard").transform;
            if (_courtyard.Find("Sanctuary dressing") != null)
                throw new InvalidOperationException("Polish already applied; edit the existing dressing.");
            foreach (string path in ArtPaths)
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    throw new InvalidOperationException("Inspected asset missing: " + path);
            var stations = _courtyard.GetComponentsInChildren<SandboxStation>(true);
            if (stations.Length != 12) throw new InvalidOperationException("Expected the inspected twelve stations.");
            Directory.CreateDirectory("Validation/GenesisPolish/Before");
            File.Copy(scene.path, "Validation/GenesisPolish/Before/Sandbox-on-disk.unity", false);
            // Save the user's existing unsaved scene work before recording the pre-polish snapshot.
            EditorSceneManager.SaveScene(scene);
            File.Copy(scene.path, "Validation/GenesisPolish/Before/Sandbox-with-editor-changes.unity", false);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Polish Genesis Sanctuary");
            _art = Group("Sanctuary dressing", _courtyard);
            Landscape();
            DressCourse();
            DressStations(stations);
            Lighting();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            Undo.RecordObject(player.transform, "Frame sanctuary arrival");
            player.transform.position = _courtyard.TransformPoint(new Vector3(-4, .03f, -11));
            player.transform.rotation = Quaternion.identity;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
            var camera = Camera.main;
            Undo.RecordObject(camera.transform, "Frame sanctuary arrival");
            camera.transform.SetPositionAndRotation(player.transform.position + new Vector3(0, 3.25f, -5), Quaternion.Euler(20, 0, 0));
            // Existing camera behaviour, with only its authored framing values adjusted.
            var follow = camera.GetComponent<ThirdPersonCamera>();
            Undo.RecordObject(follow, "Sanctuary camera framing");
            var settings = new SerializedObject(follow);
            settings.FindProperty("distance").floatValue = 5;
            settings.FindProperty("height").floatValue = 1.6f;
            settings.ApplyModifiedProperties();
            foreach (var label in _courtyard.GetComponentsInChildren<TextMesh>(true))
            {
                if (label.transform.IsChildOf(_art)) continue;
                Undo.RecordObject(label.gameObject, "Hide prototype label");
                label.gameObject.SetActive(false); // Contextual interaction prompts remain available.
            }
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Validation/GenesisPolish/StageA-applied.txt", DateTime.UtcNow.ToString("O") + "\n" + scene.path);
            Debug.Log("[Genesis Prototype] Sanctuary dressing saved in the existing Sandbox. Gameplay station identities and crouch colliders preserved.");
        }

        private static Transform Group(string name, Transform parent)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Add sanctuary dressing");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform Place(int index, Transform parent, Vector3 bottom, Vector3 size, float yaw = 0)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ArtPaths[index]), parent);
            Undo.RegisterCreatedObjectUndo(go, "Place imported sanctuary art");
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            go.transform.localScale = new Vector3(size.x / b.size.x, size.y / b.size.y, size.z / b.size.z);
            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Vector3 pivot = parent.TransformPoint(bottom);
            go.transform.position += pivot - new Vector3(b.center.x, b.min.y, b.center.z);
            go.transform.RotateAround(pivot, Vector3.up, yaw);
            // Dressing cannot silently change the validated movement course or line of sight.
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) { c.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(c); }
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            return go.transform;
        }

        private static void Landscape()
        {
            var backdrop = Group("Cliffs and distant hills", _art);
            for (int i = 0; i < 7; i++)
            {
                float x = -24 + i * 8;
                Place(0, backdrop, new Vector3(x, -2, 23 + (i % 2) * 3), new Vector3(9, 11 + i % 3 * 3, 10), i * 41);
                if (i % 2 == 0) Place(2, backdrop, new Vector3(x, -4, 43), new Vector3(30, 14, 28), i * 50);
            }
            var borders = Group("Trees rocks and planting", _art);
            foreach (var p in new[] { new Vector3(-20, 0, -10), new Vector3(-21, 0, 3), new Vector3(21, 0, -4), new Vector3(21, 0, 10) })
                Place(0, borders, p, new Vector3(6, 6, 8), p.z * 11);
            foreach (var p in new[] { new Vector3(-18, 0, 17), new Vector3(-12, 0, 18), new Vector3(9, 0, 18), new Vector3(18, 0, 17), new Vector3(-18, 0, -4), new Vector3(18, 0, -4) })
                Place(9, borders, p, new Vector3(6, 9, 5), p.x * 9);
            foreach (var p in new[] { new Vector3(-14,0,11), new Vector3(-14,0,3), new Vector3(-12,0,-4), new Vector3(-7,0,-4), new Vector3(-4.8f,0,6), new Vector3(-5.5f,0,9), new Vector3(2,0,11), new Vector3(14,0,12), new Vector3(15,0,-4), new Vector3(7,0,-11), new Vector3(-13,0,-12), new Vector3(-3,0,-14) })
                Place(1, borders, p, new Vector3(1.8f, .65f, 1.5f), p.x * 15);
            var paths = Group("Station paths", _art);
            Place(8, paths, new Vector3(-4, .015f, -5), new Vector3(2.6f, .025f, 16));
            Place(8, paths, new Vector3(0, .018f, .3f), new Vector3(2.4f, .025f, 12), 90);
            Place(8, paths, new Vector3(-3.8f, .015f, 5), new Vector3(2.5f, .025f, 5));
            Place(8, paths, new Vector3(3, .015f, -3), new Vector3(2.2f, .025f, 5), 90);
        }

        private static void DressCourse()
        {
            var course = Group("Ruined passage and stair dressing", _art);
            foreach (Transform t in _courtyard)
            {
                bool ceiling = t.name.StartsWith("B - Low ceiling");
                bool side = t.name.StartsWith("B - left") || t.name.StartsWith("B - right") || t.name.StartsWith("C - Narrow");
                bool step = t.name.StartsWith("D - Step") || t.name == "D - Top landing";
                if (!ceiling && !side && !step) continue;
                var renderer = t.GetComponent<Renderer>();
                Bounds bounds = renderer.bounds;
                Vector3 bottom = _courtyard.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                Place(side ? 12 : 11, course, bottom, bounds.size);
                Undo.RecordObject(renderer, "Dress existing collision surface");
                renderer.enabled = false;
            }
            Place(6, course, new Vector3(-2.25f, 0, -7), new Vector3(.5f, 2.5f, 6));
            Place(6, course, new Vector3(2.25f, 0, -7), new Vector3(.5f, 2.1f, 6));
            Place(1, course, new Vector3(0, 1.82f, -8), new Vector3(2, .55f, 2.5f));
            Sign("CROUCH PASSAGE\nC to crouch / stand", new Vector3(-1, 1.1f, -10.5f), course);
        }

        private static void DressStations(SandboxStation[] stations)
        {
            var stands = Group("Equipment and supplies", _art);
            var equipment = stations.Single(s => s.TargetId == "equipment");
            Undo.RecordObject(equipment.transform, "Arrange equipment station");
            equipment.transform.localPosition = new Vector3(-4.8f, 0, 2);
            Place(3, stands, new Vector3(-4.8f, 0, .3f), new Vector3(3.1f, 1, 1));
            Place(4, stands, new Vector3(-6.1f, 0, .3f), new Vector3(.8f,.75f,.8f));
            Place(5, stands, new Vector3(-6.3f, 0, 2), new Vector3(.7f,.9f,.7f));
            var placements = new[] { new Vector3(-4.2f,0,.1f), new Vector3(-5.3f,0,.1f), new Vector3(2.5f,0,-2), new Vector3(3.7f,0,-2), new Vector3(4.9f,0,-2) };
            string[] ids = { "iron-sword", "blackwood-shield", "healing-potion", "soul-coins", "ash-kin-ember" };
            for (int i = 0; i < ids.Length; i++)
            {
                var station = stations.Single(s => s.TargetId == "pickup-" + ids[i]);
                Undo.RecordObject(station.transform, "Arrange pickup display");
                station.transform.localPosition = placements[i];
                var pedestal = station.transform.Find("Pickup pedestal");
                if (pedestal != null) { Undo.RecordObject(pedestal.gameObject, "Replace debug pedestal"); pedestal.gameObject.SetActive(false); }
                if (i >= 2) Place(i == 3 ? 5 : 4, stands, placements[i], new Vector3(.85f,.6f,.85f));
            }
            Place(4, stands, new Vector3(6, 0, -2.3f), new Vector3(.8f,.9f,.8f));
            Sign("EQUIPMENT", new Vector3(-4.8f, 1.8f, 2.1f), stands);
            Sign("SUPPLIES", new Vector3(3.7f, 1.5f, -1.5f), stands);
            var shrine = stations.Single(s => s.TargetId == "observer-shrine");
            Place(11, stands, shrine.transform.localPosition + new Vector3(0, .005f, 0), new Vector3(3.5f, .04f, 3));
            foreach (var p in new[] { new Vector3(-6,0,7), new Vector3(-2,0,7), new Vector3(-6.8f,0,.5f), new Vector3(5.9f,0,-1.6f) })
            {
                Place(7, stands, p, new Vector3(.25f,.8f,.25f));
                var lamp = Group("Warm station light", stands);
                lamp.localPosition = p + Vector3.up * .75f;
                var light = Undo.AddComponent<Light>(lamp.gameObject);
                light.type = LightType.Point; light.color = new Color(1,.7f,.36f); light.range = 3; light.intensity = 1.4f; light.shadows = LightShadows.None;
            }
        }

        private static void Sign(string text, Vector3 position, Transform parent)
        {
            var go = Group(text.Split('\n')[0] + " sign", parent);
            go.localPosition = position;
            var mesh = Undo.AddComponent<TextMesh>(go.gameObject);
            mesh.text = text; mesh.fontSize = 48; mesh.characterSize = .033f;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(1,.87f,.58f);
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;
            // Fixed facing south toward the approach, not a floating camera-facing debug label.
        }

        private static void Lighting()
        {
            var sun = GameObject.Find("Directional Light").GetComponent<Light>();
            Undo.RecordObjects(new UnityEngine.Object[] { sun, sun.transform }, "Sanctuary daylight");
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            sun.color = new Color(1,.94f,.81f); sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.58f,.68f,.8f);
            RenderSettings.ambientEquatorColor = new Color(.4f,.43f,.4f);
            RenderSettings.ambientGroundColor = new Color(.22f,.24f,.2f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.57f,.69f,.76f); RenderSettings.fogStartDistance = 45; RenderSettings.fogEndDistance = 145;
        }
    }
}
