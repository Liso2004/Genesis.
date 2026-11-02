using System;
using System.Linq;
using Genesis.Combat;
using Genesis.Items;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genesis.Character.Editor
{
    /// <summary>Builds the sword animation layer and wires it to the canonical player prefab.</summary>
    public static class SwordCombatSetup
    {
        private const string ControllerPath = "Assets/Genesis/Character/Animatinos/PlayerAnimator.controller";
        private const string MaskPath = "Assets/Genesis/Character/AnimationMasks/SwordCombat.mask";
        private const string PrefabPath = "Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab";
        private const string SwordAnimations = "Assets/Synty/AnimationSwordCombat/Animations/Polygon/";
        private const string LayerName = "Sword Combat";

        [MenuItem("Genesis/Character/Setup Sword Combat Animations")]
        public static void Apply()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) throw new InvalidOperationException("Player animator controller was not found.");

            AvatarMask mask = CreateOrLoadMask();
            ConfigureController(controller, mask);
            ConfigurePlayer(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Genesis] Sword attacks, combo, armed stance, and block animations are set up on GenesisPlayer.");
        }

        private static AvatarMask CreateOrLoadMask()
        {
            const string folder = "Assets/Genesis/Character/AnimationMasks";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Genesis/Character", "AnimationMasks");

            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
            if (mask == null)
            {
                mask = new AvatarMask { name = "Sword Combat Upper Body" };
                AssetDatabase.CreateAsset(mask, MaskPath);
            }

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
                mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            EditorUtility.SetDirty(mask);
            return mask;
        }

        private static void ConfigureController(AnimatorController controller, AvatarMask mask)
        {
            EnsureParameter(controller, "HasSword", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "SwordLightAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "SwordLightComboIndex", AnimatorControllerParameterType.Int);
            EnsureParameter(controller, "SwordHeavyAttack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, "SwordBlocking", AnimatorControllerParameterType.Bool);

            for (int i = controller.layers.Length - 1; i >= 1; i--)
                if (controller.layers[i].name == LayerName) controller.RemoveLayer(i);

            controller.AddLayer(LayerName);
            var layers = controller.layers;
            int layerIndex = layers.Length - 1;
            layers[layerIndex].avatarMask = mask;
            layers[layerIndex].defaultWeight = 1f;
            layers[layerIndex].blendingMode = AnimatorLayerBlendingMode.Override;
            controller.layers = layers;

            AnimatorStateMachine machine = controller.layers[layerIndex].stateMachine;
            machine.name = LayerName;
            ClearStateMachine(machine);

            AnimatorState empty = machine.AddState("Locomotion Overlay", new Vector3(250, 100));
            AnimatorState armedIdle = machine.AddState("Sword Ready", new Vector3(520, 100));
            armedIdle.motion = Clip("Idle/Base/A_Idle_Base_Sword");

            AnimatorState[] combo =
            {
                machine.AddState("Light Attack A", new Vector3(250, 300)),
                machine.AddState("Light Attack B", new Vector3(520, 300)),
                machine.AddState("Light Attack C", new Vector3(790, 300))
            };
            combo[0].motion = Clip("Attack/LightCombo01/A_Attack_LightCombo01A_Sword");
            combo[1].motion = Clip("Attack/LightCombo01/A_Attack_LightCombo01B_Sword");
            combo[2].motion = Clip("Attack/LightCombo01/A_Attack_LightCombo01C_Sword");

            AnimatorState heavy = machine.AddState("Heavy Stab", new Vector3(520, 500));
            heavy.motion = Clip("Attack/HeavyStab01/A_Attack_HeavyStab01_Sword");
            AnimatorState blockBegin = machine.AddState("Block Begin", new Vector3(1050, 100));
            blockBegin.motion = Clip("Block/A_Block_Begin_Sword");
            AnimatorState blockLoop = machine.AddState("Block Hold", new Vector3(1300, 100));
            blockLoop.motion = Clip("Block/A_Block_Loop_Sword");
            AnimatorState blockEnd = machine.AddState("Block End", new Vector3(1550, 100));
            blockEnd.motion = Clip("Block/A_Block_End_Sword");
            machine.defaultState = empty;

            Transition(empty, armedIdle).AddCondition(AnimatorConditionMode.If, 0f, "HasSword");
            Transition(armedIdle, empty).AddCondition(AnimatorConditionMode.IfNot, 0f, "HasSword");

            for (int i = 0; i < combo.Length; i++)
            {
                AnimatorStateTransition attack = machine.AddAnyStateTransition(combo[i]);
                ConfigureTransition(attack, 0.06f, false);
                attack.AddCondition(AnimatorConditionMode.If, 0f, "HasSword");
                attack.AddCondition(AnimatorConditionMode.If, 0f, "SwordLightAttack");
                attack.AddCondition(AnimatorConditionMode.Equals, i, "SwordLightComboIndex");
                ReturnTo(Transition(combo[i], armedIdle));
            }

            AnimatorStateTransition heavyAttack = machine.AddAnyStateTransition(heavy);
            ConfigureTransition(heavyAttack, 0.06f, false);
            heavyAttack.AddCondition(AnimatorConditionMode.If, 0f, "HasSword");
            heavyAttack.AddCondition(AnimatorConditionMode.If, 0f, "SwordHeavyAttack");
            ReturnTo(Transition(heavy, armedIdle));

            // Use explicit source states so a held block cannot restart its own
            // begin animation every frame through an Any State transition.
            foreach (AnimatorState source in new[] { armedIdle, combo[0], combo[1], combo[2], heavy, blockEnd })
            {
                AnimatorStateTransition enterBlock = Transition(source, blockBegin);
                enterBlock.AddCondition(AnimatorConditionMode.If, 0f, "SwordBlocking");
            }
            var toLoop = Transition(blockBegin, blockLoop);
            toLoop.hasExitTime = true;
            toLoop.exitTime = 0.9f;
            toLoop.duration = 0.08f;
            var release = Transition(blockLoop, blockEnd);
            release.AddCondition(AnimatorConditionMode.IfNot, 0f, "SwordBlocking");
            ReturnTo(Transition(blockEnd, armedIdle));

            foreach (ChildAnimatorState state in machine.states)
                state.state.writeDefaultValues = false;
            EditorUtility.SetDirty(controller);
        }

        private static AnimationClip Clip(string relativePath)
        {
            string path = SwordAnimations + relativePath + ".fbx";
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal));
            if (clip == null) throw new InvalidOperationException("No animation clip was imported from " + path);
            if (!clip.isHumanMotion) throw new InvalidOperationException(path + " did not import as a Humanoid animation.");
            return clip;
        }

        private static void ConfigurePlayer(AnimatorController controller)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Animator animator = prefab.GetComponentInChildren<Animator>(true);
                if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                    throw new InvalidOperationException("GenesisPlayer needs its valid Humanoid Avatar before sword animations can be wired.");
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                PlayerSwordCombat combat = prefab.GetComponent<PlayerSwordCombat>();
                if (combat == null) combat = prefab.AddComponent<PlayerSwordCombat>();
                SetReference(combat, "animator", animator);
                SetReference(combat, "inventory", prefab.GetComponent<ActorInventory>());
                SetReference(combat, "movementInput", prefab.GetComponent<PlayerMovementInput>());
                PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;
            for (int i = parameters.Length - 1; i >= 0; i--)
                if (parameters[i].name == name) controller.RemoveParameter(i);
            controller.AddParameter(name, type);
        }

        private static void ClearStateMachine(AnimatorStateMachine machine)
        {
            foreach (ChildAnimatorState state in machine.states) machine.RemoveState(state.state);
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
            foreach (AnimatorTransition transition in machine.entryTransitions) machine.RemoveEntryTransition(transition);
            machine.defaultState = null;
        }

        private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            ConfigureTransition(transition, 0.08f, false);
            return transition;
        }

        private static void ConfigureTransition(AnimatorStateTransition transition, float duration, bool exitTime)
        {
            transition.hasExitTime = exitTime;
            transition.hasFixedDuration = true;
            transition.duration = duration;
            transition.canTransitionToSelf = false;
            transition.interruptionSource = TransitionInterruptionSource.Destination;
            transition.orderedInterruption = false;
        }

        private static void ReturnTo(AnimatorStateTransition transition)
        {
            transition.hasExitTime = true;
            transition.exitTime = 0.94f;
            transition.duration = 0.1f;
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            SerializedObject data = new SerializedObject(target);
            data.FindProperty(propertyName).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
