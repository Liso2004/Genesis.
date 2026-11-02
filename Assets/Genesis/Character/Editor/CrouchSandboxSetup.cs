using System;
using System.Linq;
using Genesis.NPC;
using Genesis.Skill;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genesis.Character.Editor
{
    /// <summary>Explicit authoring command for the existing Sandbox; never runs on import.</summary>
    public static class CrouchSandboxSetup
    {
        private const string PlayerPath = PlayerLocomotionMigration.PrefabPath;
        private const string ControllerPath = PlayerLocomotionMigration.ControllerPath;
        private const string Kingdom = "Assets/PolygonFantasyKingdom/Prefabs/";
        private const string Dungeon = "Assets/PolygonDungeonRealms/Prefabs/Environments/";
        private const string CrouchClips = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Crouch/";
        private static Transform _root;
        private static Material _stone;
        private static Material _marker;

        public static void Apply()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Sandbox/Sandbox.unity" || EditorApplication.isPlaying)
                throw new InvalidOperationException("Open the existing Sandbox in Edit mode first.");
            if (scene.GetRootGameObjects().Any(g => g.name == "Genesis Crouch Courtyard"))
                throw new InvalidOperationException("Courtyard already exists; edit its objects rather than duplicating it.");
            var player = Object.FindFirstObjectByType<PlayerController>();
            var setup = Object.FindFirstObjectByType<NPC_Setup>();
            if (player == null || setup == null) throw new InvalidOperationException("Existing player and perception setup required.");
            var source = new SerializedObject(setup);
            Object definition = source.FindProperty("perceptionDefinition").objectReferenceValue;
            Object progression = source.FindProperty("perceptionProgression").objectReferenceValue;
            if (definition == null || progression == null) throw new InvalidOperationException("Perception assets must be assigned.");
            ConfigureAnimator();
            ConfigurePlayer(definition, progression);
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Add Genesis crouch courtyard");
            var root = new GameObject("Genesis Crouch Courtyard");
            Undo.RegisterCreatedObjectUndo(root, "Add crouch courtyard");
            _root = root.transform;
            _root.position = new Vector3(45, -2.16f, 10);
            _stone = MakeMaterial("Courtyard Stone", new Color(0.32f, 0.36f, 0.38f));
            _marker = MakeMaterial("Crouch Test Marker", new Color(0.22f, 0.65f, 0.56f));
            Box("Courtyard ground", new Vector3(0, -0.1f, 0), new Vector3(36, 0.2f, 34));
            // Imported art is sized from its actual renderer bounds, including off-centre pivots.
            Place(Kingdom + "Castle/SM_Bld_Castle_Wall_Gate_01.prefab", new Vector3(0, 0, 14), new Vector3(5.5f, 5, 0.6f));
            Place(Kingdom + "Castle/SM_Bld_Castle_Wall_01.prefab", new Vector3(-8, 0, 14), new Vector3(10, 5, 0.6f));
            Place(Kingdom + "Castle/SM_Bld_Castle_Wall_01.prefab", new Vector3(8, 0, 14), new Vector3(10, 5, 0.6f));
            Room("Enterable house", new Vector3(-10, 0, 7), 7, 6, 3, Kingdom + "House/SM_Bld_House_Wall_01.prefab");
            Room("Castle interior", new Vector3(9, 0, 7), 7, 7, 3.4f, Kingdom + "Castle/SM_Bld_Castle_Wall_01.prefab");
            Place(Kingdom + "Castle/SM_Bld_Castle_Hoarding_Wood_Roof_01.prefab", new Vector3(-10, 3.05f, 7), new Vector3(7.5f, 1.1f, 6.5f));
            Box("Castle interior ceiling", new Vector3(9, 3.55f, 7), new Vector3(7.2f, 0.3f, 7.2f));
            Place(Dungeon + "SM_Env_Dwarf_Wall_DoorFrame_Double_01.prefab", new Vector3(12, 0, -9), new Vector3(4, 3.3f, 0.5f));
            Box("Dungeon corridor left", new Vector3(10.3f, 1.5f, -12), new Vector3(0.4f, 3, 6));
            Box("Dungeon corridor right", new Vector3(13.7f, 1.5f, -12), new Vector3(0.4f, 3, 6));
            Box("Dungeon corridor ceiling", new Vector3(12, 3.1f, -12), new Vector3(3.8f, 0.2f, 6));
            Box("Dungeon corridor end", new Vector3(12, 1.5f, -15), new Vector3(3.8f, 3, 0.3f));
            Place(Dungeon + "SM_Env_Dwarf_Floor_01.prefab", new Vector3(12, 0, -12), new Vector3(3, 0.04f, 6));
            Place(Kingdom + "Props/SM_Prop_Market_Stall_01.prefab", new Vector3(-12, 0, -1), new Vector3(3, 2.7f, 3));
            Place(Kingdom + "Props/SM_Prop_Market_Stall_02.prefab", new Vector3(-7, 0, -1), new Vector3(3, 2.7f, 3));
            foreach (var p in new[] { new Vector3(-16,0,12), new Vector3(16,0,12), new Vector3(-16,0,-12) })
                Place(Kingdom + "Environments/SM_Env_Tree_Round_01.prefab", p, new Vector3(3,5,3));
            Place(Dungeon + "SM_Env_Rock_Round_01.prefab", new Vector3(16,0,-9), new Vector3(3,2,3));
            Place(Kingdom + "Environments/SM_Env_Path_01.prefab", new Vector3(0,0.01f,5), new Vector3(3,0.03f,16));
            Place(Kingdom + "Castle/SM_Bld_Stairs_Straight_01.prefab", new Vector3(15,0,2), new Vector3(2.8f,1.2f,3));
            BuildCrouchCourse();
            AddNPC("Merchant", Kingdom + "Characters/SM_Chr_Merchant_01.prefab", new Vector3(-10,0,1), player, definition, progression);
            AddNPC("Soldier", Kingdom + "Characters/SM_Chr_Soldier_Male_01.prefab", new Vector3(3,0,12), player, definition, progression);
            AddNPC("Blacksmith", Kingdom + "Characters/SM_Chr_Blacksmith_Male_01.prefab", new Vector3(-6,0,3), player, definition, progression);
            Label("COURTYARD / C toggles crouch", new Vector3(0,0.04f,-15));
            // A visible connecting trail leads from the existing play area to the addition.
            Place(Kingdom + "Environments/SM_Env_Path_01.prefab", new Vector3(-27,0, -10), new Vector3(18,0.03f,2));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Genesis Crouch] Existing Sandbox extended; player, crouch course and three fantasy NPCs saved.");
        }

        private static void ConfigureAnimator()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (!controller.parameters.Any(p => p.name == "IsCrouching")) controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            if (machine.states.Any(s => s.state.name == "Crouch")) throw new InvalidOperationException("Crouch graph already exists.");
            AnimatorState locomotion = machine.states.Single(s => s.state.name == "Locomotion").state;
            AnimatorState fall = machine.states.Single(s => s.state.name == "Fall").state;
            AnimatorState land = machine.states.Single(s => s.state.name == "Land").state;
            var crouch = machine.AddState("Crouch", new Vector3(250, 320));
            crouch.writeDefaultValues = false;
            crouch.iKOnFeet = true;
            var tree = new BlendTree { name="Crouch Speed", blendType=BlendTreeType.Simple1D, blendParameter="Speed", useAutomaticThresholds=false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(Clip(CrouchClips + "HumanM@Crouch01_Idle.fbx"),0);
            tree.AddChild(Clip(CrouchClips + "CrouchWalk/HumanM@Crouch01_Walk_Forward.fbx"),0.8f);
            // Match the supplied in-place gait to its measured RM reference.
            var children = tree.children;
            children[1].timeScale = 0.8f / Clip(CrouchClips + "CrouchWalk/RootMotion/HumanM@Crouch01_Walk_Forward [RM].fbx").averageSpeed.magnitude;
            tree.children = children;
            crouch.motion = tree;
            var enter = Transition(locomotion,crouch);
            enter.AddCondition(AnimatorConditionMode.If,0,"IsCrouching");
            enter.AddCondition(AnimatorConditionMode.If,0,"IsGrounded");
            var exit = Transition(crouch,locomotion);
            exit.AddCondition(AnimatorConditionMode.IfNot,0,"IsCrouching");
            exit.AddCondition(AnimatorConditionMode.If,0,"IsGrounded");
            Transition(crouch,fall).AddCondition(AnimatorConditionMode.IfNot,0,"IsGrounded");
            // Land directly back into the matching posture; prevent simultaneous standing exits.
            foreach(var transition in land.transitions)
                if(transition.destinationState == locomotion) transition.AddCondition(AnimatorConditionMode.IfNot,0,"IsCrouching");
            var recover = Transition(land,crouch);
            recover.AddCondition(AnimatorConditionMode.If,0,"IsGrounded");
            recover.AddCondition(AnimatorConditionMode.If,0,"IsCrouching");
            EditorUtility.SetDirty(controller);
        }
        private static AnimationClip Clip(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c=>!c.name.StartsWith("__preview__") && c.isHumanMotion);
        private static AnimatorStateTransition Transition(AnimatorState source, AnimatorState destination)
        {
            var t=source.AddTransition(destination);t.hasExitTime=false;t.hasFixedDuration=true;t.duration=0.1f;
            t.canTransitionToSelf=false;t.interruptionSource=TransitionInterruptionSource.Destination;return t;
        }
        private static void ConfigurePlayer(Object definition, Object progression)
        {
            var prefab=PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                if(prefab.GetComponent<SkillSystem>()==null)prefab.AddComponent<SkillSystem>();
                var setup=prefab.GetComponent<NPC_Setup>() ?? prefab.AddComponent<NPC_Setup>();
                Set(setup,"perceptionDefinition",definition);Set(setup,"perceptionProgression",progression);
                var motor=prefab.GetComponent<PlayerController>();var data=new SerializedObject(motor);
                data.FindProperty("crouchSpeed").floatValue=0.8f;data.FindProperty("crouchedHeight").floatValue=1.25f;data.ApplyModifiedPropertiesWithoutUndo();
                var animator=prefab.GetComponentInChildren<Animator>();animator.applyRootMotion=false;
                var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
                if(hips.Find("PresentationWeapon")==null)
                {
                    var mount=new GameObject("PresentationWeapon").transform;mount.SetParent(hips,false);
                    mount.localPosition=new Vector3(-0.18f,0,-0.08f);mount.localRotation=Quaternion.Euler(0,0,165);
                    var sword=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Synty/PolygonFantasyHeroCharacters/Prefabs/Weapons/SM_Wep_Sword_01.prefab"),mount);
                    sword.transform.localScale=Vector3.one*0.7f;
                }
                PrefabUtility.SaveAsPrefabAsset(prefab,PlayerPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
        }
        private static void Set(Object target,string property,Object value)
        {var data=new SerializedObject(target);data.FindProperty(property).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
        private static void AddNPC(string name,string path,Vector3 location,PlayerController player,Object definition,Object progression)
        {
            var go=new GameObject("NPC_"+name);go.transform.SetParent(_root,false);go.transform.localPosition=location+Vector3.up*0.05f;
            var actor=go.AddComponent<GenesisActor>();var actorData=new SerializedObject(actor);actorData.FindProperty("actorType").enumValueIndex=(int)ActorType.NPC;actorData.ApplyModifiedPropertiesWithoutUndo();
            var capsule=go.AddComponent<CharacterController>();capsule.height=1.9f;capsule.radius=0.25f;capsule.center=Vector3.up*0.95f;capsule.skinWidth=0.03f;
            go.AddComponent<SkillSystem>();var setup=go.AddComponent<NPC_Setup>();Set(setup,"perceptionDefinition",definition);Set(setup,"perceptionProgression",progression);
            var perception=go.AddComponent<NPCPerception>();Set(perception,"target",player.transform);go.AddComponent<NPCController>();
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),go.transform);
            var animator=visual.GetComponentInChildren<Animator>();if(animator==null || animator.avatar==null || !animator.avatar.isHuman)throw new InvalidOperationException(path+" needs a Humanoid Avatar");
            animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);animator.applyRootMotion=false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);go.AddComponent<NPCAnimation>();
        }
        private static Material MakeMaterial(string name,Color color)
        {
            const string folder="Assets/Genesis/Character/Materials";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Genesis/Character","Materials");
            string path=folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);return mat;
        }
        private static GameObject Box(string name,Vector3 center,Vector3 size,bool marker=false)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(_root,false);go.transform.localPosition=center;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=marker?_marker:_stone;return go;}
        private static GameObject Place(string path,Vector3 bottom,Vector3 size)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null)throw new InvalidOperationException("Missing inspected asset: "+path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset,_root);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;go.transform.localScale=Vector3.one;
            var renderers=go.GetComponentsInChildren<Renderer>();var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            go.transform.localScale=new Vector3(size.x/Mathf.Max(b.size.x,0.01f),size.y/Mathf.Max(b.size.y,0.01f),size.z/Mathf.Max(b.size.z,0.01f));
            b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            go.transform.position+=_root.TransformPoint(bottom)-new Vector3(b.center.x,b.min.y,b.center.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);return go;
        }
        private static GameObject SideWall(string path,Vector3 bottom,float width,float height)
        {
            var go=Place(path,bottom,new Vector3(width,height,0.3f));
            go.transform.RotateAround(_root.TransformPoint(bottom),Vector3.up,90);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);return go;
        }
        private static void Room(string name,Vector3 center,float width,float depth,float height,string wall)
        {
            Place(wall,center+new Vector3(0,0,depth/2),new Vector3(width,height,0.3f)).name=name+" back";
            SideWall(wall,center+new Vector3(-width/2,0,0),depth,height).name=name+" left";
            SideWall(wall,center+new Vector3(width/2,0,0),depth,height).name=name+" right";
            float side=(width-2f)/2;
            Place(wall,center+new Vector3(-(width+2f)/4,0,-depth/2),new Vector3(side,height,0.3f));
            Place(wall,center+new Vector3((width+2f)/4,0,-depth/2),new Vector3(side,height,0.3f));
            Place(Dungeon+"SM_Env_Dwarf_Floor_01.prefab",center,new Vector3(width,0.04f,depth));
        }
        private static void BuildCrouchCourse()
        {
            Box("A - Standing approach",new Vector3(-4,0.015f,-7),new Vector3(3,0.03f,4),true);
            Box("B - Low ceiling (1.42 m clearance)",new Vector3(0,1.62f,-7),new Vector3(3,0.4f,6));
            Box("B - left support",new Vector3(-1.6f,0.8f,-7),new Vector3(0.2f,1.6f,6));
            Box("B - right support",new Vector3(1.6f,0.8f,-7),new Vector3(0.2f,1.6f,6));
            Box("C - Narrow passage left",new Vector3(4.4f,1,-7),new Vector3(0.2f,2,5));
            Box("C - Narrow passage right",new Vector3(5.6f,1,-7),new Vector3(0.2f,2,5));
            for(int i=0;i<6;i++)Box("D - Step "+(i+1),new Vector3(-10,(i+1)*0.09f,-10+i*0.45f),new Vector3(2,(i+1)*0.18f,0.45f));
            Box("D - Top landing",new Vector3(-10,0.54f,-6.75f),new Vector3(2,1.08f,1.5f));
            Label("A / STAND",new Vector3(-4,0.05f,-10));Label("B / CROUCH - C",new Vector3(0,0.05f,-11));
            Label("C / NARROW",new Vector3(5,0.05f,-10));Label("D / STAIRS",new Vector3(-10,0.05f,-12));Label("E / OPEN",new Vector3(0,0.05f,0));
        }
        private static void Label(string text,Vector3 position)
        {
            var go=new GameObject(text);go.transform.SetParent(_root,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(90,0,0);
            var mesh=go.AddComponent<TextMesh>();mesh.text=text;mesh.anchor=TextAnchor.MiddleCenter;mesh.alignment=TextAlignment.Center;mesh.characterSize=0.12f;mesh.fontSize=40;mesh.color=Color.white;
            mesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");go.GetComponent<MeshRenderer>().sharedMaterial=mesh.font.material;
        }
    }
}
