using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genesis.Character.Editor
{
    /// <summary>Explicit batch migration. Never runs automatically in an open Editor.</summary>
    public static class PlayerLocomotionMigration
    {
        public const string ControllerPath = "Assets/Genesis/Character/Animatinos/PlayerAnimator.controller";
        public const string PrefabPath = "Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab";
        private const string Animations = "Assets/Kevin Iglesias/Human Animations/Animations/Male/";
        private const string Sandbox = "Assets/Scenes/Sandbox/Sandbox.unity";
        private const string Exterior = "Assets/PolygonFantasyKingdom/Scenes/Demo_ExteriorOnly_Optimized.unity";

        public static void RunBatch()
        {
            // Avoid overwriting an established prefab on an accidental rerun.
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("Migration already applied; use the existing prefab.");
            BuildAnimator();
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(Sandbox);
            PlayerController motor = Object.FindFirstObjectByType<PlayerController>();
            Configure(motor);
            var motorData = new SerializedObject(motor);
            Transform sceneCamera = motorData.FindProperty("cameraTransform").objectReferenceValue as Transform;
            SetReference(motor, "cameraTransform", null);
            // A prefab instance must acquire its identity at spawn, not clone the scene player's ID.
            var actor = motor.GetComponent<GenesisActor>();
            string sceneActorId = actor.ActorId;
            SetString(actor, "actorId", "");
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(motor.gameObject, PrefabPath, InteractionMode.AutomatedAction);
            SetReference(motor, "cameraTransform", sceneCamera);
            SetString(actor, "actorId", sceneActorId);
            PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            EditorSceneManager.SaveScene(motor.gameObject.scene);

            EditorSceneManager.OpenScene(Exterior);
            motor = Object.FindFirstObjectByType<PlayerController>();
            Configure(motor);
            PrefabUtility.ConvertToPrefabInstance(motor.gameObject, prefab,
                new ConvertToPrefabInstanceSettings
                {
                    objectMatchMode = ObjectMatchMode.ByHierarchy,
                    recordPropertyOverridesOfMatches = true,
                    componentsNotMatchedBecomesOverride = true,
                    gameObjectsNotMatchedBecomesOverride = true,
                    changeRootNameToAssetName = false
                }, InteractionMode.AutomatedAction);
            EditorSceneManager.SaveScene(motor.gameObject.scene);
            AssetDatabase.SaveAssets();
            ValidateAssets();
        }

        private static void Configure(PlayerController motor)
        {
            if (motor == null) throw new InvalidOperationException("Expected one existing player.");
            var animator = motor.GetComponentInChildren<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new InvalidOperationException("Existing hero must have a valid Humanoid Avatar.");
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            animator.applyRootMotion = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
            // Only the inspected, redundant player mesh colliders; no environment colliders are touched.
            foreach (var collider in motor.GetComponentsInChildren<MeshCollider>(true)) Object.DestroyImmediate(collider);
            var orphanRenderer = motor.GetComponent<MeshRenderer>();
            if (orphanRenderer != null && motor.GetComponent<MeshFilter>() == null) Object.DestroyImmediate(orphanRenderer);
            if (motor.GetComponent<PlayerMovementInput>() == null) motor.gameObject.AddComponent<PlayerMovementInput>();
            var presentation = motor.GetComponent<PlayerAnimation>();
            if (presentation == null) presentation = motor.gameObject.AddComponent<PlayerAnimation>();
            SetReference(presentation, "animator", animator);
            var data = new SerializedObject(motor);
            data.FindProperty("walkSpeed").floatValue = 1.6f;
            data.FindProperty("runSpeed").floatValue = 4f;
            data.FindProperty("sprintSpeed").floatValue = 6f;
            data.FindProperty("rotationSpeed").floatValue = 18f;
            data.FindProperty("groundCheckDistance").floatValue = 0.08f;
            data.ApplyModifiedPropertiesWithoutUndo();
            var capsule = motor.GetComponent<CharacterController>();
            capsule.center = Vector3.up * (capsule.height * 0.5f);
            capsule.skinWidth = 0.03f;
            capsule.minMoveDistance = 0f;
        }

        private static AnimationClip Clip(string relative)
        {
            string path = Animations + relative + ".fbx";
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Single(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
            if (!clip.isHumanMotion) throw new InvalidOperationException(path + " is not Humanoid");
            Debug.Log($"[Genesis migration] {path}: {clip.name}, duration={clip.length}, loop={clip.isLooping}, averageSpeed={clip.averageSpeed}");
            return clip;
        }

        private static void BuildAnimator()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            // The audited old controller contains only locomotion plus an empty layer, no combat.
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
                if (asset != controller) Object.DestroyImmediate(asset, true);
            controller.parameters = Array.Empty<AnimatorControllerParameter>();
            controller.layers = Array.Empty<AnimatorControllerLayer>();
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddLayer("Base Layer");
            var layers = controller.layers;
            layers[0].defaultWeight = 1f;
            controller.layers = layers;
            var machine = controller.layers[0].stateMachine;
            var locomotion = machine.AddState("Locomotion", new Vector3(250, 100));
            var tree = new BlendTree { name = "Locomotion Speed", blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(tree, controller);
            tree.AddChild(Clip("Idles/HumanM@Idle01"), 0f);
            tree.AddChild(Clip("Movement/Walk/HumanM@Walk01_Forward"), 1.6f);
            tree.AddChild(Clip("Movement/Run/HumanM@Run01_Forward"), 4f);
            tree.AddChild(Clip("Movement/Sprint/HumanM@Sprint01_Forward"), 6f);
            // The matching walk RM FBX travels 1.6 m in 0.8 s (2 m/s).
            // Match the authored in-place cadence to the motor's 1.6 m/s walk.
            var children = tree.children;
            children[1].timeScale = 0.8f;
            tree.children = children;
            locomotion.motion = tree;
            var jump = machine.AddState("Jump", new Vector3(500, 0));
            jump.motion = Clip("Movement/Jump/HumanM@Jump01 - Begin");
            var fall = machine.AddState("Fall", new Vector3(750, 100));
            fall.motion = Clip("Movement/Jump/HumanM@Fall01");
            var land = machine.AddState("Land", new Vector3(500, 250));
            land.motion = Clip("Movement/Jump/HumanM@Jump01 - Land");
            machine.defaultState = locomotion;
            foreach (var state in machine.states)
            {
                state.state.writeDefaultValues = false;
                state.state.iKOnFeet = true;
            }
            foreach (var source in new[] { locomotion, land })
            {
                var rising = Transition(source, jump);
                rising.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
                rising.AddCondition(AnimatorConditionMode.Greater, 0, "VerticalVelocity");
                var falling = Transition(source, fall);
                falling.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
                falling.AddCondition(AnimatorConditionMode.Less, 0.01f, "VerticalVelocity");
            }
            var apex = Transition(jump, fall);
            apex.AddCondition(AnimatorConditionMode.Less, 0.01f, "VerticalVelocity");
            apex.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            foreach (var source in new[] { jump, fall })
                Transition(source, land).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            // Moving landings never lock the feet in a stationary landing pose.
            var moving = Transition(land, locomotion);
            moving.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            moving.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            var recovery = Transition(land, locomotion);
            recovery.hasExitTime = true;
            recovery.exitTime = 0.8f;
            recovery.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to)
        {
            var transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
            // Only destination transitions may interrupt; rechecking the source can restart
            // a still-true takeoff transition every frame and prevent it completing.
            transition.interruptionSource = TransitionInterruptionSource.Destination;
            transition.orderedInterruption = false;
            return transition;
        }

        private static void SetReference(Object target, string name, Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(name).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string name, string value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(name).stringValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void ValidateAssets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null || prefab.GetComponentsInChildren<PlayerController>(true).Length != 1)
                throw new InvalidOperationException("Missing or duplicated player motor");
            var animator = prefab.GetComponentInChildren<Animator>(true);
            if (!animator.avatar.isHuman || !animator.avatar.isValid || animator.applyRootMotion)
                throw new InvalidOperationException("Invalid avatar or root motion configuration");
            var controller = animator.runtimeAnimatorController;
            if (controller == null || controller.animationClips.Any(c => c == null || !c.isHumanMotion))
                throw new InvalidOperationException("Invalid animation references");
            foreach (string path in new[] { Sandbox, Exterior })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var motor = Object.FindFirstObjectByType<PlayerController>();
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(motor.gameObject) != PrefabPath)
                    throw new InvalidOperationException(path + " does not instantiate the canonical prefab");
                var camera = Object.FindFirstObjectByType<ThirdPersonCamera>();
                if (new SerializedObject(camera).FindProperty("target").objectReferenceValue != motor.transform)
                    throw new InvalidOperationException(path + " camera target changed");
                foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null && component.GetType().Name == "NPCPerception" &&
                        new SerializedObject(component).FindProperty("target").objectReferenceValue != motor.transform)
                        throw new InvalidOperationException("NPC target changed");
            }
            Debug.Log("[Genesis migration] Prefab, Avatar, clips, camera and NPC references PASS");
        }
    }
}
