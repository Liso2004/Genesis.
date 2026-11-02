using System;
using System.IO;
using System.Linq;
using Genesis.Authorities;
using Genesis.Character;
using Genesis.Creation;
using Genesis.Items;
using Genesis.Save;
using Genesis.Skill.Definitions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Genesis.Sandbox.Editor
{
    public static class SandboxSaveSetup
    {
        [MenuItem("Genesis/Prototype/5 Configure Sandbox Save Load")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!="Assets/Scenes/Sandbox/Sandbox.unity")throw new InvalidOperationException("Open the existing Sandbox in Edit Mode.");
            var player=Object.FindFirstObjectByType<PlayerController>();
            if(player==null||player.GetComponent<SandboxSaveController>()!=null)throw new InvalidOperationException("Player missing or save/load already configured.");
            if(string.IsNullOrWhiteSpace(player.GetComponent<GenesisActor>().ActorId))throw new InvalidOperationException("The scene player needs a stable authored actor ID.");
            Directory.CreateDirectory("Validation/GenesisSave/Before");
            EditorSceneManager.SaveScene(scene);File.Copy(scene.path,"Validation/GenesisSave/Before/Sandbox.unity",false);
            const string path="Assets/Content/Creation/SandboxSaveCatalog.asset";
            var catalog=AssetDatabase.LoadAssetAtPath<SaveCatalog>(path);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<SaveCatalog>();AssetDatabase.CreateAsset(catalog,path);}
            catalog.creation=AssetDatabase.LoadAssetAtPath<CreationDefinition>("Assets/Content/Creation/GenesisCreation.asset");
            catalog.items=AssetDatabase.FindAssets("t:ItemDefinition",new[]{"Assets/Content"}).Select(g=>AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            catalog.skills=AssetDatabase.FindAssets("t:SkillDefinition",new[]{"Assets/Content"}).Select(g=>new SavedSkillBinding{skill=AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g)),progression=catalog.creation.progression}).ToArray();
            catalog.authorities=AssetDatabase.FindAssets("t:AuthorityDefinition",new[]{"Assets/Content"}).Select(g=>AssetDatabase.LoadAssetAtPath<AuthorityDefinition>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            catalog.Validate();EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();
            var save=Undo.AddComponent<SandboxSaveController>(player.gameObject);SandboxSystemsAssets.Set(save,"catalog",catalog);
            var stations=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SandboxStation>(true)).ToArray();
            if(stations.Any(s=>string.IsNullOrWhiteSpace(s.TargetId))||stations.Select(s=>s.TargetId).Distinct().Count()!=stations.Length)throw new InvalidOperationException("Station IDs must be stable and unique.");
            SandboxSystemsAssets.References(save,"stations",stations.Cast<Object>().ToArray());
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("[Genesis Save] Existing Sandbox configured. F5 saves; F9 explicitly restores. Nothing loads automatically.");
        }
    }
}
