#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Genesis.Character;
using Genesis.Core;
using Genesis.Skill;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Genesis.Tests
{
    public sealed class PlayerCrouchTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private const float Dt=1f/60f;
        [SetUp] public void Setup()
        {
            if(GenesisRuntime.Instance==null){var go=new GameObject("Crouch test runtime");_objects.Add(go);go.AddComponent<GenesisRuntime>();}
        }
        [TearDown] public void Cleanup(){for(int i=_objects.Count-1;i>=0;i--)Object.DestroyImmediate(_objects[i]);_objects.Clear();}
        private GameObject Box(Vector3 position,Vector3 scale,bool trigger=false)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);_objects.Add(go);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Collider>().isTrigger=trigger;return go;}
        private PlayerController Player(bool prefab=false)
        {
            GameObject go;
            if(prefab)
            {
                go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Genesis/Character/Prefabs/GenesisPlayer.prefab"));
            }
            else {go=new GameObject("Crouch test actor");var cc=go.AddComponent<CharacterController>();cc.height=1.9f;cc.center=Vector3.up*0.95f;cc.radius=0.2f;cc.skinWidth=0.03f;cc.minMoveDistance=0;go.AddComponent<PlayerController>();}
            _objects.Add(go);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            var motor=go.GetComponent<PlayerController>();motor.enabled=false;motor.CameraTransform=null;
            var presentation=go.GetComponent<PlayerAnimation>();if(presentation!=null)presentation.enabled=false;
            return motor;
        }
        private static void Step(PlayerController p,PlayerMovementInput.Frame input,int count=1)
        {Physics.SyncTransforms();for(int i=0;i<count;i++)p.Simulate(input,Dt);}
        [UnityTest] public IEnumerator TogglePreservesFeetAndBlockedStandDoesNotExpandCapsule()
        {
            Box(new Vector3(0,-0.5f,0),new Vector3(30,1,30));var p=Player();var cc=p.GetComponent<CharacterController>();
            Step(p,new PlayerMovementInput.Frame(Vector2.zero),10);float bottom=cc.center.y-cc.height/2;Vector3 scale=p.transform.localScale;
            Step(p,new PlayerMovementInput.Frame(Vector2.zero,toggleCrouch:true));
            Assert.That(p.IsCrouching,Is.True);Assert.That(cc.height,Is.EqualTo(1.25f));Assert.That(cc.center.y-cc.height/2,Is.EqualTo(bottom).Within(0.001f));Assert.That(p.transform.localScale,Is.EqualTo(scale));
            Step(p,new PlayerMovementInput.Frame(Vector2.zero),30);Assert.That(p.IsCrouching,Is.True,"Release must not stand");
            var ceiling=Box(new Vector3(0,1.65f,0),new Vector3(4,0.4f,4));
            Step(p,new PlayerMovementInput.Frame(Vector2.zero,toggleCrouch:true));Assert.That(p.IsCrouching,Is.True);Assert.That(cc.height,Is.EqualTo(1.25f));Assert.That(p.CanStand(),Is.False);
            Step(p,new PlayerMovementInput.Frame(Vector2.zero,jump:true),10);Assert.That(p.IsGrounded,Is.True);Assert.That(p.VerticalVelocity,Is.LessThanOrEqualTo(0));
            ceiling.transform.position=Vector3.up*10;Step(p,new PlayerMovementInput.Frame(Vector2.zero,toggleCrouch:true));Assert.That(p.IsCrouching,Is.False);Assert.That(cc.height,Is.EqualTo(1.9f));Assert.That(cc.center,Is.EqualTo(Vector3.up*0.95f));
            Step(p,new PlayerMovementInput.Frame(Vector2.zero,jump:true));Assert.That(p.VerticalVelocity,Is.GreaterThan(0));yield return null;
        }
        [UnityTest] public IEnumerator CrouchMovementIgnoresRunSprintAndExitsTunnelSafely()
        {
            Box(new Vector3(0,-0.5f,0),new Vector3(30,1,30));var p=Player();Step(p,new PlayerMovementInput.Frame(Vector2.zero),10);
            Step(p,new PlayerMovementInput.Frame(Vector2.zero,toggleCrouch:true));Box(new Vector3(0,1.65f,0),new Vector3(3,0.4f,3));
            Step(p,new PlayerMovementInput.Frame(Vector2.up,run:true,sprint:true),240);
            Assert.That(p.PlanarSpeed,Is.InRange(0.7f,0.85f));Assert.That(p.transform.position.z,Is.GreaterThan(2.5f));Assert.That(p.IsGrounded,Is.True);
            Step(p,new PlayerMovementInput.Frame(Vector2.zero,toggleCrouch:true));Assert.That(p.IsCrouching,Is.False);
            Box(p.transform.position+Vector3.up*1.65f,new Vector3(2,0.3f,2),true);
            Step(p,new PlayerMovementInput.Frame(Vector2.zero,toggleCrouch:true));Step(p,new PlayerMovementInput.Frame(Vector2.zero,toggleCrouch:true));Assert.That(p.IsCrouching,Is.False,"Triggers must not obstruct standing");yield return null;
        }
        [UnityTest] public IEnumerator CrouchRetargetsAndLandingReturnsToCrouch()
        {
            var p=Player(true);var a=p.GetComponentInChildren<Animator>();a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.Rebind();a.SetBool("IsGrounded",true);
            for(int i=0;i<20;i++)a.Update(Dt);float standing=p.transform.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.Head).position).y;
            a.SetBool("IsCrouching",true);for(int i=0;i<30;i++)a.Update(Dt);
            Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Crouch"),Is.True);Assert.That(p.transform.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.Head).position).y,Is.LessThan(standing-0.25f));
            a.SetFloat("Speed",0.8f);for(int i=0;i<30;i++)a.Update(Dt);
            Assert.That(System.Array.Exists(a.GetCurrentAnimatorClipInfo(0),c=>c.clip.name=="HumanM@Crouch01_Walk_Forward"&&c.weight>0.95f),Is.True);
            Vector3 foot=a.GetBoneTransform(HumanBodyBones.LeftFoot).position;for(int i=0;i<10;i++)a.Update(Dt);Assert.That(Vector3.Distance(foot,a.GetBoneTransform(HumanBodyBones.LeftFoot).position),Is.GreaterThan(0.001f));
            a.SetBool("IsGrounded",false);a.SetFloat("VerticalVelocity",-3);for(int i=0;i<20;i++)a.Update(Dt);Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Fall"),Is.True);
            a.SetBool("IsGrounded",true);for(int i=0;i<90;i++)a.Update(Dt);Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Crouch"),Is.True);
            a.SetBool("IsCrouching",false);for(int i=0;i<20;i++)a.Update(Dt);Assert.That(a.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"),Is.True);Assert.That(a.applyRootMotion,Is.False);yield return null;
        }
        [UnityTest] public IEnumerator PrefabInputPresentationAndIndependentSkillsStayConnected()
        {
            Box(new Vector3(0,-0.5f,0),new Vector3(30,1,30));var p=Player(true);
            var input=new SerializedObject(p.GetComponent<PlayerMovementInput>());Assert.That(input.FindProperty("crouchKey").intValue,Is.EqualTo((int)KeyCode.C));
            Step(p,new PlayerMovementInput.Frame(Vector2.zero),10);Step(p,new PlayerMovementInput.Frame(Vector2.zero,toggleCrouch:true));p.GetComponent<PlayerAnimation>().SendMessage("Update");
            Assert.That(p.GetComponentInChildren<Animator>().GetBool("IsCrouching"),Is.True);
            var skills=p.GetComponent<SkillSystem>();var other=Player(true);other.transform.position=Vector3.right*10;
            var setup=new SerializedObject(p.GetComponent<Genesis.NPC.NPC_Setup>());var definition=(Genesis.Skill.Definitions.SkillDefinition)setup.FindProperty("perceptionDefinition").objectReferenceValue;
            Assert.That(skills.HasSkill(definition.SkillId),Is.True);Assert.That(other.GetComponent<SkillSystem>().GetState(definition.SkillId),Is.Not.SameAs(skills.GetState(definition.SkillId)));
            Assert.That(skills.RegisterMeaningfulUse(definition.SkillId,1),Is.True);LogAssert.NoUnexpectedReceived();yield return null;
        }
    }
}
#endif
